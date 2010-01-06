#region File Description
//-----------------------------------------------------------------------------
// MenuEntry.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace GameStateManagement
{
  /// <summary>
  /// Helper class represents a single entry in a MenuScreen. By default this
  /// just draws the entry text string, but it can be customized to display menu
  /// entries in different ways. This also provides an event that will be raised
  /// when the menu entry is selected.
  /// </summary>
  class MenuEntry : MenuItem
  {
    #region Fields

    /// <summary>
    /// The text rendered for this entry.
    /// </summary>
    string text;

    public Color Color { get; set; }

    #endregion

    #region Properties


    /// <summary>
    /// Gets or sets the text of this menu entry.
    /// </summary>
    public string Text
    {
      get { return text; }
      set { text = value; Dimensions = Screen.ScreenManager.Font.MeasureString( text ); }
    }


    #endregion

    #region Initialization


    /// <summary>
    /// Constructs a new menu entry with the specified text.
    /// </summary>
    public MenuEntry( MenuScreen screen, Vector2 position, string text )
      : base( screen, position )
    {
      this.text = text;
      if ( text.Length == 0 )
        this.Dimensions = new Vector2( 0f, (float)Screen.ScreenManager.Font.LineSpacing );
      else
        this.Dimensions = Screen.ScreenManager.Font.MeasureString( text );
    }


    #endregion


    #region Update and Draw


    public override void Update( GameTime gameTime )
    {
      base.Update( gameTime );
    }

    /// <summary>
    /// Draws the menu entry.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      // Draw the selected entry in yellow, otherwise white.
      Color color = Focused ? Color.Yellow : Color.White;

      // Pulsate the size of the selected menu entry.
      double time = gameTime.TotalGameTime.TotalSeconds;

      float pulsate = (float)Math.Sin( time * 6d ) + 1f;

      float scale = 1f + pulsate * 0.05f * selectionFade;

      // Modify the alpha to fade text out during transitions.
      color.A = Screen.TransitionAlpha;

      // Draw text, centered on the middle of each line.
      ScreenManager screenManager = Screen.ScreenManager;
      SpriteBatch spriteBatch = screenManager.SpriteBatch;
      SpriteFont font = screenManager.Font;

      Vector2 origin = new Vector2( 0, font.LineSpacing / 2 );

      spriteBatch.DrawString( font, text, curPos, color, 0,
                              origin, scale, SpriteEffects.None, 0 );
    }

    public override void UpdateTransition( float transitionPosition, ScreenState state )
    {
      base.UpdateTransition( transitionPosition, state );
    }

    #endregion
  }
}
