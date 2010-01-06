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
    public static float Size { get; private set; }
    public static float Height { get; private set; }

    public Model Model { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }

    public FloorBlock( GameplayScreen screen, Vector2 pos )
      : base( screen )
    {
      Model = screen.Content.Load<Model>( "block" );

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
  }
}