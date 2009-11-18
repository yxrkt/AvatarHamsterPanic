using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Physics;

namespace GameObjects
{
  class FloorBlock
  {
    private ContentManager content = null;

    public FloorBlock( Vector2 pos, ContentManager content )
    {
      Released = false;

      this.content = content;

      Model = this.content.Load<Model>( "block" );

      BoundingPolygon = new PhysPolygon( Scale, Scale / 4f, pos, 10 );
      BoundingPolygon.Flags = PhysBodyFlags.Anchored;
    }

    ~FloorBlock()
    {
      if ( BoundingPolygon != null )
        BoundingPolygon.Release();
    }

    public void Release()
    {
      Released = true;
    }

    static FloorBlock()
    {
      Scale = 1.6f;
    }

    public bool Released { get; private set; }

    public static float Scale { get; private set; }

    public Model Model { get; private set; }
    public PhysPolygon BoundingPolygon { get; set; }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans, matRot;
      Matrix.CreateScale( Scale, out transform );
      Matrix.CreateRotationZ( BoundingPolygon.Angle, out matRot );
      Matrix.CreateTranslation( BoundingPolygon.Position.X, BoundingPolygon.Position.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }
  }
}