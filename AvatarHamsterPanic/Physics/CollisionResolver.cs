using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MathLibrary;

namespace Physics
{
  public static class CollisionResolver
  {
    public static void ResolveCollision( Collision result )
    {
      PhysBody bodyA = result.BodyA;
      PhysBody bodyB = result.BodyB;

      bodyA.Touching = bodyB;
      bodyB.Touching = bodyA;

      bodyA.TouchNormal = result.Normal;
      bodyB.TouchNormal = -result.Normal;

      if ( !bodyA.OnCollision( result ) || !bodyB.OnCollision( result.GetInvert() ) )
        return;

      float e = Math.Min( bodyA.Elasticity, bodyB.Elasticity );
      float u = Math.Max( bodyA.Friction, bodyB.Friction );
      Vector2 n = result.Normal;

      // adjust normal in case of floating point error
      if ( n.X == 0f && Math.Abs( n.Y ) != 1f )
        n.Y = Math.Sign( n.Y );
      else if ( n.Y == 0f && Math.Abs( n.X ) != 1f )
        n.X = Math.Sign( n.X );

      Vector2 rA = result.Intersection - bodyA.Position;
      Vector2 rB = result.Intersection - bodyB.Position;
      Vector2 vA = bodyA.Velocity + Geometry.Perp( rA ) * -bodyA.AngularVelocity;
      Vector2 vB = bodyB.Velocity + Geometry.Perp( rB ) * -bodyB.AngularVelocity;
      Vector2 vAB = vA - vB;
      Vector2 fricDir = -( vAB - Vector2.Dot( vAB, n ) * n );

      if ( fricDir != Vector2.Zero )
        fricDir.Normalize();
      if ( float.IsInfinity( fricDir.X ) || float.IsInfinity( fricDir.Y ) )
        fricDir = Vector2.Zero;

      float oneByMassA = 1f / bodyA.Mass;
      float oneByMassB = 1f / bodyB.Mass;
      float oneByIA = 1f / bodyA.MomentOfInertia;
      float oneByIB = 1f / bodyB.MomentOfInertia;

      if ( bodyB.Flags.HasFlags( BodyFlags.Anchored ) )
      {
        oneByMassB = 0f;
        oneByIB = 0f;
      }

      float dotASq = Geometry.PerpDot( rA, n ); dotASq *= dotASq;
      float dotBSq = Geometry.PerpDot( rB, n ); dotBSq *= dotBSq;
      float jc = Vector2.Dot( vAB, n ) / ( oneByMassA + oneByMassB + dotASq * oneByIA + dotBSq * oneByIB );

      if ( jc > -.7f )
        jc = -.7f;
      //==
      PhysicsSpace.LastImpulse = jc;
      //==

      dotASq = Geometry.PerpDot( rA, fricDir ); dotASq *= dotASq;
      dotBSq = Geometry.PerpDot( rB, fricDir ); dotBSq *= dotBSq;
      float jf = Vector2.Dot( vAB, fricDir ) / ( oneByMassA + oneByMassB + dotASq * oneByIA + dotBSq * oneByIB );

      if ( Math.Abs( jf ) > Math.Abs( jc * u ) )
        jf = Math.Abs( jc * u ) * Math.Sign( jc );

      Vector2 impulse = ( jc * -( 1f + e ) ) * n - jf * fricDir;

      bodyA.LastImpulse = impulse;
      bodyB.LastImpulse = -impulse;

      bodyA.Velocity += ( impulse * oneByMassA );
      bodyB.Velocity -= ( impulse * oneByMassB );

      bodyA.AngularVelocity += ( Geometry.PerpDot( rA, impulse ) * oneByIA );
      bodyB.AngularVelocity -= ( Geometry.PerpDot( rB, impulse ) * oneByIB );

      bodyA.OnResponse( result );
      bodyB.OnResponse( result.GetInvert() );

      bodyA.CollisionList.Add( bodyB );
      bodyB.CollisionList.Add( bodyA );
    }
  }
}