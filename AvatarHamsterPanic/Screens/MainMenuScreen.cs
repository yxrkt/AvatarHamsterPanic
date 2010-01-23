#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// The main menu screen is the first thing displayed when the game starts up.
  /// </summary>
  class MainMenuScreen : MenuScreen
  {
    #region Initialization


    /// <summary>
    /// Constructor fills in the menu contents.
    /// </summary>
    public MainMenuScreen()
    {

    }

    public override void LoadContent()
    {
      // Create menu title
      string menuTitle = "Avatar Hamster Panic";
      SpriteFont font = ScreenManager.Font;
      MenuText mainMenuTitle = new MenuText( this, new Vector2( 426, 80 ), menuTitle, font );
      mainMenuTitle.Origin = font.MeasureString( menuTitle ) / 2f;
      mainMenuTitle.Color = new Color( 192, 192, 192, 255 );
      mainMenuTitle.Scale = 1.25f;
      mainMenuTitle.TransitionOnPosition = mainMenuTitle.Position + new Vector2( 0f, -100f );
      mainMenuTitle.TransitionOffPosition = mainMenuTitle.TransitionOnPosition;
      MenuItems.Add( mainMenuTitle );

      // Create our menu entries.
      string[] entryStrings = { "Play", "Leaderboard", "Options", "Credits", "Exit" };
      EventHandler<PlayerIndexEventArgs>[] entryEvents = 
      {
        PlayMenuEntrySelected,
        LeaderboardMenuEntrySelected,
        OptionsMenuEntrySelected,
        CreditsMenuEntrySelected,
        OnCancel,
      };

      Vector2 entryPosition = new Vector2( 100f, 150f );
      int nEntries = entryStrings.Length;
      for ( int i = 0; i < nEntries; ++i )
      {
        MenuEntry entry = new MenuEntry( this, entryPosition, entryStrings[i] );
        entry.Selected += entryEvents[i];
        entry.TransitionOnPosition = entryPosition + new Vector2( -256f, 0f );
        entry.TransitionOffPosition = entryPosition + new Vector2( 512f, 0f );
        MenuItems.Add( entry );
        entryPosition.Y += entry.Dimensions.Y;
      }
      MenuEntries[0].Focused = true;
    }


    #endregion

    #region Handle Input


    /// <summary>
    /// Event handler for when the Play menu entry is selected.
    /// </summary>
    void PlayMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      /*/
      LoadingScreen.Load( ScreenManager, true, e.PlayerIndex,
                          new GameplayScreen() );
      /*/
      ScreenManager.AddScreen( new SignInMenuScreen(), e.PlayerIndex );
      /**/
    }

    /// <summary>
    /// Event handler for when the Leaderboard menu entry is selected.
    /// </summary>
    void LeaderboardMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new LeaderboardMenuScreen(), e.PlayerIndex );
    }

    /// <summary>
    /// Event handler for when the Options menu entry is selected.
    /// </summary>
    void OptionsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new OptionsMenuScreen(), e.PlayerIndex );
    }

    /// <summary>
    /// Event handler for when the Credits menu entry is selected.
    /// </summary>
    void CreditsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new CreditsMenuScreen(), e.PlayerIndex );
    }


    /// <summary>
    /// When the user cancels the main menu, ask if they want to exit the sample.
    /// </summary>
    protected override void OnCancel( PlayerIndex playerIndex )
    {
      const string message = "ZOMG! Do you really want to exit Avatar Hamster Panic?";

      MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen( message );

      confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmExitMessageBox, playerIndex );
    }


    /// <summary>
    /// Event handler for when the user selects ok on the "are you sure
    /// you want to exit" message box.
    /// </summary>
    void ConfirmExitMessageBoxAccepted( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.Game.Exit();
    }


    #endregion
  }
}
