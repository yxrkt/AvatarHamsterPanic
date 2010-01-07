using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;

namespace GameObjects
{
  class Boundary
  {
    PhysPolygon poly = null;

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

    public Boundary( float xPos )
    {
      poly = new PhysPolygon( .01f, 10000f, new Vector2( xPos, 0f ), 1f );
      poly.Elasticity = 1f;
      poly.Friction = 1.5f;
      poly.Flags = PhysBodyFlags.Anchored;
    }

    ~Boundary()
    {
      poly.Release();
    }
  }
}