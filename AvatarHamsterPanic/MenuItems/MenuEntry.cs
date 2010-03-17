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
using MathLibrary;
#endregion

namespace Menu
{
  class MenuEntry : MenuItem
  {
    #region Fields and Properties

    string text;
    SpringInterpolater scaleSpring;

    public Color Color { get; set; }
    public string Text
    {
      get { return text; }
      set { text = value; Dimensions = Screen.ScreenManager.Font.MeasureString( text ); }
    }

    #endregion

    #region Initialization


    public MenuEntry( MenuScreen screen, Vector2 position, string text )
      : base( screen, position )
    {
      this.text = text;
      if ( text.Length == 0 )
        this.Dimensions = new Vector2( 0f, (float)Screen.ScreenManager.Font.LineSpacing );
      else
        this.Dimensions = Screen.ScreenManager.Font.MeasureString( text );

      scaleSpring = new SpringInterpolater( 1, 700, .4f * SpringInterpolater.GetCriticalDamping( 700 ) );
      scaleSpring.SetSource( 1 );
      scaleSpring.SetDest( 1 );
      scaleSpring.Active = true;
    }


    #endregion

    #region Update and Draw


    public override void Update( GameTime gameTime )
    {
      base.Update( gameTime );

      if ( Focused && scaleSpring.GetDest()[0] != 1.25f )
        scaleSpring.SetDest( 1.25f );
      else if ( !Focused && scaleSpring.GetDest()[0] != 1f )
        scaleSpring.SetDest( 1f );

      scaleSpring.Update( (float)gameTime.ElapsedGameTime.TotalSeconds );
    }

    public override void Draw( GameTime gameTime )
    {
      Color color = Focused ? Color.Yellow : Color.White;

      double time = gameTime.TotalGameTime.TotalSeconds;

      float pulsate = (float)Math.Sin( time * 6d ) + 1f;

      // scale is scale spring stuff
      float scale = Scale * scaleSpring.GetSource()[0];

      color.A = Screen.TransitionAlpha;

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
