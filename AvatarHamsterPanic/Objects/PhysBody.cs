using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLib;
using System.Diagnostics;

namespace Physics
{
  // PhysBody special flags
  [Flags]
  enum PhysBodyFlags
  {
    None,
    Anchored,
    Ghost,
  }

  class CollisResult
  {
    public CollisResult()
      : this( false, float.MaxValue, null, Vector2.Zero, Vector2.Zero )
    {
    }

    public CollisResult( bool collision, float time, PhysBody obj, Vector2 normal, Vector2 intersection )
    {
      Collision    = collision;
      Time         = time;
      Object       = obj;
      Normal       = normal;
      Intersection = intersection;
    }

    public bool Collision { get; set; }
    public float Time { get; set; }
    public PhysBody Object { get; set; }
    public Vector2 Normal { get; set; }
    public Vector2 Intersection { get; set; }

    public CollisResult GetInvert( PhysBody collider )
    {
      return new CollisResult( Collision, Time, collider, -Normal, Intersection );
    }
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
    protected float m_torque = 0f;
    protected Vector2 m_force = Vector2.Zero;
    protected PhysBodyFlags m_flags = PhysBodyFlags.None;
    protected float m_elasticity = .5f;
    protected float m_friction = .5f;

    public delegate bool CollisionEvent( PhysBody collider, CollisResult data );
    public event CollisionEvent Collided = null;
    public event CollisionEvent Responded = null;

    private bool released = false;

    // Construct PhysBody and add it to the list
    public PhysBody( Vector2 pos, Vector2 vel, float mass )
    {
      m_pos  = pos;
      m_vel  = vel;
      m_mass = mass;
      MomentOfIntertia = 1f;

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
    public Vector2 Force { get { return m_force; } set { m_force = value; } }
    public Vector2 TouchNormal { get { return m_touchN; } set { m_touchN = value; } }
    public PhysBody Touching { get { return m_touching; } set { m_touching = value; } }
    public float Angle { get { return m_angle; } set { m_angle = value; } }
    public float AngularVelocity { get { return m_angVel; } set { m_angVel = value; } }
    public float Torque { get { return m_torque; } set { m_torque = value; } }
    public PhysBodyFlags Flags { get { return m_flags; } set { m_flags = value; } }
    public float Mass { get { return m_mass; } set { m_mass = value; } }
    public float Elasticity { get { return m_elasticity; } set { m_elasticity = value; } }
    public float Friction { get { return m_friction; } set { m_friction = value; } }
    public bool Released { get { return released; } }
    public float MomentOfIntertia { get; set; }
    public object Parent { get; set; }

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

      return new CollisResult();
    }

    public void ApplyResponseFrom( CollisResult result )
    {
      PhysBody body = result.Object;

      this.m_touching = body;
      body.m_touching = this;

      this.m_touchN = result.Normal;
      body.m_touchN = -result.Normal;

      bool ignoreResponse = false;
      if ( Collided != null )
        ignoreResponse = !Collided( this, result );
      if ( body.Collided != null )
        ignoreResponse = ignoreResponse || !body.Collided( body, result.GetInvert( this ) );
      if ( ignoreResponse ) return;

      float e = Math.Min( this.Elasticity, body.Elasticity );
      float u = Math.Max( this.Friction, body.Friction );
      Vector2 n = result.Normal;

      // adjust normal in case of floating point error
      if ( n.X == 0f && Math.Abs( n.Y ) != 1f )
        n.Y = Math.Sign( n.Y );
      else if ( n.Y == 0f && Math.Abs( n.X ) != 1f )
        n.X = Math.Sign( n.X );

      Vector2 rA = result.Intersection - this.Position;
      Vector2 rB = result.Intersection - body.Position;
      Vector2 vA = this.Velocity + Geometry.Perp( rA ) * -this.AngularVelocity;
      Vector2 vB = body.Velocity + Geometry.Perp( rB ) * -body.AngularVelocity;
      Vector2 vAB = vA - vB;
      Vector2 fricDir = -( vAB - Vector2.Dot( vAB, n ) * n );

      if ( fricDir != Vector2.Zero )
        fricDir.Normalize();
      if ( float.IsInfinity( fricDir.X ) || float.IsInfinity( fricDir.Y ) )
        fricDir = Vector2.Zero;

      float oneByMassA = 1f / Mass;
      float oneByMassB = 1f / body.Mass;
      float oneByIA = 1f / MomentOfIntertia;
      float oneByIB = 1f / body.MomentOfIntertia;

      if ( ( body.Flags & PhysBodyFlags.Anchored ) != 0 )
      {
        oneByMassB = 0f;
        oneByIB = 0f;
      }

      float dotASq = Geometry.PerpDot( rA, n ); dotASq *= dotASq;
      float dotBSq = Geometry.PerpDot( rB, n ); dotBSq *= dotBSq;
      float jc = Vector2.Dot( vAB, n ) / ( oneByMassA + oneByMassB + dotASq * oneByIA + dotBSq * oneByIB );

      dotASq = Geometry.PerpDot( rA, fricDir ); dotASq *= dotASq;
      dotBSq = Geometry.PerpDot( rB, fricDir ); dotBSq *= dotBSq;
      float jf = Vector2.Dot( vAB, fricDir ) / ( oneByMassA + oneByMassB + dotASq * oneByIA + dotBSq * oneByIB );

      if ( Math.Abs( jf ) > Math.Abs( jc * u ) )
        jf = Math.Abs( jc * u ) * Math.Sign( jc );

      Vector2 impulse = ( jc * -( 1f + e ) ) * n - jf * fricDir;

      this.Velocity += ( impulse * oneByMassA );
      body.Velocity -= ( impulse * oneByMassB );

      this.AngularVelocity += ( Geometry.PerpDot( rA, impulse ) * oneByIA );
      body.AngularVelocity -= ( Geometry.PerpDot( rB, impulse ) * oneByIB );

      if ( Responded != null )
        Responded( this, result );
      if ( body.Responded != null )
        body.Responded( body, result.GetInvert( this ) );
    }

    public bool HandleCollision( CollisResult data )
    {
      if ( Collided != null )
      {
        Collided( this, data );
        return true;
      }
      return false;
    }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans;
      Matrix.CreateRotationZ( m_angle, out transform );
      Matrix.CreateTranslation( m_pos.X, m_pos.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public static float GetForceRequired( float targetVel, float curVel, float curForce, float mass, float t )
    {
      return ( ( ( mass * ( targetVel - curVel ) ) / t ) - curForce );
    }

    protected abstract CollisResult TestVsCircle( PhysCircle circle, float t );
    protected abstract CollisResult TestVsPolygon( PhysPolygon box, float t );
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
      MomentOfIntertia = .5f * Mass * ( radius * radius );
    }

    public float Radius { get { return m_radius; } set { m_radius = value; } }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      CollisResult result = new CollisResult();

      Vector2 relVel    = Vector2.Subtract( m_vel, circle.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT    = Vector2.Add( m_pos, relVelByT );

      float   time   = 0.0f;
      Vector2 normal = Vector2.Zero;
      if ( Geometry.SegmentVsCircle( out time, out normal, m_pos, posAtT, circle.Position, m_radius + circle.m_radius ) )
      {
        float timeStep = time * t;
        result.Time = timeStep;
        result.Collision = true;
        result.Normal = normal;
        result.Object = circle;

        Vector2 dispAtCollision = ( circle.m_pos + circle.m_vel * timeStep ) - ( m_pos + m_vel * timeStep );
        result.Intersection = m_pos + ( m_radius / ( m_radius + circle.m_radius ) ) * dispAtCollision;
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
              return new CollisResult( true, time * t, poly, n, m_pos + t * time * ( m_vel ) - n * m_radius );
            else if ( time * t < bestResult.Time )
              bestResult = new CollisResult( true, time * t, poly, n, m_pos + t * time * ( m_vel ) - n * m_radius );
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
                return new CollisResult( true, time * t, poly, normal, transfVert + t * time * poly.Velocity );
              else if ( time * t < bestResult.Time )
                bestResult = new CollisResult( true, time * t, poly, normal, transfVert + t * time * poly.Velocity );
            }
          }
        }

        lastVert = transfVert;
      }

      return bestResult;
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

      MomentOfIntertia = GetMomentOfInertia( this );

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

      MomentOfIntertia = GetMomentOfInertia( this );

      Convex = Geometry.PolyIsConvex( m_verts.ToArray() );
    }

    private static float GetMomentOfInertia( PhysPolygon poly )
    {
      float sum = 0f;
      foreach ( Vector2 vert in poly.Vertices )
        sum += ( vert - poly.Position ).LengthSquared();

      return ( ( poly.Mass / poly.Vertices.Count ) * sum );
    }

    public List<Vector2> Vertices { get { return m_verts; } }
    public bool Convex { get; private set; }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      return circle.TestVsBody( this, t );
    }

    protected override CollisResult TestVsPolygon( PhysPolygon box, float t )
    {
      return new CollisResult();
    }
  }
}