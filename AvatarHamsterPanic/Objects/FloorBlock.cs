using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Physics;
using GameStateManagement;

namespace GameObjects
{
  class FloorBlock : GameObject
  {
    Effect effect;
    EffectParameter effectParameterWorld;
    EffectParameter effectParameterView;
    EffectParameter effectParameterProjection;
    EffectParameter effectParameterEye;

    public static float Size { get; private set; }
    public static float Height { get; private set; }

    public Model Model { get; private set; }
    public Texture2D DiffuseMap { get; private set; }
    public Texture2D NormalMap { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }

    public FloorBlock( GameplayScreen screen, Vector2 pos )
      : base( screen )
    {
      ContentManager content = screen.Content;
      Model = content.Load<Model>( "Models/block" );
      DiffuseMap = content.Load<Texture2D>( "Textures/glassDiffuse" );
      NormalMap  = content.Load<Texture2D>( "Textures/glassNormal" );

      effect = content.Load<Effect>( "Effects/basic" ).Clone( screen.ScreenManager.GraphicsDevice );
      effect.CurrentTechnique = effect.Techniques["Basic"];
      effect.Parameters["DiffuseMap"].SetValue( DiffuseMap );
      effect.Parameters["NormalMap"].SetValue( NormalMap );

      effectParameterWorld = effect.Parameters["World"];
      effectParameterView = effect.Parameters["View"];
      effectParameterProjection = effect.Parameters["Projection"];
      effectParameterEye = effect.Parameters["Eye"];

      BoundingPolygon = new PhysPolygon( Size, Height, pos, 10 );
      BoundingPolygon.Elasticity = 1f;
      BoundingPolygon.Friction = 1.5f;
      BoundingPolygon.Flags = PhysBodyFlags.Anchored;
      BoundingPolygon.Parent = this;
    }

    static FloorBlock()
    {
      Size = 1.6f;
      Height = 2f * Size / 8f;
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
      float clearLine = Screen.CameraInfo.DeathLine + Screen.Camera.Position.Y;
      if ( BoundingPolygon.Position.Y > clearLine )
      {
        Screen.ObjectTable.MoveToTrash( this );
        BoundingPolygon.Release();
      }
    }

    public override void Draw()
    {
      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = new VertexDeclaration( graphics, VertexPositionNormalTexture.VertexElements );
      SetRenderState( graphics.RenderState );

      effect.Begin();

      Matrix world;
      GetTransform( out world );
      effectParameterWorld.SetValue( world );
      effectParameterView.SetValue( Screen.View );
      effectParameterProjection.SetValue( Screen.Projection );
      effectParameterEye.SetValue( Screen.Camera.Position );

      foreach ( EffectPass pass in effect.CurrentTechnique.Passes )
      {
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
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.None;

      renderState.AlphaBlendEnable = true;
      renderState.SourceBlend = Blend.SourceAlpha;
      renderState.DestinationBlend = Blend.InverseSourceAlpha;

      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }
  }
}