using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MathLib
{
  class RandomBag
  {
    static List<int> s_values = new List<int>( 100 );
    static Random s_rand = new Random();

    private static int s_nItems = 0;

    private RandomBag() { }

    public static int LastFillSize { get { return s_nItems; } }
    public static int ItemsRemaining { get { return s_values.Count; } }

    public static void Seed( int seed )
    {
      s_rand = new Random( seed );
    }

    public static void Reset( int nItems )
    {
      s_values.Clear();
      if ( s_nItems < nItems )
        s_values.Capacity = nItems;
      s_nItems = nItems;

      for ( int i = 0; i < nItems; ++i )
        s_values.Add( i );
    }

    public static int PullNext()
    {
      int nValues = s_values.Count;
      if ( nValues != 0 )
      {
        int value = s_values[s_rand.Next( s_values.Count )];
        s_values.Remove( value );
        return value;
      }

      return -1;
    }
  }
}