using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;

namespace MathLib
{
  /// <summary>
  /// Geometry tools
  /// </summary>
  class Geometry
  {
    private Geometry() { }

    /// <summary>
    /// Tests for an intersection between two line segments.
    /// </summary>
    /// <param name="tp">The t value of collision with respect to segment p.</param>
    /// <param name="p0">The starting point of segment p.</param>
    /// <param name="p1">The ending poing of segment p.</param>
    /// <param name="q0">The starting point of segment q.</param>
    /// <param name="q1">The ending point of segment q.</param>
    /// <returns>True if there is an intersection.</returns>
    public static bool SegmentVsSegment( out float tp, Vector2 p0, Vector2 p1, Vector2 q0, Vector2 q1 )
    {
      // set default in case returning false
      tp = float.MaxValue;

      // edge vectors
      Vector2 vp = Vector2.Subtract( p1, p0 );
      Vector2 vq = Vector2.Subtract( q1, q0 );
      
      // edge normals
      Vector2 vpn = new Vector2( vp.Y, -vp.X );
      vpn.Normalize();
      Vector2 vqn = new Vector2( vq.Y, -vq.X );
      vqn.Normalize();
      
      // signed distances from q's end points to p
      Vector2 p0q0 = Vector2.Subtract( q0, p0 );
      Vector2 p0q1 = Vector2.Subtract( q1, p0 );
      
      float q0toP = Vector2.Dot( p0q0, vpn );
      float q1toP = Vector2.Dot( p0q1, vpn );
      
      // rejection - q is on one side of p or parallel to p
      if ( q0toP < 0f && q1toP < 0f ) return false;
      if ( q0toP > 0f && q1toP > 0f ) return false;
      if ( q0toP == 0f && q1toP == 0f ) return false;
      
      // signed distances from p's end points to q
      Vector2 q0p0 = Vector2.Subtract( p0, q0 );
      Vector2 q0p1 = Vector2.Subtract( p1, q0 );
      
      float p0toQ = Vector2.Dot( q0p0, vqn );
      float p1toQ = Vector2.Dot( q0p1, vqn );
      
      // rejection - p is on one side of q or parallel to q -- second time necessary for floating point error
      if ( p0toQ < 0f && p1toQ < 0f ) return false;
      if ( p0toQ > 0f && p1toQ > 0f ) return false;
      if ( p0toQ == 0f && p1toQ == 0f ) return false;
      
      tp = p0toQ / ( p0toQ - p1toQ );
      
      return true;
    }

    /// <summary>
    /// Tests for an intersection between a line segment and a circle.
    /// </summary>
    /// <param name="t">The t value of the intersection along the segment.</param>
    /// <param name="n">The normal on the circle at the point of intersection.</param>
    /// <param name="p0">The staring point of the segment p.</param>
    /// <param name="p1">The ending point of the segment p.</param>
    /// <param name="c">The center point of the circle.</param>
    /// <param name="r">The radius of the circle.</param>
    /// <returns>True if there is an intersection.</returns>
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
        float p0toIsectSq = MathHelper.Max( vpc.LengthSquared() - r * r, 0f );
        float vpLenSq     = vp.LengthSquared();
        if ( p0toIsectSq > vpLenSq )
          return false;

        t = (float)( Math.Sqrt( (double)p0toIsectSq ) / Math.Sqrt( (double)vpLenSq ) );
      }
      
      // intersecting at two points of circle
      else if ( cToP < r )
      {
        float triLegB = (float)Math.Sqrt( (double)MathHelper.Max( ( vpc.LengthSquared() - cToP * cToP ), 0f ) );
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

    /// <summary>
    /// Determines if a point is inside a convex CCW defined polygon.
    /// </summary>
    /// <param name="verts">The vertices of the polygon.</param>
    /// <param name="p">The point to be tested.</param>
    /// <returns>True if point is inside the polygon.</returns>
    public static bool PolyContains( Vector2[] verts, Vector2 p )
    {
      Vector2 u, v;

      int nVerts = verts.Length;
      Vector2 lastVert = verts[nVerts - 1];
      for ( int i = 0; i < nVerts; ++i )
      {
        Vector2 vert = verts[i];

        u = p - lastVert;
        v = vert - p;

        if ( u.X * v.Y - u.Y * v.X >= 0f )
          return false;

        lastVert = vert;
      }

      return true;
    }

    /// <summary>
    /// Determines if a CCW defined polygon is convex.
    /// </summary>
    /// <param name="verts">The vertices of the polygon.</param>
    /// <returns>True if polygon is convex.</returns>
    public static bool PolyIsConvex( Vector2[] verts )
    {
      int nVerts = verts.Length;
      Vector2 last = verts.Last();
      for ( int i = 0; i <= nVerts; ++i )
      {
        Vector2 vert = verts[( i + 1 ) % nVerts];
        Vector2 next = verts[( i + 2 ) % nVerts];

        Vector2 u = vert - last;
        Vector2 v = next - vert;

        if ( u.X * v.Y - u.Y * v.X < 0f )
          return false;
      }

      return true;
    }

    /// <summary>
    /// Gets the distance from a point to a line.
    /// </summary>
    /// <param name="p">The point being tested.</param>
    /// <param name="r0">A point on the line.</param>
    /// <param name="r1">A different point on the line.</param>
    /// <returns>The distance from the point to the line.</returns>
    public static float Distance( Vector2 p, Vector2 r0, Vector2 r1 )
    {
      Vector2 n = new Vector2( r1.Y - r0.Y, r0.X - r1.X );
      n.Normalize();
      return Math.Abs( Vector2.Dot( p - r0, n ) );
    }

    public static float PerpDot( Vector2 v1, Vector2 v2 )
    {
      return ( v1.X * v2.Y - v1.Y * v2.X );
    }

    public static Vector2 Perp( Vector2 v )
    {
      return new Vector2( v.Y, -v.X );
    }
  }
}