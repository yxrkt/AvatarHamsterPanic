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
    public static float Scale { get; private set; }

    public Model Model { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }

    public FloorBlock( GameplayScreen screen, Vector2 pos )
      : base( screen )
    {
      Model = screen.Content.Load<Model>( "block" );

      BoundingPolygon = new PhysPolygon( Scale, Scale / 4f, pos, 10 );
      BoundingPolygon.Flags = PhysBodyFlags.Anchored;
    }

    ~FloorBlock()
    {
      if ( BoundingPolygon != null )
        BoundingPolygon.Release();
    }

    static FloorBlock()
    {
      Scale = 1.6f;
    }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans, matRot;
      Matrix.CreateScale( Scale, out transform );
      Matrix.CreateRotationZ( BoundingPolygon.Angle, out matRot );
      Matrix.CreateTranslation( BoundingPolygon.Position.X, BoundingPolygon.Position.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public override void Update( GameTime gameTime )
    {
      float clearLine = Screen.CameraInfo.DeathLine + Screen.Camera.Position.Y;
      if ( BoundingPolygon.Position.Y > clearLine )
        Screen.ObjectTable.MoveToTrash( this );
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