#region Using Statements
using Microsoft.Xna.Framework;
using System;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using AvatarHamsterPanic;
using Microsoft.Xna.Framework.GamerServices;
#endregion

namespace Menu
{
  class PauseMenuScreen : MenuScreen
  {
    #region Fields and Properties

    const float entryFocusScale = 1f;
    const float entryIdleScale = .55f;

    float screenScale;
    OptionsMenuScreen optionsScreen;

    #endregion
    
    #region Initialization


    public PauseMenuScreen( ScreenManager screenManager )
    {
      IsPopup = true;
      TransitionOnTime = TimeSpan.FromSeconds( .125 );
      TransitionOffTime = TimeSpan.FromSeconds( .125 );

      ScreenManager = screenManager;
      ContentManager content = screenManager.Game.Content;
      GraphicsDevice device = screenManager.GraphicsDevice;
      screenScale = (float)device.Viewport.Height / 1080f;

      // Background box
      Texture2D pauseBoxTexture = content.Load<Texture2D>( "Textures/pauseBox" );
      Vector2 pauseBoxPosition = new Vector2( device.Viewport.Width / 2, device.Viewport.Height / 2 );
      StaticImageMenuItem pauseBox = new StaticImageMenuItem( this, pauseBoxPosition, pauseBoxTexture );
      pauseBox.TransitionOffPosition = pauseBoxPosition;
      pauseBox.TransitionOnPosition = pauseBoxPosition;
      pauseBox.SetImmediateScale( screenScale * 1.25f );
      MenuItems.Add( pauseBox );

      Vector2 transOnOffset  = pauseBox.TransitionOnPosition  - pauseBox.Position;
      Vector2 transOffOffset = pauseBox.TransitionOffPosition - pauseBox.Position;

      Vector2 entryPosition = pauseBoxPosition;
      Texture2D entryTexture;
      ImageMenuEntry entry;

      float entrySpacing = 45f;

      // Resume entry
      entryPosition += new Vector2( 0, -20 ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/resumeText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += OnCancel;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      entry.Focused = true;
      MenuItems.Add( entry );

      // Options entry
      optionsScreen = new OptionsMenuScreen( ScreenManager );
      //optionsScreen.IsPopup = true;
      entryPosition += new Vector2( 0, entrySpacing ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/optionsText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += OptionsMenuEntrySelected;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      MenuItems.Add( entry );

      // Buy entry
      if ( Guide.IsTrialMode )
      {
        entryPosition += new Vector2( 0, entrySpacing ) * screenScale;
        entryTexture = content.Load<Texture2D>( "Textures/buyText" );
        entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
        entry.TransitionOffPosition = entryPosition + transOffOffset;
        entry.TransitionOnPosition = entryPosition + transOnOffset;
        entry.Selected += GameCore.Instance.ShowBuy;
        entry.FocusScale = entryFocusScale * screenScale;
        entry.IdleScale = entryIdleScale * screenScale;
        MenuItems.Add( entry );
      }

      // Restart entry
      entryPosition += new Vector2( 0, entrySpacing ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/restartText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += RestartMenuEntrySelected;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      MenuItems.Add( entry );

      // Exit entry
      entryPosition += new Vector2( 0, entrySpacing ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/exitText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += ExitMenuEntrySelected;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      MenuItems.Add( entry );
    }

    public override void LoadContent()
    {
      // content loaded in ctor
      for ( int i = 0; i < ImageMenuEntries.Count; ++i )
      {
        ImageMenuEntries[i].ScaleSpring.SetSource( 0 );
        ImageMenuEntries[i].ScaleSpring.Active = false;
        ImageMenuEntries[i].Focused = false;
      }

      ImageMenuEntries[0].ScaleSpring.Active = true;
      ImageMenuEntries[0].Focused = true;
      selectedEntry = 0;

      GameplayScreen.Instance.BackgroundMusic.SetVariable( "Volume", XACTHelper.GetDecibels( .8f ) );
    }

    public override void UnloadContent()
    {
      GameplayScreen.Instance.BackgroundMusic.SetVariable( "Volume", XACTHelper.GetDecibels( 1f ) );
    }


    #endregion

    #region Update

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      if ( ScreenState == ScreenState.TransitionOn )
      {
        float t = 1 - TransitionPosition;
        if ( t > .25f )
          ImageMenuEntries[2].ScaleSpring.Active = true;
        if ( t > .5f )
          ImageMenuEntries[1].ScaleSpring.Active = true;
      }
      else if ( ScreenState == ScreenState.Active )
      {
        for ( int i = 0; i < ImageMenuEntries.Count; ++i )
          ImageMenuEntries[i].ScaleSpring.Active = true;
      }

      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );
    }

    #endregion

    #region Handle Input

    void OptionsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( optionsScreen, null );
    }

    void RestartMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      const string message = "Ack! Do you really want to quit this game?";

      MessageBoxScreen confirmRestartMessageBox = new MessageBoxScreen( message );

      confirmRestartMessageBox.Accepted += ConfirmRestartMessageBoxAccepted;

      ScreenManager.AddScreen( confirmRestartMessageBox, ControllingPlayer );
    }

    protected override void OnCancel( PlayerIndex playerIndex )
    {
      GameCore.Instance.AudioManager.Play2DCue( "unpause", 1f );
      ExitScreen();
    }

    void ConfirmRestartMessageBoxAccepted( object sender, PlayerIndexEventArgs e )
    {
      Slot[] slots = GameplayScreen.Instance.Slots;
      LoadingScreen.Load( ScreenManager, true, null, new GameplayScreen( slots ) );
    }

    void ExitMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      const string message = "Ack! Do you really want to quit this game?";

      MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen( message );

      confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmQuitMessageBox, ControllingPlayer );

      ScreenManager.MenuTrack.Resume();
    }

    void ConfirmQuitMessageBoxAccepted( object sender, PlayerIndexEventArgs e )
    {
      LoadingScreen.Load( ScreenManager, false, null, new BackgroundScreen(),
                                                      new MainMenuScreen() );
      ScreenManager.MenuTrack.Resume();
    }


    #endregion

    #region Draw


    /// <summary>
    /// Draws the pause menu screen. This darkens down the gameplay screen
    /// that is underneath us, and then chains to the base MenuScreen.Draw.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      ScreenManager.FadeBackBufferToBlack( TransitionAlpha * 2 / 3 );

      base.Draw( gameTime );
    }


    #endregion
  }
}
