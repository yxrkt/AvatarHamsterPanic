using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLib;
using System.Diagnostics;

namespace Physics
{
  // Body flags for special behavior
  [Flags]
  public enum BodyFlags
  {
    None,
    Anchored,
    Ghost,
  }

  static class BodyFlagsHelper
  {
    public static bool HasFlags( this BodyFlags flags, BodyFlags value )
    {
      return ( ( flags & value ) == value );
    }
  }

  public struct MotionBounds
  {
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
    public Vector2 PositionPlusMotion;
  }

  /// <summary>
  /// Base physics object class
  /// </summary>
  public abstract class PhysBody : IComparable
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
    public BodyFlags Flags;
    public float Elasticity;
    public float Friction;
    public Vector2 TouchNormal;
    public PhysBody Touching;
    public object Parent;

    public static long TestVsCircleTicks = 0;
    public static int TestVsCirlceHits = 0;
    public static long TestVsPolygonTicks = 0;
    public static int TestVsPolygonHits = 0;

    // Properties
    public static List<PhysBody> AllBodies { get { return s_bodies; } }
    public List<PhysBody> CollisionList { get; private set; }

    public delegate bool CollisionEvent( Collision data );
    public event CollisionEvent Collided = null;
    public event CollisionEvent Responded = null;

    public uint CollisionIndex = 0;
    public Collision LastResult = new Collision();
    public bool Moved = false;
    public MotionBounds MotionBounds;

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
      Flags           = BodyFlags.None;
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

    public void ApplyResponseFrom( Collision result )
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

      if ( body.Flags.HasFlags( BodyFlags.Anchored ) )
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

    public bool HandleCollision( Collision result )
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

    protected abstract void UpdateMotionBounds( float elapsed );
    public virtual void UpdateInternalData( float elapsed )
    {
      UpdateMotionBounds( elapsed );
    }

    public int CompareTo( object obj )
    {
      return ( (int)( this.Flags & BodyFlags.Anchored ) - (int)(( (PhysBody)obj ).Flags & BodyFlags.Anchored ) );
    }
  }

  /// <summary>
  /// Physics bounding circle object
  /// </summary>
  public class PhysCircle : PhysBody
  {
    public float Radius;

    public PhysCircle( float radius, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      Radius = radius;
      MomentOfInertia = .5f * Mass * ( radius * radius );
    }

    protected override void UpdateMotionBounds( float elapsed )
    {
      Vector2 motion;
      Vector2.Multiply( ref Velocity, elapsed, out motion );

      Vector2.Add( ref Position, ref motion, out MotionBounds.PositionPlusMotion );

      if ( motion.X < 0 )
      {
        MotionBounds.Left = MotionBounds.PositionPlusMotion.X - Radius;
        MotionBounds.Right = Position.X + Radius;
      }
      else
      {
        MotionBounds.Left = Position.X - Radius;
        MotionBounds.Right = MotionBounds.PositionPlusMotion.X + Radius;
      }

      if ( motion.Y < 0 )
      {
        MotionBounds.Top = Position.Y + Radius;
        MotionBounds.Bottom = MotionBounds.PositionPlusMotion.Y - Radius;
      }
      else
      {
        MotionBounds.Top = MotionBounds.PositionPlusMotion.Y + Radius;
        MotionBounds.Bottom = Position.Y - Radius;
      }
    }
  }

  /// <summary>
  /// Physics bounding convex polygon object
  /// </summary>
  public class PhysPolygon : PhysBody
  {
    public Vector2[] Vertices;
    public Vector2[] TransformedVertices;
    public bool Convex;

    public Vector2 Center;
    public float RadiusSquared;

    public PhysPolygon( Vector2[] verts, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      int nVerts = verts.Length;
      Vertices = new Vector2[nVerts];
      Array.Copy( verts, Vertices, nVerts );

      TransformedVertices = new Vector2[nVerts];

      MomentOfInertia = GetMomentOfInertia( this );

      Convex = Geometry.PolyIsConvex( Vertices );
    }

    public PhysPolygon( float width, float height, Vector2 pos, float mass )
      : base( pos, Vector2.Zero, mass )
    {
      float widthByTwo  = width  / 2.0f;
      float heightByTwo = height / 2.0f;

      Vertices = new Vector2[]
      {
        new Vector2(  widthByTwo,  heightByTwo ),
        new Vector2( -widthByTwo,  heightByTwo ),
        new Vector2( -widthByTwo, -heightByTwo ),
        new Vector2(  widthByTwo, -heightByTwo )
      };

      TransformedVertices = new Vector2[4];

      MomentOfInertia = GetMomentOfInertia( this );

      Convex = Geometry.PolyIsConvex( Vertices );

      ComputeBoundingCircle();
    }

    public void ComputeBoundingCircle()
    {
      Vector2 sum = Vector2.Zero;
      foreach ( Vector2 vert in Vertices )
        sum += vert;
      Center = sum / Vertices.Length;

      RadiusSquared = 0;
      foreach ( Vector2 vert in Vertices )
        RadiusSquared = Math.Max( Vector2.DistanceSquared( vert, Center ), RadiusSquared );
    }

    public void SetPivotPoint( Vector2 origin )
    {
      Position += origin;
      for ( int i = 0; i < Vertices.Length; ++i )
        Vertices[i] -= origin;
    }

    private static float GetMomentOfInertia( PhysPolygon poly )
    {
      float sum = 0f;
      foreach ( Vector2 vert in poly.Vertices )
        sum += ( vert - poly.Position ).LengthSquared();

      return ( ( poly.Mass / poly.Vertices.Length ) * sum );
    }

    public override void UpdateInternalData( float elapsed )
    {
      UpdateTransformedVertices();
      base.UpdateInternalData( elapsed );
    }

    private void UpdateTransformedVertices()
    {
      Matrix transform;
      GetTransform( out transform );

      int nVerts = Vertices.Length;
      for ( int i = 0; i < nVerts; ++i )
        Vector2.Transform( ref Vertices[i], ref transform, out TransformedVertices[i] );
    }

    protected override void UpdateMotionBounds( float elapsed )
    {
      MotionBounds.Left = MotionBounds.Right = TransformedVertices[0].X;
      MotionBounds.Top = MotionBounds.Bottom = TransformedVertices[0].Y;

      int nVerts = TransformedVertices.Length;
      for ( int i = 1; i < nVerts; ++i )
      {
        if ( TransformedVertices[i].X < MotionBounds.Left )
          MotionBounds.Left = TransformedVertices[i].X;
        else if ( TransformedVertices[i].X > MotionBounds.Right )
          MotionBounds.Right = TransformedVertices[i].X;

        if ( TransformedVertices[i].Y < MotionBounds.Bottom )
          MotionBounds.Bottom = TransformedVertices[i].Y;
        else if ( TransformedVertices[i].Y > MotionBounds.Top )
          MotionBounds.Top = TransformedVertices[i].Y;
      }

      Vector2 motion;
      Vector2.Multiply( ref Velocity, elapsed, out motion );

      Vector2.Add( ref Position, ref motion, out MotionBounds.PositionPlusMotion );

      if ( motion.X < 0 )
        MotionBounds.Left -= motion.X;
      else if ( motion.X > 0 )
        MotionBounds.Right += motion.X;

      if ( motion.Y < 0 )
        MotionBounds.Bottom -= motion.Y;
      else if ( motion.Y > 0 )
        MotionBounds.Top += motion.Y;
    }
  }
}