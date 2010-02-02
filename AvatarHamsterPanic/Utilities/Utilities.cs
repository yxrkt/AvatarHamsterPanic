using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Utilities
{
  public static class RandomExtension
  {
    public static Vector3 NextVector3( this Random rand )
    {
      return new Vector3( (float)rand.NextDouble() - .5f,
                          (float)rand.NextDouble() - .5f,
                          (float)rand.NextDouble() - .5f );
    }

    public static float NextFloat( this Random rand, float min, float max )
    {
      return MathHelper.Lerp( min, max, (float)rand.NextDouble() );
    }

    public static Vector3 NextConeDirection( this Random rand, Vector3 axis, float angle )
    {
      Vector3 randomDirection = rand.NextVector3();

      Vector3 radiusDirection = Vector3.Normalize( Vector3.Cross( axis, randomDirection ) );
      float radius = (float)Math.Tan( angle / 2 );
      return ( axis + radiusDirection * radius * (float)rand.NextDouble() );
    }
  }

  public static class StringBuilderExtention
  {
    public static StringBuilder AppendInt( this StringBuilder builder, int n )
    {
      if ( n < 0 )
        builder.Append( '-' );

      int index = builder.Length;
      do
      {
        builder.Insert( index, digits, n % 10 + 9, 1 );
        n /= 10;
      } while ( n != 0 );

      return builder;
    }

    public static StringBuilder Clear( this StringBuilder builder )
    {
      return builder.Remove( 0, builder.Length );
    }

    private static readonly char[] digits = new char[]
    {
      '9', '8', '7', '6', '5', '4', '3', '2', '1', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };
  }
}
