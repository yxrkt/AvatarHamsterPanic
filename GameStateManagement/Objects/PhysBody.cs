using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLib;

namespace Physics
{
  // PhysBody special flags
  [Flags]
  enum PhysBodyFlags
  {
    None,
    Anchored,
  }

  class CollisResult
  {
    public CollisResult()
      : this( false, float.MaxValue, null, Vector2.Zero )
    {
    }

    public CollisResult( bool collision, float time, PhysBody obj, Vector2 normal )
    {
      Collision = collision;
      Time      = time;
      Object    = obj;
      Normal    = normal;
    }

    public bool Collision { get; set; }
    public float Time { get; set; }
    public PhysBody Object { get; set; }
    public Vector2 Normal { get; set; }
  }

  /// <summary>
  /// Base physics object class
  /// </summary>
  abstract class PhysBody
  {
    // Static list available 
    private static List<PhysBody> s_bodies = new List<PhysBody>();

    // Initialized in the constructor
    protected Vector2 m_pos, m_vel;
    protected float m_mass;

    // Default values
    protected Vector2 m_touchN = Vector2.Zero;
    protected PhysBody m_touching = null;
    protected float m_angle = 0.0f, m_angVel = 0.0f;
    protected Vector2 m_force = Vector2.Zero;
    protected PhysBodyFlags m_flags = PhysBodyFlags.None;
    protected event EventHandler OnCollision = null;

    private bool released = false;

    // Construct PhysBody and add it to the list
    public PhysBody( Vector2 pos, Vector2 vel, float mass )
    {
      m_pos  = new Vector2( pos.X, pos.Y );
      m_vel  = new Vector2( vel.X, vel.Y );
      m_mass = mass;
      Skip   = false;

      s_bodies.Add( this );
    }

    // Remove PhysBody from the list
    ~PhysBody()
    {
      s_bodies.Remove( this );
    }

    // Properties
    public static List<PhysBody> AllBodies { get { return s_bodies; } }
    public Vector2 Position { get { return m_pos; } set { m_pos = value; } }
    public Vector2 Velocity { get { return m_vel; } set { m_vel = value; } }
    public Vector2 TouchNormal { get { return m_touchN; } set { m_touchN = value; } }
    public PhysBody Touching { get { return m_touching; } set { m_touching = value; } }
    public float Angle { get { return m_angle; } set { m_angle = value; } }
    public float AngularVelocity { get { return m_angVel; } set { m_angVel = value; } }
    public Vector2 Force { get { return m_force; } set { m_force = value; } }
    public PhysBodyFlags Flags { get { return m_flags; } set { m_flags = value; } }
    public float Mass { get { return m_mass; } set { m_mass = value; } }
    public bool Released { get { return released; } }
    public bool Skip { get; set; }

    public void Release()
    {
      released = true;
    }

    public CollisResult TestVsBody( PhysBody body, float t )
    {
      if ( body is PhysCircle )
        return TestVsCircle( (PhysCircle)body, t );
      if ( body is PhysPolygon )
        return TestVsPolygon( (PhysPolygon)body, t );
      if ( body is PhysLine )
        return TestVsLine( (PhysLine)body, t );

      return new CollisResult();
    }

    public void HandleCollision()
    {
      if ( OnCollision != null )
        OnCollision( this, null );
    }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans;
      Matrix.CreateRotationZ( m_angle, out transform );
      Matrix.CreateTranslation( m_pos.X, m_pos.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public abstract void UpdateTouching();
    public abstract void ApplyResponseFrom( CollisResult result );
    protected abstract CollisResult TestVsCircle( PhysCircle circle, float t );
    protected abstract CollisResult TestVsPolygon( PhysPolygon box, float t );
    protected abstract CollisResult TestVsLine( PhysLine line, float t );
  }

  /// <summary>
  /// Physics bounding circle object
  /// </summary>
  class PhysCircle : PhysBody
  {
    protected float m_radius;

    public PhysCircle( float radius, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      m_radius = radius;
    }

    public float Radius { get { return m_radius; } set { m_radius = value; } }

    public override void UpdateTouching()
    {
      if ( m_touching != null )
      {
        if ( !PhysBody.AllBodies.Exists( element => element == m_touching ) )
        {
            m_touching = null;
            return;
        }
        
        if ( m_touching is PhysPolygon )
        {
          PhysPolygon body = (PhysPolygon)m_touching;

          Matrix transform;
          body.GetTransform( out transform );

          List<Vector2> verts = body.Vertices;
          int nVerts = verts.Count;

          for ( int i = 0; i < nVerts; ++i )
          {
            int j = ( i + 1 ) % 4;

            Vector2 edge = Vector2.Subtract( verts[j], verts[i] );

            float edgeMag = edge.Length();

            edge.Normalize();
            Vector2 n = new Vector2( edge.Y, -edge.X );

            Vector2 vec = Vector2.Subtract( m_pos, Vector2.Transform( verts[i], transform ) );
            
            // check corner
            if ( vec.Length() <= m_radius )
            {
              m_touchN = vec;
              return;
            }

            // check dist from side
            float normalDot = Vector2.Dot( vec, n );
            if ( ( normalDot > ( m_radius + float.Epsilon ) ) || ( normalDot < 0f ) )
              continue; // can't be touching this side

            // check segment on line
            float edgeDot = Vector2.Dot( vec, edge );
            if ( ( edgeDot >= 0f ) && ( edgeDot <= edgeMag ) )
              return; // keep touching
          }

          m_touching = null;
        }
        else if ( m_touching is PhysLine )
        {
          // get normal of line
          // get vector from p0 on line to ball center
          // if abs( dot ) <= radius
          //   if dot( p0c, p0p1.normalized() ) >= 0f && <= p0p1.length() )
          //     return;
          // m_touching = null;

          PhysLine line = (PhysLine)m_touching;

          Matrix transform;
          line.GetTransform( out transform );

          Vector2[] verts = new Vector2[2];
          Array.Copy( line.Vertices, verts, 2 );
          Vector2.Transform( ref verts[0], ref transform, out verts[0] );
          Vector2.Transform( ref verts[1], ref transform, out verts[1] );

          Vector2 edge = Vector2.Subtract( verts[1], verts[0] );
          float edgeLen = edge.Length();
          edge /= edgeLen;

          Vector2 n    = new Vector2( edge.Y, -edge.X );
          n.Normalize();

          Vector2 v0c = Vector2.Subtract( m_pos, verts[0] );
          float dist = Vector2.Dot( v0c, edge );
          if ( dist <= m_radius )
          {
            float skew = Vector2.Dot( v0c, edge );
            if ( skew >= 0f && skew <= edgeLen )
              return;
          }

          m_touching = null;
        }
      }
    }

    public override void ApplyResponseFrom( CollisResult result )
    {
      float bounceThresh = 1f;
      float bouncePct = .275f;

      Vector2 nNormal = new Vector2( result.Normal.Y, -result.Normal.X );

      if ( Math.Abs( Vector2.Dot( m_vel, result.Normal ) ) > bounceThresh )
      {
        Vector2 negVel, temp;

        Vector2.Multiply( ref m_vel, -1f, out negVel );
        float scale = -2f * Vector2.Dot( negVel, nNormal );
        Vector2.Multiply( ref nNormal, scale, out temp );
        Vector2.Add( ref negVel, ref temp, out m_vel );
        Vector2.Multiply( ref m_vel, bouncePct, out m_vel );
      }
      else
      {
        float mag = Vector2.Dot( m_vel, nNormal );
        Vector2.Multiply( ref nNormal, mag, out m_vel );
        //m_touching = result.Object;
        //m_touchN = result.Normal;
      }

      // update "angular velocity"
      m_angVel = -Vector2.Dot( m_vel, nNormal ) / m_radius;
    }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      CollisResult result = new CollisResult();

      Vector2 relVel    = Vector2.Subtract( m_vel, circle.m_vel );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT    = Vector2.Add( m_pos, relVelByT );

      float   time   = 0.0f;
      Vector2 normal = Vector2.Zero;
      if ( Geometry.SegmentVsCircle( out time, out normal, m_pos, posAtT, circle.m_pos, m_radius + circle.m_radius ) )
      {
        result.Time = time * t;
        result.Collision = true;
        result.Normal = normal;
        result.Object = circle;
      }

      return result;
    }

    protected override CollisResult TestVsPolygon( PhysPolygon poly, float t )
    {
      // transform that takes local vertex coordinates to world space
      Matrix transform;
      poly.GetTransform( out transform );

      Vector2 relVel    = Vector2.Subtract( m_vel, poly.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT    = Vector2.Add( m_pos, relVelByT );

      List<Vector2> verts = poly.Vertices;
      Vector2 lastVert = verts.Last();
      lastVert = Vector2.Transform( lastVert, transform );

      CollisResult bestResult = new CollisResult();

      int nVerts = verts.Count;
      for ( int i = 0; i < nVerts; ++i )
      {
        Vector2 vert = verts[i];
        Vector2 transfVert = Vector2.Transform( vert, transform );
        Vector2 edge = Vector2.Subtract( transfVert, lastVert );
        Vector2 n = new Vector2( edge.Y, -edge.X );

        float time;

        // ball is moving towards the segment
        if ( Vector2.Dot( n, relVel ) < 0.0f )
        {
          n.Normalize();
          Vector2 offset = Vector2.Multiply( n, m_radius );

          if ( Geometry.SegmentVsSegment( out time, m_pos, posAtT, lastVert + offset, transfVert + offset ) )
          {
            // if collision with segment (and polygon is convex), we're done
            if ( poly.Convex )
              return new CollisResult( true, time * t, poly, n );
            else if ( time * t < bestResult.Time )
              bestResult = new CollisResult( true, time * t, poly, n );
          }
        }

        // check corner
        Vector2 normal;
        if ( Geometry.SegmentVsCircle( out time, out normal, m_pos, posAtT, transfVert, m_radius ) )
        {
          // additional checks to see if hitting correct sector of circle
          if ( Vector2.Dot( normal, edge ) > 0.0f )
          {
            Vector2 nextVert = verts[( i + 1 ) % nVerts];
            Vector2.Transform( ref nextVert, ref transform, out nextVert );
            Vector2 edge2 = Vector2.Subtract( nextVert, transfVert );
            if ( Vector2.Dot( normal, edge2 ) < 0.0f )
            {
              if ( poly.Convex )
                return new CollisResult( true, time * t, poly, normal );
              else if ( time * t < bestResult.Time )
                bestResult = new CollisResult( true, time * t, poly, normal );
            }
          }
        }

        lastVert = transfVert;
      }

      return bestResult;
    }

    protected override CollisResult TestVsLine( PhysLine line, float t )
    {
      Matrix transform;
      line.GetTransform( out transform );

      Vector2 relVel = Vector2.Subtract( m_vel, line.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT = Vector2.Add( m_pos, relVelByT );

      Vector2[] verts = new Vector2[2];
      line.Vertices.CopyTo( verts, 0 );
      Vector2.Transform( ref verts[0], ref transform, out verts[0] );
      Vector2.Transform( ref verts[1], ref transform, out verts[1] );

      float u;
      if ( Geometry.SegmentVsSegment( out u, m_pos, posAtT, verts[0], verts[1] ) )
      {
        Vector2 n = new Vector2( verts[1].Y - verts[0].Y, verts[0].X - verts[1].X );
        n.Normalize();
        if ( Vector2.Dot( n, relVel ) > 0f )
          n *= -1f;
        return new CollisResult( true, u * t, line, n );
      }

      return new CollisResult();
    }
  }

  /// <summary>
  /// Physics bounding convex polygon object
  /// </summary>
  class PhysPolygon : PhysBody
  {
    private List<Vector2> m_verts = new List<Vector2>();

    public PhysPolygon( Vector2[] verts, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      foreach ( Vector2 vert in verts )
        m_verts.Add( vert );

      Convex = Geometry.PolyIsConvex( m_verts.ToArray() );
    }

    public PhysPolygon( float width, float height, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      float widthByTwo  = width  / 2.0f;
      float heightByTwo = height / 2.0f;

      m_verts.Add( new Vector2(  widthByTwo,  heightByTwo ) );
      m_verts.Add( new Vector2( -widthByTwo,  heightByTwo ) );
      m_verts.Add( new Vector2( -widthByTwo, -heightByTwo ) );
      m_verts.Add( new Vector2(  widthByTwo, -heightByTwo ) );

      Convex = Geometry.PolyIsConvex( m_verts.ToArray() );
    }

    public List<Vector2> Vertices { get { return m_verts; } }
    public bool Convex { get; private set; }

    public override void ApplyResponseFrom( CollisResult result )
    {
      //throw new NotImplementedException();
    }

    public override void UpdateTouching()
    {
      //throw new NotImplementedException();
    }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      return circle.TestVsBody( this, t );
    }

    protected override CollisResult TestVsPolygon( PhysPolygon box, float t )
    {
      return new CollisResult();
    }

    protected override CollisResult TestVsLine( PhysLine line, float t )
    {
      return new CollisResult();
    }
  }

  /// <summary>
  /// Physics line object
  /// </summary>
  class PhysLine : PhysBody
  {
    Vector2[] m_verts = new Vector2[2];

    public PhysLine( Vector2 v1, Vector2 v2, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      m_verts[0] = v1;
      m_verts[1] = v2;
    }

    public Vector2[] Vertices { get { return m_verts; } }

    public override void ApplyResponseFrom( CollisResult result )
    {
      //throw new NotImplementedException();
    }

    public override void UpdateTouching()
    {
      //throw new NotImplementedException();
    }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      return circle.TestVsBody( this, t );
    }

    protected override CollisResult TestVsPolygon( PhysPolygon box, float t )
    {
      return new CollisResult();
    }

    protected override CollisResult TestVsLine( PhysLine line, float t )
    {
      return new CollisResult();
    }
  }
}