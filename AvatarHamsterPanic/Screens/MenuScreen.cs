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
using AvatarHamsterPanic;
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
    protected int selectedEntry = 0;

    #endregion

    #region Properties


    /// <summary>
    /// Gets the object table of menu items, so derived classes can add
    /// or change the menu contents.
    /// </summary>
    protected ObjectTable<MenuItem> MenuItems { get { return menuItems; } }
    protected ReadOnlyCollection<MenuEntry> MenuEntries { get { return MenuItems.GetObjects<MenuEntry>(); } }
    protected ReadOnlyCollection<ImageMenuEntry> ImageMenuEntries { get { return MenuItems.GetObjects<ImageMenuEntry>(); } }


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
      ReadOnlyCollection<ImageMenuEntry> imageMenuEntries = ImageMenuEntries;

      PlayerIndex playerIndex;

      // If there is a wheel menu, handle left and right input
      if ( menuItems.AllObjectsList.Exists( item => item is WheelMenu ) )
      {
        WheelMenu wheel = menuItems.GetObjects<WheelMenu>()[0];
        if ( wheel.AcceptingInput )
        {
          if ( input.IsMenuLeft( ControllingPlayer ) )
            wheel.RotateCW();
          if ( input.IsMenuRight( ControllingPlayer ) )
            wheel.RotateCCW();
        }
      }
      else if ( imageMenuEntries != null && imageMenuEntries.Count != 0 )
      {
        // Move to the previous menu entry?
        if ( input.IsMenuUp( ControllingPlayer ) )
        {
          imageMenuEntries[selectedEntry--].Focused = false;

          if ( selectedEntry < 0 )
            selectedEntry = imageMenuEntries.Count - 1;

          imageMenuEntries[selectedEntry].Focused = true;

          GameCore.Instance.AudioManager.Play2DCue( "whoosh", 1f );
        }

        // Move to the next menu entry?
        if ( input.IsMenuDown( ControllingPlayer ) )
        {
          imageMenuEntries[selectedEntry++].Focused = false;

          if ( selectedEntry >= imageMenuEntries.Count )
            selectedEntry = 0;

          imageMenuEntries[selectedEntry].Focused = true;

          GameCore.Instance.AudioManager.Play2DCue( "whoosh", 1f );
        }
      }
      else if ( menuEntries != null && menuEntries.Count != 0 )
      {
        // Move to the previous menu entry?
        if ( input.IsMenuUp( ControllingPlayer ) )
        {
          menuEntries[selectedEntry--].Focused = false;

          if ( selectedEntry < 0 )
            selectedEntry = menuEntries.Count - 1;

          menuEntries[selectedEntry].Focused = true;

          GameCore.Instance.AudioManager.Play2DCue( "whoosh", 1f );
        }

        // Move to the next menu entry?
        if ( input.IsMenuDown( ControllingPlayer ) )
        {
          menuEntries[selectedEntry++].Focused = false;

          if ( selectedEntry >= menuEntries.Count )
            selectedEntry = 0;

          menuEntries[selectedEntry].Focused = true;

          GameCore.Instance.AudioManager.Play2DCue( "whoosh", 1f );
        }
      }


      if ( input.IsMenuSelect( ControllingPlayer, out playerIndex ) )
        OnSelectEntry( playerIndex );
      else if ( input.IsMenuRight( playerIndex, out playerIndex ) )
        OnIncrementEntry( playerIndex );
      else if ( input.IsMenuLeft( playerIndex, out playerIndex ) )
        OnDecrementEntry( playerIndex );
      else if ( input.IsMenuCancel( ControllingPlayer, out playerIndex ) )
        OnCancel( playerIndex );
    }


    protected virtual void OnSelectEntry( PlayerIndex playerIndex )
    {
      GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );
      if ( menuItems.AllObjectsList.Exists( item => item is WheelMenu ) )
      {
        menuItems.GetObjects<WheelMenu>()[0].OnSelect( playerIndex );
      }
      else if ( ImageMenuEntries != null && ImageMenuEntries.Count != 0 )
      {
        ImageMenuEntries[selectedEntry].OnSelect( playerIndex );
      }
      else if ( MenuEntries != null && MenuEntries.Count != 0 )
      {
        MenuEntries[selectedEntry].OnSelect( playerIndex );
      }
    }


    protected virtual void OnIncrementEntry( PlayerIndex playerIndex )
    {
      if ( MenuEntries != null && MenuEntries.Count != 0 )
      {
        MenuEntries[selectedEntry].OnIncrement( playerIndex );
      }
    }


    protected virtual void OnDecrementEntry( PlayerIndex playerIndex )
    {
      if ( MenuEntries != null && MenuEntries.Count != 0 )
      {
        MenuEntries[selectedEntry].OnDecrement( playerIndex );
      }
    }


    protected virtual void OnCancel( PlayerIndex playerIndex )
    {
      GameCore.Instance.AudioManager.Play2DCue( "onCancel", 1f );
      ExitScreen();
    }


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
