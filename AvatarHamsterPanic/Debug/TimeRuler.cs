#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace Debug
{
  public class TimeRuler : DrawableGameComponent
  {
    #region Constant Declarations

    const int MaxBars = 8;
    const int MaxSamples = 256;
    const int MaxNestCall = 32;
    const int MaxSampleFrames = 4;
    const int LogSnapDuration = 120;
    const int BarHeight = 8;
    const int BarPadding = 2;
    const int AutoAdjustDelay = 30;

    #endregion

    #region Properties

    public bool ShowLog { get; set; }
    public int TargetSampleFrames { get; set; }
    public Vector2 Position { get { return position; } set { position = value; } }
    public int Width { get; set; }

    #endregion

    #region Fields

#if TRACE

    private struct Marker
    {
      public int MarkerId;
      public float BeginTime;
      public float EndTime;
      public Color Color;
    }

    private class MarkerCollection
    {
      public Marker[] Markers = new Marker[MaxSamples];
      public int MarkCount;

      public int[] MarkerNests = new int[MaxNestCall];
      public int NestCount;
    }

    private class FrameLog
    {
      public MarkerCollection[] Bars;

      public FrameLog()
      {
        Bars = new MarkerCollection[MaxBars];
        for ( int i = 0; i < MaxBars; ++i )
          Bars[i] = new MarkerCollection();
      }
    }

    private class MarkerInfo
    {
      public string Name;

      public MarkerLog[] Logs = new MarkerLog[MaxBars];

      public MarkerInfo( string name )
      {
        Name = name;
      }
    }

    private struct MarkerLog
    {
      public float SnapMin;
      public float SnapMax;
      public float SnapAvg;

      public float Min;
      public float Max;
      public float Avg;

      public int Samples;

      public Color Color;

      public bool Initialized;
    }

    DebugManager debugManager;
    FrameLog[] logs;
    FrameLog prevLog;
    FrameLog curLog;
    int frameCount;
    Stopwatch stopwatch = new Stopwatch();
    List<MarkerInfo> markers = new List<MarkerInfo>();
    Dictionary<string, int> markerNameToIdMap = new Dictionary<string, int>();
    int frameAdjust;
    int sampleFrames;
    StringBuilder logString = new StringBuilder( 512 );

#endif
    Vector2 position;

    #endregion

    #region Initialization

    public TimeRuler( Game game )
      : base( game )
    {
      Game.Services.AddService( typeof( TimeRuler ), this );
    }

    public override void Initialize()
    {
#if TRACE
      debugManager =
          Game.Services.GetService( typeof( DebugManager ) ) as DebugManager;

      if ( debugManager == null )
        throw new InvalidOperationException( "DebugManager is not registered." );

      IDebugCommandHost host =
                          Game.Services.GetService( typeof( IDebugCommandHost ) )
                                                              as IDebugCommandHost;

      if ( host != null )
      {
        host.RegisterCommand( "tr", "TimeRuler", this.CommandExecute );
        this.Visible = false;
        this.Enabled = false;
      }

      logs = new FrameLog[2];
      for ( int i = 0; i < logs.Length; ++i )
        logs[i] = new FrameLog();

      sampleFrames = TargetSampleFrames = 1;
#endif
      base.Initialize();
    }

    protected override void LoadContent()
    {
      Width = (int)( GraphicsDevice.Viewport.Width * 0.8f );

      Layout layout = new Layout( GraphicsDevice.Viewport );
      position = layout.Place( new Vector2( Width, BarHeight ),
                                              0, 0.01f, Alignment.BottomCenter );

      base.LoadContent();
    }

#if TRACE
    void CommandExecute( IDebugCommandHost host, string command,
                                                            IList<string> arguments )
    {
      if ( arguments.Count == 0 )
        Visible = !Visible;

      char[] subArgSeparator = new[] { ':' };
      foreach ( string orgArg in arguments )
      {
        string arg = orgArg.ToLower();
        string[] subargs = arg.Split( subArgSeparator );
        switch ( subargs[0] )
        {
          case "on":
            Visible = true;
            break;
          case "off":
            Visible = false;
            break;
          case "reset":
            ResetLog();
            break;
          case "log":
            if ( subargs.Length > 1 )
            {
              if ( String.Compare( subargs[1], "on" ) == 0 )
                ShowLog = true;
              if ( String.Compare( subargs[1], "off" ) == 0 )
                ShowLog = false;
            }
            else
            {
              ShowLog = !ShowLog;
            }
            break;
          case "frame":
            int a = Int32.Parse( subargs[1] );
            a = Math.Max( a, 1 );
            a = Math.Min( a, MaxSampleFrames );
            TargetSampleFrames = a;
            break;
          case "/?":
          case "--help":
            host.Echo( "tr [log|on|off|reset|frame]" );
            host.Echo( "Options:" );
            host.Echo( "       on     Display TimeRuler." );
            host.Echo( "       off    Hide TimeRuler." );
            host.Echo( "       log    Show/Hide marker log." );
            host.Echo( "       reset  Reset marker log." );
            host.Echo( "       frame:sampleFrames" );
            host.Echo( "              Change target sample frame count" );
            break;
          default:
            break;
        }
      }
    }
#endif

    #endregion

    #region Measurement Methods

    [Conditional( "TRACE" )]
    public void StartFrame()
    {
#if TRACE
      lock ( this )
      {
        prevLog = logs[frameCount++ & 0x1];
        curLog = logs[frameCount & 0x1];

        float endFrameTime = (float)stopwatch.Elapsed.TotalMilliseconds;

        for ( int barIdx = 0; barIdx < prevLog.Bars.Length; ++barIdx )
        {
          MarkerCollection prevBar = prevLog.Bars[barIdx];
          MarkerCollection nextBar = curLog.Bars[barIdx];

          for ( int nest = 0; nest < prevBar.NestCount; ++nest )
          {
            int markerIdx = prevBar.MarkerNests[nest];

            prevBar.Markers[markerIdx].EndTime = endFrameTime;

            nextBar.MarkerNests[nest] = nest;
            nextBar.Markers[nest].MarkerId =
                prevBar.Markers[markerIdx].MarkerId;
            nextBar.Markers[nest].BeginTime = 0;
            nextBar.Markers[nest].EndTime = -1;
            nextBar.Markers[nest].Color = prevBar.Markers[markerIdx].Color;
          }

          for ( int markerIdx = 0; markerIdx < prevBar.MarkCount; ++markerIdx )
          {
            float duration = prevBar.Markers[markerIdx].EndTime -
                                prevBar.Markers[markerIdx].BeginTime;

            int markerId = prevBar.Markers[markerIdx].MarkerId;
            MarkerInfo m = markers[markerId];

            m.Logs[barIdx].Color = prevBar.Markers[markerIdx].Color;

            if ( !m.Logs[barIdx].Initialized )
            {
              m.Logs[barIdx].Min = duration;
              m.Logs[barIdx].Max = duration;
              m.Logs[barIdx].Avg = duration;

              m.Logs[barIdx].Initialized = true;
            }
            else
            {
              m.Logs[barIdx].Min = Math.Min( m.Logs[barIdx].Min, duration );
              m.Logs[barIdx].Max = Math.Min( m.Logs[barIdx].Max, duration );
              m.Logs[barIdx].Avg += duration;
              m.Logs[barIdx].Avg *= 0.5f;

              if ( m.Logs[barIdx].Samples++ >= LogSnapDuration )
              {
                m.Logs[barIdx].SnapMin = m.Logs[barIdx].Min;
                m.Logs[barIdx].SnapMax = m.Logs[barIdx].Max;
                m.Logs[barIdx].SnapAvg = m.Logs[barIdx].Avg;
                m.Logs[barIdx].Samples = 0;
              }
            }
          }

          nextBar.MarkCount = prevBar.NestCount;
          nextBar.NestCount = prevBar.NestCount;
        }

        stopwatch.Reset();
        stopwatch.Start();
      }
#endif
    }

    [Conditional( "TRACE" )]
    public void BeginMark( string markerName, Color color )
    {
#if TRACE
      BeginMark( 0, markerName, color );
#endif
    }

    [Conditional( "TRACE" )]
    public void BeginMark( int barIndex, string markerName, Color color )
    {
#if TRACE
      lock ( this )
      {
        if ( barIndex < 0 || barIndex >= MaxBars )
          throw new ArgumentOutOfRangeException( "barIndex" );

        MarkerCollection bar = curLog.Bars[barIndex];

        if ( bar.MarkCount >= MaxSamples )
        {
          throw new OverflowException(
              "The number of samples exceeded MaxSample. \n" +
              "Increase the value of TimeRuler.MaxSmpale or " +
              "please reduce the number of samples." );
        }

        if ( bar.NestCount >= MaxNestCall )
        {
          throw new OverflowException(
              "Exceeded the number of nesting MaxNestCall.\n" +
              "Increase the value of TimeRuler.MaxNestCall or " +
              "please reduce the number of nested calls." );
        }

        int markerId;
        if ( !markerNameToIdMap.TryGetValue( markerName, out markerId ) )
        {
          markerId = markers.Count;
          markerNameToIdMap.Add( markerName, markerId );
          markers.Add( new MarkerInfo( markerName ) );
        }

        bar.MarkerNests[bar.NestCount++] = bar.MarkCount;

        bar.Markers[bar.MarkCount].MarkerId = markerId;
        bar.Markers[bar.MarkCount].Color = color;
        bar.Markers[bar.MarkCount].BeginTime =
                                (float)stopwatch.Elapsed.TotalMilliseconds;

        bar.Markers[bar.MarkCount].EndTime = -1;

        bar.MarkCount++;
      }
#endif
    }

    [Conditional( "TRACE" )]
    public void EndMark( string markerName )
    {
#if TRACE
      EndMark( 0, markerName );
#endif
    }

    public float GetAverageTime( int barIndex, string markerName )
    {
#if TRACE
      if ( barIndex < 0 || barIndex >= MaxBars )
        throw new ArgumentOutOfRangeException( "barIndex" );

      float result = 0;
      int markerId;
      if ( markerNameToIdMap.TryGetValue( markerName, out markerId ) )
        result = markers[markerId].Logs[barIndex].Avg;

      return result;
#else
      return 0f;
#endif
    }

    [Conditional( "TRACE" )]
    public void EndMark( int barIndex, string markerName )
    {
#if TRACE
      lock ( this )
      {
        if ( barIndex < 0 || barIndex >= MaxBars )
          throw new ArgumentOutOfRangeException( "barIndex" );

        MarkerCollection bar = curLog.Bars[barIndex];

        if ( bar.NestCount <= 0 )
        {
          throw new InvalidOperationException(
              "BeginMark must be called before EndMark." );
        }

        int markerId;
        if ( !markerNameToIdMap.TryGetValue( markerName, out markerId ) )
        {
          throw new InvalidOperationException(
              String.Format( "The name {0} is not registered. Make sure " +
                  "the name used in BeginMark matches.", markerName ) );
        }

        int markerIdx = bar.MarkerNests[--bar.NestCount];
        if ( bar.Markers[markerIdx].MarkerId != markerId )
        {
          throw new InvalidOperationException(
          "BeginMark/EndMark call sequence is invalid. " +
          "Use like so: BeginMark(A), BeginMark(B), EndMark(B), EndMark(A)" );
        }

        bar.Markers[markerIdx].EndTime =
            (float)stopwatch.Elapsed.TotalMilliseconds;
      }
#endif
    }

    [Conditional( "TRACE" )]
    public void ResetLog()
    {
#if TRACE
      lock ( this )
      {
        foreach ( MarkerInfo markerInfo in markers )
        {
          for ( int i = 0; i < markerInfo.Logs.Length; ++i )
          {
            markerInfo.Logs[i].Initialized = false;
            markerInfo.Logs[i].SnapMin = 0;
            markerInfo.Logs[i].SnapMax = 0;
            markerInfo.Logs[i].SnapAvg = 0;

            markerInfo.Logs[i].Min = 0;
            markerInfo.Logs[i].Max = 0;
            markerInfo.Logs[i].Avg = 0;

            markerInfo.Logs[i].Samples = 0;
          }
        }
      }
#endif
    }

    #endregion

    #region Draw

    public override void Draw( GameTime gameTime )
    {
      Draw( position, Width );
      base.Draw( gameTime );
    }

    [Conditional( "TRACE" )]
    public void Draw( Vector2 position, int width )
    {
#if TRACE
      SpriteBatch spriteBatch = debugManager.SpriteBatch;
      SpriteFont font = debugManager.DebugFont;
      Texture2D texture = debugManager.WhiteTexture;

      int height = 0;
      float maxTime = 0;
      foreach ( MarkerCollection bar in prevLog.Bars )
      {
        if ( bar.MarkCount > 0 )
        {
          height += BarHeight + BarPadding * 2;
          maxTime = Math.Max( maxTime,
                                  bar.Markers[bar.MarkCount - 1].EndTime );
        }
      }

      const float frameSpan = 1.0f / 60.0f * 1000f;
      float sampleSpan = (float)sampleFrames * frameSpan;

      if ( maxTime > sampleSpan )
        frameAdjust = Math.Max( 0, frameAdjust ) + 1;
      else
        frameAdjust = Math.Min( 0, frameAdjust ) - 1;

      if ( Math.Abs( frameAdjust ) > AutoAdjustDelay )
      {
        sampleFrames = Math.Min( MaxSampleFrames, sampleFrames );
        sampleFrames =
            Math.Max( TargetSampleFrames, (int)( maxTime / frameSpan ) + 1 );

        frameAdjust = 0;
      }

      float msToPs = (float)width / sampleSpan;

      int startY = (int)position.Y - ( height - BarHeight );

      int y = startY;

      spriteBatch.Begin();

      Rectangle rc = new Rectangle( (int)position.X, y, width, height );
      spriteBatch.Draw( texture, rc, new Color( 0, 0, 0, 128 ) );

      rc.Height = BarHeight;
      foreach ( MarkerCollection bar in prevLog.Bars )
      {
        rc.Y = y + BarPadding;
        if ( bar.MarkCount > 0 )
        {
          for ( int j = 0; j < bar.MarkCount; ++j )
          {
            float bt = bar.Markers[j].BeginTime;
            float et = bar.Markers[j].EndTime;
            int sx = (int)( position.X + bt * msToPs );
            int ex = (int)( position.X + et * msToPs );
            rc.X = sx;
            rc.Width = Math.Max( ex - sx, 1 );

            spriteBatch.Draw( texture, rc, bar.Markers[j].Color );
          }
        }

        y += BarHeight + BarPadding;
      }

      rc = new Rectangle( (int)position.X, (int)startY, 1, height );
      for ( float t = 1.0f; t < sampleSpan; t += 1.0f )
      {
        rc.X = (int)( position.X + t * msToPs );
        spriteBatch.Draw( texture, rc, Color.Gray );
      }

      for ( int i = 0; i <= sampleFrames; ++i )
      {
        rc.X = (int)( position.X + frameSpan * (float)i * msToPs );
        spriteBatch.Draw( texture, rc, Color.White );
      }

      if ( ShowLog )
      {
        y = startY - font.LineSpacing;
        logString.Length = 0;
        foreach ( MarkerInfo markerInfo in markers )
        {
          for ( int i = 0; i < MaxBars; ++i )
          {
            if ( markerInfo.Logs[i].Initialized )
            {
              if ( logString.Length > 0 )
                logString.Append( "\n" );

              logString.Append( " Bar " );
              logString.AppendNumber( i );
              logString.Append( " " );
              logString.Append( markerInfo.Name );

              logString.Append( " Avg.:" );
              logString.AppendNumber( markerInfo.Logs[i].SnapAvg );
              logString.Append( "ms " );

              y -= font.LineSpacing;
            }
          }
        }

        Vector2 size = font.MeasureString( logString );
        rc = new Rectangle( (int)position.X, (int)y, (int)size.X + 12, (int)size.Y );
        spriteBatch.Draw( texture, rc, new Color( 0, 0, 0, 128 ) );

        spriteBatch.DrawString( font, logString,
                                new Vector2( position.X + 12, y ), Color.White );


        // Draw log color boxes.
        y += (int)( (float)font.LineSpacing * 0.3f );
        rc = new Rectangle( (int)position.X + 4, y, 10, 10 );
        Rectangle rc2 = new Rectangle( (int)position.X + 5, y + 1, 8, 8 );
        foreach ( MarkerInfo markerInfo in markers )
        {
          for ( int i = 0; i < MaxBars; ++i )
          {
            if ( markerInfo.Logs[i].Initialized )
            {
              rc.Y = y;
              rc2.Y = y + 1;
              spriteBatch.Draw( texture, rc, Color.White );
              spriteBatch.Draw( texture, rc2, markerInfo.Logs[i].Color );

              y += font.LineSpacing;
            }
          }
        }


      }

      spriteBatch.End();
#endif
    }

    #endregion
  }
}
