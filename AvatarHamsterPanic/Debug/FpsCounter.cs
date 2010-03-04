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
  public class FpsCounter : DrawableGameComponent
  {
    #region Properties

    public float Fps { get; private set; }
    public TimeSpan SampleSpan { get; set; }

    #endregion

    #region Fields

    private DebugManager debugManager;
    private Stopwatch stopwatch;
    private int sampleFrames;
    private StringBuilder stringBuilder = new StringBuilder( 16 );

    #endregion

    #region Initialization

    public FpsCounter( Game game )
      : base( game )
    {
      SampleSpan = TimeSpan.FromSeconds( 1 );
    }

    public override void Initialize()
    {
      debugManager =
          Game.Services.GetService( typeof( DebugManager ) ) as DebugManager;

      if ( debugManager == null )
        throw new InvalidOperationException( "DebugManager is not registered." );

      IDebugCommandHost host =
                          Game.Services.GetService( typeof( IDebugCommandHost ) )
                                                          as IDebugCommandHost;

      if ( host != null )
      {
        host.RegisterCommand( "fps", "FPS Counter", this.CommandExecute );
        Visible = false;
      }

      Fps = 0;
      sampleFrames = 0;
      stopwatch = Stopwatch.StartNew();
      stringBuilder.Length = 0;

      base.Initialize();
    }

    #endregion

    private void CommandExecute( IDebugCommandHost host,
                                string command, IList<string> arguments )
    {
      if ( arguments.Count == 0 )
        Visible = !Visible;

      foreach ( string arg in arguments )
      {
        switch ( arg.ToLower() )
        {
          case "on":
            Visible = true;
            break;
          case "off":
            Visible = false;
            break;
        }
      }
    }

    #region Update and Draw

    public override void Update( GameTime gameTime )
    {
      if ( stopwatch.Elapsed > SampleSpan )
      {
        Fps = (float)sampleFrames / (float)stopwatch.Elapsed.TotalSeconds;

        stopwatch.Reset();
        stopwatch.Start();
        sampleFrames = 0;

        stringBuilder.Length = 0;
        stringBuilder.Append( "FPS: " );
        stringBuilder.AppendNumber( Fps );
      }
    }

    public override void Draw( GameTime gameTime )
    {
      sampleFrames++;

      SpriteBatch spriteBatch = debugManager.SpriteBatch;
      SpriteFont font = debugManager.DebugFont;

      Vector2 size = font.MeasureString( "X" );
      Rectangle rc =
          new Rectangle( 0, 0, (int)( size.X * 14f ), (int)( size.Y * 1.3f ) );

      Layout layout = new Layout( spriteBatch.GraphicsDevice.Viewport );
      rc = layout.Place( rc, 0.01f, 0.01f, Alignment.TopLeft );

      size = font.MeasureString( stringBuilder );
      layout.ClientArea = rc;
      Vector2 pos = layout.Place( size, 0, 0.1f, Alignment.Center );

      spriteBatch.Begin();
      spriteBatch.Draw( debugManager.WhiteTexture, rc, new Color( 0, 0, 0, 128 ) );
      spriteBatch.DrawString( font, stringBuilder, pos, Color.White );
      spriteBatch.End();

      base.Draw( gameTime );
    }

    #endregion
  }
}
