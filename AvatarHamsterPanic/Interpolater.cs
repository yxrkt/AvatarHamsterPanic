using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AvatarHamsterPanic
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

    public void SetSource( params float[] source )
    {
      for ( int i = 0; i < dims; ++i )
      {
        this.source[i] = source[i];
        this.prev[i] = source[i];
      }
    }

    public float[] GetDest()
    {
      return dest;
    }

    public void SetDest( params float[] dest )
    {
      for ( int i = 0; i < dims; ++i )
        this.dest[i] = dest[i];
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