#region File Description
//-----------------------------------------------------------------------------
// MessageBoxScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using Utilities;
using System.Collections.Generic;
using AvatarHamsterPanic;
#endregion

namespace Menu
{
  /// <summary>
  /// A popup message box screen, used to display "are you sure?"
  /// confirmation messages.
  /// </summary>
  class MessageBoxScreen : GameScreen
  {
    #region Fields

    const int textSidePadding = 35;
    const int textTopPadding  = 30;

    string message;
    SpriteFont font;
    Texture2D messageBox;
    Rectangle outerRectangle;
    Rectangle innerRectangle;
    float screenScale;

    #endregion

    #region Events

    public event EventHandler<PlayerIndexEventArgs> Accepted;
    public event EventHandler<PlayerIndexEventArgs> Cancelled;

    #endregion

    #region Initialization

    public MessageBoxScreen( string message )
    {
      this.message = message + ' ';

      IsPopup = true;
      TransitionOnTime = TimeSpan.FromSeconds( 0.2 );
      TransitionOffTime = TimeSpan.FromSeconds( 0.2 );
    }

    private void WrapMessage()
    {
      int lastLine = 0;
      int curWord = 0;
      StringBuilder buffer = new StringBuilder( message.Length + 4 );
      for ( int i = 0; i < message.Length; ++i )
      {
        char a = message[i];
        if ( a == '\n' )
        {
          lastLine = i + 1;
          buffer.Append( a );
        }
        else if ( a == ' ' )
        {
          buffer.Append( message, curWord, i - curWord + 1 );
          //buffer.Append( a );
          curWord = i + 1;
        }
        else
        {
          float stringLength = font.MeasureString( message.Substring( lastLine, i - lastLine + 1 ) ).X;
          if ( stringLength * screenScale > innerRectangle.Width )
          {
            buffer.Append( '\n' );
            lastLine = curWord;
          }
        }
      }

      message = buffer.ToString();
    }

    public override void LoadContent()
    {
      GraphicsDevice device = ScreenManager.Game.GraphicsDevice;
      ContentManager content = ScreenManager.Game.Content;

      font = ScreenManager.Font;

      messageBox = content.Load<Texture2D>( "Textures/messageBox" );

      screenScale = device.Viewport.Height / 1080f;
      int boxWidth  = (int)( messageBox.Width  * screenScale + .5f );
      int boxHeight = (int)( messageBox.Height * screenScale + .5f );

      int x = ( device.Viewport.Width  - boxWidth  ) / 2;
      int y = ( device.Viewport.Height - boxHeight ) / 2;
      outerRectangle = new Rectangle( x, y, boxWidth, boxHeight );
      innerRectangle = new Rectangle( (int)( x + textSidePadding * screenScale ),
                                      (int)( y + textTopPadding * screenScale ),
                                      (int)( outerRectangle.Width - 2 * textSidePadding * screenScale ),
                                      (int)( outerRectangle.Height - 2 * textTopPadding * screenScale ) );

      WrapMessage();
    }


    #endregion

    #region Handle Input

    public override void HandleInput( InputState input )
    {
      PlayerIndex playerIndex;

      if ( input.IsMenuSelect( ControllingPlayer, out playerIndex ) )
      {
        // Raise the accepted event, then exit the message box.
        if ( Accepted != null )
          Accepted( this, new PlayerIndexEventArgs( playerIndex ) );

        GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );

        ExitScreen();
      }
      else if ( input.IsMenuCancel( ControllingPlayer, out playerIndex ) )
      {
        // Raise the cancelled event, then exit the message box.
        if ( Cancelled != null )
          Cancelled( this, new PlayerIndexEventArgs( playerIndex ) );

        GameCore.Instance.AudioManager.Play2DCue( "onCancel", 1f );

        ExitScreen();
      }
    }


    #endregion

    #region Draw


    public override void Draw( GameTime gameTime )
    {
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      // Darken down any other screens that were drawn beneath the popup.
      ScreenManager.FadeBackBufferToBlack( TransitionAlpha * 2 / 3 );

      //Fade the popup alpha during transitions.
      Color color = new Color( 255, 255, 255, TransitionAlpha );
      Vector2 textPosition = new Vector2( innerRectangle.X, innerRectangle.Y );

      spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );
      spriteBatch.Draw( messageBox, outerRectangle, color );
      spriteBatch.DrawString( font, message, textPosition, color, 0f, Vector2.Zero,
                              screenScale, SpriteEffects.None, 0 );
      spriteBatch.End();
    }


    #endregion
  }
}
