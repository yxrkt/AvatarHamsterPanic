using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Physics;
using AvatarHamsterPanic.Objects;
using Menu;
using Graphics;
using Audio;

namespace AvatarHamsterPanic.Objects
{
  class FloorBlock : GameObject
  {
    static Effect effect;
    static EffectParameter effectParameterWorld;
    static EffectParameter effectParameterView;
    static EffectParameter effectParameterProjection;
    static EffectParameter effectParameterEye;
    static VertexDeclaration vertexDeclaration;
    static int lastDrawOrder = 11;

    public static float Size { get; private set; }
    public static float Height { get; private set; }
    public static float BirthLine { get; private set; }
    public static float DeathLine { get; private set; }
    public static Model Model { get; private set; }
    public static Model ShatteredModel { get; private set; }
    public static Texture2D DiffuseMap { get; private set; }
    public static Texture2D NormalMap { get; private set; }

    public PhysPolygon BoundingPolygon { get; set; }
    bool alive;
    const int maxBlocks = 50;
    static ModelExplosionSettings explosionSettings = new ModelExplosionSettings();
    private static FloorBlock[] pool = new FloorBlock[maxBlocks];

    FloorBlock( GameplayScreen screen )
      : base( screen )
    {
      BoundingPolygon = new PhysPolygon( Size, Height, Vector2.Zero, 1 );
      BoundingPolygon.Elasticity = 1f;
      BoundingPolygon.Friction = 1.5f;
      BoundingPolygon.Flags = BodyFlags.Anchored;
      BoundingPolygon.Parent = this;
      BoundingPolygon.Collided += KillSelfIfPwnt;
    }

    void Initialize( Vector2 pos )
    {
      alive = true;
      BoundingPolygon.Position = pos;
      BoundingPolygon.Flags = BodyFlags.Anchored;
      Screen.PhysicsSpace.AddBody( BoundingPolygon );
      DrawOrder = lastDrawOrder++;
    }

    static FloorBlock()
    {
      Size = 1.6f;
      Height = 2f * Size / 8f;
    }

    public static void Initialize( GameplayScreen screen )
    {
      Camera camera = screen.Camera;
      ContentManager content =  screen.Content;

      float depth = camera.Position.Z + Size / 2f;
      float height = depth * (float)Math.Tan( camera.Fov / 2f );
      DeathLine = height + Height / 2f;
      BirthLine = -DeathLine;

      vertexDeclaration = new VertexDeclaration( screen.ScreenManager.GraphicsDevice, 
                                                 VertexPositionNormalTexture.VertexElements );

      Model = content.Load<Model>( "Models/block" );
      DiffuseMap = content.Load<Texture2D>( "Textures/glassDiffuse" );
      NormalMap = content.Load<Texture2D>( "Textures/glassNormal" );

      ShatteredModel = content.Load<Model>( "Models/block_broken" );

      effect = content.Load<Effect>( "Effects/basic" );
      effect.CurrentTechnique = effect.Techniques["DiffuseColor"];
      effect.Parameters["DiffuseMap"].SetValue( DiffuseMap );
      effect.Parameters["NormalMap"].SetValue( NormalMap );

      effectParameterWorld = effect.Parameters["World"];
      effectParameterView = effect.Parameters["View"];
      effectParameterProjection = effect.Parameters["Projection"];
      effectParameterEye = effect.Parameters["Eye"];

      for ( int i = 0; i < maxBlocks; ++i )
        pool[i] = new FloorBlock( screen );
    }

    public static FloorBlock CreateFloorBlock( Vector2 pos )
    {
      foreach ( FloorBlock block in pool )
      {
        if ( !block.alive )
        {
          block.Initialize( pos );
          return block;
        }
      }

      return null;
    }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans, matRot;
      Matrix.CreateScale( Size, out transform );
      Matrix.CreateRotationZ( BoundingPolygon.Angle, out matRot );
      Matrix.CreateTranslation( BoundingPolygon.Position.X, BoundingPolygon.Position.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public override void Update( GameTime gameTime )
    {
      float clearLine = DeathLine + Screen.Camera.Position.Y;
      if ( BoundingPolygon.Position.Y > clearLine )
      {
        Screen.ObjectTable.MoveToTrash( this );
        Screen.PhysicsSpace.RemoveBody( BoundingPolygon );
        alive = false;
      }
    }

    public override void Draw()
    {
      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = vertexDeclaration;
      SetRenderState( graphics.RenderState );

      effect.Begin();

      Matrix world;
      GetTransform( out world );
      effectParameterWorld.SetValue( world );
      effectParameterView.SetValue( Screen.View );
      effectParameterProjection.SetValue( Screen.Projection );
      effectParameterEye.SetValue( Screen.Camera.Position );

      EffectPassCollection passes = effect.CurrentTechnique.Passes;
      int nPasses = passes.Count;
      for ( int i = 0; i < nPasses; ++i )
      {
        EffectPass pass = passes[i];

        pass.Begin();
        foreach ( ModelMesh mesh in Model.Meshes )
        {
          foreach ( ModelMeshPart part in mesh.MeshParts )
          {
            graphics.Vertices[0].SetSource( mesh.VertexBuffer, part.StreamOffset, part.VertexStride );
            graphics.Indices = mesh.IndexBuffer;
            graphics.DrawIndexedPrimitives( PrimitiveType.TriangleList, part.BaseVertex, 0,
                                            part.NumVertices, part.StartIndex, part.PrimitiveCount );
          }
        }
        pass.End();
      }

      effect.End();

      //graphics.RenderState.DepthBufferEnable = true;
      //graphics.RenderState.DepthBufferWriteEnable = true;
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.None;

      renderState.AlphaBlendEnable = true;
      renderState.SourceBlend = Blend.SourceAlpha;
      renderState.DestinationBlend = Blend.InverseSourceAlpha;
      renderState.AlphaTestEnable = false;

      //renderState.DepthBufferEnable = false;
      //renderState.DepthBufferWriteEnable = false;
    }

    private bool KillSelfIfPwnt( Collision result )
    {
      Player player = result.BodyB.Parent as Player;

      if ( player != null && ( player.Respawning || player.Crushing ) )
      {
        if ( !BoundingPolygon.Flags.HasFlags( BodyFlags.Ghost ) )
        {
          // remove the block
          BoundingPolygon.Flags |= BodyFlags.Ghost;
          Screen.PhysicsSpace.RemoveBody( BoundingPolygon );
          Screen.ObjectTable.MoveToTrash( this );
          alive = false;

          // rumble the controller
          if ( player.PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.RumbleLow( player.PlayerIndex, .25f, .3f );

          // add the exploding block particle system
          Vector3 position = new Vector3( BoundingPolygon.Position, 0f );
          ModelExplosion explosion = ModelExplosion.CreateExplosion( position, Size, 
                                                                     ShatteredModel, explosionSettings );
          Screen.ParticleManager.Add( explosion );
          GameCore.Instance.AudioManager.Play3DCue( "blockExplosion", DummyAudioEmitter.InstanceAtPos( position ), 1f );

          return false;
        }
      }
      return true;
    }
  }
}