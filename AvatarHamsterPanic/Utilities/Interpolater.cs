using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MathLibrary
{
  abstract class Interpolater
  {
    protected int dims;
    protected float[] prev;
    protected float[] source;
    protected float[] dest;

    public bool Active { get; set; }

    public Interpolater( int dims )
    {
      this.dims = dims;
      prev   = new float[dims];
      source = new float[dims];
      dest   = new float[dims];
    }

    public float[] GetSource()
    {
      return source;
    }

    public void SetSource( float source )
    {
      this.source[0] = source;
      this.prev[0] = source;
    }

    public void SetSource( Vector2 source )
    {
      this.source[0] = source.X;
      this.source[1] = source.Y;
      this.prev[0] = source.X;
      this.prev[1] = source.Y;
    }

    public void SetSource( Vector3 source )
    {
      this.source[0] = source.X;
      this.source[1] = source.Y;
      this.source[2] = source.Z;
      this.prev[0] = source.X;
      this.prev[1] = source.Y;
      this.prev[2] = source.Z;
    }

    public void SetSource( Vector4 source )
    {
      this.source[0] = source.X;
      this.source[1] = source.Y;
      this.source[2] = source.Z;
      this.source[3] = source.W;
      this.prev[0] = source.X;
      this.prev[1] = source.Y;
      this.prev[2] = source.Z;
      this.prev[3] = source.W;
    }

    public float[] GetDest()
    {
      return dest;
    }

    public void SetDest( float dest )
    {
      this.dest[0] = dest;
    }

    public void SetDest( Vector2 dest )
    {
      this.dest[0] = dest.X;
      this.dest[1] = dest.Y;
    }

    public void SetDest( Vector3 dest )
    {
      this.dest[0] = dest.X;
      this.dest[1] = dest.Y;
      this.dest[2] = dest.Z;
    }

    public void SetDest( Vector4 dest )
    {
      this.dest[0] = dest.X;
      this.dest[1] = dest.Y;
      this.dest[2] = dest.Z;
      this.dest[3] = dest.W;
    }

    public abstract void Update( float elapsed );
  }

  class SpringInterpolater : Interpolater
  {
    public float K { get; set; }
    public float B { get; set; }

    public static float GetCriticalDamping( float k )
    {
      return -(float)Math.Sqrt( 4f * k );
    }

    public SpringInterpolater( int dims, float k, float b )
      : base( dims )
    {
      K = k;
      B = b;
    }

    public override void Update( float elapsed )
    {
      if ( Active && elapsed != 0f )
      {
        for ( int i = 0; i < dims; ++i )
        {
          float vel = ( source[i] - prev[i] ) / elapsed;
          float dist = dest[i] - source[i];
          float force = K * dist + vel * B;
          vel += ( force * elapsed );

          prev[i] = source[i];
          source[i] += ( vel * elapsed );
        }
      }
    }
  }
}