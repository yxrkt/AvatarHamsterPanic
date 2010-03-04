#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using AvatarHamsterPanic.Utilities;
#endregion

namespace Menu
{
  /// <summary>
  /// The options screen is brought up over the top of the main menu
  /// screen, and gives the user a chance to configure the game
  /// in various hopefully useful ways.
  /// </summary>
  class OptionsMenuScreen : MenuScreen
  {
    #region Fields

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public OptionsMenuScreen()
    {

    }

    public override void LoadContent()
    {
      // Create our menu entries.
      Rectangle screenRect = ScreenRects.FourByThree;
      Vector2 entryPosition = new Vector2( screenRect.Center.X, screenRect.Center.Y );
      Vector2 entryStep = new Vector2( 0, 50 );

      // Back
      ImageMenuEntry entry;
      Texture2D texture;

      ContentManager content = ScreenManager.Game.Content;

      texture = content.Load<Texture2D>( "Textures/exitText" );
      entry = new ImageMenuEntry( this, entryPosition, texture, null );
      entry.Selected += OnCancel;
      entry.TransitionOnPosition = entryPosition + new Vector2( 0, -300 );
      entry.TransitionOffPosition = entryPosition + new Vector2( 0, 300 );
      entry.Focused = true;
      MenuItems.Add( entry );
    }


    #endregion

    #region Handle Input

    #endregion
  }
}
