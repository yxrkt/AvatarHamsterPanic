#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using Physics;
using GameObjects;
using System.Collections.Generic;
using MathLib;
using Microsoft.Xna.Framework.Audio;
using CustomAvatarAnimationFramework;
#endregion

namespace GameStateManagement
{
  /// <summary>
  /// This screen implements the actual game logic. It is just a
  /// placeholder to get the idea across: you'll probably want to
  /// put some more interesting gameplay in here!
  /// </summary>
  class GameplayScreen : GameScreen
  {
    #region Fields

    ContentManager content;
    SpriteFont gameFont;
    Camera camera;
    float cameraSpawnLine = 0f, cameraKillLine = 0f;
    float camScrollSpeed = 0f;
    float lastRowY = 0f;
    float rowSpacing = 5f * FloorBlock.Scale / 2f;
    List<FloorBlock> floorBlocks = new List<FloorBlock>();
    float stageWidth = FloorBlock.Scale * 8f;
    Boundary leftBoundary  = null;
    Boundary rightBoundary = null;

    Random random = new Random();

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public GameplayScreen()
    {
      TransitionOnTime = TimeSpan.FromSeconds( 1.5 );
      TransitionOffTime = TimeSpan.FromSeconds( 0.5 );
    }


    /// <summary>
    /// Load graphics content for the game.
    /// </summary>
    public override void LoadContent()
    {
      if ( content == null )
        content = new ContentManager( ScreenManager.Game.Services, "Content" );

      gameFont = content.Load<SpriteFont>( "gamefont" );
      content.Load<CustomAvatarAnimationData>( "Animations/Walk" );
      content.Load<CustomAvatarAnimationData>( "Animations/Run" );
      content.Load<Model>( "wheel" );
      content.Load<Model>( "block" );
      content.Load<Model>( "basket" );

      // init game stuff
      InitCamera();
      InitStage();

      // create players
      new Player( new Vector2( 0f, 3f ), content );

      // test SpawnRow
      SpawnRow( 0f, 50, 70 );
      lastRowY = 0f;
      UpdateStage(); // spawn additional rows before loading screen is over

      // set gravity
      PhysicsManager.Instance.Gravity = new Vector2( 0f, -4.5f );

      //Thread.Sleep( 1000 ); // TODO: Remove

      ScreenManager.Game.ResetElapsedTime();
    }


    /// <summary>
    /// Unload graphics content used by the game.
    /// </summary>
    public override void UnloadContent()
    {
      floorBlocks.Clear();
      Player.AllPlayers.Clear();
      content.Unload();
    }


    #endregion

    #region Update and Draw


    /// <summary>
    /// Updates the state of the game. This method checks the GameScreen.IsActive
    /// property, so the game will stop updating when the pause menu is active,
    /// or if you tab away to a different application.
    /// </summary>
    public override void Update( GameTime gameTime, bool otherScreenHasFocus,
                                                    bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      if ( IsActive )
      {
        double elapsed = /*/gameTime.ElapsedGameTime.TotalSeconds/*/1.0 / 60.0/**/;

        UpdateCamera( (float)elapsed );
        UpdateStage();

        // Update physics
        PhysicsManager.Instance.Update( elapsed );

        // Remove all released floor blocks
        floorBlocks.RemoveAll( block => block.Released );

        // Update each player
        List<AutoContain> players = Player.AllPlayers;
        foreach ( Player player in players )
          player.Update( gameTime );
      }
    }


    /// <summary>
    /// Lets the game respond to player input. Unlike the Update method,
    /// this will only be called when the gameplay screen is active.
    /// </summary>
    public override void HandleInput( InputState input )
    {
      if ( input == null )
        throw new ArgumentNullException( "input" );

      // Look up inputs for the active player profile.
      int playerIndex = (int)ControllingPlayer.Value;

      KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
      GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

      // The game pauses either if the user presses the pause button, or if
      // they unplug the active gamepad. This requires us to keep track of
      // whether a gamepad was ever plugged in, because we don't want to pause
      // on PC if they are playing with a keyboard and have no gamepad at all!
      bool gamePadDisconnected = !gamePadState.IsConnected &&
                                 input.GamePadWasConnected[playerIndex];

      if ( input.IsPauseGame( ControllingPlayer ) || gamePadDisconnected )
      {
        ScreenManager.AddScreen( new PauseMenuScreen(), ControllingPlayer );
      }
      else
      {
        List<AutoContain> players = Player.AllPlayers;
        PhysCircle circle = ( (Player)players[0] ).BoundingCircle;

        float maxVelX = 8f;
        float forceScale = ( circle.Touching != null ) ? 300f : 150f;

        Vector2 leftStick = gamePadState.ThumbSticks.Left;
        if ( leftStick.X != 0f )
        {
          // ignore input if moving at or faster than max velocity
          if ( leftStick.X * circle.Velocity.X < 0f || Math.Abs( circle.Velocity.X ) < maxVelX )
            circle.Force += new Vector2( forceScale * leftStick.X, 0f );
        }
      }
    }


    /// <summary>
    /// Draws the gameplay screen.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      // This game has a blue background. Why? Because!
      ScreenManager.GraphicsDevice.Clear( ClearOptions.Target,
                                          Color.CornflowerBlue, 0, 0 );

      // Camera stuff here
      Matrix view = Matrix.CreateLookAt( camera.Position, camera.Target, camera.Up );
      Matrix proj = Matrix.CreatePerspectiveFieldOfView( camera.Fov, camera.Aspect, camera.Near, camera.Far );

      // Draw players
      List<AutoContain> players = Player.AllPlayers;
      foreach ( Player player in players )
      {
        // Draw hamster wheels
        foreach ( ModelMesh mesh in player.WheelModel.Meshes )
        {
          foreach ( BasicEffect effect in mesh.Effects )
          {
            effect.EnableDefaultLighting();
            effect.DiffuseColor = Color.White.ToVector3();

            Matrix matWorld;
            player.GetWheelTransform( out matWorld );

            effect.World = matWorld;
            effect.View = view;
            effect.Projection = proj;
          }

          mesh.Draw();
        }

        // Draw avatars
        Avatar avatar = player.Avatar;
        avatar.Renderer.View = view;
        avatar.Renderer.Projection = proj;

        Matrix matRot   = Matrix.CreateWorld( Vector3.Zero, avatar.Direction, camera.Up );
        Matrix matTrans = Matrix.CreateTranslation( avatar.Position );
        avatar.Renderer.World = Matrix.CreateScale( avatar.Scale ) * matRot * matTrans;
        avatar.Renderer.Draw( avatar.BoneTransforms, avatar.Expression );
      }

      // Draw floor blocks
      foreach ( FloorBlock block in floorBlocks )
      {
        foreach ( ModelMesh mesh in block.Model.Meshes )
        {
          foreach ( BasicEffect effect in mesh.Effects )
          {
            effect.EnableDefaultLighting();
            effect.DiffuseColor = Color.White.ToVector3();

            Matrix matWorld;
            block.GetTransform( out matWorld );

            effect.World = matWorld;
            effect.View = view;
            effect.Projection = proj;
          }

          mesh.Draw();
        }
      }

      // Our player and enemy are both actually just text strings.
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      spriteBatch.Begin();

      //double fps = 1f / gameTime.ElapsedGameTime.TotalSeconds;
      //spriteBatch.DrawString( gameFont, "X Velocity: " + players[0].BoundingCircle.Velocity.X.ToString(), 
      //                        new Vector2( 100f, 100f ), Color.Black );
      //string debugString = "Bodies: " + PhysBody.AllBodies.Count.ToString();
      //string debugString = players[0].BoundingCircle.Position.ToString();
      string debugString = ( (Player)players[0] ).BoundingCircle.Velocity.ToString() + '\n' + ( (Player)players[0] ).BoundingCircle.Position.ToString();
      spriteBatch.DrawString( gameFont, debugString, new Vector2( 100f, 100f ), Color.BlanchedAlmond );

      spriteBatch.End();

      // If the game is transitioning on or off, fade it out to black.
      if ( TransitionPosition > 0 )
        ScreenManager.FadeBackBufferToBlack( 255 - TransitionAlpha );
    }


    #endregion

    #region Helpers

    private void InitStage()
    {
      // create side boundaries
      leftBoundary = new Boundary( -.5f * stageWidth );
      rightBoundary = new Boundary( -leftBoundary.X );

      // make boxen for all the players
      // platform!

      float doorPosY = cameraKillLine - Player.Scale / 2f;
      float doorPosX = leftBoundary.X;// +FloorBlock.Scale / 2f;
      float doorPosXStep = stageWidth / 3f - FloorBlock.Scale / 3f;

      camScrollSpeed = 0f;
      Vector2 doorPos = new Vector2( doorPosX, doorPosY );
      for ( int i = 0; i < 4; ++i )
      {
        // trap door
        FloorBlock door = new FloorBlock( doorPos, content );
        floorBlocks.Add( door );

        doorPos.X += doorPosXStep;
      }
    }

    private void InitCamera()
    {
      // create the camers
      camera = new Camera( MathHelper.ToRadians( 30f ), 16f / 9f, 10f, 1000f,
                           new Vector3( 0f, 0f, 16f ), Vector3.Zero );

      // set initial camera scroll speed
      camScrollSpeed = -.2f;

      // set relative spawn and kill lines for spawning and killing rows of blocks
      Viewport viewport = this.ScreenManager.Game.GraphicsDevice.Viewport;
      Vector3 nearScreen = new Vector3( viewport.Width / 2f, viewport.Height, 0f );
      Vector3 farScreen = new Vector3( viewport.Width / 2f, viewport.Height, 1f );

      // unproject lower screen point on near plane and far plan
      Matrix proj = Matrix.CreatePerspectiveFieldOfView( camera.Fov, camera.Aspect, camera.Near, camera.Far );
      Matrix view = Matrix.CreateLookAt( camera.Position, camera.Target, camera.Up );
      Matrix world = Matrix.Identity;
      Vector3 nearWorld = viewport.Unproject( nearScreen, proj, view, world );
      Vector3 farWorld = viewport.Unproject( farScreen, proj, view, world );

      // intersect ray with vertical plane aligned with backs of blocks
      Ray ray = new Ray( nearWorld, farWorld - nearWorld );
      ray.Direction.Normalize();
      float? dist = ray.Intersects( new Plane( 0f, 0f, 1f, FloorBlock.Scale / 2f ) );

      if ( dist == null )
        throw new NullReferenceException( "Ray does not intersect with plane." );

      cameraSpawnLine = ( dist.Value * ray.Direction + ray.Position ).Y - FloorBlock.Scale / 8f;
      cameraKillLine  = -cameraSpawnLine;

    }

    private void SpawnRow( float yPos, int lowPct, int hiPct )
    {
      float spacesPerRow = stageWidth / FloorBlock.Scale;
      int   nSpaces = (int)Math.Ceiling( (double)spacesPerRow );
      float xStep   = FloorBlock.Scale;
      float xStart  = -.5f * ( xStep * spacesPerRow - xStep );
        
      // get random percent that floor is filled
      float pct = 0f;
      if ( lowPct < hiPct )
          pct = (float)random.Next( lowPct, hiPct + 1 ) / 100f;
      else
          pct = (float)lowPct / 100f;

      int nBlocks = (int)( (float)nSpaces * pct );
      RandomBag.Reset( nSpaces );

      Vector2 blockPos = Vector2.Zero;
      blockPos.Y = yPos;

      blockPos.X = xStart;
      for ( int i = 0; i < nSpaces; ++i )
      {
        if ( RandomBag.PullNext() < nBlocks )
          floorBlocks.Add( new FloorBlock( blockPos, content ) );

        blockPos.X += xStep;
      }
    }

    private void UpdateStage()
    {

      float screenLine = cameraSpawnLine + camera.Position.Y;

      // spawn rows
      while ( lastRowY > screenLine )
      {
        SpawnRow( lastRowY, 50, 70 );
        lastRowY -= rowSpacing;
      }

      float clearLine = cameraKillLine + camera.Position.Y;
      foreach ( FloorBlock block in floorBlocks )
      {
        if ( block.BoundingPolygon.Position.Y > clearLine )
          block.Release();
      }
    }

    private void UpdateCamera( float elapsed )
    {
      // Camera scroll
      camera.Translate( new Vector3( 0f, camScrollSpeed * (float)elapsed, 0f ) );
    }

    #endregion
  }
}
