using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLibrary;
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
    public List<PhysBody> CollisionList { get; private set; }

    public delegate bool CollisionEvent( Collision result );
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
    }

    public void ClearEvents()
    {
      Collided = null;
      Responded = null;
    }

    public bool OnCollision( Collision result )
    {
      if ( Collided != null && !CollisionList.Contains( result.BodyB ) )
        return Collided( result );
      return true;
    }

    public bool OnResponse( Collision result )
    {
      if ( Responded != null && !CollisionList.Contains( result.BodyB ) )
        return Responded( result );
      return true;
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