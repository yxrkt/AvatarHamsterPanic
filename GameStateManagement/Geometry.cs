using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MathLib
{
  /// <summary>
  /// Geometry library
  /// </summary>
  class Geometry
  {
    private Geometry() { }

    public static bool SegmentVsSegment( out float tp, Vector2 p0, Vector2 p1, Vector2 q0, Vector2 q1 )
    {
      // set default in case returning false
      tp = float.MaxValue;

      // edge vectors
      Vector2 vp = Vector2.Subtract( p1, p0 );
      Vector2 vq = Vector2.Subtract( q1, q0 );
      
      // edge normals
      Vector2 vpn = new Vector2( -vp.Y, vp.X );
      vpn.Normalize();
      Vector2 vqn = new Vector2( -vq.Y, vq.X );
      vqn.Normalize();
      
      // signed distances from q's end points to p
      Vector2 p0q0 = Vector2.Subtract( q0, p0 );
      Vector2 p0q1 = Vector2.Subtract( q1, p0 );
      
      float q0toP = Vector2.Dot( p0q0, vpn );
      float q1toP = Vector2.Dot( p0q1, vpn );
      
      // rejection - q is on one side of p or parallel to p
      if ( q0toP < -float.Epsilon && q1toP < -float.Epsilon ) return false;
      if ( q0toP >  float.Epsilon && q1toP >  float.Epsilon ) return false;
      if ( Math.Abs( q0toP ) < float.Epsilon && Math.Abs( q1toP ) < float.Epsilon ) return false;
      
      // signed distances from p's end points to q
      Vector2 q0p0 = Vector2.Subtract( p0, q0 );
      Vector2 q0p1 = Vector2.Subtract( p1, q0 );
      
      float p0toQ = Vector2.Dot( q0p0, vqn );
      float p1toQ = Vector2.Dot( q0p1, vqn );
      
      // rejection - p is on one side of q or parallel to q -- second time necessary for floating point error
      if ( p0toQ < -float.Epsilon && p1toQ < -float.Epsilon ) return false;
      if ( p0toQ >  float.Epsilon && p1toQ >  float.Epsilon ) return false;
      if ( Math.Abs( p0toQ ) < float.Epsilon && Math.Abs( p1toQ ) < float.Epsilon ) return false;
      
      tp = p0toQ / ( p0toQ - p1toQ );
      
      return true;
    }

    public static bool SegmentVsCircle( out float t, out Vector2 n, Vector2 p0, Vector2 p1, Vector2 c, float r )
    {
      // set defaults in case returning false
      t = float.MaxValue;
      n = Vector2.Zero;

      Vector2 vp = Vector2.Subtract( p1, p0 );
      if ( vp == Vector2.Zero ) return false;
        
      Vector2 vpc = Vector2.Subtract( c, p0 );
      
      // rejection - not moving towards circle
      if ( Vector2.Dot( vp, vpc ) < 0d ) return false;

      Vector2 vpn = new Vector2( -vp.Y, vp.X );
      vpn.Normalize();
      
      float cToP = Math.Abs( Vector2.Dot( vpc, vpn ) );
      
      // rejection - distance from line is greater than radius
      if ( cToP > r ) return false;
      
      // touching edge of circle
      if ( cToP == r )
      {
        float p0toIsectSq = vpc.LengthSquared() - r * r;
        float vpLenSq     = vp.LengthSquared();
        if ( p0toIsectSq > vpLenSq )
          return false;

        t = (float)( Math.Sqrt( (double)p0toIsectSq ) / Math.Sqrt( (double)vpLenSq ) );
      }
      
      // intersecting at two points of circle
      else if ( cToP < r )
      {
        float triLegB = (float)Math.Sqrt( (double)( vpc.LengthSquared() - cToP * cToP ) );
        float p0toIsect = triLegB - (float)Math.Sqrt( (double)( r * r - cToP * cToP ) );
        float vpLenSq = vp.LengthSquared();
        if ( ( p0toIsect * p0toIsect ) > vpLenSq )
          return false;

        t = p0toIsect / (float)Math.Sqrt( vpLenSq );
      }

      Vector2 vpByT = Vector2.Multiply( vp, t );
      Vector2 isect = Vector2.Add( p0, vpByT );
      n = Vector2.Subtract( isect, c );
      n.Normalize();
      
      return true;
    }
  }
}