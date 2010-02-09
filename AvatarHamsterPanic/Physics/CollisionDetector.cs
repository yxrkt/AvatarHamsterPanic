using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLibrary;
using AvatarHamsterPanic.Objects;
using System.Diagnostics;

namespace Physics
{
  public struct Collision
  {
    public Collision( bool collided, float time, PhysBody bodyA, PhysBody bodyB, Vector2 normal, Vector2 isect )
    {
      Collided = collided;
      Time = time;
      BodyA = bodyA;
      BodyB = bodyB;
      Normal = normal;
      Intersection = isect;
    }

    public bool Collided;
    public float Time;
    public PhysBody BodyA;
    public PhysBody BodyB;
    public Vector2 Normal;
    public Vector2 Intersection;

    public Collision GetInvert()
    {
      return new Collision( Collided, Time, BodyB, BodyA, -Normal, Intersection );
    }
  }

  public static class CollisionDetector
  {
    public static Collision TestForCollision( PhysBody bodyA, PhysBody bodyB, float elapsed )
    {
      if ( bodyA.MotionBounds.Left > bodyB.MotionBounds.Right ||
           bodyB.MotionBounds.Left > bodyA.MotionBounds.Right ||
           bodyA.MotionBounds.Top < bodyB.MotionBounds.Bottom ||
           bodyB.MotionBounds.Top < bodyA.MotionBounds.Bottom )
        return new Collision();

      if ( bodyA is PhysCircle && bodyB is PhysCircle )
        return CircleVsCircle( (PhysCircle)bodyA, (PhysCircle)bodyB, elapsed );

      if ( bodyA is PhysCircle && bodyB is PhysPolygon )
        return CircleVsPolygon( (PhysCircle)bodyA, (PhysPolygon)bodyB, elapsed );

      if ( bodyA is PhysPolygon && bodyB is PhysCircle )
        return CircleVsPolygon( (PhysCircle)bodyB, (PhysPolygon)bodyA, elapsed ).GetInvert();

      if ( bodyA is PhysPolygon && bodyB is PhysPolygon )
        return PolygonVsPolygon( (PhysPolygon)bodyA, (PhysPolygon)bodyB, elapsed );

      throw new InvalidCastException( "Unknown physbody type." );
    }

    private static Collision CircleVsCircle( PhysCircle bodyA, PhysCircle bodyB, float elapsed )
    {
      Collision result = new Collision();

      Vector2 normal;

      // if intersecting at t = 0
      Vector2 popoutPos = Vector2.Zero;
      bool popout = false;

      float totalRadius = bodyA.Radius + bodyB.Radius;
      if ( Vector2.DistanceSquared( bodyA.Position, bodyB.Position ) < .95f * ( totalRadius * totalRadius ) )
      {
        if ( !bodyA.Flags.HasFlags( BodyFlags.Ghost ) && !bodyB.Flags.HasFlags( BodyFlags.Ghost ) )
        {
          normal = Vector2.Normalize( bodyA.Position - bodyB.Position );
          popoutPos = bodyB.Position + 1.0001f * totalRadius * normal - bodyA.Velocity * elapsed;
          popout = true;
        }
        else
        {
          return new Collision( true, 0f, bodyA, bodyB, Vector2.Zero, Vector2.Zero );
        }
      }

      // if not intersecting at t = 0
      Vector2 relVel = Vector2.Subtract( bodyA.Velocity, bodyB.Velocity );
      Vector2 relVelByT = Vector2.Multiply( relVel, elapsed );
      Vector2 posAtT = Vector2.Add( bodyA.Position, relVelByT );

      float time;
      if ( Geometry.SegmentVsCircle( out time, out normal, bodyA.Position, posAtT, bodyB.Position, bodyA.Radius + bodyB.Radius ) )
      {
        float timeStep = Math.Max( 0f, time * elapsed );
        result.Time = timeStep;
        result.Collided = true;
        result.Normal = normal;
        result.BodyA = bodyA;
        result.BodyB = bodyB;

        Vector2 dispAtCollision = ( bodyB.Position + bodyB.Velocity * timeStep ) - ( bodyA.Position + bodyA.Velocity * timeStep );
        result.Intersection = bodyA.Position + ( bodyA.Radius / ( bodyA.Radius + bodyB.Radius ) ) * dispAtCollision;
      }

      if ( popout && !result.Collided )
        bodyA.Position = popoutPos;
      return result;
    }

    private static Collision CircleVsPolygon( PhysCircle bodyA, PhysPolygon bodyB, float elapsed )
    {
      Vector2 relVel, relVelByT, posAtT;
      Vector2.Subtract( ref bodyA.Velocity, ref bodyB.Velocity, out relVel );
      Vector2.Multiply( ref relVel, elapsed, out relVelByT );
      Vector2.Add( ref bodyA.Position, ref relVelByT, out posAtT );

      Vector2[] verts = bodyB.TransformedVertices;
      Vector2 lastVert = verts.Last();

      Collision bestResult = new Collision();
      bestResult.Time = float.MaxValue;

      Vector2 popoutPos = Vector2.Zero;
      Vector2 popoutNormal = Vector2.Zero;
      Vector2 popoutIsect = Vector2.Zero;
      int popoutPriority = 0;

      int nVerts = verts.Length;
      for ( int i = 0; i < nVerts; ++i )
      {
        Vector2 vert = verts[i];
        Vector2 edge = Vector2.Subtract( vert, lastVert );
        Vector2 n = new Vector2( edge.Y, -edge.X );

        float time;
        Vector2 normal;

        // ball is moving towards the segment
        if ( Vector2.Dot( n, relVel ) < 0.0f )
        {
          n.Normalize();
          Vector2 offset = Vector2.Multiply( n, bodyA.Radius );
          Vector2 q0 = lastVert + offset;
          Vector2 q1 = vert + offset;

          // check if intersecting segment at elapsed = 0
          if ( Geometry.SegmentVsCircle( out time, out normal, lastVert, vert, bodyA.Position, bodyA.Radius ) )
          {
            if ( time < .95f && popoutPriority != 1 )
            {
              float dot = Vector2.Dot( normal, -n );
              if ( dot > 0f )
              {
                popoutNormal = -normal;
                popoutIsect = edge * time;
                popoutPos = bodyA.Position + n * 1.0001f * bodyA.Radius * ( 1f - dot ) - bodyA.Velocity * elapsed;
                popoutPriority = 1;
              }
            }
          }

          if ( Geometry.SegmentVsSegment( out time, bodyA.Position, posAtT, q0, q1 ) )
          {
            // if collision with segment (and polygon is convex), we're done
            if ( bodyB.Convex )
              return new Collision( true, time * elapsed, bodyA, bodyB, n, bodyA.Position + elapsed * time * ( bodyA.Velocity ) - n * bodyA.Radius );
            else if ( time * elapsed < bestResult.Time )
              bestResult = new Collision( true, time * elapsed, bodyA, bodyB, n, bodyA.Position + elapsed * time * ( bodyA.Velocity ) - n * bodyA.Radius );
          }
        }

        // CHECK CORNER
        // inside circle?
        if ( Vector2.DistanceSquared( bodyA.Position, vert ) < ( bodyA.Radius * bodyA.Radius ) )
        {
          if ( popoutPriority == 0 )
          {
            popoutPriority = 2;
            normal = Vector2.Normalize( bodyA.Position - vert );
            popoutPos = vert + bodyA.Radius * normal;
            popoutNormal = normal;
            popoutIsect = vert;
          }
        }

        // intersecting circle
        if ( Geometry.SegmentVsCircle( out time, out normal, bodyA.Position, posAtT, vert, bodyA.Radius ) )
        {
          // additional checks to see if hitting correct sector of circle
          if ( Vector2.Dot( normal, edge ) > 0.0f )
          {
            Vector2 nextVert = verts[( i + 1 ) % nVerts];
            Vector2 edge2;
            Vector2.Subtract( ref nextVert, ref vert, out edge2 );
            if ( Vector2.Dot( normal, edge2 ) < 0.0f )
            {
              if ( bodyB.Convex )
                return new Collision( true, time * elapsed, bodyA, bodyB, normal, vert + elapsed * time * bodyB.Velocity );
              else if ( time * elapsed < bestResult.Time )
                bestResult = new Collision( true, time * elapsed, bodyA, bodyB, normal, vert + elapsed * time * bodyB.Velocity );
            }
          }
        }

        lastVert = vert;
      }

      // hack to keep objects from penetrating in rare cases
      if ( !bestResult.Collided && popoutPriority != 0 )
      {
        if ( !bodyA.Flags.HasFlags( BodyFlags.Ghost ) && !bodyB.Flags.HasFlags( BodyFlags.Ghost ) )
          bodyA.Position = popoutPos;
        else return new Collision( true, 0, bodyA, bodyB, popoutNormal, popoutIsect );
      }

      return bestResult;
    }

    private static Collision PolygonVsPolygon( PhysPolygon bodyA, PhysPolygon bodyB, float elapsed )
    {
      return new Collision();
    }
  }
}