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
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// The pause menu comes up over the top of the game,
  /// giving the player options to resume or quit.
  /// </summary>
  class PauseMenuScreen : MenuScreen
  {
    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public PauseMenuScreen()
    {
    }

    public override void LoadContent()
    {
      // Flag that there is no need for the game to transition
      // off when the pause menu is on top of it.
      IsPopup = true;

      Vector2 entryPosition = new Vector2( 100f, 100f );

      // Resume game entry
      MenuEntry resumeGameMenuEntry = new MenuEntry( this, entryPosition, "Resume Game" );
      resumeGameMenuEntry.Selected += OnCancel;
      resumeGameMenuEntry.Focused = true;
      MenuItems.Add( resumeGameMenuEntry );

      entryPosition.Y += resumeGameMenuEntry.Dimensions.Y;

      // Quit game entry
      MenuEntry quitGameMenuEntry = new MenuEntry( this, entryPosition, "Quit Game" );
      quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;
      MenuItems.Add( quitGameMenuEntry );
    }


    #endregion

    #region Handle Input


    /// <summary>
    /// Event handler for when the Quit Game menu entry is selected.
    /// </summary>
    void QuitGameMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      const string message = "Are you sure you want to quit this game?";

      MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen( message );

      confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmQuitMessageBox, ControllingPlayer );
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
    }


    #endregion

    #region Draw


    /// <summary>
    /// Draws the pause menu screen. This darkens down the gameplay screen
    /// that is underneath us, and then chains to the base MenuScreen.Draw.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      //ScreenManager.FadeBackBufferToBlack( TransitionAlpha * 2 / 3 );

      base.Draw( gameTime );
    }


    #endregion
  }
}
