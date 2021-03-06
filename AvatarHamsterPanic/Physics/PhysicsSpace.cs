using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLibrary;
using AvatarHamsterPanic.Objects;
using Utilities;
using System.Diagnostics;


namespace Physics
{
  public class PhysicsSpace
  {
    public Vector2 Gravity = new Vector2( 0.0f, -10.0f );

    uint collisionIndex = 0;
    List<PhysBody> bodies;

    // initialization
    public PhysicsSpace()
    {
      bodies = new List<PhysBody>( 50 );
    }

    // public methods
    public void AddBody( PhysBody body )
    {
#if DEBUG
      if ( bodies.Contains( body ) && !body.Released )
        throw new InvalidOperationException( "body already in space" );
#endif
      // if Released is marked true, then the body hasn't been removed yet
      if ( !bodies.Contains( body ) )
        bodies.Add( body );
      body.Released = false;
    }

    public void RemoveBody( PhysBody body )
    {
#if DEBUG
      //if ( !bodies.Contains( body ) || body.Released )
      //  throw new InvalidOperationException( "invalid body release" );
#endif
      body.Released = true;
    }

    // update
    public void Update( double elapsed )
    {
      if ( elapsed <= 0d ) return;

      // cleanup released bodies
      bodies.RemoveAll( body => body.Released );

      // bubbles anchored bodies to the bottom of the list
      bodies.Sort();

      int nBodies = bodies.Count;

      // factor in forces for each object
      for ( int i = 0; i < nBodies; ++i )
      {
        PhysBody body = bodies[i];
        body.Touching = null;
        body.CollisionList.Clear();

        if ( body.Flags.HasFlags( BodyFlags.Anchored ) )
          continue;

        // factor in net linear force
        body.Force += ( Gravity * body.Mass );
        Vector2 accel = body.Force / body.Mass;
        body.Force = Vector2.Zero;
        body.Velocity += ( accel * (float)elapsed );

        // factor in torque
        float angAccel = body.Torque / body.MomentOfInertia;
        body.Torque = 0f;
        body.AngularVelocity += ( angAccel * (float)elapsed );
      }

      int nIterations = 0;

      // update until the end of the frame
      float timeLeft = (float)elapsed;
      while ( timeLeft > 0f )
      {
        collisionIndex++;
        int nCollisions = 0;
        float bestTime = float.MaxValue;

        foreach ( PhysBody body in bodies )
          body.UpdateInternalData( timeLeft );

        for ( int i = 0; i < nBodies; ++i )
        {
          PhysBody bodyA = bodies[i];
          bodyA.Moved = false;
          bodyA.LastResult.BodyB = null;

          if ( bodyA.Flags.HasFlags( BodyFlags.Anchored ) )
            break;

          for ( int j = i + 1; j < nBodies; ++j )
          {
            PhysBody bodyB = bodies[j];

            Collision result = CollisionDetector.TestForCollision( bodyA, bodyB, timeLeft );

            if ( result.Collided )
            {
              if ( bodyA.Flags.HasFlags( BodyFlags.Ghost ) || bodyB.Flags.HasFlags( BodyFlags.Ghost ) )
              {
                bodyA.OnCollision( result );
                bodyB.OnCollision( result.GetInvert() );
              }
              else
              {
                if ( nCollisions == 0 )
                {
                  bodyA.LastResult = result;
                  bodyA.CollisionIndex = collisionIndex;
                  bodyB.CollisionIndex = bodyA.CollisionIndex;
                  bestTime = result.Time;
                  nCollisions++;
                }
                else if ( result.Time < bestTime )
                {
                  bodyA.LastResult = result;
                  bodyA.CollisionIndex = ++collisionIndex;
                  bodyB.CollisionIndex = bodyA.CollisionIndex;
                  bestTime = result.Time;
                  nCollisions = 1;
                }
                else if ( result.Time == bestTime )
                {
                  bodyA.LastResult = result;
                  bodyA.CollisionIndex = collisionIndex;
                  bodyB.CollisionIndex = bodyA.CollisionIndex;
                  nCollisions++;
                }
              }
            }
          }
        }

        if ( nCollisions == 0 )
        {
          foreach ( PhysBody body in bodies )
          {
            if ( body.Flags.HasFlags( BodyFlags.Anchored ) )
              break;
            MoveBody( body, timeLeft, 0f );
          }

          timeLeft = 0f;
        }
        else
        {
          foreach ( PhysBody body in bodies )
          {
            if ( body.CollisionIndex == collisionIndex && body.LastResult.BodyB != null )
            {
              if ( !body.Moved )
                MoveBody( body, body.LastResult.Time, .001f );
              if ( !body.LastResult.BodyB.Moved && !body.LastResult.BodyB.Flags.HasFlags( BodyFlags.Anchored ) )
                MoveBody( body.LastResult.BodyB, bestTime, .001f );
              CollisionResolver.ResolveCollision( body.LastResult );
            }
            else if ( !body.Moved )
            {
              MoveBody( body, bestTime, 0f );
            }
          }

          timeLeft -= bestTime;
          if ( ++nIterations > 30 )
            timeLeft = 0f;
        }
      }
    }

    // private helpers
    private void MoveBody( PhysBody body, float timeStep, float pullBackPct )
    {
      if ( body.Velocity == Vector2.Zero ) return;
      if ( timeStep <= 0f ) return;

      body.Moved = true;
      Vector2 disp = body.Velocity * timeStep;
      if ( pullBackPct != 0f )
        disp *= ( 1f - pullBackPct );
      body.Position += disp;
      body.Angle += body.AngularVelocity * timeStep;
    }
  }
}
