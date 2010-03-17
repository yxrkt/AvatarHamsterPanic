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
using Graphics;
using AvatarHamsterPanic;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.GamerServices;
#endregion

namespace Menu
{
  class MainMenuScreen : MenuScreen
  {
    #region Fields

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

    SignInMenuScreen signInMenuScreen;
    OptionsMenuScreen optionsMenuScreen;
    CreditsMenuScreen creditsMenuScreen;
    HighscoreScreen highscoreScreen;
    WheelMenu wheelMenu;

    #endregion

    #region Initialization


    public MainMenuScreen()
    {
      transitioningOn = false;
      transitioningOff = false;

      TransitionOnTime = TimeSpan.FromSeconds( 1.25f );
      TransitionOffTime = TimeSpan.FromSeconds( .75 );
    }

    public override void LoadContent()
    {
      // load the fancy title effects
      LoadTitleContent();

      // create the wheel menu
      GraphicsDevice device = ScreenManager.GraphicsDevice;
      scale = (float)device.Viewport.Height / 1080f;

      ContentManager content = ScreenManager.Game.Content;

      Camera camera = new Camera( MathHelper.PiOver4, device.Viewport.AspectRatio, 
                                  1f, 100f, new Vector3( 0, 3f, 10 ), new Vector3( 0, 3f, 0 ) );

      // this should prevent spikes in sign-in screen when creating first avatar
      new Avatar( AvatarDescription.CreateRandom(), AvatarAnimationPreset.Stand0, 
                                                  1f, Vector3.UnitZ, Vector3.Zero );

      float wheelScale = 2.5f;
      wheelMenu = new WheelMenu( this, camera, wheelScale, scale, -3, 0, 3, wheelScale / 2 );

      WheelMenuEntry entry;
      wheelMenu.AcceptingInput = false;

      entry = new WheelMenuEntry( wheelMenu, content.Load<Texture2D>( "Textures/playText" ) );
      entry.Selected += PlayMenuEntrySelected;
      wheelMenu.AddEntry( entry );

      entry = new WheelMenuEntry( wheelMenu, content.Load<Texture2D>( "Textures/leaderboardText" ) );
      entry.Selected += LeaderboardMenuEntrySelected;
      wheelMenu.AddEntry( entry );

      entry = new WheelMenuEntry( wheelMenu, content.Load<Texture2D>( "Textures/optionsText" ) );
      entry.Selected += OptionsMenuEntrySelected;
      wheelMenu.AddEntry( entry );

      entry = new WheelMenuEntry( wheelMenu, content.Load<Texture2D>( "Textures/creditsText" ) );
      entry.Selected += CreditsMenuEntrySelected;
      wheelMenu.AddEntry( entry );

      entry = new WheelMenuEntry( wheelMenu, content.Load<Texture2D>( "Textures/exitText" ) );
      entry.Selected += OnCancel;
      wheelMenu.AddEntry( entry );

      MenuItems.Add( wheelMenu );

      signInMenuScreen = new SignInMenuScreen( ScreenManager );
      optionsMenuScreen = new OptionsMenuScreen( ScreenManager );
      creditsMenuScreen = new CreditsMenuScreen( ScreenManager );
      highscoreScreen = new HighscoreScreen( ScreenManager );

      GameCore.Instance.AudioManager.Listener.Position = new Vector3( 0, 0, 10 );

      // pre-load other stuff here
      content.Load<Texture2D>( "Textures/messageBox" );
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

      screenEffect = content.Load<Effect>( "Effects/screenAlignedEffect" ).Clone( device );
      screenEffect.CurrentTechnique = screenEffect.Techniques["Texture"];
      screenEffect.Parameters["ScreenWidth"].SetValue( viewport.Width );
      screenEffect.Parameters["ScreenHeight"].SetValue( viewport.Height );
      screenTextureParameter = screenEffect.Parameters["Texture"];

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
      panicPositionSpring = new SpringInterpolater( 2, 120, SpringInterpolater.GetCriticalDamping( 120 ) );
      panicSizeSpring = new SpringInterpolater( 1, 750, .75f * SpringInterpolater.GetCriticalDamping( 200 ) );
    }

    private void InitializeTransitionOn()
    {
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

      // 'Avatar'
      SetSpringDests( avatarTexture, new Vector2( viewport.Width / 2, 39f * scale ), scale, avatarVertSprings );
      float right = -100f * scale;
      float left  = right - avatarTexture.Width * scale;
      avatarVertSprings[0].SetSource( left );
      avatarVertSprings[1].SetSource( right );
      avatarVertSprings[2].SetSource( right );
      avatarVertSprings[3].SetSource( left );
      foreach ( SpringInterpolater spring in avatarVertSprings )
        spring.Active = false;

      // 'Hamster'
      SetSpringDests( hamsterTexture, new Vector2( viewport.Width / 2, 166.5f * scale ), scale, hamsterVertSprings );
      left = viewport.Width + 100f * scale;
      right = left + hamsterTexture.Width * scale;
      hamsterVertSprings[0].SetSource( left );
      hamsterVertSprings[1].SetSource( right );
      hamsterVertSprings[2].SetSource( right );
      hamsterVertSprings[3].SetSource( left );
      foreach ( SpringInterpolater spring in hamsterVertSprings )
        spring.Active = false;

      // 'Panic'
      Vector2 position = new Vector2( viewport.Width / 2, ( 244.5f + panicTexture.Height / 2 ) * scale );
      panicPositionSpring.SetSource( position );
      panicPositionSpring.SetDest( position );
      panicPositionSpring.Active = false;
      panicSizeSpring.SetSource( 0 );
      panicSizeSpring.SetDest( scale );
      panicSizeSpring.Active = false;

      // set entries to their initial positions
      wheelMenu.ConfigureEntries();
      wheelMenu.AcceptingInput = false;

      // play intro sound effects
      GameCore.Instance.AudioManager.Play2DCue( "intro", 1f );

      // set wheel to pi over 4
      wheelMenu.Angle = MathHelper.PiOver4;
    }

    private void InitializeTransitionOff()
    {
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

      // 'Avatar'
      foreach ( SpringInterpolater spring in avatarVertSprings )
      {
        spring.SetDest( new Vector2( spring.GetDest()[0], spring.GetDest()[1] - 350 * scale ) );
        spring.Active = false;
      }

      // 'Hamster'
      foreach ( SpringInterpolater spring in hamsterVertSprings )
      {
        spring.SetDest( new Vector2( spring.GetDest()[0], spring.GetDest()[1] - 350 * scale ) );
        spring.Active = false;
      }

      // 'Panic'
      panicPositionSpring.SetDest( new Vector2( panicPositionSpring.GetDest()[0], 
                                   panicPositionSpring.GetDest()[1] - 600 * scale ) );
      panicPositionSpring.Active = false;

      wheelMenu.AcceptingInput = false;
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

    public override void HandleInput( InputState input )
    {
      if ( IsActive )
        base.HandleInput( input );
    }

    void PlayMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( signInMenuScreen, e.PlayerIndex );
    }

    void LeaderboardMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      //ScreenManager.AddScreen( new LeaderboardMenuScreen(), e.PlayerIndex );
      ScreenManager.AddScreen( highscoreScreen, e.PlayerIndex );
    }

    void OptionsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( optionsMenuScreen, e.PlayerIndex );
    }

    void CreditsMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( creditsMenuScreen, e.PlayerIndex );
    }


    protected override void OnCancel( PlayerIndex playerIndex )
    {
      const string message = "Egads! Do you really want to exit Avatar Hamster Panic?";

      MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen( message );

      confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

      ScreenManager.AddScreen( confirmExitMessageBox, playerIndex );
    }


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
        wheelMenu.AcceptingInput = true;
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
      float elapsed = Math.Min( (float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f );

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
