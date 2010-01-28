using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AvatarHamsterPanic
{
  public static class Performance
  {
    static int frameRate;
    static int frameCounter;
    static TimeSpan elapsedTime;

    public static int FrameRate { get { return frameRate; } }

    public static void Update( TimeSpan elapsedTime )
    {
      Performance.elapsedTime += elapsedTime;

      TimeSpan oneSecond = TimeSpan.FromSeconds( 1 );
      if ( Performance.elapsedTime > oneSecond )
      {
        Performance.elapsedTime -= oneSecond;
        frameRate = frameCounter;
        frameCounter = 0;
      }
    }

    public static void CountFrame()
    {
      frameCounter++;
    }
  }
}