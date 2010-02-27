using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using Utilities;
using MathLibrary;
using Menu;

namespace AvatarHamsterPanic.Objects
{
  class PlayerHUD
  {
    GamerProfile profile;
    Rectangle profileRect;

    Texture2D hudCageTexture;
    Texture2D hudTexture;
    Rectangle hudRect;

    SpriteFont nameFont;
    float nameScale;
    Vector2 namePos;
    Vector2 nameOrigin;

    SpriteFont scoreFont;
    Vector2 scorePos;
    SpringInterpolater scoreSpring;
    PopupText scorePopup;
    StringBuilder scoreString;

    int place;
    SpriteFont placeFont;
    SpriteFont placeSmallFont;
    Vector2 placePos;
    StringBuilder placeNumber;
    SpringInterpolater placeSpring;

    Rectangle boostRect;
    Effect boostEffect;
    EffectParameter boostEffectParamBoost;
    EffectParameter boostEffectParamBoosting;
    EffectParameter boostEffectParamTime;
    Texture2D boostTexture;

    float lastTime;

    static readonly string[] placeStrings = { "st", "nd", "rd", "th" };
    static int xPadding = 10;
    static int yPadding = 10;

    public Player Player { get; private set; }
    public int Score { get; private set; }
    public int TotalScore { get { return Math.Max( Score + scorePopup.Points, 0 ); } }
    public string Name { get; set; }
    public float Boost { get; set; }
    public int Place
    {
      get { return place; }
      set { if ( place != value ) { place = value; placeSpring.SetSource( 1.25f ); } }
    }

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
      Boost = 1f;

      Rectangle fourByThree = screen.SafeRect;

      // base hud object
      hudCageTexture = screen.Content.Load<Texture2D>( "Textures/playerHUDCage" );
      hudTexture = screen.Content.Load<Texture2D>( "Textures/playerHUD" );
      int hudWidth = hudTexture.Width;
      int hudHeight = hudTexture.Height;

      int x0 = xPadding + fourByThree.X + Player.PlayerNumber * ( fourByThree.Width - hudWidth - 2 * xPadding ) / 3;
      int y0 = -Math.Abs( yPadding ) + fourByThree.Y + fourByThree.Height - hudHeight;

      hudRect = new Rectangle( x0, y0, hudWidth, hudHeight );

      // profile picture
      profileRect = new Rectangle( x0 + 88, y0 + 26, 60, 60 );

      // boost meter
      boostRect = new Rectangle( x0 + 90, y0 + 111, 142, 18 );
      boostEffect = screen.Content.Load<Effect>( "Effects/meterEffect" );
      boostEffect.CurrentTechnique = boostEffect.Techniques[0];
      boostEffectParamBoost = boostEffect.Parameters["Boost"];
      boostEffectParamBoosting = boostEffect.Parameters["Boosting"];
      boostEffectParamTime = boostEffect.Parameters["Time"];
      boostTexture = new Texture2D( screen.ScreenManager.GraphicsDevice, boostRect.Width, boostRect.Height );

      // name
      namePos = new Vector2( x0 + 162, y0 + 12 );
      nameFont = screen.Content.Load<SpriteFont>( "Fonts/HUDNameFont" );
      nameOrigin = nameFont.MeasureString( Name ) / 2;
      float nameLength = nameOrigin.X * 2;
      nameScale = Math.Min( 150f / nameLength, 1f );

      // score
      scorePos = new Vector2( x0 + 230, y0 + 100 );
      scoreFont = screen.Content.Load<SpriteFont>( "Fonts/HUDScoreFont" );
      scoreSpring = new SpringInterpolater( 1, 700f, .25f * SpringInterpolater.GetCriticalDamping( 700f ) );
      scoreSpring.SetSource( 1f );
      scoreSpring.SetDest( 1f );
      scoreSpring.Active = true;
      scoreString = new StringBuilder( 1 );

      // score popup
      scorePopup = new PopupText( 1f, scorePos + new Vector2( -25f, -120f ), 
                                  scorePos + new Vector2( -15f, -15f ), 1f );

      // place
      placePos = new Vector2( x0 + 36, y0 + 91 );
      placeFont = screen.Content.Load<SpriteFont>( "Fonts/HUDPlaceFont" );
      placeSmallFont = screen.Content.Load<SpriteFont>( "Fonts/HUDPlaceTagFont" );
      placeNumber = new StringBuilder( "0" );
      placeSpring = new SpringInterpolater( 1, 700f, .25f * SpringInterpolater.GetCriticalDamping( 700f ) );
      placeSpring.SetSource( 1f );
      placeSpring.SetDest( 1f );
      placeSpring.Active = true;
      Place = 1;
    }

    public void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      if ( elapsed > 1f / 60f )
        elapsed = 1f / 60f;

      int scoreChange = scorePopup.Update( elapsed );
      if ( scoreChange != 0 )
        scoreSpring.SetSource( 1.3f * scoreSpring.GetSource()[0] );
      Score += scoreChange;
      Score = Math.Max( 0, Score );

      scoreSpring.Update( elapsed );
      placeSpring.Update( elapsed );

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
      GameCore game = Player.Screen.ScreenManager.Game as GameCore;
      spriteBatch.Draw( hudCageTexture, hudRect, Color.White );
      spriteBatch.Draw( hudTexture, hudRect, /**/Color.White/*/game.PlayerColors[Player.PlayerNumber]/**/ );

      // profile picture
      if ( profile != null )
        spriteBatch.Draw( profile.GamerPicture, profileRect, Color.White );

      // name
      Vector2 scale = new Vector2( nameScale, 1 );
      spriteBatch.DrawString( nameFont, Name, namePos, Color.Black, 0f,
                              nameOrigin, scale, SpriteEffects.None, 0f );

      // score
      scoreString.Remove( 0, scoreString.Length );
      scoreString.AppendInt( Score );
      Vector2 scoreOrigin = scoreFont.MeasureString( scoreString );
      spriteBatch.DrawString( scoreFont, scoreString, scorePos, Color.Black, 0f, 
                              scoreOrigin, scoreSpring.GetSource()[0], SpriteEffects.None, 0f );

      // score popup
      if ( scorePopup.Active )
        scorePopup.Draw( spriteBatch, scoreFont, Color.Black );

      // place
      placeNumber[0] = (char)( '0' + Place );
      string placeTag = placeStrings[Place - 1];
      Vector2 placeNumberSize = placeFont.MeasureString( placeNumber );
      Vector2 placeTagSize = placeSmallFont.MeasureString( placeTag );
      Vector2 placeNumberOrigin = new Vector2( ( placeNumberSize.X + placeTagSize.X ) / 2, placeNumberSize.Y );
      Vector2 placeTagOrigin = new Vector2( placeNumberOrigin.X - placeNumberSize.X, placeTagSize.Y + 50 );
      spriteBatch.DrawString( placeFont, placeNumber, placePos, Color.Black, 0f,
                              placeNumberOrigin, placeSpring.GetSource()[0], SpriteEffects.None, 0f );
      spriteBatch.DrawString( placeSmallFont, placeTag, placePos, Color.Black, 0f, placeTagOrigin, 
                              1, SpriteEffects.None, 0f );
    }

    public void AddPoints( int points )
    {
      scorePopup.Add( points );
    }
  }
}