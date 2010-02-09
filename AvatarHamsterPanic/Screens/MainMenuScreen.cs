#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework.Content;
using MathLibrary;
#endregion

namespace Menu
{
  /// <summary>
  /// The main menu screen is the first thing displayed when the game starts up.
  /// </summary>
  class MainMenuScreen : MenuScreen
  {
    // 'Avatar'
    Texture2D avatarTexture;
    SpringInterpolater[] avatarVertSprings;

    // 'Hamster'
    Texture2D hamsterTexture;
    SpringInterpolater[] hamsterVertSprings;

    // 'Panic'
    Texture2D panicTexture;
    SpringInterpolater panicSizeSpring;
    SpringInterpolater panicPositionSpring;

    VertexPositionTexture[] vertArray;
    VertexDeclaration vertexDeclaration;
    Effect screenEffect;
    EffectParameter screenTextureParameter;

    float scale;
    bool transitioningOn;
    bool transitioningOff;
    float transitionTime;

    #region Initialization


    /// <summary>
    /// Constructor fills in the menu contents.
    /// </summary>
    public MainMenuScreen()
    {
      transitioningOn = false;
      transitioningOff = false;

      TransitionOffTime = TimeSpan.FromSeconds( .75 );
    }

    public override void LoadContent()
    {
      LoadTitleContent();

      // Create our menu entries.
      string[] entryStrings = { "Play", "Leaderboard", "Options", "Credits", "Exit" };
      EventHandler<PlayerIndexEventArgs>[] entryEvents = 
      {
        PlayMenuEntrySelected,
        LeaderboardMenuEntrySelected,
        OptionsMenuEntrySelected,
        CreditsMenuEntrySelected,
        OnCancel,
      };

      Vector2 entryPosition = new Vector2( 100f, 150f );
      int nEntries = entryStrings.Length;
      for ( int i = 0; i < nEntries; ++i )
      {
        MenuEntry entry = new MenuEntry( this, entryPosition, entryStrings[i] );
        entry.Selected += entryEvents[i];
        entry.TransitionOnPosition = entryPosition + new Vector2( -256f, 0f );
        entry.TransitionOffPosition = entryPosition + new Vector2( 512f, 0f );
        MenuItems.Add( entry );
        entryPosition.Y += entry.Dimensions.Y;
      }
      MenuEntries[0].Focused = true;
    }

    private void LoadTitleContent()
    {
      GraphicsDevice device = ScreenManager.Game.GraphicsDevice;
      ContentManager content = ScreenManager.Game.Content;
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

      vertArray = new VertexPositionTexture[4];
      vertArray[0].TextureCoordinate = new Vector2( 0, 0 );
      vertArray[1].TextureCoordinate = new Vector2( 1, 0 );
      vertArray[2].TextureCoordinate = new Vector2( 1, 1 );
      vertArray[3].TextureCoordinate = new Vector2( 0, 1 );
      vertexDeclaration = new VertexDeclaration( device, VertexPositionTexture.VertexElements );

      screenEffect = content.Load<Effect>( "Effects/screenAlignedEffect" );
      screenEffect.CurrentTechnique = screenEffect.Techniques["Texture"];
      screenEffect.Parameters["ScreenWidth"].SetValue( viewport.Width );
      screenEffect.Parameters["ScreenHeight"].SetValue( viewport.Height );
      screenTextureParameter = screenEffect.Parameters["Texture"];

      scale = (float)viewport.Height / 720f;

      // 'Avatar'
      avatarTexture = content.Load<Texture2D>( "Textures/avatarText" );
      avatarVertSprings = new SpringInterpolater[4];
      for ( int i = 0; i < 4; ++i )
      {
        float k = i < 2 ? 125 : 220;
        float bs = i < 2 ? .7f : 1f;
        avatarVertSprings[i] = new SpringInterpolater( 3, k, bs * SpringInterpolater.GetCriticalDamping( k ) );
      }

      // 'Hamster'
      hamsterTexture = content.Load<Texture2D>( "Textures/hamsterText" );
      hamsterVertSprings = new SpringInterpolater[4];
      for ( int i = 0; i < 4; ++i )
      {
        float k = i < 2 ? 145 : 250;
        float bs = i < 2 ? .8f : 1f;
        hamsterVertSprings[i] = new SpringInterpolater( 3, k, bs * SpringInterpolater.GetCriticalDamping( k ) );
      }

      // 'Panic'
      panicTexture = content.Load<Texture2D>( "Textures/panicText" );
      panicPositionSpring = new SpringInterpolater( 2, 155, SpringInterpolater.GetCriticalDamping( 155 ) );
      panicSizeSpring = new SpringInterpolater( 1, 750, .75f * SpringInterpolater.GetCriticalDamping( 200 ) );
    }

    private void InitializeTransitionOn()
    {
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

      // 'Avatar'
      SetSpringDests( avatarTexture, new Vector2( viewport.Width / 2, 26f * scale ), scale, avatarVertSprings );
      float right = -100f * scale;
      float left  = right - avatarTexture.Width * scale;
      avatarVertSprings[0].SetSource( left );
      avatarVertSprings[1].SetSource( right );
      avatarVertSprings[2].SetSource( right );
      avatarVertSprings[3].SetSource( left );
      foreach ( SpringInterpolater spring in avatarVertSprings )
        spring.Active = false;

      // 'Hamster'
      SetSpringDests( hamsterTexture, new Vector2( viewport.Width / 2, 111f * scale ), scale, hamsterVertSprings );
      left = viewport.Width + 100f * scale;
      right = left + hamsterTexture.Width * scale;
      hamsterVertSprings[0].SetSource( left );
      hamsterVertSprings[1].SetSource( right );
      hamsterVertSprings[2].SetSource( right );
      hamsterVertSprings[3].SetSource( left );
      foreach ( SpringInterpolater spring in hamsterVertSprings )
        spring.Active = false;

      // 'Panic'
      Vector2 position = new Vector2( viewport.Width / 2, ( 163f + panicTexture.Height / 2 ) * scale );
      panicPositionSpring.SetSource( position );
      panicPositionSpring.SetDest( position );
      panicPositionSpring.Active = false;
      panicSizeSpring.SetSource( 0 );
      panicSizeSpring.SetDest( scale );
      panicSizeSpring.Active = false;
    }

    private void InitializeTransitionOff()
    {
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

      // 'Avatar'
      foreach ( SpringInterpolater spring in avatarVertSprings )
      {
        spring.SetDest( new Vector2( spring.GetDest()[0], spring.GetDest()[1] - 200 * scale ) );
        spring.Active = false;
      }

      // 'Hamster'
      foreach ( SpringInterpolater spring in hamsterVertSprings )
      {
        spring.SetDest( new Vector2( spring.GetDest()[0], spring.GetDest()[1] - 200 * scale ) );
        spring.Active = false;
      }

      // 'Panic'
      panicPositionSpring.SetDest( new Vector2( panicPositionSpring.GetDest()[0], panicPositionSpring.GetDest()[1] - 300 * scale ) );
      panicPositionSpring.Active = false;
    }

    private void SetSpringDests( Texture2D texture, Vector2 position, float scale, SpringInterpolater[] springs )
    {
      float halfWidth  = scale * texture.Width  / 2;
      float height = scale * texture.Height;

      springs[0].SetDest( new Vector3( -halfWidth + position.X, position.Y, 0 ) );
      springs[1].SetDest( new Vector3(  halfWidth + position.X, position.Y, 0 ) );
      springs[2].SetDest( new Vector3(  halfWidth + position.X, height + position.Y, 0 ) );
      springs[3].SetDest( new Vector3( -halfWidth + position.X, height + position.Y, 0 ) );

      springs[0].SetSource( new Vector3( -halfWidth + position.X, position.Y, 0 ) );
      springs[1].SetSource( new Vector3(  halfWidth + position.X, position.Y, 0 ) );
      springs[2].SetSource( new Vector3(  halfWidth + position.X, height + position.Y, 0 ) );
      springs[3].SetSource( new Vector3( -halfWidth + position.X, height + position.Y, 0 ) );
    }

    #endregion

    #region Handle Input


    /// <summary>
    /// Event handler for when the Play menu entry is selected.
    /// </summary>
    void PlayMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      /*/
      LoadingScreen.Load( ScreenManager, true, e.PlayerIndex,
                          new GameplayScreen() );
      /*/
      ScreenManager.AddScreen( new SignInMenuScreen(), e.PlayerIndex );
      /**/
    }

    /// <summary>
    /// Event handler for when the Leaderboard menu entry is selected.
    /// </summary>
    void LeaderboardMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new LeaderboardMenuScreen(), e.PlayerIndex );
    }

    /// <summary>
    /// Event handler for when the Options menu entry is selected.
    /// </summary>
    void OptionsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new OptionsMenuScreen(), e.PlayerIndex );
    }

    /// <summary>
    /// Event handler for when the Credits menu entry is selected.
    /// </summary>
    void CreditsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new CreditsMenuScreen(), e.PlayerIndex );
    }


    /// <summary>
    /// When the user cancels the main menu, ask if they want to exit the sample.
    /// </summary>
    protected override void OnCancel( PlayerIndex playerIndex )
    {
      const string message = "ZOMG! Do you really want to exit Avatar Hamster Panic?";

      MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen( message );

      confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmExitMessageBox, playerIndex );
    }


    /// <summary>
    /// Event handler for when the user selects ok on the "are you sure
    /// you want to exit" message box.
    /// </summary>
    void ConfirmExitMessageBoxAccepted( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.Game.Exit();
    }


    #endregion

    #region Update and Draw

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      // initialize transitions
      if ( ScreenState == ScreenState.TransitionOn && !transitioningOn )
      {
        InitializeTransitionOn();
        transitionTime = 0;
        transitioningOn = true;
      }
      else if ( ScreenState == ScreenState.TransitionOff && !transitioningOff )
      {
        InitializeTransitionOff();
        transitionTime = 0;
        transitioningOff = true;
      }
      if ( ScreenState == ScreenState.Active )
      {
        transitioningOn = false;
        transitioningOff = false;
      }

      // transitioning on
      if ( ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.Active )
      {
        // zoom 'Avatar' in
        if ( IsActive && transitionTime > .0625f && !avatarVertSprings[0].Active )
        {
          foreach ( SpringInterpolater spring in avatarVertSprings )
            spring.Active = true;
        }

        // zoom 'Hamster' in
        if ( IsActive && transitionTime > .45f && !hamsterVertSprings[0].Active )
        {
          foreach ( SpringInterpolater spring in hamsterVertSprings )
            spring.Active = true;
        }

        // zoom 'Panic' in
        if ( IsActive && transitionTime > .95f && !panicSizeSpring.Active )
          panicSizeSpring.Active = true;
      }

      // transitioning off
      if ( ScreenState == ScreenState.TransitionOff )
      {
        if ( transitionTime > .1f && !avatarVertSprings[0].Active )
        {
          foreach ( SpringInterpolater spring in avatarVertSprings )
            spring.Active = true;
        }

        if ( transitionTime > .15f && !hamsterVertSprings[0].Active )
        {
          foreach ( SpringInterpolater spring in hamsterVertSprings )
            spring.Active = true;
        }

        if ( transitionTime > .2f && !panicPositionSpring.Active )
          panicPositionSpring.Active = true;
      }

      // update springs
      float elapsed = Math.Min( (float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 60f );

      foreach ( SpringInterpolater spring in avatarVertSprings )
        spring.Update( elapsed );

      foreach ( SpringInterpolater spring in hamsterVertSprings )
        spring.Update( elapsed );

      panicPositionSpring.Update( elapsed );
      panicSizeSpring.Update( elapsed );

      transitionTime += elapsed;

      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );
    }

    public override void Draw( GameTime gameTime )
    {
      GraphicsDevice device = ScreenManager.Game.GraphicsDevice;

      device.VertexDeclaration = vertexDeclaration;
      device.RenderState.AlphaBlendEnable = true;
      device.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
      device.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;

      // 'Avatar'
      SetVertexBuffer( avatarVertSprings );
      screenTextureParameter.SetValue( avatarTexture );
      screenEffect.Begin();
      screenEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.TriangleFan, vertArray, 0, 2 );
      screenEffect.CurrentTechnique.Passes[0].End();
      screenEffect.End();

      // 'Hamster'
      SetVertexBuffer( hamsterVertSprings );
      screenTextureParameter.SetValue( hamsterTexture );
      screenEffect.Begin();
      screenEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.TriangleFan, vertArray, 0, 2 );
      screenEffect.CurrentTechnique.Passes[0].End();
      screenEffect.End();

      // 'Panic'
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
      spriteBatch.Begin();
      Vector2 origin   = new Vector2( panicTexture.Width / 2, panicTexture.Height / 2 );
      Vector2 position = new Vector2( panicPositionSpring.GetSource()[0], panicPositionSpring.GetSource()[1] );
      spriteBatch.Draw( panicTexture, position, null, Color.White, 0f, 
                        origin, panicSizeSpring.GetSource()[0], SpriteEffects.None, 0 );
      spriteBatch.End();

      // draw the lame buttons
      base.Draw( gameTime );
    }

    private void SetVertexBuffer( SpringInterpolater[] springs )
    {
      int index = 0;
      foreach ( SpringInterpolater spring in springs )
      {
        float[] vert = spring.GetSource();
        vertArray[index++].Position = new Vector3( vert[0], vert[1], 0 );
      }
    }

    #endregion
  }
}
