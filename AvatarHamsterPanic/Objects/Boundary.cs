using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Objects
{
  class Boundary : GameObject
  {
    PhysPolygon poly;
    Model model;
    Matrix deform;
    Effect effect;
    EffectParameter effectParamWorld;
    EffectParameter effectParamView;
    EffectParameter effectParamProjection;
    EffectParameter effectParamEye;

    public float X
    {
      get { return poly.Position.X; }
      set { poly.Position = new Vector2( value, poly.Position.Y ); }
    }

    public float Y
    {
      get { return poly.Position.Y; }
      set { poly.Position = new Vector2( poly.Position.X, value ); }
    }

    public Boundary( GameplayScreen screen, float xPos )
      : base( screen )
    {
      // polygon
      poly = new PhysPolygon( .01f, 10000f, new Vector2( xPos, 0f ), 1f );
      poly.Elasticity = 1f;
      poly.Friction = 1.5f;
      poly.Flags = PhysBodyFlags.Anchored;

      // model
      model = screen.Content.Load<Model>( "Models/block" );
      float height = FloorBlock.DeathLine - FloorBlock.BirthLine;
      Matrix rotate = new Matrix( 0, 1, 0, 0,
                                 -1, 0, 0, 0,
                                  0, 0, 1, 0,
                                  0, 0, 0, 1 );
      Matrix scale = Matrix.CreateScale( 1, height, 1 );
      Matrix trans = Matrix.CreateTranslation( Math.Sign( X ) * .125f * FloorBlock.Size, 0f, 0f );
      deform = Matrix.CreateScale( FloorBlock.Size ) * rotate * scale * trans;

      // effect
      effect = screen.Content.Load<Effect>( "Effects/basic" ).Clone( screen.ScreenManager.GraphicsDevice );
      effect.CurrentTechnique = effect.Techniques["Color"];
      effect.Parameters["Color"].SetValue( new Vector4( .69f, .75f, .82f, 1f ) );
      effectParamWorld = effect.Parameters["World"];
      effectParamView = effect.Parameters["View"];
      effectParamProjection = effect.Parameters["Projection"];
      effectParamEye = effect.Parameters["Eye"];
    }

    public override void Update( GameTime gameTime )
    {
      this.Y = Screen.Camera.Position.Y;
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      device.VertexDeclaration = new VertexDeclaration( device, VertexPositionNormalTexture.VertexElements );
      SetRenderState( device.RenderState );

      effectParamView.SetValue( Screen.View );
      effectParamProjection.SetValue( Screen.Projection );
      effectParamEye.SetValue( Screen.Camera.Position );
      effectParamWorld.SetValue( deform * Matrix.CreateTranslation( poly.Position.X, poly.Position.Y, 0f ) );

      effect.Begin();
      effect.CurrentTechnique.Passes[0].Begin();

      foreach ( ModelMesh mesh in model.Meshes )
      {
        foreach ( ModelMeshPart part in mesh.MeshParts )
        {
          device.Vertices[0].SetSource( mesh.VertexBuffer, part.StreamOffset, part.VertexStride );
          device.Indices = mesh.IndexBuffer;
          device.DrawIndexedPrimitives( PrimitiveType.TriangleList, part.BaseVertex, 0, part.NumVertices,
                                        part.StartIndex, part.PrimitiveCount );
        }
      }

      effect.CurrentTechnique.Passes[0].End();
      effect.End();
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.AlphaBlendEnable = false;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }
  }
}