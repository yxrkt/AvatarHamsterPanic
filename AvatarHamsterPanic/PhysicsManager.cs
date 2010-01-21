using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLib;


namespace Physics
{
  sealed class PhysicsManager
  {
    static readonly PhysicsManager instance = new PhysicsManager();

    Vector2 m_gravity = new Vector2( 0.0f, -10.0f );

    // initialization
    private PhysicsManager()
    {
    }

    public static PhysicsManager Instance { get { return instance; } }

    public Vector2 Gravity { get { return m_gravity; } set { m_gravity = value; } }

    // update
    public void Update( double elapsed )
    {
      if ( elapsed <= 0d ) return;

      List<PhysBody> bodies = PhysBody.AllBodies;

      // cleanup released bodies
      bodies.RemoveAll( body => body.Released );

      // bubbles anchored bodies to the bottom of the list
      bodies.Sort( (lhs, rhs) => (lhs.Flags & PhysBodyFlags.Anchored) - (rhs.Flags & PhysBodyFlags.Anchored) );

      int nBodies = bodies.Count;

      // factor in forces for each object
      foreach ( PhysBody body in bodies )
      {
        body.Touching = null;

        if ( body.Flags.HasFlags( PhysBodyFlags.Anchored ) )
          continue;

        // factor in net linear force
        body.Force += ( Gravity * body.Mass );
        Vector2 accel = body.Force / body.Mass;
        body.Force = Vector2.Zero;
        body.Velocity += ( accel * (float)elapsed );

        // factor in torque
        float angAccel = body.Torque / body.MomentOfIntertia;
        body.Torque = 0f;
        body.AngularVelocity += ( angAccel * (float)elapsed );
      }

      // update 'til the end of the frame
      float timeLeft = (float)elapsed;
      while ( timeLeft > 0f )
      {
        Dictionary<PhysBody, CollisResult> collisions = new Dictionary<PhysBody,CollisResult>();

        for ( int i = 0; i < nBodies; ++i )
        {
          PhysBody bodyA = bodies[i];

          // this assumes that all non-anchored bodies come before anchored ones
          if ( bodyA.Flags.HasFlags( PhysBodyFlags.Anchored ) )
            break;

          for ( int j = i + 1; j < nBodies; ++j )
          {
            PhysBody bodyB = bodies[j];
            CollisResult result = bodyA.TestVsBody( bodyB, timeLeft );
            if ( result.Collision )
            {
              if ( bodyA.Flags.HasFlags( PhysBodyFlags.Ghost ) || bodyB.Flags.HasFlags( PhysBodyFlags.Ghost ) )
              {
                bodyA.HandleCollision( result );
                bodyB.HandleCollision( result.GetInvert() );
              }
              else
              {
                if ( collisions.Count == 0 )
                {
                  collisions.Add( bodyA, result );
                }
                else if ( result.Time < collisions.First().Value.Time )
                {
                  collisions.Clear();
                  collisions.Add( bodyA, result );
                }
                else if ( result.Time == collisions.First().Value.Time )
                {
                  if ( !collisions.ContainsKey( bodyA ) )
                    collisions.Add( bodyA, result );
                }
              }
            }
          }
        }

        if ( collisions.Count == 0 )
        {
          foreach ( PhysBody body in bodies )
          {
            if ( body.Flags.HasFlags( PhysBodyFlags.Anchored ) )
              break;
            MoveBody( body, timeLeft, 0f );
          }

          timeLeft = 0f;
        }
        else
        {
          CollisResult firstCollision = collisions.First().Value;
          foreach ( PhysBody body in bodies )
          {
            if ( collisions.ContainsKey( body ) )
            {
              CollisResult collision = collisions[body];
              MoveBody( body, collision.Time, .001f );
              if ( !collision.BodyB.Flags.HasFlags( PhysBodyFlags.Anchored ) )
                MoveBody( collision.BodyB, collision.Time, .001f );
              body.ApplyResponseFrom( collision );
            }
            else if ( collisions.Count( kvp => kvp.Value.BodyB == body ) == 0 )
            {
              MoveBody( body, firstCollision.Time, 0f );
            }
          }

          timeLeft -= firstCollision.Time;
        }
      }
    }

    // private helpers
    private void MoveBody( PhysBody body, float timeStep, float pullBackPct )
    {
      if ( body.Velocity == Vector2.Zero ) return;
      if ( timeStep <= 0f ) return;

      Vector2 disp = body.Velocity * timeStep;
      if ( pullBackPct != 0f )
        disp *= ( 1f - pullBackPct );
      body.Position += disp;
      body.Angle += body.AngularVelocity * timeStep;
    }
  }
}
