using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

public static class ColorHelper
{
  public static Color ColorFromUintRgba( uint color )
  {
    return new Color( (byte)( ( color >> 24 ) & 0xFF ),
                      (byte)( ( color >> 16 ) & 0xFF ),
                      (byte)( ( color >> 8  ) & 0xFF ),
                      (byte)( ( color >> 0  ) & 0xFF ) );
  }

  public static Color ColorFromUintRgb( uint color )
  {
    return new Color( (byte)( ( color >> 16 ) & 0xFF ),
                      (byte)( ( color >> 8  ) & 0xFF ),
                      (byte)( ( color >> 0  ) & 0xFF ) );
  }
}

public static class PlayerIndexHelper
{
  public static bool IsHuman( this PlayerIndex playerIndex )
  {
    return ( playerIndex >= PlayerIndex.One && playerIndex <= PlayerIndex.Four );
  }

  public static bool IsPlayer( this PlayerIndex playerIndex )
  {
    return ( playerIndex >= (PlayerIndex)(-1) && playerIndex <= PlayerIndex.Four );
  }
}

public class DescendingComparer<T> : IComparer<T> where T : IComparable
{
  public int Compare( T x, T y )
  {
    return y.CompareTo( x );
  }
}

public static class XACTHelper
{
  public static float GetDecibels( float volume )
  {
    return MathHelper.Lerp( -96, 6, volume );
  }

  public static float GetLogDecibels( float volume )
  {
    return MathHelper.Clamp( 10f * (float)Math.Log10( volume ), -96, 6 );
  }
}
