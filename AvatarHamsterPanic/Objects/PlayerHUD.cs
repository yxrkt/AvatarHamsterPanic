using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace AvatarHamsterPanic.Objects
{
  class PlayerHUD
  {
    GamerProfile profile;
    Rectangle profileRect;
    
    Texture2D hudTexture;
    Rectangle hudRect;

    SpriteFont nameFont;
    float nameScale;
    Vector2 namePos;
    Vector2 nameOrigin;

    SpriteFont scoreFont;
    Vector2 scorePos;

    SpriteFont placeFont;
    Vector2 placePos;

    Rectangle boostRect;
    Effect boostEffect;
    EffectParameter boostEffectParamBoost;
    EffectParameter boostEffectParamBoosting;
    EffectParameter boostEffectParamTime;
    Texture2D boostTexture;

    float lastTime;

    static string[] placeStrings = { "st", "nd", "rd", "th" };
    static int xPadding = 10;
    static int yPadding = 10;

    public Player Player { get; private set; }
    public int Score { get; private set; }
    public int Place { get; set; }
    public string Name { get; set; }
    public float Boost { get; set; }

    public PlayerHUD( Player player, SignedInGamer gamer )
    {
      GameplayScreen screen = player.Screen;

      if ( gamer != null )
      {
        profile = gamer.GetProfile();
        Name = gamer.Gamertag;
      }
      else Name = "CPU";

      Player = player;
      Score = 0;
      Place = 1;
      Boost = 1f;

      Rectangle safeRect = screen.SafeRect;

      // base hud object
      hudTexture = screen.Content.Load<Texture2D>( "Textures/playerHUD" );
      int hudWidth = hudTexture.Width;
      int hudHeight = hudTexture.Height;

      int x0 = xPadding + safeRect.X + Player.PlayerNumber * ( safeRect.Width - hudWidth - 2 * xPadding ) / 3;
      int y0 = -Math.Abs( yPadding ) + safeRect.Y + safeRect.Height - hudHeight;

      hudRect = new Rectangle( x0, y0, hudWidth, hudHeight );

      // profile picture
      profileRect = new Rectangle( x0 + 91, y0 + 16, 55, 55 );

      // boost meter
      boostRect = new Rectangle( x0 + 93, y0 + 97, 142, 18 );
      boostEffect = screen.Content.Load<Effect>( "Effects/meterEffect" );
      boostEffect.CurrentTechnique = boostEffect.Techniques[0];
      boostEffectParamBoost = boostEffect.Parameters["Boost"];
      boostEffectParamBoosting = boostEffect.Parameters["Boosting"];
      boostEffectParamTime = boostEffect.Parameters["Time"];
      boostTexture = new Texture2D( screen.ScreenManager.GraphicsDevice, boostRect.Width, boostRect.Height );

      // name
      namePos = new Vector2( x0 + 239, y0 + 34 );
      nameFont = screen.Content.Load<SpriteFont>( "Fonts/HUDNameFont" );
      nameOrigin = nameFont.MeasureString( Name );
      float nameLength = nameOrigin.X;
      nameScale = Math.Min( 90f / nameLength, 1f );

      // score
      scorePos = new Vector2( x0 + 235, y0 + 88 );
      scoreFont = screen.Content.Load<SpriteFont>( "Fonts/HUDScoreFont" );

      // place
      placePos = new Vector2( x0 + 37, y0 + 65 );
      placeFont = screen.Content.Load<SpriteFont>( "Fonts/HUDPlaceFont" );
    }

    public void Update( GameTime gameTime )
    {
      // TODO: springy score stuff

      lastTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw()
    {
      SpriteBatch spriteBatch = Player.Screen.ScreenManager.SpriteBatch;

      spriteBatch.End(); // textures rendered with shaders need to have their own begin/end

      // boost meter
      spriteBatch.Begin( SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None );
      boostEffect.Begin();
      boostEffectParamBoost.SetValue( Boost );
      boostEffectParamBoosting.SetValue( Player.Boosting );
      boostEffectParamTime.SetValue( lastTime );
      boostEffect.CurrentTechnique.Passes[0].Begin();
      spriteBatch.Draw( boostTexture, boostRect, Color.White );
      spriteBatch.End();
      boostEffect.CurrentTechnique.Passes[0].End();
      boostEffect.End();

      spriteBatch.Begin(); // resume...

      // draw the base object
      spriteBatch.Draw( hudTexture, hudRect, Color.White );

      // profile picture
      if ( profile != null )
        spriteBatch.Draw( profile.GamerPicture, profileRect, Color.White );

      // name
      spriteBatch.DrawString( nameFont, Name, namePos, Color.Black, 0f, 
                              nameOrigin, nameScale, SpriteEffects.None, 0f );

      // score
      string score = Score.ToString();
      Vector2 scoreOrigin = scoreFont.MeasureString( score );
      spriteBatch.DrawString( scoreFont, score, scorePos, Color.Black, 0f, 
                              scoreOrigin, 1f, SpriteEffects.None, 0f );
      
      // place
      string placeNumber = Place.ToString();
      string placeTag = placeStrings[Place - 1];
      Vector2 placeNumberSize = placeFont.MeasureString( placeNumber );
      Vector2 placeTagSize = nameFont.MeasureString( placeTag );
      Vector2 placeNumberOrigin = new Vector2( ( placeNumberSize.X + placeTagSize.X ) / 2, placeNumberSize.Y );
      Vector2 placeTagOrigin = new Vector2( placeNumberOrigin.X - placeNumberSize.X - 1, placeTagSize.Y + 12 );
      spriteBatch.DrawString( placeFont, placeNumber, placePos, Color.Black, 0f, placeNumberOrigin, 1f, SpriteEffects.None, 0f );
      spriteBatch.DrawString( nameFont, placeTag, placePos, Color.Black, 0f, placeTagOrigin, 1f, SpriteEffects.None, 0f );
    }
  }
}