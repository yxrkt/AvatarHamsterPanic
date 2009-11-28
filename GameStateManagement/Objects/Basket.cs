using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;

namespace GameObjects
{
  class Basket : GameObject
  {
    public static float Scale { get; set; }
    public Model Model { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }
    public bool Released { get; private set; }

    static Basket()
    {
      Scale = 1.6f;
    }

    public Basket( GameplayScreen screen, Vector2 pos )
      : base( screen )
    {
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
      BoundingPolygon.Flags = PhysBodyFlags.Anchored;

      // load the mesh
      Model = screen.Content.Load<Model>( "basket" );
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
      
    }

    public override void Draw()
    {
      foreach ( ModelMesh mesh in Model.Meshes )
      {
        foreach ( BasicEffect effect in mesh.Effects )
        {
          Matrix world;
          GetTransform( out world );

          effect.World = world;
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
        }

        mesh.Draw();
      }
    }
  }
}