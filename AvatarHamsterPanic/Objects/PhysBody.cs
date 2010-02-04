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

  static class PhysBodyFlagsHelper
  {
    public static bool HasFlags( this PhysBodyFlags flags, PhysBodyFlags value )
    {
      return ( ( flags & value ) == value );
    }
  }

  struct CollisResult
  {
    public CollisResult( bool collision, float time, PhysBody bodyA, PhysBody bodyB, Vector2 normal, Vector2 isect )
    {
      Collision    = collision;
      Time         = time;
      BodyA        = bodyA;
      BodyB        = bodyB;
      Normal       = normal;
      Intersection = isect;
    }

    public bool Collision;
    public float Time;
    public PhysBody BodyA;
    public PhysBody BodyB;
    public Vector2 Normal;
    public Vector2 Intersection;

    public CollisResult GetInvert()
    {
      return new CollisResult( Collision, Time, BodyB, BodyA, -Normal, Intersection );
    }
  }

  /// <summary>
  /// Base physics object class
  /// </summary>
  abstract class PhysBody : IComparable
  {
    // Static list available 
    private static List<PhysBody> s_bodies = new List<PhysBody>();

    // Public fields
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Force;
    public float Mass;
    public float Angle;
    public float AngularVelocity;
    public float Torque;
    public float MomentOfInertia;
    public PhysBodyFlags Flags;
    public float Elasticity;
    public float Friction;
    public Vector2 TouchNormal;
    public PhysBody Touching;
    public object Parent;

    // Properties
    public static List<PhysBody> AllBodies { get { return s_bodies; } }
    public List<PhysBody> CollisionList { get; private set; }

    public delegate bool CollisionEvent( CollisResult data );
    public event CollisionEvent Collided = null;
    public event CollisionEvent Responded = null;

    public uint CollisionIndex = 0;
    public CollisResult LastResult = new CollisResult();
    public bool Moved = false;

    public bool Released = false;

    // Constructor
    public PhysBody( Vector2 pos, Vector2 vel, float mass )
    {
      Position        = pos;
      Velocity        = vel;
      Mass            = mass;

      MomentOfInertia = 1f;
      Force           = Vector2.Zero;
      Angle           = 0.0f;
      AngularVelocity = 0.0f;
      Torque          = 0f;
      Flags           = PhysBodyFlags.None;
      Elasticity      = .5f;
      Friction        = .5f;
      TouchNormal     = Vector2.Zero;
      Touching        = null;
      Parent          = null;

      CollisionList = new List<PhysBody>( 4 );

      s_bodies.Add( this );
    }

    public void Release()
    {
      Released = true;
    }

    public void ClearEvents()
    {
      Collided = null;
      Responded = null;
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
      PhysBody body = result.BodyB;

      this.Touching = body;
      body.Touching = this;

      this.TouchNormal = result.Normal;
      body.TouchNormal = -result.Normal;

      bool ignoreResponse = false;
      if ( this.Collided != null && !this.CollisionList.Contains( body ) )
        ignoreResponse = !Collided( result );
      if ( body.Collided != null && !body.CollisionList.Contains( this ) )
        ignoreResponse = ignoreResponse || !body.Collided( result.GetInvert() );
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
      float oneByIA = 1f / MomentOfInertia;
      float oneByIB = 1f / body.MomentOfInertia;

      if ( body.Flags.HasFlags( PhysBodyFlags.Anchored ) )
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

      if ( this.Responded != null && !this.CollisionList.Contains( body ) )
        Responded( result );
      if ( body.Responded != null && !body.CollisionList.Contains( this ) )
        body.Responded( result.GetInvert() );

      this.CollisionList.Add( body );
      body.CollisionList.Add( this );
    }

    public bool HandleCollision( CollisResult result )
    {
      if ( Collided != null )
      {
        Collided( result );
        return true;
      }
      return false;
    }

    public void GetTransform( out Matrix transform )
    {
      Matrix matTrans;
      Matrix.CreateRotationZ( Angle, out transform );
      Matrix.CreateTranslation( Position.X, Position.Y, 0f, out matTrans );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public static float GetForceRequired( float targetVel, float curVel, float curForce, float mass, float t )
    {
      if ( t <= 0 )
        return 0f;
      return ( ( ( mass * ( targetVel - curVel ) ) / t ) - curForce );
    }

    protected abstract CollisResult TestVsCircle( PhysCircle circle, float t );
    protected abstract CollisResult TestVsPolygon( PhysPolygon box, float t );

    public int CompareTo( object obj )
    {
      return ( (int)( this.Flags & PhysBodyFlags.Anchored ) - (int)(( (PhysBody)obj ).Flags & PhysBodyFlags.Anchored ) );
    }
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
      MomentOfInertia = .5f * Mass * ( radius * radius );
    }

    public float Radius { get { return m_radius; } set { m_radius = value; } }

    protected override CollisResult TestVsCircle( PhysCircle circle, float t )
    {
      CollisResult result = new CollisResult();

      Vector2 normal;

      // if intersecting at t = 0
      Vector2 popoutPos = Vector2.Zero;
      bool popout = false;

      float totalRadius = m_radius + circle.m_radius;
      if ( Vector2.DistanceSquared( Position, circle.Position ) < .95f * ( totalRadius * totalRadius ) )
      {
        if ( !Flags.HasFlags( PhysBodyFlags.Ghost ) && !circle.Flags.HasFlags( PhysBodyFlags.Ghost ) )
        {
          normal = Vector2.Normalize( Position - circle.Position );
          popoutPos = circle.Position + 1.0001f * totalRadius * normal - Velocity * t;
          popout = true;
        }
        else
        {
          return new CollisResult( true, 0f, this, circle, Vector2.Zero, Vector2.Zero );
        }
      }

      // if not intersecting at t = 0
      Vector2 relVel    = Vector2.Subtract( Velocity, circle.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT    = Vector2.Add( Position, relVelByT );

      float time;
      if ( Geometry.SegmentVsCircle( out time, out normal, Position, posAtT, circle.Position, m_radius + circle.m_radius ) )
      {
        float timeStep = Math.Max( 0f, time * t );
        result.Time = timeStep;
        result.Collision = true;
        result.Normal = normal;
        result.BodyA = this;
        result.BodyB = circle;

        Vector2 dispAtCollision = ( circle.Position + circle.Velocity * timeStep ) - ( Position + Velocity * timeStep );
        result.Intersection = Position + ( m_radius / ( m_radius + circle.m_radius ) ) * dispAtCollision;
      }

      if ( popout && !result.Collision )
        Position = popoutPos;
      return result;
    }

    protected override CollisResult TestVsPolygon( PhysPolygon poly, float t )
    {
      // transform that takes local vertex coordinates to world space
      Matrix transform;
      poly.GetTransform( out transform );

      Vector2 relVel    = Vector2.Subtract( Velocity, poly.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, t );
      Vector2 posAtT    = Vector2.Add( Position, relVelByT );

      Vector2[] verts = poly.Vertices;
      Vector2 lastVert = verts.Last();
      lastVert = Vector2.Transform( lastVert, transform );

      CollisResult bestResult = new CollisResult();
      bestResult.Time = float.MaxValue;
      Vector2 popoutPos = Vector2.Zero;
      Vector2 popoutNormal = Vector2.Zero;
      Vector2 popoutIsect = Vector2.Zero;
      int popoutPriority = 0;

      int nVerts = verts.Length;
      for ( int i = 0; i < nVerts; ++i )
      {
        Vector2 vert = verts[i];
        Vector2 transfVert = Vector2.Transform( vert, transform );
        Vector2 edge = Vector2.Subtract( transfVert, lastVert );
        Vector2 n = new Vector2( edge.Y, -edge.X );

        float time;
        Vector2 normal;

        // ball is moving towards the segment
        if ( Vector2.Dot( n, relVel ) < 0.0f )
        {
          n.Normalize();
          Vector2 offset = Vector2.Multiply( n, m_radius );
          Vector2 q0 = lastVert + offset;
          Vector2 q1 = transfVert + offset;

          // check if intersecting segment at t = 0
          if ( Geometry.SegmentVsCircle( out time, out normal, lastVert, transfVert, Position, m_radius ) )
          {
            if ( time < .95f && popoutPriority != 1 )
            {
              float dot = Vector2.Dot( normal, -n );
              if ( dot > 0f )
              {
                popoutNormal = -normal;
                popoutIsect = edge * time;
                popoutPos = Position + n * 1.0001f * m_radius * ( 1f - dot ) - Velocity * t;
                popoutPriority = 1;
              }
            }
          }

          if ( Geometry.SegmentVsSegment( out time, Position, posAtT, q0, q1 ) )
          {
            // if collision with segment (and polygon is convex), we're done
            if ( poly.Convex )
              return new CollisResult( true, time * t, this, poly, n, Position + t * time * ( Velocity ) - n * m_radius );
            else if ( time * t < bestResult.Time )
              bestResult = new CollisResult( true, time * t, this, poly, n, Position + t * time * ( Velocity ) - n * m_radius );
          }
        }

        // CHECK CORNER
        // inside circle?
        if ( Vector2.DistanceSquared( Position, transfVert ) < ( m_radius * m_radius ) )
        {
          if ( popoutPriority == 0 )
          {
            popoutPriority = 2;
            normal = Vector2.Normalize( Position - transfVert );
            popoutPos = transfVert + m_radius * normal;
            popoutNormal = normal;
            popoutIsect = transfVert;
          }
        }

        // intersecting circle
        if ( Geometry.SegmentVsCircle( out time, out normal, Position, posAtT, transfVert, m_radius ) )
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
                return new CollisResult( true, time * t, this, poly, normal, transfVert + t * time * poly.Velocity );
              else if ( time * t < bestResult.Time )
                bestResult = new CollisResult( true, time * t, this, poly, normal, transfVert + t * time * poly.Velocity );
            }
          }
        }

        lastVert = transfVert;
      }

      // hack to keep objects from penetrating in rare cases
      if ( !bestResult.Collision && popoutPriority != 0 )
      {
        if ( !this.Flags.HasFlags( PhysBodyFlags.Ghost ) && !poly.Flags.HasFlags( PhysBodyFlags.Ghost ) )
          Position = popoutPos;
        else return new CollisResult( true, 0, this, poly, popoutNormal, popoutIsect );
      }

      return bestResult;
    }
  }

  /// <summary>
  /// Physics bounding convex polygon object
  /// </summary>
  class PhysPolygon : PhysBody
  {
    Vector2[] m_verts;

    public PhysPolygon( Vector2[] verts, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      int nVerts = verts.Length;
      m_verts = new Vector2[nVerts];
      Array.Copy( verts, m_verts, nVerts );

      MomentOfInertia = GetMomentOfInertia( this );

      Convex = Geometry.PolyIsConvex( m_verts );
    }

    public PhysPolygon( float width, float height, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      float widthByTwo  = width  / 2.0f;
      float heightByTwo = height / 2.0f;

      m_verts = new Vector2[]
      {
        new Vector2(  widthByTwo,  heightByTwo ),
        new Vector2( -widthByTwo,  heightByTwo ),
        new Vector2( -widthByTwo, -heightByTwo ),
        new Vector2(  widthByTwo, -heightByTwo )
      };

      MomentOfInertia = GetMomentOfInertia( this );

      Convex = Geometry.PolyIsConvex( m_verts );
    }

    public void SetPivotPoint( Vector2 origin )
    {
      Position += origin;
      for ( int i = 0; i < m_verts.Length; ++i )
        m_verts[i] -= origin;
    }

    private static float GetMomentOfInertia( PhysPolygon poly )
    {
      float sum = 0f;
      foreach ( Vector2 vert in poly.m_verts )
        sum += ( vert - poly.Position ).LengthSquared();

      return ( ( poly.Mass / poly.m_verts.Length ) * sum );
    }

    public Vector2[] Vertices { get { return m_verts; } }
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