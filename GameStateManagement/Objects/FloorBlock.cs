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
    private static float m_scale = 1.6f;
    private ContentManager m_content = null;
    private PhysPolygon m_physPoly = null;
    private Model m_model = null;

    public FloorBlock( Vector2 pos, ContentManager content )
      : this( pos, content, true )
    {
    }

    public FloorBlock( Vector2 pos, ContentManager content, bool phys )
    {
      Released = false;

      m_content = content;
      m_model   = m_content.Load<Model>( "block" );

      if ( phys )
      {
        m_physPoly = new PhysPolygon( m_scale, m_scale / 4f, pos, 10 );
        m_physPoly.Flags = PhysBodyFlags.ANCHORED;
      }
    }

    ~FloorBlock()
    {
      if ( m_physPoly != null )
        m_physPoly.Release();
    }

    public void Release()
    {
      Released = true;
    }

    public bool Released { get; private set; }

    public static float Scale { get { return m_scale; } }

    public Model Model { get { return m_model; } }
    public PhysPolygon BoundingPolygon { get { return m_physPoly; } set { m_physPoly = value; } }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans, matRot;
      Matrix.CreateScale( m_scale, out transform );
      Matrix.CreateRotationZ( m_physPoly.Angle, out matRot );
      Matrix.CreateTranslation( m_physPoly.Position.X, m_physPoly.Position.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }
  }
}