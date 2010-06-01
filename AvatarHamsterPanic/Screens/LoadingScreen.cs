#region File Description
//-----------------------------------------------------------------------------
// LoadingScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using AvatarHamsterPanic;
using AvatarHamsterPanic.Utilities;
using CustomModelSample;
#endregion

namespace Menu
{
  class LoadingScreen : GameScreen
  {
    #region Fields

    bool loadingIsSlow;
    bool otherScreensAreGone;
    bool doneLoading;

    GameScreen[] screensToLoad;

    Thread backgroundThread;
    EventWaitHandle backgroundThreadExit;

    GraphicsDevice graphicsDevice;

    GameTime loadStartTime;
    TimeSpan loadAnimationTimer;

    Texture2D[] howToPlay;
    Texture2D loadingText;
    Texture2D pressStartText;
    CustomModel hamsterBall;
    Vector2 textPosition;
    float howToPlayScale;
    Vector2 howToPlayPosition;
    Vector2 howToPlayOrigin;
    float screenScale;

    #endregion

    #region Initialization


    private LoadingScreen( ScreenManager screenManager, bool loadingIsSlow,
                           GameScreen[] screensToLoad )
    {
      this.loadingIsSlow = loadingIsSlow;
      this.screensToLoad = screensToLoad;
      doneLoading = false;

      TransitionOnTime = TimeSpan.FromSeconds( 0.5 );

      if ( loadingIsSlow )
      {
        backgroundThread = new Thread( BackgroundWorkerThread );
        backgroundThreadExit = new ManualResetEvent( false );

        graphicsDevice = screenManager.GraphicsDevice;

        screenScale = graphicsDevice.Viewport.Height / 1080f;

        ContentManager content = GameCore.Instance.Content;

        howToPlay = new Texture2D[3];
        for ( int i = 0; i < 3; ++i )
          howToPlay[i] = content.Load<Texture2D>( "Textures/howtoplay" + ( i + 1 ).ToString() );

        loadingText = content.Load<Texture2D>( "Textures/loadingText" );
        pressStartText = content.Load<Texture2D>( "Textures/pressStartText" );
        hamsterBall = content.Load<CustomModel>( "Models/hamsterBall" );
        foreach ( CustomModel.ModelPart part in hamsterBall.ModelParts )
        {
          part.Effect.CurrentTechnique = part.Effect.Techniques["DiffuseColor"];
          part.EffectParamColor.SetValue( new Vector4( .8f, .7f, 1f, .5f ) );
          part.Effect.Parameters["SpecularPower"].SetValue( 400 );
        }
        Viewport viewport = screenManager.GraphicsDevice.Viewport;
        Vector2 viewportSize = new Vector2( viewport.Width, viewport.Height );
        textPosition = new Vector2( viewportSize.X / 2, .93f * viewportSize.Y );
        howToPlayScale = viewportSize.Y / 1080f;
        howToPlayPosition = viewportSize / 2;
        howToPlayOrigin = new Vector2( howToPlay[0].Width, howToPlay[0].Height ) / 2;
      }
    }


    public static void Load( ScreenManager screenManager, bool loadingIsSlow,
                             PlayerIndex? controllingPlayer,
                             params GameScreen[] screensToLoad )
    {
      foreach ( GameScreen screen in screenManager.GetScreens() )
        screen.ExitScreen();

      LoadingScreen loadingScreen = new LoadingScreen( screenManager,
                                                       loadingIsSlow,
                                                       screensToLoad );

      screenManager.AddScreen( loadingScreen, controllingPlayer );
    }


    #endregion

    #region Update and Draw


    public override void Update( GameTime gameTime, bool otherScreenHasFocus,
                                                   bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      if ( otherScreensAreGone )
      {
        if ( backgroundThread != null )
        {
          loadStartTime = gameTime;
          backgroundThread.Start();
        }

        ScreenManager.RemoveScreen( this );

        foreach ( GameScreen screen in screensToLoad )
        {
          if ( screen != null )
          {
            ScreenManager.AddScreen( screen, ControllingPlayer );
          }
        }

        if ( backgroundThread != null )
        {
          doneLoading = true;
          InputState input = new InputState();
          PlayerIndex actingPlayer;

          do {
            input.Update();
          } while ( !input.IsNewButtonPress( Buttons.Start, null, out actingPlayer ) &&
                    !input.IsNewButtonPress( Buttons.A, null, out actingPlayer ) );

          backgroundThreadExit.Set();
          backgroundThread.Join();

          if ( GameplayScreen.Instance.BackgroundMusic == null )
          {
            ScreenManager.MenuTrack.Pause();
            GameplayScreen.Instance.BackgroundMusic = GameCore.Instance.AudioManager.Play2DCue( "banjoBreakdown",
                                                                        GameCore.Instance.MusicVolume );
            GameplayScreen.Instance.BackgroundMusic.Pause();
            GameCore.Instance.MusicVolumeChanged += GameplayScreen.Instance.ChangeMusicVolume;
          }
        }

        ScreenManager.Game.ResetElapsedTime();
      }
    }

    public override void Draw( GameTime gameTime )
    {
      if ( ( ScreenState == ScreenState.Active ) &&
          ( ScreenManager.GetScreens().Length == 1 ) )
      {
        otherScreensAreGone = true;
      }

      if ( loadingIsSlow )
      {
        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

        loadAnimationTimer += gameTime.ElapsedGameTime;

        Color color = new Color( Color.White, TransitionAlpha );

        spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );


        // how to play
        float totalTime = (float)loadAnimationTimer.TotalSeconds;
        int floor = (int)totalTime;

        float mod = totalTime % 4f;
        if ( mod <= 3.5f )
        {
          int screen = (int)( totalTime / 4f ) % 3;
          spriteBatch.Draw( howToPlay[screen], howToPlayPosition, null, color, 0f,
                            howToPlayOrigin, howToPlayScale, SpriteEffects.None, 0f );
        }
        else
        {
          int screen1 = (int)( totalTime / 4f ) % 3;
          int screen2 = ( screen1 + 1 ) % 3;

          float screen2alpha = 2f * ( mod - 3.5f );
          float screen1alpha = 1 - screen2alpha;

          Color whiteWithAlpha = Color.White;

          whiteWithAlpha.A = (byte)( screen1alpha * TransitionAlpha );
          spriteBatch.Draw( howToPlay[screen1], howToPlayPosition, null, whiteWithAlpha, 0f,
                            howToPlayOrigin, howToPlayScale, SpriteEffects.None, 0f );

          whiteWithAlpha.A = (byte)( screen2alpha * TransitionAlpha );
          spriteBatch.Draw( howToPlay[screen2], howToPlayPosition, null, whiteWithAlpha, 0f,
                            howToPlayOrigin, howToPlayScale, SpriteEffects.None, 0f );
        }
        // end how to play

        if ( !doneLoading )
        {
          Vector2 origin = new Vector2( loadingText.Width, loadingText.Height ) / 2;
          spriteBatch.Draw( loadingText, textPosition, null, color,
                            0f, origin, screenScale, SpriteEffects.None, 0f );
        }
        else
        {
          color.A = (byte)( 255f * ( .5f + .5f * Math.Sin( 3 * loadAnimationTimer.TotalSeconds ) ) + .5f );
          Vector2 origin = new Vector2( pressStartText.Width, pressStartText.Height ) / 2;
          spriteBatch.Draw( pressStartText, textPosition, null, color, 
                            0f, origin, screenScale, SpriteEffects.None, 0f );
        }
        spriteBatch.End();

        if ( !doneLoading )
        {
          Vector3 eye = new Vector3( 0, 0, 5 );
          Matrix view = Matrix.CreateLookAt( eye, Vector3.Zero, Vector3.Up );
          float aspect = ScreenManager.GraphicsDevice.Viewport.AspectRatio;
          Matrix projection = Matrix.CreatePerspectiveFieldOfView( MathHelper.PiOver4, aspect, 1, 100 );

          Matrix world = Matrix.CreateScale( .145f );
          world *= Matrix.CreateRotationZ( -.5f * (float)loadAnimationTimer.TotalSeconds * MathHelper.TwoPi );
          world *= Matrix.CreateRotationX( -.4f );
          world *= Matrix.CreateTranslation( -.325f, -1.78f, 0 );

          graphicsDevice.RenderState.AlphaBlendEnable = true;
          graphicsDevice.RenderState.DepthBufferEnable = true;
          graphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
          graphicsDevice.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;

          graphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
          hamsterBall.Draw( eye, world, view, projection );

          graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
          hamsterBall.Draw( eye, world, view, projection );
        }
      }
    }


    #endregion

    #region Background Thread


    void BackgroundWorkerThread()
    {
      long lastTime = Stopwatch.GetTimestamp();

      while ( !backgroundThreadExit.WaitOne( 1000 / 30, false ) )
      {
        GameTime gameTime = GetGameTime( ref lastTime );

        DrawLoadAnimation( gameTime );
      }
    }


    GameTime GetGameTime( ref long lastTime )
    {
      long currentTime = Stopwatch.GetTimestamp();
      long elapsedTicks = currentTime - lastTime;
      lastTime = currentTime;

      TimeSpan elapsedTime = TimeSpan.FromTicks( elapsedTicks *
                                                TimeSpan.TicksPerSecond /
                                                Stopwatch.Frequency );

      return new GameTime( loadStartTime.TotalRealTime + elapsedTime, elapsedTime,
                          loadStartTime.TotalGameTime + elapsedTime, elapsedTime );
    }


    void DrawLoadAnimation( GameTime gameTime )
    {
      if ( ( graphicsDevice == null ) || graphicsDevice.IsDisposed )
        return;

      try
      {
        graphicsDevice.Clear( Color.Black );

        Draw( gameTime );

        graphicsDevice.Present();
      }
      catch
      {
        graphicsDevice = null;
      }
    }

    #endregion
  }
}
