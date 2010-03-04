using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Debug
{
  [Flags]
  public enum AppendNumberOptions
  {
    None = 0,
    PositiveSign = 1,
    NumberGroup = 2,
  }


  public static class StringBuilderExtensions
  {
    #region Fields

    static int[] numberGroupSizes =
        CultureInfo.CurrentCulture.NumberFormat.NumberGroupSizes;

    static char[] numberString = new char[32];

    #endregion

    public static void AppendNumber( this StringBuilder builder, int number )
    {
      AppendNumbernternal( builder, number, 0, AppendNumberOptions.None );
    }

    public static void AppendNumber( this StringBuilder builder, int number,
                                                        AppendNumberOptions options )
    {
      AppendNumbernternal( builder, number, 0, options );
    }

    public static void AppendNumber( this StringBuilder builder, float number )
    {
      AppendNumber( builder, number, 2, AppendNumberOptions.None );
    }

    public static void AppendNumber( this StringBuilder builder, float number,
                                                        AppendNumberOptions options )
    {
      AppendNumber( builder, number, 2, options );
    }

    public static void AppendNumber( this StringBuilder builder, float number,
                                    int decimalCount, AppendNumberOptions options )
    {
      if ( float.IsNaN( number ) )
      {
        builder.Append( "NaN" );
      }
      else if ( float.IsNegativeInfinity( number ) )
      {
        builder.Append( "-Infinity" );
      }
      else if ( float.IsPositiveInfinity( number ) )
      {
        builder.Append( "+Infinity" );
      }
      else
      {
        int intNumber =
                (int)( number * (float)Math.Pow( 10, decimalCount ) + 0.5f );

        AppendNumbernternal( builder, intNumber, decimalCount, options );
      }
    }


    static void AppendNumbernternal( StringBuilder builder, int number,
                                    int decimalCount, AppendNumberOptions options )
    {
      NumberFormatInfo nfi = CultureInfo.CurrentCulture.NumberFormat;

      int idx = numberString.Length;
      int decimalPos = idx - decimalCount;

      if ( decimalPos == idx )
        decimalPos = idx + 1;

      int numberGroupIdx = 0;
      int numberGroupCount = numberGroupSizes[numberGroupIdx] + decimalCount;

      bool showNumberGroup = ( options & AppendNumberOptions.NumberGroup ) != 0;
      bool showPositiveSign = ( options & AppendNumberOptions.PositiveSign ) != 0;

      bool isNegative = number < 0;
      number = Math.Abs( number );

      do
      {
        if ( idx == decimalPos )
        {
          numberString[--idx] = nfi.NumberDecimalSeparator[0];
        }

        if ( --numberGroupCount < 0 && showNumberGroup )
        {
          numberString[--idx] = nfi.NumberGroupSeparator[0];

          if ( numberGroupIdx < numberGroupSizes.Length - 1 )
            numberGroupIdx++;

          numberGroupCount = numberGroupSizes[numberGroupIdx++];
        }

        numberString[--idx] = (char)( '0' + ( number % 10 ) );
        number /= 10;

      } while ( number > 0 || decimalPos <= idx );


      if ( isNegative )
      {
        numberString[--idx] = nfi.NegativeSign[0];
      }
      else if ( showPositiveSign )
      {
        numberString[--idx] = nfi.PositiveSign[0];
      }

      builder.Append( numberString, idx, numberString.Length - idx );
    }
  }
}