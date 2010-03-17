using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Menu;
using MathLibrary;
using Utilities;

namespace AvatarHamsterPanic.Objects
{
  class GameTimer : GameObject
  {
    readonly TimeSpan redClockTime = TimeSpan.FromSeconds( 10 );
    readonly Color defaultClockColor = new Color( Color.Teal, .75f );
    readonly Color panicClockColor = new Color( Color.Red, .85f );

    TimeSpan fullTime;
    TimeSpan timeLeft;

    StringBuilder clockTimeString;
    Vector2 clockPosition;
    SpringInterpolater clockScaleSpring;
    SpriteFont clockFont;

    Texture2D timeUpText;
    Vector2 timeUpPosition;
    Vector2 timeUpOrigin;
    SpringInterpolater timeUpSpring;

    SpriteBatch spriteBatch;

    float ss; // screen scale

    public GameTimer( TimeSpan fullTime )
      : base( GameplayScreen.Instance )
    {
      this.fullTime = fullTime;
      this.timeLeft = fullTime;

      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      ss = device.Viewport.Height / 1080f;

      clockTimeString = new StringBuilder( "XX:XX:XX" );
      clockPosition = new Vector2( device.Viewport.Width / 2, 50 * ss );
      clockScaleSpring = new SpringInterpolater( 1, 700, .3f * SpringInterpolater.GetCriticalDamping( 700 ) );
      clockScaleSpring.SetSource( 0f );
      clockScaleSpring.SetDest( 1f );

      clockFont = Screen.Content.Load<SpriteFont>( "Fonts/clockFont" );

      timeUpText = Screen.Content.Load<Texture2D>( "Textures/timeUpText" );

      Viewport viewport = GameCore.Instance.GraphicsDevice.Viewport;
      timeUpPosition = new Vector2( viewport.Width / 2, viewport.Height / 2 );
      timeUpOrigin = new Vector2( timeUpText.Width, timeUpText.Height ) / 2;
      timeUpSpring = new SpringInterpolater( 1, 700, .3f * SpringInterpolater.GetCriticalDamping( 700 ) );
      timeUpSpring.SetSource( 0 );
      timeUpSpring.SetDest( 1 );

      spriteBatch = Screen.ScreenManager.SpriteBatch;
    }

    public void ShowClock()
    {
      clockScaleSpring.Active = true;
    }

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      timeUpSpring.Update( elapsed );
      clockScaleSpring.Update( elapsed );

      if ( Screen.GameOver ) return;

      if ( !clockScaleSpring.Active && Screen.CountdownTime > Screen.CountdownEnd )
        clockScaleSpring.Active = true;

      if ( clockScaleSpring.Active )
      {
        timeLeft -= gameTime.ElapsedGameTime;
        if ( timeLeft < TimeSpan.Zero )
          timeLeft = TimeSpan.Zero;
      }

      clockTimeString.Clear();
      if ( timeLeft.Minutes > 0 )
      {
        clockTimeString.AppendInt( timeLeft.Minutes );
        clockTimeString.Append( ':' );
      }
      if ( timeLeft.Seconds < 10 )
        clockTimeString.AppendInt( 0 );
      clockTimeString.AppendInt( timeLeft.Seconds );
      if ( timeLeft < redClockTime )
      {
        clockTimeString.Append( ':' );
        clockTimeString.AppendInt( ( timeLeft.Milliseconds % 1000 ) / 10 );

        clockScaleSpring.SetDest( 1.3f );
      }

      if ( timeLeft == TimeSpan.Zero && !Screen.GameOver )
      {
        Screen.EndGame();
        timeUpSpring.Active = true;
      }
    }

    public override void Draw()
    {
      // see Draw2D
    }

    public void Draw2D()
    {
      // clock
      if ( clockScaleSpring.Active )
      {
        Color color = timeLeft > redClockTime ? defaultClockColor : panicClockColor;
        float scale = timeLeft > redClockTime ? 1f : 1.3f;
        Vector2 clockOrigin = clockFont.MeasureString( clockTimeString ) / 2;
        spriteBatch.DrawString( clockFont, clockTimeString, clockPosition, color, 0f,
                                clockOrigin, clockScaleSpring.GetSource()[0] * ss, SpriteEffects.None, 0f );
      }

      // time up
      if ( timeUpSpring.Active )
      {
        spriteBatch.Draw( timeUpText, timeUpPosition, null, Color.White, 0f,
                          timeUpOrigin, timeUpSpring.GetSource()[0] * ss, SpriteEffects.None, 0f );
      }
    }
  }
}