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
  public class GameplayScreen : GameScreen
  {
    #region Fields and Properties

    public ContentManager Content { get; private set; }
    public Camera Camera { get; private set; }
    public CameraInfo CameraInfo { get; private set; }
    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public ObjectTable ObjectTable { get; private set; }

    SpriteFont gameFont;
    float lastRowY = 0f;
    float rowSpacing = 5f * FloorBlock.Scale / 2f;
    float stageWidth = FloorBlock.Scale * 6f;
    Boundary leftBoundary  = null;
    Boundary rightBoundary = null;

    int frameCount = 0;

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
      if ( Content == null )
        Content = new ContentManager( ScreenManager.Game.Services, "Content" );

      gameFont = Content.Load<SpriteFont>( "gamefont" );

      // pre-load
      Content.Load<CustomAvatarAnimationData>( "Animations/Walk" );
      Content.Load<CustomAvatarAnimationData>( "Animations/Run" );
      Content.Load<CustomAvatarAnimationData>( "Animations/Crawl" );
      Content.Load<Model>( "wheel" );
      Content.Load<Model>( "block" );
      Content.Load<Model>( "basket" );

      // init game stuff
      ObjectTable = new ObjectTable();
      InitCamera();
      InitStage();

      SpawnRow( 0f, 50, 70 );
      lastRowY = 0f;
      UpdateStage(); // spawn additional rows before loading screen is over

      // set gravity
      PhysicsManager.Instance.Gravity = new Vector2( 0f, -4.5f );

      //Thread.Sleep( 1000 );

      ScreenManager.Game.ResetElapsedTime();
    }


    /// <summary>
    /// Unload graphics content used by the game.
    /// </summary>
    public override void UnloadContent()
    {
      ObjectTable.Clear();
      Content.Unload();
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
        //UpdateStage();

        if ( frameCount == 97 )
          frameCount--;

        // Update physics
        PhysicsManager.Instance.Update( elapsed );

        // Delete objects
        ObjectTable.EmptyTrash();

        // Update objects
        foreach ( GameObject obj in ObjectTable.AllObjects )
          obj.Update( gameTime );

        //Geometry.Player  = ObjectTable.GetObjects<Player>()[0];
        //Geometry.TestObj = ObjectTable.GetObjects<TestObject>()[0];
        //if ( Geometry.PolyContains( Geometry.TestObj.Bound.Vertices.ToArray(), Geometry.Player.BoundingCircle.Position ) )
        //  frameCount--;

        frameCount++;
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

      //// Look up inputs for the active player profile.
      //int playerIndex = (int)ControllingPlayer.Value;

      for ( int i = 0; i < Gamer.SignedInGamers.Count; ++i )
      {

        KeyboardState keyboardState = input.CurrentKeyboardStates[i];
        GamePadState gamePadState = input.CurrentGamePadStates[i];

        // The game pauses either if the user presses the pause button, or if
        // they unplug the active gamepad. This requires us to keep track of
        // whether a gamepad was ever plugged in, because we don't want to pause
        // on PC if they are playing with a keyboard and have no gamepad at all!
        bool gamePadDisconnected = !gamePadState.IsConnected &&
                                   input.GamePadWasConnected[i];

        if ( input.IsPauseGame( (PlayerIndex)i ) || gamePadDisconnected )
        {
          ScreenManager.AddScreen( new PauseMenuScreen(), (PlayerIndex)i );
        }
        else
        {
          PhysCircle circle = ObjectTable.GetObjects<Player>()[i].BoundingCircle;

          float maxVelX = 8f;
          float forceScale = ( circle.Touching != null ) ? 300f : 150f;

          Vector2 leftStick = gamePadState.ThumbSticks.Left;
          if ( leftStick.X != 0f )
          {
            // ignore input if moving at or faster than max velocity
            if ( leftStick.X * circle.Velocity.X < 0f || Math.Abs( circle.Velocity.X ) < maxVelX )
              circle.Force += new Vector2( forceScale * leftStick.X, 0f );
          }
          if ( leftStick.Y != 0f )
          {
            // ignore input if moving at or faster than max velocity
            if ( leftStick.Y * circle.Velocity.Y < 0f || Math.Abs( circle.Velocity.Y ) < maxVelX )
              circle.Force += new Vector2( 0f, forceScale * leftStick.Y );
          }
        }
      }
    }


    /// <summary>
    /// Draws the gameplay screen.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      ScreenManager.GraphicsDevice.Clear( ClearOptions.Target,
                                          Color.CornflowerBlue, 0, 0 );

      Projection = Matrix.CreatePerspectiveFieldOfView( Camera.Fov, Camera.Aspect,
                                                  Camera.Near, Camera.Far );
      View = Matrix.CreateLookAt( Camera.Position, Camera.Target, Camera.Up );

      foreach ( GameObject obj in ObjectTable.AllObjects )
        obj.Draw();

      // Our player and enemy are both actually just text strings.
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      spriteBatch.Begin();

      //double fps = 1f / gameTime.ElapsedGameTime.TotalSeconds;
      //spriteBatch.DrawString( gameFont, "X Velocity: " + players[0].BoundingCircle.Velocity.X.ToString(), 
      //                        new Vector2( 100f, 100f ), Color.Black );
      //string debugString = "Bodies: " + PhysBody.AllBodies.Count.ToString();
      //string debugString = players[0].BoundingCircle.Position.ToString();
      //string debugString = players[0].BoundingCircle.Velocity.ToString() + '\n' + players[0].BoundingCircle.Position.ToString();
      //string debugString = "Floor Blocks: " + ObjectTable.GetObjects<FloorBlock>().Count.ToString();
      //spriteBatch.DrawString( gameFont, debugString, new Vector2( 100f, 100f ), Color.BlanchedAlmond );

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

      // trap doors and players
      float doorPosY = CameraInfo.DeathLine - 2f * Player.Scale;
      float doorPosX = leftBoundary.X;
      float doorPosXStep = stageWidth / 3f - FloorBlock.Scale / 3f;

      Vector2 doorPos = new Vector2( doorPosX, doorPosY );
      for ( int i = 0; i < 4; ++i )
      {
        Basket door = new Basket( this, doorPos );
        ObjectTable.Add( door );

        if ( i < Gamer.SignedInGamers.Count )
        {
          Vector2 playerPos = doorPos;
          playerPos.Y += Player.Scale;
          Player player = new Player( this, (PlayerIndex)i, playerPos );
          ObjectTable.Add( player );
        }

        doorPos.X += doorPosXStep;
      }

      // other
      CameraInfo.ScrollSpeed = 0f;
    }

    private void InitCamera()
    {
      float fov = MathHelper.ToRadians( 30f );
      float aspect = ScreenManager.GraphicsDevice.DisplayMode.AspectRatio;
      Camera = new Camera( fov, aspect, 5f, 100f, new Vector3( 0f, 0f, 16f ), Vector3.Zero );

      // set relative spawn and kill lines for spawning and killing rows of blocks
      Viewport viewport = this.ScreenManager.Game.GraphicsDevice.Viewport;
      Vector3 nearScreen = new Vector3( viewport.Width / 2f, viewport.Height, 0f );
      Vector3 farScreen = new Vector3( viewport.Width / 2f, viewport.Height, 1f );

      // unproject lower screen point on near plane and far plan
      Matrix proj = Matrix.CreatePerspectiveFieldOfView( Camera.Fov, Camera.Aspect, Camera.Near, Camera.Far );
      Matrix view = Matrix.CreateLookAt( Camera.Position, Camera.Target, Camera.Up );
      Matrix world = Matrix.Identity;
      Vector3 nearWorld = viewport.Unproject( nearScreen, proj, view, world );
      Vector3 farWorld = viewport.Unproject( farScreen, proj, view, world );

      // intersect ray with vertical plane aligned with backs of blocks
      Ray ray = new Ray( nearWorld, farWorld - nearWorld );
      ray.Direction.Normalize();
      float? dist = ray.Intersects( new Plane( 0f, 0f, 1f, FloorBlock.Scale / 2f ) );

      if ( dist == null )
        throw new NullReferenceException( "Ray does not intersect with plane." );

      float birthLine = ( dist.Value * ray.Direction + ray.Position ).Y - FloorBlock.Scale / 8f;
      float deathLine = -birthLine;

      CameraInfo = new CameraInfo( birthLine, deathLine, -.2f );
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
          ObjectTable.Add( new FloorBlock( this, blockPos ) );

        blockPos.X += xStep;
      }
    }

    private void UpdateStage()
    {

      float screenLine = CameraInfo.BirthLine + Camera.Position.Y;

      // spawn rows
      while ( lastRowY > screenLine )
      {
        SpawnRow( lastRowY, 50, 70 );
        lastRowY -= rowSpacing;
      }
    }

    private void UpdateCamera( float elapsed )
    {
      // Camera scroll
      Camera.Translate( new Vector3( 0f, CameraInfo.ScrollSpeed * (float)elapsed, 0f ) );
    }

    #endregion
  }

  public class CameraInfo
  {
    public float BirthLine { get; set; }
    public float DeathLine { get; set; }
    public float ScrollSpeed { get; set; }

    public CameraInfo( float birthLine, float deathLine, float scrollSpeed )
    {
      BirthLine = birthLine;
      DeathLine = deathLine;
      ScrollSpeed = scrollSpeed;
    }
  }
}
