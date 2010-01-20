using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;

namespace AvatarHamsterPanic.Objects
{
  class Basket : GameObject
  {
    public static float Scale { get; set; }
    public Model Model { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }
    public bool Released { get; private set; }

    private bool WarpingOut { get { return warpTimeEnd != 0f; } }
    private float warpTime = 0f, warpTimeEnd = 0f;
    private Effect warpEffect = null;

    static Basket()
    {
      Scale = 1.6f;
    }

    public Basket( GameplayScreen screen, Vector2 pos )
      : base( screen )
    {
      warpEffect = Screen.Content.Load<Effect>( "Effects/Warp" );

      // make the poly
      Vector2[] verts = new Vector2[14];
      verts[0].X  = 0.977f; verts[0].Y  =  0.002f;
      verts[1].X  = 0.757f; verts[1].Y  = -0.218f;
      verts[2].X  = 0.241f; verts[2].Y  = -0.218f;
      verts[3].X  = 0.045f; verts[3].Y  = -0.022f;
      verts[4].X  = 0.045f; verts[4].Y  =  0.023f;
      verts[5].X  = 0.009f; verts[5].Y  =  0.05f;
      verts[6].X  =-0.031f; verts[6].Y  =  0.04f;
      verts[7].X  =-0.051f; verts[7].Y  =  0.0f;
      verts[8].X  =-0.031f; verts[8].Y  = -0.039f;
      verts[9].X  = 0.004f; verts[9].Y  = -0.05f;
      verts[10].X = 0.023f; verts[10].Y = -0.045f;
      verts[11].X = 0.228f; verts[11].Y = -0.25f;
      verts[12].X = 0.77f;  verts[12].Y = -0.25f;
      verts[13].X = 1.0f;   verts[13].Y = -0.02f;

      int nVerts = verts.Length;
      for ( int i = 0; i < nVerts; ++i )
        verts[i] *= Basket.Scale;

      BoundingPolygon = new PhysPolygon( verts, pos, 1f );
      BoundingPolygon.Parent = this;
      BoundingPolygon.Flags |= PhysBodyFlags.Anchored;
      BoundingPolygon.Elasticity = 1f;
      BoundingPolygon.Friction = 1.5f;

      // load the mesh
      Model = screen.Content.Load<Model>( "Models/basket" );
    }

    public void WarpOut( float totalTime )
    {
      if ( !WarpingOut )
      {
        warpTime = 0f;
        warpTimeEnd = totalTime;
      }
    }

    public void GetTransform( out Matrix transform )
    {
      PhysPolygon poly = BoundingPolygon;

      Matrix.CreateScale( Basket.Scale, out transform );

      Matrix matRot;
      Matrix.CreateRotationZ( poly.Angle, out matRot );

      Matrix matTrans;
      Matrix.CreateTranslation( poly.Position.X, poly.Position.Y, 0f, out matTrans );

      Matrix.Multiply( ref transform, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public override void Update( GameTime gameTime )
    {
      if ( WarpingOut )
      {
        warpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if ( warpTime >= warpTimeEnd )
        {
          BoundingPolygon.Release();
          Screen.ObjectTable.MoveToTrash( this );
        }
      }
    }

    public override void Draw()
    {
      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = new VertexDeclaration( graphics, VertexPositionNormalTexture.VertexElements );
      SetRenderState( graphics.RenderState );

      if ( !WarpingOut )
      {
        foreach ( ModelMesh mesh in Model.Meshes )
        {
          foreach ( BasicEffect effect in mesh.Effects )
          {
            Matrix world;
            GetTransform( out world );

            effect.EnableDefaultLighting();
            effect.World = world;
            effect.View = Screen.View;
            effect.Projection = Screen.Projection;
          }

          mesh.Draw();
        }
      }
      else
      {
        warpEffect.CurrentTechnique = warpEffect.Techniques[0];
        warpEffect.Begin();

        Matrix world;
        GetTransform( out world );
        warpEffect.Parameters["matWorldViewProj"].SetValue( world * Screen.View * Screen.Projection );
        warpEffect.Parameters["time"].SetValue( Math.Min( warpTime / warpTimeEnd, 1f ) );

        foreach ( EffectPass pass in warpEffect.CurrentTechnique.Passes )
        {
          pass.Begin();
          foreach ( ModelMesh mesh in Model.Meshes )
          {
            foreach ( ModelMeshPart part in mesh.MeshParts )
            {
              part.Effect = warpEffect;
              graphics.Vertices[0].SetSource( mesh.VertexBuffer, part.StreamOffset, part.VertexStride );
              graphics.Indices = mesh.IndexBuffer;
              graphics.DrawIndexedPrimitives( PrimitiveType.TriangleList, part.BaseVertex, 0, part.NumVertices,
                                              part.StartIndex, part.PrimitiveCount );
            }
          }
          pass.End();
        }

        warpEffect.End();
      }
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.CullCounterClockwiseFace;

      renderState.AlphaBlendEnable = true;
      renderState.SourceBlend = Blend.SourceAlpha;
      renderState.DestinationBlend = Blend.InverseSourceAlpha;

      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }
  }
}