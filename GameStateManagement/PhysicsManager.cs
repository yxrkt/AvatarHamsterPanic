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
      List<PhysBody> bodies = PhysBody.AllBodies;

      bodies.RemoveAll( body => body.Released );
      
      int nBodies = bodies.Count;
      for ( int i = 0; i < nBodies; ++i )
      {
        PhysBody body = bodies[i];
        
        if ( ( body.Flags & PhysBodyFlags.Anchored ) == PhysBodyFlags.Anchored ) continue;
        
        Vector2 fg = Vector2.Multiply( m_gravity, body.Mass );
        body.Force += fg;

        if ( body.Touching != null && Vector2.Dot( body.TouchNormal, body.Force ) < 0f )
        {
          Vector2 edge = new Vector2( body.TouchNormal.Y, -body.TouchNormal.X );
          float scale = Vector2.Dot( body.Force, edge );
          body.Force = Vector2.Multiply( edge, scale );
        }

        // factor in forces for final velocity before testing collision
        Vector2 accel = Vector2.Multiply( body.Force, 1.0f / body.Mass );
        body.Velocity += ( accel * (float)elapsed );
        body.Force = Vector2.Zero;

        UpdateBodyToT( body, elapsed );
        body.UpdateTouching();
      }
    }


    // private helpers
    private void UpdateBodyToT( PhysBody body, double t )
    {
      if ( t <= 0.0f ) return;
      
      List<PhysBody> bodies = PhysBody.AllBodies;
      
      CollisResult best = new CollisResult();
      
      int nBodies = bodies.Count;
      for ( int i = 0; i < nBodies; ++i )
      {
        PhysBody body2 = bodies[i];
          
        if ( body == body2 || body.Touching == body2 ) continue;

        CollisResult result = body.TestVsBody( body2, (float)t );
          
        if ( result.Collision )
        {
          if ( !best.Collision || ( result.Time < best.Time ) )
            best = result;
        }
      }
      
      Vector2 disp;
      
      if ( best.Collision )
      {
        disp = Vector2.Multiply( body.Velocity, best.Time );
        float len = disp.Length();
        disp = Vector2.Multiply( disp, ( len - .0001f ) / len );
        body.Position += disp;
        body.Angle += body.AngularVelocity * best.Time;

        body.HandleCollision();
        body.ApplyResponseFrom( best );
        UpdateBodyToT( body, (float)t - best.Time );
      }
      else
      {
        disp = Vector2.Multiply( body.Velocity, (float)t );
        body.Position = Vector2.Add( body.Position, disp );

        if ( ( body.Touching != null ) && ( body is PhysCircle ) )
        {
          PhysCircle circle = (PhysCircle)body;
          Vector2 nNormal = new Vector2( body.TouchNormal.Y, -body.TouchNormal.X );
          circle.AngularVelocity = -Vector2.Dot( circle.Velocity, nNormal ) / circle.Radius;
        }

        body.Angle += body.AngularVelocity * (float)t;
      }
    }
  }
}