using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace GameObjects
{
  class PlayerHUD
  {
    GamerProfile profile;
    Texture2D hudTexture;
    int hudWidth;
    int hudHeight;
    SpriteFont hudNameFont;
    SpriteFont hudScoreFont;
    SpriteFont hudPlaceFont;

    Rectangle hudRect;
    Rectangle profileRect;
    Vector2 namePos;
    Vector2 nameOrigin;
    float nameScale;
    Vector2 scorePos;
    Vector2 placePos;

    static string[] placeStrings = { "st", "nd", "rd", "th" };
    static int xPadding = 10;
    static int yPadding = 20;

    public Player Player { get; private set; }
    public int Score { get; private set; }
    public int Place { get; set; }
    public string Name { get; set; }
    public float Boost { get; set; }

    public PlayerHUD( Player player, SignedInGamer gamer )
    {
      if ( gamer != null )
      {
        profile = gamer.GetProfile();
        Name = gamer.Gamertag;
      }
      else
      {
        Name = "CPU";
      }

      GameplayScreen screen = player.Screen;

      hudNameFont = screen.Content.Load<SpriteFont>( "Fonts/HUDNameFont" );
      hudScoreFont = screen.Content.Load<SpriteFont>( "Fonts/HUDScoreFont" );
      hudPlaceFont = screen.Content.Load<SpriteFont>( "Fonts/HUDPlaceFont" );

      hudTexture = screen.Content.Load<Texture2D>( "Textures/playerHUD" );
      hudWidth  = hudTexture.Width;
      hudHeight = hudTexture.Height;

      Player = player;
      Score = 0;
      Place = 1;
      Boost = 1f;

      // positions, origins, scales, etc of each hud element
      Rectangle safeRect = screen.SafeRect;

      int x0 = xPadding + safeRect.X + Player.PlayerNumber * ( safeRect.Width - hudWidth - 2 * xPadding ) / 3;
      int y0 = -yPadding + safeRect.Y + safeRect.Height - hudHeight;

      hudRect = new Rectangle( x0, y0, hudWidth, hudHeight );

      profileRect = new Rectangle( x0 + 91, y0 + 16, 55, 55 );

      namePos = new Vector2( x0 + 239, y0 + 34 );
      nameOrigin = hudNameFont.MeasureString( Name );
      float nameLength = nameOrigin.X;
      nameScale = Math.Min( 90f / nameLength, 1f );

      scorePos = new Vector2( x0 + 235, y0 + 88 );

      placePos = new Vector2( x0 + 37, y0 + 65 );
    }

    public void Update( GameTime gameTime )
    {
      // TODO: stuff
    }

    public void Draw()
    {
      SpriteBatch spriteBatch = Player.Screen.ScreenManager.SpriteBatch;

      // draw the base object
      spriteBatch.Draw( hudTexture, hudRect, Color.White );

      // profile picture
      if ( profile != null )
        spriteBatch.Draw( profile.GamerPicture, profileRect, Color.White );

      // name
      spriteBatch.DrawString( hudNameFont, Name, namePos, Color.Black, 0f, 
                              nameOrigin, nameScale, SpriteEffects.None, 0f );

      // score
      string score = Score.ToString();
      Vector2 scoreOrigin = hudScoreFont.MeasureString( score );
      spriteBatch.DrawString( hudScoreFont, score, scorePos, Color.Black, 0f, 
                              scoreOrigin, 1f, SpriteEffects.None, 0f );
      
      // place
      string placeNumber = Place.ToString();
      string placeTag = placeStrings[Place - 1];
      Vector2 placeNumberSize = hudPlaceFont.MeasureString( placeNumber );
      Vector2 placeTagSize = hudNameFont.MeasureString( placeTag );
      Vector2 placeNumberOrigin = new Vector2( ( placeNumberSize.X + placeTagSize.X ) / 2, placeNumberSize.Y );
      Vector2 placeTagOrigin = new Vector2( placeNumberOrigin.X - placeNumberSize.X - 1, placeTagSize.Y + 12 );
      spriteBatch.DrawString( hudPlaceFont, placeNumber, placePos, Color.Black, 0f, placeNumberOrigin, 1f, SpriteEffects.None, 0f );
      spriteBatch.DrawString( hudNameFont, placeTag, placePos, Color.Black, 0f, placeTagOrigin, 1f, SpriteEffects.None, 0f );
    }
  }
}