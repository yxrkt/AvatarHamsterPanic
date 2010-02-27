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
          if ( font.MeasureString( message.Substring( lastLine, i - lastLine + 1 ) ).X > innerRectangle.Width )
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
      int x = ( device.Viewport.Width  - messageBox.Width  ) / 2;
      int y = ( device.Viewport.Height - messageBox.Height ) / 2;
      outerRectangle = new Rectangle( x, y, messageBox.Width, messageBox.Height );
      innerRectangle = new Rectangle( x + textSidePadding, y + textTopPadding,
                                      outerRectangle.Width - 2 * textSidePadding,
                                      outerRectangle.Height - 2 * textTopPadding );

      WrapMessage();
    }


    #endregion

    #region Handle Input

    public override void HandleInput( InputState input )
    {
      PlayerIndex playerIndex;

      // We pass in our ControllingPlayer, which may either be null (to
      // accept input from any player) or a specific index. If we pass a null
      // controlling player, the InputState helper returns to us which player
      // actually provided the input. We pass that through to our Accepted and
      // Cancelled events, so they can tell which player triggered them.
      if ( input.IsMenuSelect( ControllingPlayer, out playerIndex ) )
      {
        // Raise the accepted event, then exit the message box.
        if ( Accepted != null )
          Accepted( this, new PlayerIndexEventArgs( playerIndex ) );

        ExitScreen();
      }
      else if ( input.IsMenuCancel( ControllingPlayer, out playerIndex ) )
      {
        // Raise the cancelled event, then exit the message box.
        if ( Cancelled != null )
          Cancelled( this, new PlayerIndexEventArgs( playerIndex ) );

        ExitScreen();
      }
    }


    #endregion

    #region Draw


    /// <summary>
    /// Draws the message box.
    /// </summary>
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
      spriteBatch.DrawString( font, message, textPosition, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0 );
      spriteBatch.End();

      //// Center the message text in the viewport.
      //Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
      //Vector2 viewportSize = new Vector2( viewport.Width, viewport.Height );
      //Vector2 textSize = font.MeasureString( message );
      //Vector2 textPosition = ( viewportSize - textSize ) / 2;

      //// The background includes a border somewhat larger than the text itself.
      //const int hPad = 32;
      //const int vPad = 16;

      //Rectangle backgroundRectangle = new Rectangle( (int)textPosition.X - hPad,
      //                                               (int)textPosition.Y - vPad,
      //                                               (int)textSize.X + hPad * 2,
      //                                               (int)textSize.Y + vPad * 2 );

      //// Fade the popup alpha during transitions.
      //Color color = new Color( 255, 255, 255, TransitionAlpha );

      //spriteBatch.Begin();

      //// Draw the background rectangle.
      //spriteBatch.Draw( gradientTexture, backgroundRectangle, color );

      //// Draw the message box text.
      //spriteBatch.DrawString( font, message, textPosition, color );

      //spriteBatch.End();
    }


    #endregion
  }
}
