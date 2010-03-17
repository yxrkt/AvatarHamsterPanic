using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using AvatarHamsterPanic;
using Microsoft.Xna.Framework.Content;

namespace Menu
{
  class HighscoreScreen : MenuScreen
  {
    int leaderBoardTypeIndex;
    Highscore[] highscores;
    int scoreCount;
    List<LeaderBoardType> leaderBoardTypes = new List<LeaderBoardType>();
    int scoreDisplayStartIndex;
    int scoresDisplayedPerPage = 5;

    // content
    static readonly string trialModeString = "Please purchase full version for High Scores.";
    SpriteFont defaultFont;
    SpriteFont scoreFont;
    Texture2D barTexture;
    Texture2D arrowTexture;

    float ss;

    public HighscoreScreen( ScreenManager screenManager )
    {
      TransitionOnTime = TimeSpan.FromSeconds( .25f );
      TransitionOffTime = TimeSpan.FromSeconds( .25f );
      IsPopup = true;
      leaderBoardTypeIndex = 1;

      leaderBoardTypes.Add( LeaderBoardType.Local );
      leaderBoardTypes.Add( LeaderBoardType.Friend );
      leaderBoardTypes.Add( LeaderBoardType.Global );

      if ( HighscoreComponent.Global.Storage == null )
        HighscoreComponent.Global.UserWantsToLoad = true;

      // load content
      ScreenManager = screenManager;
      ContentManager content = screenManager.Game.Content;

      defaultFont = content.Load<SpriteFont>( "Fonts/menufont" );
      scoreFont = content.Load<SpriteFont>( "Fonts/scoreboardFont" );
      barTexture = content.Load<Texture2D>( "Textures/playerScoreboardBar" );
      arrowTexture = content.Load<Texture2D>( "Textures/arrow" );
      Texture2D background = content.Load<Texture2D>( "Textures/menuBackground" );
      Texture2D titleText = content.Load<Texture2D>( "Textures/highScoresTitleText" );
      Texture2D filterText = content.Load<Texture2D>( "Textures/xChangeFilterText" );
      Texture2D viewFAQText = content.Load<Texture2D>( "Textures/yFAQText" );

      // static items
      StaticImageMenuItem item;
      Vector2 position;

      Viewport viewport = screenManager.GraphicsDevice.Viewport;
      Rectangle safeArea = viewport.TitleSafeArea;
      ss = viewport.Height / 1080f;
      float textScale = ss * .5f;

      // background
      item = new StaticImageMenuItem( this, Vector2.Zero, background );
      if ( viewport.AspectRatio < 16f / 9f )
      {
        item.Scale = (float)viewport.Height / (float)background.Height;
        item.Origin.Y = 0;
        item.Origin.X = ( background.Height * viewport.AspectRatio - background.Width ) / 2;
      }
      else
      {
        item.Scale = (float)viewport.Width / (float)background.Width;
        item.Origin.X = 0;
        item.Origin.Y = ( background.Width / viewport.AspectRatio - background.Height ) / 2;
      }
      MenuItems.Add( item );

      // title
      position = new Vector2( safeArea.Center.X, safeArea.Top );
      item = new StaticImageMenuItem( this, position, titleText );
      item.Scale = ss;
      item.Origin.Y = 0;
      MenuItems.Add( item );

      // change filter
      position = new Vector2( safeArea.X, safeArea.Bottom - viewFAQText.Height * textScale );
      item = new StaticImageMenuItem( this, position, filterText );
      item.Origin = new Vector2( 0, filterText.Height );
      item.Scale = textScale;
      MenuItems.Add( item );

      // view FAQ
      position = new Vector2( safeArea.X, safeArea.Bottom );
      item = new StaticImageMenuItem( this, position, viewFAQText );
      item.Origin = new Vector2( 0, viewFAQText.Height );
      item.Scale = textScale;
      MenuItems.Add( item );
    }

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      highscores = new Highscore[HighscoreComponent.MaxAggregateCount];
      if ( leaderBoardTypes[leaderBoardTypeIndex] != LeaderBoardType.Local ||
          SignedInGamer.SignedInGamers[(PlayerIndex)ControllingPlayer].IsLiveEnabled() == false )
      {
        //Change the leaderboard index to something other than 0 ("Local") if this player cannot record highscores
        if ( SignedInGamer.SignedInGamers[(PlayerIndex)ControllingPlayer].IsLiveEnabled() == false &&
            leaderBoardTypeIndex == 0 )
        {
          leaderBoardTypeIndex = 1;
        }
        scoreCount = HighscoreComponent.Global.GetHighscores( highscores, leaderBoardTypes[leaderBoardTypeIndex], ControllingPlayer );
      }
      else
      {
        scoreCount = HighscoreComponent.Global.GetHighscores( highscores, (PlayerIndex)ControllingPlayer );
      }

      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );
    }

    public override void HandleInput( InputState input )
    {
      PlayerIndex pi;

      //Exit
      if ( input.IsNewButtonPress( Microsoft.Xna.Framework.Input.Buttons.B, null, out pi ) )
      {
        ExitScreen();
      }

      if ( input.IsNewButtonPress( Buttons.Y, null, out pi ) )
      {
        //ScreenManager.AddScreen( new HighScoreFAQScreen(), null );
      }

      //Control which filter type is being used
      if ( input.IsNewButtonPress( Buttons.X, null, out pi ) )
      {
        leaderBoardTypeIndex++;
        if ( leaderBoardTypeIndex >= leaderBoardTypes.Count )
          leaderBoardTypeIndex = 0;
      }

      //Control the scores that are shown on the screen
      if ( input.IsMenuDown( null ) )
        scoreDisplayStartIndex++;
      if ( input.IsMenuUp( null ) )
        scoreDisplayStartIndex--;
      if ( input.IsNewButtonPress( Buttons.LeftTrigger, null, out pi ) )
        scoreDisplayStartIndex -= scoresDisplayedPerPage;
      if ( input.IsNewButtonPress( Buttons.RightTrigger, null, out pi ) )
        scoreDisplayStartIndex += scoresDisplayedPerPage;

      scoreDisplayStartIndex = (int)MathHelper.Clamp( scoreDisplayStartIndex, 0, scoreCount - scoresDisplayedPerPage );

      base.HandleInput( input );
    }

    public override void Draw( GameTime gameTime )
    {
      //ScreenManager.FadeBackBufferToBlack( (byte)MathHelper.Clamp( (float)TransitionAlpha * 3f / 3f, 0, 255 ) );
      base.Draw( gameTime );

      var safeArea = ScreenManager.GraphicsDevice.Viewport.TitleSafeArea;
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      if ( Guide.IsTrialMode )
      {
        ShowNoScoresText();
      }
      else
      {
        spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );

        DrawFilterType();
        DrawNoDeviceText();

        var color = new Color( Color.White, TransitionAlpha );

        if ( highscores != null )
        {
          Vector2 screenMiddle = new Vector2( safeArea.X + safeArea.Width * .5f, safeArea.Y + safeArea.Height * .5f );

          //Create the bar and center it on-screen.
          Vector2 barMiddle = new Vector2( barTexture.Width * .5f, barTexture.Height * .5f );
          Vector2 barCountYOffset = new Vector2( 0, barTexture.Height * scoresDisplayedPerPage * .5f - 10 );
          Vector2 barPos = screenMiddle - barMiddle - barCountYOffset;

          Vector2 fontSize = scoreFont.MeasureString( " " );
          Vector2 scorePos = barPos + new Vector2( 27, 9 );
          List<Highscore> scoresToDraw = highscores.ToList().GetRange( scoreDisplayStartIndex, scoresDisplayedPerPage );

          // Up Arrow
          if ( scoreDisplayStartIndex != 0 )
          {
            Vector2 offset = -new Vector2( arrowTexture.Width * .5f, arrowTexture.Height * 1.8f + 20 );
            spriteBatch.Draw( arrowTexture, barPos + barMiddle + offset, null,
                              color, 0f, Vector2.Zero, ss, SpriteEffects.None, 0 );
          }

          for ( int i = 0; i < scoresToDraw.Count; ++i )
          {
            Highscore score = scoresToDraw[i];

            if ( score == null ) break;

            Vector2 scoreNumPos = scorePos;
            Vector2 namePos = scorePos + new Vector2( fontSize.X * 10, 0 );
            Vector2 numberPos = scorePos + new Vector2( fontSize.X * 70, 0 );
            string scoreNumber = ( scoreDisplayStartIndex + i + 1 ).ToString();
            string scoreText = string.Format( "{0}       {1}", score.Score, score.Message );//{0:00000} for zeroes

            spriteBatch.Draw( barTexture, barPos, color );

            spriteBatch.DrawString( scoreFont, scoreNumber, scoreNumPos, color );
            spriteBatch.DrawString( scoreFont, score.Gamer, namePos, color );
            spriteBatch.DrawString( scoreFont, scoreText, numberPos, color );

            scorePos.Y += barTexture.Height;
            barPos.Y += barTexture.Height;
          }

          // Down Arrow
          //if ( scoreCount > scoresDisplayedPerPage )
          if ( scoreDisplayStartIndex + scoresDisplayedPerPage < highscores.Count( i => i != null ) )
          {
            // draw down arrow
            Vector2 offset = new Vector2( -arrowTexture.Width * .5f, -arrowTexture.Height * 1f - 4 );
            spriteBatch.Draw( arrowTexture, barPos + barMiddle + offset, null,
                              color, 0, Vector2.Zero, ss, SpriteEffects.FlipVertically, 0 );
          }
        }
        else
        {
          //sb.DrawString( Game1.ContentHandle.Load<SpriteFont>("highScoreFont"), "No highscores saved", new Vector2(300, 300), color);
        }

        //ScreenManager.DrawBackButtonB( sb, false, color );

        spriteBatch.End();
      }

      //if ( Guide.IsTrialMode )
      //  ShowNoScoresText();
    }

    private void DrawNoDeviceText()
    {
      if ( HighscoreComponent.Global.Storage != null )
        return;

      Viewport viewport = GameCore.Instance.GraphicsDevice.Viewport;
      string text = "No storage device selected.\nPlease enable \"High Score Sharing\" in the options menu.";
      Vector2 textSize = defaultFont.MeasureString( text );
      Vector2 textPos = new Vector2( ( viewport.Width - textSize.X ) * .5f, viewport.Height * .4f );

      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
      Color color = new Color( Color.White, TransitionAlpha );
      spriteBatch.DrawString( defaultFont, text, textPos, color );
    }

    private void DrawFilterType()
    {
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
      float ss = viewport.Height / 1080f;

      Color color = new Color( 255, 255, 255, TransitionAlpha );
      string filterText = GetLeaderBoardTypeText();

      Rectangle safeArea = viewport.TitleSafeArea;
      Vector2 screenMiddle = new Vector2( safeArea.X + safeArea.Width * .5f, safeArea.Y + safeArea.Height * .5f );
      Vector2 barMiddle = new Vector2( barTexture.Width * .5f, barTexture.Height * .5f );
      Vector2 barCountYOffset = new Vector2( 0, barTexture.Height * scoresDisplayedPerPage * .5f - 10 );
      Vector2 titlePos = screenMiddle - barMiddle - barCountYOffset;
      titlePos.Y -= 40 * ss;

      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
      spriteBatch.DrawString( defaultFont, filterText, titlePos, color );
    }

    private string GetLeaderBoardTypeText()
    {
      switch ( leaderBoardTypes[leaderBoardTypeIndex] )
      {
        case LeaderBoardType.Local:
          if ( ControllingPlayer.HasValue && SignedInGamer.SignedInGamers[ControllingPlayer.Value] != null )
            return SignedInGamer.SignedInGamers[ControllingPlayer.Value].Gamertag;
          else
            return "Xbox Live Friends";
        case LeaderBoardType.Friend:
          return "Xbox Live Friends";
        case LeaderBoardType.Global:
          return "Xbox Live All";
        default:
          return "Unknown";
      }
    }

    public void ShowNoScoresText()
    {
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
      Viewport viewport = ScreenManager.Game.GraphicsDevice.Viewport;

      Color color = new Color( Color.DarkOrange, TransitionAlpha );

      Vector2 textSize = defaultFont.MeasureString( trialModeString );
      Vector2 textPos = new Vector2( ( viewport.Width - textSize.X ) * .5f, viewport.Height * .5f );
      spriteBatch.Begin();
      spriteBatch.DrawString( defaultFont, trialModeString, textPos, color );
      spriteBatch.End();
    }
  }
}
