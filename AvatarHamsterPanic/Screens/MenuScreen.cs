#region File Description
//-----------------------------------------------------------------------------
// MenuScreen.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic.Objects;
using System.Collections.ObjectModel;
#endregion

namespace Menu
{
  /// <summary>
  /// Base class for screens that contain a menu of options. The user can
  /// move up and down to select an entry, or cancel to back out of the screen.
  /// </summary>
  abstract class MenuScreen : GameScreen
  {
    #region Fields

    ObjectTable<MenuItem> menuItems = new ObjectTable<MenuItem>();
    int selectedEntry = 0;

    #endregion

    #region Properties


    /// <summary>
    /// Gets the object table of menu items, so derived classes can add
    /// or change the menu contents.
    /// </summary>
    protected ObjectTable<MenuItem> MenuItems { get { return menuItems; } }
    protected ReadOnlyCollection<MenuEntry> MenuEntries { get { return MenuItems.GetObjects<MenuEntry>(); } }


    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public MenuScreen()
    {
      TransitionOnTime = TimeSpan.FromSeconds( 0.5 );
      TransitionOffTime = TimeSpan.FromSeconds( 0.5 );
    }

    #endregion

    #region Handle Input


    /// <summary>
    /// Responds to user input, changing the selected entry and accepting
    /// or cancelling the menu.
    /// </summary>
    public override void HandleInput( InputState input )
    {
      ReadOnlyCollection<MenuEntry> menuEntries = MenuEntries;

      PlayerIndex playerIndex;

      if ( menuEntries == null )
      {
        if ( input.IsMenuCancel( ControllingPlayer, out playerIndex ) )
          OnCancel( playerIndex );
        return;
      }

      // Move to the previous menu entry?
      if ( input.IsMenuUp( ControllingPlayer ) )
      {
        menuEntries[selectedEntry--].Focused = false;

        if ( selectedEntry < 0 )
          selectedEntry = menuEntries.Count - 1;

        menuEntries[selectedEntry].Focused = true;
      }

      // Move to the next menu entry?
      if ( input.IsMenuDown( ControllingPlayer ) )
      {
        menuEntries[selectedEntry++].Focused = false;

        if ( selectedEntry >= menuEntries.Count )
          selectedEntry = 0;

        menuEntries[selectedEntry].Focused = true;
      }

      // Accept or cancel the menu? We pass in our ControllingPlayer, which may
      // either be null (to accept input from any player) or a specific index.
      // If we pass a null controlling player, the InputState helper returns to
      // us which player actually provided the input. We pass that through to
      // OnSelectEntry and OnCancel, so they can tell which player triggered them.
      if ( input.IsMenuSelect( ControllingPlayer, out playerIndex ) )
      {
        OnSelectEntry( playerIndex );
      }
      else if ( input.IsMenuCancel( ControllingPlayer, out playerIndex ) )
      {
        OnCancel( playerIndex );
      }
    }


    /// <summary>
    /// Handler for when the user has chosen a menu entry.
    /// </summary>
    protected virtual void OnSelectEntry( PlayerIndex playerIndex )
    {
      MenuEntries[selectedEntry].OnSelect( playerIndex );
    }


    /// <summary>
    /// Handler for when the user has cancelled the menu.
    /// </summary>
    protected virtual void OnCancel( PlayerIndex playerIndex )
    {
      ExitScreen();
    }


    /// <summary>
    /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
    /// </summary>
    protected void OnCancel( object sender, PlayerIndexEventArgs e )
    {
      OnCancel( e.PlayerIndex );
    }


    #endregion

    #region Update and Draw


    /// <summary>
    /// Updates the menu.
    /// </summary>
    public override void Update( GameTime gameTime, bool otherScreenHasFocus,
                                                    bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      ReadOnlyCollection<MenuItem> menuItems = MenuItems.AllObjects;
      for ( int i = 0; i < menuItems.Count; ++i )
      {
        MenuItem item = menuItems[i];
        item.Update( gameTime );
        item.UpdateTransition( TransitionPosition, ScreenState );
      }
    }


    /// <summary>
    /// Draws the menu.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      //ScreenManager.GraphicsDevice.Clear( Color.CornflowerBlue );

      SpriteFont font = ScreenManager.Font;
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      spriteBatch.Begin();
      ReadOnlyCollection<MenuItem> menuItems = MenuItems.AllObjects;
      for ( int i = 0; i < menuItems.Count; ++i )
        menuItems[i].Draw( gameTime );
      spriteBatch.End();
    }

    #endregion
  }
}
