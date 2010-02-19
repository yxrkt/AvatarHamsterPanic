using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomModelSample;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Utilities;

namespace Menu
{
  struct ScoreboardPlayer
  {
    public Avatar Avatar;
    public SignedInGamer Gamer;
    public int Score;
    public int PlayerNumber;
  }

  class ScoreboardMenuScreen : MenuScreen
  {
    const float swipeOutDuration = .75f;
    const float swipeInDuration = .75f;

    CustomModel podiumModel;
    Texture2D swipeMask;
    Matrix podiumTransform;
    SpriteBatch spriteBatch;
    Camera camera;
    GraphicsDevice device;
    RenderState renderState;
    float elapsed = 0;
    VertexPositionTexture[] screenTextureQuad;
    VertexPositionColor[] screenColorQuad;
    VertexPositionTexture[] swipeQuad;
    VertexPositionTexture[] vertexBuffer;
    ScreenQuadEffect effect;
    Vector3 screenCenter;
    float dadtOut;
    float dadtIn;
    float maskRadius;
    ScoreboardPlayer[] players;
    Vector3[] places;
    float podiumSize = 1f;
    float[] animSpeeds;
    ScoreboardPopupMenuScreen popupScreen;
    Texture2D pressAToContinueText;
    Vector2 pressAPosition;
    Vector2 pressAOrigin;
    CustomModel loserBoxModel;
    Matrix loserBoxTransform;
    Rectangle scoreboardRect;
    Texture2D playerScoreBar;
    Texture2D playerNameBox;
    Texture2D scoreText;
    Texture2D winsText;
    Vector2[] playerScoreBarPositions;
    Vector2[] playerNameBoxPositions;
    SpriteFont scoreboardFont;
    SpriteFont scoreboardNumberFont;
    SpriteFont scoreboardSubscriptFont;
    StringBuilder stringBuffer;
    string cpuNameString;

    static readonly string[] placeStrings = { "st", "nd", "rd", "th" };


    public ScoreboardMenuScreen( ScreenManager screenManager, Slot[] slots )
    {
      TransitionOnTime = TimeSpan.FromSeconds( 0 );

      ScreenManager = screenManager;

      ContentManager content = ScreenManager.Game.Content;

      podiumModel = content.Load<CustomModel>( "Models/podium" );
      foreach ( CustomModel.ModelPart part in podiumModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.EffectParamColor.SetValue( Color.White.ToVector4() );
      }

      swipeMask = content.Load<Texture2D>( "Textures/swipeMask" );

      podiumTransform = Matrix.CreateScale( podiumSize );

      spriteBatch = ScreenManager.SpriteBatch;

      float aspect = ScreenManager.GraphicsDevice.Viewport.AspectRatio;
      camera = new Camera( MathHelper.PiOver4, aspect, 1f, 100f, new Vector3( 0, 2, 10 ), new Vector3( 0, 2, 0 ) );

      device = ScreenManager.GraphicsDevice;
      renderState = device.RenderState;

      screenTextureQuad = new VertexPositionTexture[4];
      screenTextureQuad[0].Position = new Vector3( 0, 0, 0 );
      screenTextureQuad[1].Position = new Vector3( device.Viewport.Width, 0, 0 );
      screenTextureQuad[2].Position = new Vector3( device.Viewport.Width, device.Viewport.Height, 0 );
      screenTextureQuad[3].Position = new Vector3( 0, device.Viewport.Height, 0 );
      screenTextureQuad[0].TextureCoordinate = new Vector2( 0, 0 );
      screenTextureQuad[1].TextureCoordinate = new Vector2( 1, 0 );
      screenTextureQuad[2].TextureCoordinate = new Vector2( 1, 1 );
      screenTextureQuad[3].TextureCoordinate = new Vector2( 0, 1 );

      screenColorQuad = new VertexPositionColor[4];
      screenColorQuad[0].Position = new Vector3( 0, 0, 0 );
      screenColorQuad[1].Position = new Vector3( device.Viewport.Width, 0, 0 );
      screenColorQuad[2].Position = new Vector3( device.Viewport.Width, device.Viewport.Height, 0 );
      screenColorQuad[3].Position = new Vector3( 0, device.Viewport.Height, 0 );
      for ( int i = 0; i < 4; ++i )
        screenColorQuad[i].Color = Color.Black;

      swipeQuad = new VertexPositionTexture[4];
      float a = device.Viewport.Width / 2;
      float b = device.Viewport.Height / 2;
      maskRadius = (float)Math.Sqrt( a * a + b * b );
      float xMin = a - maskRadius;
      float xMax = a + maskRadius;
      float yMin = b - maskRadius;
      float yMax = b + maskRadius;
      swipeQuad[0].Position = new Vector3( xMin, yMin, 0 );
      swipeQuad[1].Position = new Vector3( xMax, yMin, 0 );
      swipeQuad[2].Position = new Vector3( xMax, yMax, 0 );
      swipeQuad[3].Position = new Vector3( xMin, yMax, 0 );
      swipeQuad[0].TextureCoordinate = new Vector2( 0, 0 );
      swipeQuad[1].TextureCoordinate = new Vector2( 1, 0 );
      swipeQuad[2].TextureCoordinate = new Vector2( 1, 1 );
      swipeQuad[3].TextureCoordinate = new Vector2( 0, 1 );

      vertexBuffer = new VertexPositionTexture[4];
      vertexBuffer[0].TextureCoordinate = new Vector2( 0, 0 );
      vertexBuffer[1].TextureCoordinate = new Vector2( 1, 0 );
      vertexBuffer[2].TextureCoordinate = new Vector2( 1, 1 );
      vertexBuffer[3].TextureCoordinate = new Vector2( 0, 1 );

      screenCenter = new Vector3( a, b, 0 );

      effect = ScreenQuadEffect.CreateScreenQuadEffect( device, content );

      dadtOut = MathHelper.Pi * maskRadius * maskRadius / swipeOutDuration;
      dadtIn = MathHelper.Pi * maskRadius * maskRadius / swipeInDuration;

      players = new ScoreboardPlayer[4];

      places = new Vector3[4];
      places[0] = podiumTransform.Translation + new Vector3( 0, podiumSize, 0 );
      places[1] = places[0] + new Vector3( -podiumSize, -.4f * podiumSize, 0 );
      places[2] = places[0] + new Vector3( podiumSize, -.5f * podiumSize, 0 );
      places[3] = podiumTransform.Translation + new Vector3( 3 * podiumSize, .125f, 0 );

      animSpeeds = new float[4];
      animSpeeds[0] = .95f;
      animSpeeds[1] = .8f;
      animSpeeds[2] = 1.1f;
      animSpeeds[3] = 1f;

      popupScreen = new ScoreboardPopupMenuScreen( screenManager, slots );

      pressAToContinueText = content.Load<Texture2D>( "Textures/pressAToContinueText" );
      pressAPosition = new Vector2( screenCenter.X, .8f * device.Viewport.Height );
      pressAOrigin = new Vector2( pressAToContinueText.Width, pressAToContinueText.Height ) / 2;

      loserBoxModel = content.Load<CustomModel>( "Models/loserBox" );
      foreach ( CustomModel.ModelPart part in loserBoxModel.ModelParts )
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
      loserBoxTransform = Matrix.CreateTranslation( 3 * podiumSize, 0, 0 );

      int rectWidth  = 900;
      int rectHeight = 245;
      scoreboardRect = new Rectangle( ( device.Viewport.Width - rectWidth ) / 2, 60, rectWidth, rectHeight );

      playerScoreBar = content.Load<Texture2D>( "Textures/playerScoreboardBar" );
      playerNameBox = content.Load<Texture2D>( "Textures/playerNameBox" );
      scoreText = content.Load<Texture2D>( "Textures/scoreText" );
      winsText = content.Load<Texture2D>( "Textures/winsText" );

      playerScoreBarPositions = new Vector2[4];
      int y = scoreboardRect.Top + 41;
      for ( int i = 0; i < 4; ++i )
      {
        playerScoreBarPositions[i] = new Vector2( scoreboardRect.Left + 1, y );
        y += playerScoreBar.Height + 2;
      }

      playerNameBoxPositions = new Vector2[4];
      for ( int i = 0; i < 4; ++i )
        playerNameBoxPositions[i] = new Vector2( scoreboardRect.Left + 58, playerScoreBarPositions[i].Y + 5 );

      scoreboardFont = content.Load<SpriteFont>( "Fonts/scoreboardFont" );
      scoreboardNumberFont = content.Load<SpriteFont>( "Fonts/scoreboardNumberFont" );
      scoreboardSubscriptFont = content.Load<SpriteFont>( "Fonts/scoreboardSubscriptFont" );
      stringBuffer = new StringBuilder( 2 );
      cpuNameString = "CPU";
    }

    public override void LoadContent()
    {
      // Content is loaded in the constructor, so the GameplayScreen can load this screen's
      // content during its loading screen. This prevents spikes when transitioning to
      // this screen, which is very important for the swipe transition.
    }

    public void SetPlayer( int index, int playerNumber, Avatar avatar, SignedInGamer gamer, int score, uint id )
    {
      players[index].Avatar = avatar;
      players[index].Gamer  = gamer;
      players[index].Score  = score;
      players[index].PlayerNumber = playerNumber;

      // stuff for keeping track of wins
      popupScreen.SetPlayerID( playerNumber, id );
      GameCore game = (GameCore)ScreenManager.Game;
      if ( index == 0 )
      {
        if ( game.PlayerWins.ContainsKey( id ) )
          game.PlayerWins[id]++;
        else
          game.PlayerWins.Add( id, 1 );
      }
      else if ( !game.PlayerWins.ContainsKey( id ) )
      {
        game.PlayerWins.Add( id, 0 );
      }
    }

    private void AssemblePlayers()
    {
      for ( int i = 0; i < 4; ++i )
      {
        if ( players[i].Avatar != null )
        {
          players[i].Avatar.Position = places[i];
          players[i].Avatar.Direction = Vector3.UnitZ;
        }
      }

      if ( players[0].Avatar != null )
        players[0].Avatar.SetAnimation( AvatarAnimationPreset.Celebrate );

      if ( players[1].Avatar != null )
        players[1].Avatar.SetAnimation( AvatarAnimationPreset.Clap );

      if ( players[2].Avatar != null )
        players[2].Avatar.SetAnimation( AvatarAnimationPreset.Clap );

      if ( players[3].Avatar != null )
      {
        if ( players[3].Avatar.Description.BodyType == AvatarBodyType.Male )
          players[3].Avatar.SetAnimation( AvatarAnimationPreset.MaleCry );
        else
          players[3].Avatar.SetAnimation( AvatarAnimationPreset.FemaleAngry );
      }
    }

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      if ( elapsed > swipeOutDuration && elapsed - swipeOutDuration < swipeInDuration )
        AssemblePlayers();

      for ( int i = 0; i < 4; ++i )
      {
        if ( players[i].Avatar != null )
          players[i].Avatar.Update( TimeSpan.FromSeconds( animSpeeds[i] * gameTime.ElapsedGameTime.TotalSeconds ) , true );
      }
    }

    public override void HandleInput( InputState input )
    {
      if ( elapsed > swipeOutDuration + swipeInDuration )
      {
        PlayerIndex playerIndex;
        if ( input.IsNewButtonPress( Buttons.A, null, out playerIndex ) ||
             input.IsNewButtonPress( Buttons.Start, null, out playerIndex ) )
        {
          ScreenManager.AddScreen( popupScreen, null );
        }
      }
    }

    protected override void OnCancel( PlayerIndex playerIndex )
    {
    }

    public override void Draw( GameTime gameTime )
    {
      if ( elapsed < swipeOutDuration )
      {
        //float t = swipeOutDuration - elapsed;
        //float r = maskRadius - (float)Math.Sqrt( dadtOut * t / MathHelper.Pi );
        float u = /*/r / maskRadius/*/elapsed / swipeOutDuration/**/;
        DrawSwipe( u );
      }
      else if ( elapsed < swipeOutDuration + swipeInDuration )
      {
        Draw();
        //float t = swipeInDuration - ( elapsed - swipeOutDuration );
        //float r = (float)Math.Sqrt( dadtIn * t / MathHelper.Pi );
        float u = /*/r / maskRadius/*/1 - ( elapsed - swipeOutDuration ) / swipeInDuration/**/;
        DrawSwipe( u );
      }
      else
      {
        Draw();
      }

      elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    private void Draw()
    {
      device.Clear( Color.CornflowerBlue );
      //TODO: Draw background here

      Matrix view = camera.GetViewMatrix();
      Matrix projection = camera.GetProjectionMatrix();

      DrawScoreboard();

      renderState.DepthBufferEnable = true;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;

      podiumModel.Draw( camera.Position, podiumTransform, view, projection );
      loserBoxModel.Draw( camera.Position, loserBoxTransform, view, projection );

      for ( int i = 0; i < 4; ++i )
      {
        Avatar avatar = players[i].Avatar;
        if ( avatar != null )
        {
          players[i].Avatar.Renderer.View = view;
          players[i].Avatar.Renderer.Projection = projection;
          Matrix matRot = Matrix.CreateWorld( Vector3.Zero, avatar.Direction, camera.Up );
          Matrix matTrans = Matrix.CreateTranslation( avatar.Position );
          avatar.Renderer.World = matRot * matTrans;
          avatar.Renderer.Draw( avatar.BoneTransforms, avatar.Expression );
        }
      }

      GameScreen topScreen = ScreenManager.GetScreens().Last();
      ScoreboardMenuScreen scoreboardScreen = topScreen as ScoreboardMenuScreen;
      ScoreboardPopupMenuScreen popupScreen = topScreen as ScoreboardPopupMenuScreen;

      if ( scoreboardScreen != null || popupScreen != null && popupScreen.GoingBackToScoreboard )
      {
        spriteBatch.Begin();
        Color color = new Color( Color.White, .5f + .5f * (float)Math.Sin( elapsed * MathHelper.PiOver4 ) );
        spriteBatch.Draw( pressAToContinueText, pressAPosition, null, color, 0f,
                          pressAOrigin, .65f, SpriteEffects.None, 0 );
        spriteBatch.End();
      }
    }

    private void DrawScoreboard()
    {
      GameCore game = ScreenManager.Game as GameCore;

      spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );
      spriteBatch.Draw( scoreText, new Vector2( scoreboardRect.X + 622, scoreboardRect.Y + 3 ), Color.White );
      spriteBatch.Draw( winsText, new Vector2( scoreboardRect.X + 777, scoreboardRect.Y + 3 ), Color.White );
      for ( int i = 0; i < 4; ++i )
      {
        if ( players[i].Avatar != null )
        {
          // background bar
          Color barColor = game.PlayerColors[players[i].PlayerNumber];
          spriteBatch.Draw( playerScoreBar, playerScoreBarPositions[i], barColor );

          // place
          stringBuffer.Clear();
          stringBuffer.AppendInt( i + 1 );
          string placeTag = placeStrings[i];
          Vector2 placeNumberSize = scoreboardNumberFont.MeasureString( stringBuffer );
          Vector2 placeTagSize = scoreboardFont.MeasureString( placeTag );
          Vector2 placeNumberOrigin = new Vector2( ( placeNumberSize.X + placeTagSize.X ) / 2, placeNumberSize.Y );
          Vector2 placeTagOrigin = new Vector2( placeNumberOrigin.X - placeNumberSize.X - 1, placeTagSize.Y + 12 );
          Vector2 placePos = playerScoreBarPositions[i] + new Vector2( 34, 50 );
          spriteBatch.DrawString( scoreboardNumberFont, stringBuffer, placePos, Color.Black, 0f,
                                  placeNumberOrigin, 1, SpriteEffects.None, 0f );
          spriteBatch.DrawString( scoreboardSubscriptFont, placeTag, placePos, Color.Black, 0f,
                                  placeTagOrigin, 1, SpriteEffects.None, 0f );

          // name box
          spriteBatch.Draw( playerNameBox, playerNameBoxPositions[i], Color.White );

          // name
          Vector2 namePos = playerScoreBarPositions[i] + new Vector2( 64, 10 );
          if ( players[i].Gamer != null )
            spriteBatch.DrawString( scoreboardFont, players[i].Gamer.Gamertag, namePos, Color.Black );
          else
            spriteBatch.DrawString( scoreboardFont, cpuNameString, namePos, Color.Black );

          // score
          stringBuffer.Clear();
          stringBuffer.AppendInt( players[i].Score );
          Vector2 scoreOrigin = scoreboardNumberFont.MeasureString( stringBuffer ) / 2;
          Vector2 scorePos = playerScoreBarPositions[i] + new Vector2( 674, 26 );
          spriteBatch.DrawString( scoreboardNumberFont, stringBuffer, scorePos, Color.Black, 
                                  0, scoreOrigin, 1, SpriteEffects.None, 0 );

          // wins
          stringBuffer.Clear();
          stringBuffer.AppendInt( game.PlayerWins[popupScreen.Slots[players[i].PlayerNumber].ID] );
          Vector2 winsOrigin = scoreboardNumberFont.MeasureString( stringBuffer ) / 2;
          Vector2 winsPos = playerScoreBarPositions[i] + new Vector2( 822, 26 );
          spriteBatch.DrawString( scoreboardNumberFont, stringBuffer, winsPos, Color.Black,
                                  0, winsOrigin, 1, SpriteEffects.None, 0 );
        }
      }
      spriteBatch.End();
    }

    /// <summary>
    /// Swipe to black and back.
    /// </summary>
    /// <param name="u">0 when there is no blackness, 1 when fully black.</param>
    private void DrawSwipe( float u )
    {
      renderState.CullMode = CullMode.None;

      device.Clear( ClearOptions.Stencil, Color.Black, 0, 1 );

      renderState.StencilEnable = true;
      renderState.StencilPass = StencilOperation.Replace;
      renderState.StencilFunction = CompareFunction.Always;
      renderState.ReferenceStencil = 0;
      renderState.AlphaBlendEnable = true;
      renderState.SourceBlend = Blend.Zero;
      renderState.DestinationBlend = Blend.One;

      device.VertexDeclaration = effect.TextureVertexDeclaration;
      effect.Technique = ScreenQuadEffectTechnique.Texture;
      for ( int i = 0; i < 4; ++i )
        vertexBuffer[i].Position = Vector3.Lerp( swipeQuad[i].Position, screenCenter, u );
      effect.Texture = swipeMask;
      effect.Begin();
      effect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.TriangleFan, vertexBuffer, 0, 2 );
      effect.CurrentTechnique.Passes[0].End();
      effect.End();

      renderState.SourceBlend = Blend.SourceAlpha;
      renderState.DestinationBlend = Blend.InverseSourceAlpha;
      device.VertexDeclaration = effect.ColorVertexDeclaration;
      effect.Technique = ScreenQuadEffectTechnique.Color;
      renderState.ReferenceStencil = 1;
      renderState.StencilFunction = CompareFunction.Equal;
      effect.Begin();
      effect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.TriangleFan, screenColorQuad, 0, 2 );
      effect.CurrentTechnique.Passes[0].End();
      effect.End();

      renderState.StencilEnable = false;
    }
  }
}