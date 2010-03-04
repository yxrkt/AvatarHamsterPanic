#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using AvatarHamsterPanic;
#endregion

namespace Menu
{
  /// <summary>
  /// The pause menu comes up over the top of the game,
  /// giving the player options to resume or quit.
  /// </summary>
  class PauseMenuScreen : MenuScreen
  {
    #region Fields and Properties

    const float entryFocusScale = 1f;
    const float entryIdleScale = .55f;

    float screenScale;

    #endregion

    
    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
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
      pauseBox.Scale = screenScale;
      MenuItems.Add( pauseBox );

      Vector2 transOnOffset  = pauseBox.TransitionOnPosition  - pauseBox.Position;
      Vector2 transOffOffset = pauseBox.TransitionOffPosition - pauseBox.Position;

      Vector2 entryPosition = pauseBoxPosition;
      Texture2D entryTexture;
      ImageMenuEntry entry;

      // Resume entry
      entryPosition += new Vector2( 0, -5 ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/resumeText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += OnCancel;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      entry.Focused = true;
      MenuItems.Add( entry );

      // Restart entry
      entryPosition += new Vector2( 0, 50 ) * screenScale;
      entryTexture = content.Load<Texture2D>( "Textures/restartText" );
      entry = new ImageMenuEntry( this, entryPosition, entryTexture, null );
      entry.TransitionOffPosition = entryPosition + transOffOffset;
      entry.TransitionOnPosition = entryPosition + transOnOffset;
      entry.Selected += RestartMenuEntrySelected;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.IdleScale = entryIdleScale * screenScale;
      MenuItems.Add( entry );

      // Exit entry
      entryPosition += new Vector2( 0, 50 ) * screenScale;
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

    /// <summary>
    /// Event handler for when the Restart menu entry is selected.
    /// </summary>
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

    /// <summary>
    /// Event handler for when the Exit menu entry is selected.
    /// </summary>
    void ExitMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      const string message = "Ack! Do you really want to quit this game?";

      MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen( message );

      confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmQuitMessageBox, ControllingPlayer );

      ScreenManager.MenuTrack.Resume();
    }


    /// <summary>
    /// Event handler for when the user selects ok on the "are you sure
    /// you want to quit" message box. This uses the loading screen to
    /// transition from the game back to the main menu screen.
    /// </summary>
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
