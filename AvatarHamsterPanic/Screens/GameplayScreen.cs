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
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using Physics;
using AvatarHamsterPanic.Objects;
using Utilities;
using System.Collections.Generic;
using MathLib;
using Microsoft.Xna.Framework.Audio;
using CustomAvatarAnimationFramework;
using System.Collections.ObjectModel;
using System.Diagnostics;
using InstancedModelSample;
using System.Text;
using Particle3DSample;
using CustomModelSample;
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// This screen implements the actual game logic.
  /// </summary>
  public class GameplayScreen : GameScreen
  {
    #region Fields and Properties

    public static GameplayScreen Instance { get; private set; }

    public ContentManager Content { get; private set; }
    public Camera Camera { get; private set; }
    public float CameraScrollSpeed { get { return camScrollSpeed; } }
    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public ObjectTable<GameObject> ObjectTable { get; private set; }
    public float CountdownTime { get; set; }
    public float CountdownEnd { get; set; }
    public Rectangle SafeRect { get; private set; }
    public ParticleManager ParticleManager { get; private set; }
    public static StringBuilder DebugString { get; set; }
    public PixieParticleSystem PixieParticleSystem { get; set; }
    public SparkParticleSystem SparkParticleSystem { get; set; }
    public PinkPixieParticleSystem PinkPixieParticleSystem { get; set; }
    public GameTime LastGameTime { get; private set; }

    TubeMaze tubeMaze;
    SpriteFont gameFont;
    float lastRowY = 0f;
    float lastCamY = 0f;
    float rowSpacing = 5f * FloorBlock.Size / 2f;
    float stageWidth = FloorBlock.Size * 8f;
    int lastRowPattern = int.MaxValue;
    SlotState[] initSlotInfo;
    bool firstFrame;
    float camScrollSpeed = 0;//-1.25f;
    bool camIsScrolling = false;
    Rectangle backgroundRect;
    Texture2D backgroundTexture;

    long physicsTicks;
    long updateTicks;

    RenderTarget2D basicSceneRenderTarget;
    RenderTarget2D maskRenderTarget;
    Rectangle screenRectangle;
    GameComponentCollection components = new GameComponentCollection();

    Random random = new Random();

    #endregion

    #region Initialization

    static GameplayScreen()
    {
      DebugString = new StringBuilder( 100 );
    }


    /// <summary>
    /// Constructor.
    /// </summary>
    public GameplayScreen( SlotState[] slots )
    {
      TransitionOnTime = TimeSpan.FromSeconds( 1.5 );
      TransitionOffTime = TimeSpan.FromSeconds( 0.5 );
      initSlotInfo = slots;

      Instance = this;
    }
    
    /// <summary>
    /// Load graphics content for the game.
    /// </summary>
    public override void LoadContent()
    {
      if ( Content == null )
        Content = new ContentManager( ScreenManager.Game.Services, "Content" );

      // render targets
      GraphicsDevice device = ScreenManager.GraphicsDevice;
      PostProcessor.Initialize( device, ScreenManager.SpriteBatch, Content );
      PresentationParameters pars = device.PresentationParameters;
      basicSceneRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth, pars.BackBufferHeight, 1, pars.BackBufferFormat );
      maskRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth, pars.BackBufferHeight, 1, SurfaceFormat.Bgr32 );

      screenRectangle = new Rectangle( 0, 0, pars.BackBufferWidth, pars.BackBufferHeight );

      // this prevents the game from pausing after the player presses start to exit the loading screen
      firstFrame = true;

      // load fonts
      gameFont = Content.Load<SpriteFont>( "Fonts/gamefont" );
      Content.Load<SpriteFont>( "Fonts/HUDNameFont" );

      Game game = ScreenManager.Game;

      // model explosion particles
      ParticleManager = new ParticleManager( game, Content );
      ParticleManager.Initialize();
      //game.Components.Add( ParticleManager );
      components.Add( ParticleManager );

      // other particles
      PixieParticleSystem = new PixieParticleSystem( game, Content );
      SparkParticleSystem = new SparkParticleSystem( game, Content );
      PinkPixieParticleSystem = new PinkPixieParticleSystem( game, Content );
      //game.Components.Add( PixieParticleSystem );
      //game.Components.Add( SparkParticleSystem );
      //game.Components.Add( PinkPixieParticleSystem );
      components.Add( PixieParticleSystem );
      components.Add( SparkParticleSystem );
      components.Add( PinkPixieParticleSystem );

      foreach ( DrawableGameComponent component in components )
        component.Initialize();

      // pre-load
      LaserBeam.Initialize();
      Content.Load<CustomAvatarAnimationData>( "Animations/Walk" );
      Content.Load<CustomAvatarAnimationData>( "Animations/Run" );
      backgroundTexture = Content.Load<Texture2D>( "Textures/background" );
      int left = -( backgroundTexture.Width - ScreenManager.GraphicsDevice.Viewport.Width ) / 2;
      Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
      if ( left > 0 )
        backgroundRect = new Rectangle( 0, 0, viewport.Width, viewport.Height );
      else
        backgroundRect = new Rectangle( left, 0, backgroundTexture.Width, viewport.Height );

      // init game stuff
      ObjectTable = new ObjectTable<GameObject>();

      // ready, go!
      ObjectTable.Add( new ReadyGo( this, new Vector2( viewport.Width / 2, viewport.Height / 2 ) ) );

      float fov = MathHelper.ToRadians( 30f );
      float aspect = ScreenManager.GraphicsDevice.DisplayMode.AspectRatio;
      Camera = new Camera( fov, aspect, 1f, 100f, new Vector3( 0f, 0f, 20f ), Vector3.Zero );

      FloorBlock.Initialize( this );
      Powerup.Initialize( this );

      lastRowY = rowSpacing - Player.Size * 1.5f;

      InitSafeRectangle();
      InitStage();

      CountdownTime = 0f;
      CountdownEnd = 3f;
      //ObjectTable.Add( new OneByOneByOne( this ) );

      lastCamY = Camera.Position.Y;
      SpawnRows(); // spawn additional rows before loading screen is over

      // set gravity
      PhysicsManager.Instance.Gravity = new Vector2( 0f, -5.5f );

      ScreenManager.Game.ResetElapsedTime();
    }

    private void SpawnRows()
    {
      while ( lastRowY > FloorBlock.BirthLine + Camera.Position.Y )
      {
        lastRowY -= rowSpacing;
        SpawnRow( lastRowY, 58, 83 );
      }
    }


    /// <summary>
    /// Unload graphics content used by the game.
    /// </summary>
    public override void UnloadContent()
    {
      PhysBody.AllBodies.Clear();
      LaserBeam.Unload();
      ObjectTable.Clear();
      Game game = ScreenManager.Game;
      game.Components.Remove( ParticleManager );
      game.Components.Remove( PixieParticleSystem );
      game.Components.Remove( SparkParticleSystem );
      game.Components.Remove( PinkPixieParticleSystem );
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

      LastGameTime = gameTime;

      if ( IsActive )
      {
        long updateBegin = Stopwatch.GetTimestamp();

        foreach ( GameComponent component in components )
          component.Update( gameTime );

        double elapsed = gameTime.ElapsedGameTime.TotalSeconds;

        long begin = Stopwatch.GetTimestamp();
        // Update physics
        PhysicsManager.Instance.Update( elapsed );
        physicsTicks += Stopwatch.GetTimestamp() - begin;

        Projection = Matrix.CreatePerspectiveFieldOfView( Camera.Fov, Camera.Aspect,
                                                          Camera.Near, Camera.Far );
        View = Matrix.CreateLookAt( Camera.Position, Camera.Target, Camera.Up );

        ParticleManager.SetCamera( Camera.Position, View, Projection );
        PixieParticleSystem.SetCamera( View, Projection );
        SparkParticleSystem.SetCamera( View, Projection );
        PinkPixieParticleSystem.SetCamera( View, Projection );

        // avoid scrolling the camera while the countdown is running
        if ( CountdownTime < CountdownEnd )
          CountdownTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateCamera( (float)elapsed );

        SpawnRows();

        // Delete objects
        ObjectTable.EmptyTrash();

        // Update objects
        ReadOnlyCollection<GameObject> objects = ObjectTable.AllObjects;
        int nObjects = objects.Count;
        for ( int i = 0; i < nObjects; ++i )
          objects[i].Update( gameTime );

        Pool.CleanUpAll();

        updateTicks += Stopwatch.GetTimestamp() - updateBegin;
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

      if ( firstFrame )
      {
        firstFrame = false;
        return;
      }

      // The game pauses either if the user presses the pause button, or if
      // they unplug the active gamepad. This requires us to keep track of
      // whether a gamepad was ever plugged in, because we don't want to pause
      // on PC if they are playing with a keyboard and have no gamepad at all!
      bool gamePadDisconnected = false;

      for ( int i = 0; i < 4; ++i )
      {
        if ( input.LastGamePadStates[i].IsConnected && !input.CurrentGamePadStates[i].IsConnected )
        {
          gamePadDisconnected = true;
          break;
        }
      }

      if ( input.IsPauseGame( null ) || gamePadDisconnected )
      {
        ScreenManager.AddScreen( new PauseMenuScreen(), null );
      }
      else
      {
        ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
        int nPlayers = players.Count;
        for ( int i = 0; i < nPlayers; ++i )
          players[i].HandleInput( input );
      }
    }


    /// <summary>
    /// Draws the gameplay screen.
    /// </summary>
    public override void Draw( GameTime gameTime )
    {
      GraphicsDevice device = ScreenManager.GraphicsDevice;
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      device.SetRenderTarget( 0, basicSceneRenderTarget );
      device.SetRenderTarget( 1, maskRenderTarget );

      device.Clear( ClearOptions.Target, Color.TransparentBlack, 0, 0 );

      // draw background texture
      spriteBatch.Begin();
      spriteBatch.Draw( backgroundTexture, backgroundRect, Color.White );
      spriteBatch.End();

      // draw 3D geometry
      List<GameObject> objects = ObjectTable.AllObjectsList;
      objects.Sort( ( a, b ) => a.DrawOrder.CompareTo( b.DrawOrder ) );
      foreach ( GameObject obj in objects )
        obj.Draw();

      foreach ( DrawableGameComponent component in components )
        component.Draw( gameTime );

      //DrawSafeRect( device );

      device.SetRenderTarget( 0, null );
      device.SetRenderTarget( 1, null );

      Texture2D scene = basicSceneRenderTarget.GetTexture();
      Texture2D mask  = maskRenderTarget.GetTexture();

      //post processing here
      Texture2D glow = PostProcessor.Glow( scene, mask );
      //Texture2D motionBlur = PostProcessor.MotionBlur( scene, mask );

      // render scene to backbuffer
      spriteBatch.Begin( SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None );
      spriteBatch.Draw( scene, Vector2.Zero, Color.White );
      spriteBatch.End();

      Rectangle haxOffset = screenRectangle;
      haxOffset.Location = new Point( haxOffset.Location.X - 6, haxOffset.Location.Y - 6 );
      spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );
      spriteBatch.Draw( glow, haxOffset, Color.White );
      //spriteBatch.Draw( motionBlur, screenRectangle, Color.White );
      Draw2D( spriteBatch );
      spriteBatch.End();

      // If the game is transitioning on or off, fade it out to black.
      if ( TransitionPosition > 0 )
        ScreenManager.FadeBackBufferToBlack( 255 - TransitionAlpha );

      Performance.Update( gameTime.ElapsedGameTime );
      Performance.CountFrame();
    }

    private void Draw2D( SpriteBatch spriteBatch )
    {
      // player HUDs
      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
      int nPlayers = players.Count;
      for ( int i = 0; i < nPlayers; ++i )
        players[i].HUD.Draw();

      ReadOnlyCollection<ReadyGo> readyGoes = ObjectTable.GetObjects<ReadyGo>();
      if ( readyGoes != null && readyGoes.Count != 0 )
        readyGoes[0].Draw2D();

      // debugging stuff
      DebugString.Clear();
      Vector2 position = new Vector2( 20, 20 );
      spriteBatch.DrawString( gameFont, FormatDebugString(), position, Color.Tomato );
      //spriteBatch.DrawString( gameFont, DebugString.AppendInt( Performance.FrameRate ), position, Color.Black );
      //spriteBatch.DrawString( gameFont, PhysicsManager.DebugString, position, Color.Black );
    }

    string strFrameRate = "FPS: ";
    //string strPhysTicks = "Physics MS: ";
    //string strCricleTicks = "TestVsCircle MS: ";
    //string strPolygonTicks = "TestVsPolygon MS: ";
    string strPolygonPercentage = "Polygon Percentage: ";
    string strCirclePercentage = "Circle Percentage: ";
    string strPhysicsPercentage = "Physics Percentage: ";

    private StringBuilder FormatDebugString()
    {
      DebugString.Clear();

      // framerate
      DebugString.Append( strFrameRate );
      DebugString.AppendInt( Performance.FrameRate );
      DebugString.Append( '\n' );

      //// physics ticks
      //DebugString.Append( strPhysTicks );
      //DebugString.AppendInt( (int)( 1000 * (double)physicsTicks / (double)Stopwatch.Frequency ) );
      //DebugString.Append( '\n' );

      //// test vs circle ticks
      //DebugString.Append( strCricleTicks );
      //DebugString.AppendInt( (int)( 1000 * (double)PhysBody.TestVsCircleTicks / (double)Stopwatch.Frequency ) );
      //DebugString.Append( '\n' );

      //// test vs polygon ticks
      //DebugString.Append( strPolygonTicks );
      //DebugString.AppendInt( (int)( 1000 * (double)PhysBody.TestVsPolygonTicks / (double)Stopwatch.Frequency ) );
      //DebugString.Append( '\n' );
      int updateMS = (int)( 1000 * (double)updateTicks / (double)Stopwatch.Frequency );
      int physMS = (int)( 1000 * (double)physicsTicks / (double)Stopwatch.Frequency );
      int polyMS = (int)( 1000 * (double)PhysBody.TestVsPolygonTicks / (double)Stopwatch.Frequency );
      int circleMS = (int)( 1000 * (double)PhysBody.TestVsCircleTicks / (double)Stopwatch.Frequency );

      // percentage of time Physics is taking
      DebugString.Append( strPhysicsPercentage );
      DebugString.AppendInt( (int)( .5f + 100f * (float)physMS / (float)updateMS ) );
      DebugString.Append( '\n' );

      // percentage of time TestVsCircle is taking
      DebugString.Append( strCirclePercentage );
      DebugString.AppendInt( (int)( .5f + 100f * (float)circleMS / (float)physMS ) );
      DebugString.Append( '\n' );

      // percentage of time TestVsPolygon is taking
      DebugString.Append( strPolygonPercentage );
      DebugString.AppendInt( (int)( .5f + 100f * (float)polyMS / (float)physMS ) );
      DebugString.Append( '\n' );



      return DebugString;
    }

    #endregion

    #region Helpers

    private void InitSafeRectangle()
    {
      GraphicsDevice device = ScreenManager.GraphicsDevice;

      int screenWidth  = device.Viewport.Width;
      int screenHeight = device.Viewport.Height;

      float safeRectAspect = 4f / 3f;

      if ( device.Viewport.AspectRatio > safeRectAspect )
      {
        int rectWidth = (int)( (float)screenHeight * safeRectAspect );
        int rectX = ( screenWidth - rectWidth ) / 2;
        SafeRect = new Rectangle( rectX, 0, rectWidth, screenHeight );
      }
      else
      {
        int rectHeight = (int)( (float)screenWidth / safeRectAspect );
        int rectY = ( screenHeight - rectHeight ) / 2;
        SafeRect = new Rectangle( 0, rectY, screenWidth, rectHeight );
      }
    }

    private void InitStage()
    {
      // add tube maze first
      tubeMaze = new TubeMaze( this, -5f, 2.3f );
      ObjectTable.Add( tubeMaze );

      // side boundaries
      float leftBoundX = -.5f * stageWidth;
      Boundary boundary = new Boundary( this, leftBoundX, -leftBoundX, lastRowY, rowSpacing );
      boundary.DrawOrder = 2;
      ObjectTable.Add( boundary );

      // starting shelves
      Shelves shelves = new Shelves( this );
      ObjectTable.Add( shelves );

      // players
      AddPlayers( shelves );
    }

    private void SpawnRow( float yPos, int lowPct, int hiPct )
    {
      float spacesPerRow = stageWidth / FloorBlock.Size;
      int   nSpaces = (int)Math.Ceiling( (double)spacesPerRow );
      float xStep   = FloorBlock.Size;
      float xStart  = -.5f * ( xStep * spacesPerRow - xStep );
        
      // get random percent that floor is filled
      float pct = 0f;
      if ( lowPct < hiPct )
          pct = (float)random.Next( lowPct, hiPct + 1 ) / 100f;
      else
          pct = (float)lowPct / 100f;

      int nBlocks = (int)( (float)nSpaces * pct );
      Vector2 blockPos = new Vector2( xStart, yPos );
      int curPattern = 0;

      // cover as many holes as possible
      int nBits = nSpaces;
      for ( int i = 0; i < nBits && nBlocks > 0; ++i )
      {
        if ( random.Next( 10 ) < 3 )
          ObjectTable.Add( Powerup.CreatePowerup( blockPos + new Vector2( 0, 1f ), PowerupType.ScoreCoin ) );

        if ( ( lastRowPattern & ( 1 << i ) ) == 0 )
        {
          ObjectTable.Add( FloorBlock.CreateFloorBlock( blockPos ) );
          curPattern |= ( 1 << i );
          nBlocks--;
          nSpaces--;
        }

        blockPos.X += xStep;
      }

      RandomBag.Reset( nSpaces );

      blockPos.X = xStart;
      for ( int i = 0, j = 0; i < nSpaces; ++i )
      {
        while ( ( curPattern & ( 1 << j++ ) ) != 0 )
          blockPos.X += xStep;

        if ( RandomBag.PullNext() < nBlocks )
        {
          ObjectTable.Add( FloorBlock.CreateFloorBlock( blockPos ) );
          curPattern |= ( 1 << ( j - 1 ) );
        }

        blockPos.X += xStep;
      }

      lastRowPattern = curPattern;
    }

    private void AddPlayers( Shelves shelf )
    {
      for ( int i = 0; i < 4; ++i )
      {
        if ( initSlotInfo == null )
        {
          if ( i < Gamer.SignedInGamers.Count )
          {
            Avatar avatar = new Avatar( Gamer.SignedInGamers[i].Avatar, AvatarAnimationPreset.Stand0,
                                        Player.Size, Vector3.UnitX, Vector3.Zero );
            ObjectTable.Add( new Player( this, i, (PlayerIndex)i, avatar, shelf.GetPlayerPos( i ) ) );
          }
        }
        else if ( i < initSlotInfo.Length && initSlotInfo[i].Avatar != null )
        {
          initSlotInfo[i].Avatar.Scale = Player.Size;
          ObjectTable.Add( new Player( this, i, initSlotInfo[i].Player, initSlotInfo[i].Avatar, 
                                       shelf.GetPlayerPos( i ) ) );
        }
      }
    }

    private void UpdateCamera( float elapsed )
    {
      if ( elapsed == 0f ) return;

      float scrollLine  = .2f * FloorBlock.BirthLine;  // camera will be pulled by a spring
      float scrollLine2 = .7f * FloorBlock.BirthLine;  // camera will be snapped down

      float prevY = Camera.Position.Y;

      // get the body of the lowest player
      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
      PhysCircle lowestPlayer = null;
      int nPlayers = players.Count;
      for ( int i = 0; i < nPlayers; ++i )
      {
        PhysCircle playerCircle = players[i].BoundingCircle;
        if ( lowestPlayer == null || playerCircle.Position.Y < lowestPlayer.Position.Y )
          lowestPlayer = playerCircle;
      }

      // start scrolling camera if needed
      if ( !camIsScrolling && lowestPlayer != null && lowestPlayer.Position.Y < 0f )
        camIsScrolling = true;

      // scroll camera
      float begCamPos = Camera.Position.Y;

      if ( lowestPlayer.Position.Y < begCamPos + scrollLine )
      {
        if ( lowestPlayer.Position.Y > begCamPos + scrollLine2 )
        {
          float k = 65f;
          float b = -(float)Math.Sqrt( (double)( 4f * k ) );
          float x = ( begCamPos + scrollLine ) - lowestPlayer.Position.Y;
          float vel0 = ( begCamPos - lastCamY ) / elapsed;
          float accel = -k * x + vel0 * b;
          float vel = vel0 + accel * elapsed;
          float trans = Math.Min( vel * elapsed, camScrollSpeed * (float)elapsed );
          Camera.Translate( new Vector3( 0f, trans, 0f ) );
        }
        else
        {
          Camera.Translate( new Vector3( 0f, lowestPlayer.Position.Y - ( begCamPos + scrollLine2 ), 0f ) );
        }
      }
      else if ( camIsScrolling )
      {
        Camera.Translate( new Vector3( 0f, camScrollSpeed * (float)elapsed, 0f ) );
      }
    
      lastCamY = begCamPos;
    }

#if DEBUG
    private void DrawSafeRect( GraphicsDevice device )
    {
      Effect debugEffect = Content.Load<Effect>( "Effects/debugLine" );
      debugEffect.CurrentTechnique = debugEffect.Techniques[0];
      debugEffect.Begin();
      debugEffect.Parameters["ScreenWidth"].SetValue( device.Viewport.Width );
      debugEffect.Parameters["ScreenHeight"].SetValue( device.Viewport.Height );

      Rectangle safeRect = SafeRect;

      device.VertexDeclaration = new VertexDeclaration( device, VertexPositionColor.VertexElements );
      VertexPositionColor[] safeRectVerts = 
      {
        new VertexPositionColor( new Vector3( safeRect.X, safeRect.Y, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( safeRect.X, safeRect.Y + safeRect.Height - 1, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( safeRect.X + safeRect.Width, safeRect.Y + safeRect.Height - 1, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( safeRect.X + safeRect.Width, safeRect.Y, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( safeRect.X, safeRect.Y, 0 ), Color.Red ),
      };

      foreach ( EffectPass pass in debugEffect.CurrentTechnique.Passes )
      {
        pass.Begin();
        device.DrawUserPrimitives( PrimitiveType.LineStrip, safeRectVerts, 0, 4 );
        pass.End();
      }
      debugEffect.End();
    }
#endif

    #endregion
  }
}
