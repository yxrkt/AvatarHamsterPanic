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
    PhysPolygon m_poly = null;

    public float X
    {
      get { return m_poly.Position.X; }
      set { m_poly.Position = new Vector2( value, 0f ); }
    }

    public Boundary( float xPos )
    {
      //Vector2[] line = new Vector2[2];
      //line[0].X = xPos;
      //line[0].Y = -10000f;
      //line[1].X = xPos;
      //line[1].Y = 10000f;
      //m_line = new PhysLine( line[0], line[1], new Vector2( xPos, 0f ), 1f );
      //m_line.Flags = PhysBodyFlags.ANCHORED;
      m_poly = new PhysPolygon( .01f, 10000f, new Vector2( xPos, 0f ), 1f );
      m_poly.Flags = PhysBodyFlags.ANCHORED;
    }

    ~Boundary()
    {
      m_poly.Release();
    }
  }
}