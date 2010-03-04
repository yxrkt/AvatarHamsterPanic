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
using MathLibrary;
using Microsoft.Xna.Framework.Audio;
using CustomAvatarAnimationFramework;
using System.Collections.ObjectModel;
using System.Diagnostics;
using InstancedModelSample;
using System.Text;
using Particle3DSample;
using CustomModelSample;
using Graphics;
using Audio;
using AvatarHamsterPanic;
#endregion

namespace Menu
{
  /// <summary>
  /// This screen implements the actual game logic.
  /// </summary>
  public class GameplayScreen : GameScreen
  {
    #region Fields and Properties

    static GameplayScreen instance = null;
    static GameplayScreen nextInstance = null;
    public static GameplayScreen Instance
    {
      get { return instance; }
      set
      {
        if ( instance != null )
          nextInstance = value;
        else
          instance = value;
      }
    }

    public float AccumulatedTime { get; private set; }
    public Cue BackgroundMusic { get; internal set; }
    public ContentManager Content { get; private set; }
    public bool CameraIsScrolling { get { return camIsScrolling; } }
    public bool GameOver { get { return gameEndTime != 0; } }
    public float FirstRow { get; private set; }
    public float RowSpacing { get { return rowSpacing; } }
    public PhysicsSpace PhysicsSpace { get; private set; }
    public Camera Camera { get; private set; }
    public float CameraScrollSpeed { get { return camScrollSpeed; } }
    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public ObjectTable<GameObject> ObjectTable { get; private set; }
    public float CountdownTime { get; set; }
    public float CountdownEnd { get; set; }
    public Rectangle SafeRect { get; private set; }
    public ParticleManager ParticleManager { get; private set; }
    public PixieParticleSystem PixieParticleSystem { get; set; }
    public SparkParticleSystem SparkParticleSystem { get; set; }
    public PinkPixieParticleSystem PinkPixieParticleSystem { get; set; }
    public GameTime LastGameTime { get; private set; }
    public Slot[] Slots { get { return initSlotInfo; } }
    public bool ShakeIsOut { get; set; }

    public const int BlocksPerRow = 8;

    const float CelebrationTime = 3f;

    TubeMaze tubeMaze;
    SpriteFont gameFont;
    float lastRowY = 0f;
    float lastCamY = 0f;
    float rowSpacing = 5f * FloorBlock.Size / 2f;
    float stageWidth = FloorBlock.Size * BlocksPerRow;
    int lastRowPattern = int.MaxValue;
    Slot[] initSlotInfo;
    bool firstFrame;
    float camScrollSpeed = -1.25f;
    bool camIsScrolling = false;
    Rectangle backgroundRect;
    Texture2D backgroundTexture;
    float gameEndTime = 0;
    SpringInterpolater winnerSpring;
    Vector3 winnerCameraOffset;
    float cameraDistance = 20;
    float winCameraDistance = 7;
    Player winner;
    bool addedPodium = false;
    ScoreboardMenuScreen scoreboardMenuScreen;
    PauseMenuScreen pauseScreen;

    // Debug
    string updateEntityMark = "Entity Update";
    string updatePhysicsMark = "Physics";
    string drawEntityMark = "Entity Draw";
    string drawPostProcessMark = "Post Processing";
    string drawParticleMark = "Particles Draw";

    RenderTarget2D basicSceneRenderTarget;
    RenderTarget2D maskRenderTarget;
    Rectangle screenRectangle;
    List<DrawableGameComponent> components = new List<DrawableGameComponent>();

    Random random = new Random();

    #endregion

    #region Initialization

    static GameplayScreen()
    {
    }


    /// <summary>
    /// Constructor.
    /// </summary>
    public GameplayScreen( Slot[] slots )
    {
      TransitionOnTime = TimeSpan.FromSeconds( .5 );
      TransitionOffTime = TimeSpan.FromSeconds( .75 );
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

      Game game = ScreenManager.Game;

      // initialize physics
      PhysicsSpace = new PhysicsSpace();
      PhysicsSpace.Gravity = new Vector2( 0f, -5.5f );

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

      // load screens ahead of time
      scoreboardMenuScreen = new ScoreboardMenuScreen( ScreenManager, initSlotInfo );
      pauseScreen = new PauseMenuScreen( ScreenManager );

      // model explosion particles
      ParticleManager = new ParticleManager( game, Content );
      ParticleManager.Initialize();
      components.Add( ParticleManager );

      // other particles
      PixieParticleSystem = new PixieParticleSystem( game, Content );
      SparkParticleSystem = new SparkParticleSystem( game, Content );
      PinkPixieParticleSystem = new PinkPixieParticleSystem( game, Content );
      components.Add( PixieParticleSystem );
      components.Add( SparkParticleSystem );
      components.Add( PinkPixieParticleSystem );

      foreach ( DrawableGameComponent component in components )
        component.Initialize();

      // pre-load
      LaserBeam.Initialize();
      Content.Load<CustomAvatarAnimationData>( "Animations/Walk" );
      Content.Load<CustomAvatarAnimationData>( "Animations/Run" );
      backgroundTexture = Content.Load<Texture2D>( "Textures/gameBackground" );
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
      Camera = new Camera( fov, aspect, 1f, 100f, new Vector3( 0f, 0f, cameraDistance ), Vector3.Zero );

      winnerSpring = new SpringInterpolater( 1, 10, SpringInterpolater.GetCriticalDamping( 10 ) );
      winnerSpring.SetSource( 1 );
      winnerSpring.SetDest( 0 );

      FloorBlock.Initialize( this );
      Powerup.Initialize( this );

      lastRowY = rowSpacing - Player.Size * 1.5f;

      InitSafeRectangle();
      InitStage();

      CountdownTime = 0f;
      CountdownEnd = 3f;

      lastCamY = Camera.Position.Y;
      SpawnRows(); // spawn additional rows before loading screen is over

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
      LaserBeam.Unload();
      PlayerAI.RemoveAllRows();
      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
      for ( int i = 0; i < players.Count; ++i )
        players[i].OnDestruct();
      ObjectTable.Clear();
      Game game = ScreenManager.Game;
      ParticleManager.Unload();
      components.Remove( ParticleManager );
      components.Remove( PixieParticleSystem );
      components.Remove( SparkParticleSystem );
      components.Remove( PinkPixieParticleSystem );
      Content.Unload();

      if ( BackgroundMusic != null )
      {
        BackgroundMusic.Dispose();
        BackgroundMusic = null;
      }

      if ( nextInstance != null )
      {
        instance = nextInstance;
        nextInstance = null;
      }
      else
      {
        instance = null;
      }
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

      if ( IsActive || ( ScreenState == ScreenState.TransitionOff && GameOver ) )
      {
        double elapsed = Math.Min( gameTime.ElapsedGameTime.TotalSeconds, 1.0 / 30.0 );

        if ( BackgroundMusic == null )
          BackgroundMusic = GameCore.Instance.AudioManager.Play2DCue( "banjoBreakdown", 1f );

        Projection = Matrix.CreatePerspectiveFieldOfView( Camera.Fov, Camera.Aspect,
                                                          Camera.Near, Camera.Far );
        View = Matrix.CreateLookAt( Camera.Position, Camera.Target, Camera.Up );

        ParticleManager.SetCamera( Camera.Position, View, Projection );
        PixieParticleSystem.SetCamera( View, Projection );
        SparkParticleSystem.SetCamera( View, Projection );
        PinkPixieParticleSystem.SetCamera( View, Projection );

        foreach ( DrawableGameComponent component in components )
          component.Update( gameTime );

        GameCore.Instance.TimeRuler.BeginMark( 0, updatePhysicsMark, Color.Purple );
        PhysicsSpace.Update( elapsed );
        GameCore.Instance.TimeRuler.EndMark( 0, updatePhysicsMark );

        // avoid scrolling the camera while the countdown is running
        if ( CountdownTime < CountdownEnd )
          CountdownTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateCamera( (float)elapsed );
        AudioListener listener = GameCore.Instance.AudioManager.Listener;
        listener.Position = Camera.Position;
        listener.Forward = Vector3.Forward;
        listener.Up = Camera.Up;
        listener.Velocity = Vector3.Zero;

        SpawnRows();

        if ( GameOver && !addedPodium )
        {
          if ( gameEndTime > CelebrationTime )
          {
            BackgroundMusic.Dispose();
            BackgroundMusic = null;
            TransitionToScoreboard();
            addedPodium = true;
          }
          else
          {
            gameEndTime += (float)elapsed;
          }
        }
        else if ( GameOver )
        {
          BackgroundMusic.SetVariable( "Volume", XACTHelper.GetDecibels( 1 - TransitionPosition ) );
        }

        GameCore.Instance.TimeRuler.BeginMark( 0, updateEntityMark, Color.Red );
        ReadOnlyCollection<GameObject> objects = ObjectTable.AllObjects;
        int nObjects = objects.Count;
        for ( int i = 0; i < nObjects; ++i )
          objects[i].Update( gameTime );
        GameCore.Instance.TimeRuler.EndMark( 0, updateEntityMark );

        // Cleanup
        ObjectTable.EmptyTrash();
        Pool.CleanUpAll();

        AccumulatedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
      }
    }

    Func<Powerup, bool> powerupIsShake = ( p => p.Type == PowerupType.GoldenShake );

    public void EndGame()
    {
      winnerSpring.Active = true;

      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
      for ( int i = 0; i < players.Count; ++i )
      {
        Player player = players[i];
        if ( player.Powerup != null && player.Powerup.Type == PowerupType.GoldenShake )
        {
          player.WinState = PlayerWinState.Win;
          winnerCameraOffset = Camera.Target - new Vector3( player.BoundingCircle.Position, 0 );
          winner = player;
        }
        else
        {
          player.WinState = PlayerWinState.Lose;
        }
      }
      gameEndTime += (float)LastGameTime.ElapsedGameTime.TotalSeconds;
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

      if ( ( input.IsPauseGame( null ) || gamePadDisconnected ) && ( ScreenState == ScreenState.Active ) && !GameOver )
      {
        GameCore.Instance.AudioManager.Play2DCue( "pause", 1f );
        ScreenManager.AddScreen( pauseScreen, null );
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

      GameCore.Instance.TimeRuler.BeginMark( 1, drawEntityMark, Color.OrangeRed );
      List<GameObject> objects = ObjectTable.AllObjectsList;
      objects.Sort( ( a, b ) => a.DrawOrder.CompareTo( b.DrawOrder ) );
      foreach ( GameObject obj in objects )
        obj.Draw();
      GameCore.Instance.TimeRuler.EndMark( 1, drawEntityMark );

      // particles
      GameCore.Instance.TimeRuler.BeginMark( 1, drawParticleMark, Color.White );
      foreach ( DrawableGameComponent component in components )
        component.Draw( gameTime );
      GameCore.Instance.TimeRuler.EndMark( 1, drawParticleMark );

      //DrawSafeRect( device );

      device.SetRenderTarget( 0, null );
      device.SetRenderTarget( 1, null );

      Texture2D scene = basicSceneRenderTarget.GetTexture();
      Texture2D mask = maskRenderTarget.GetTexture();

      //post processing here
      GameCore.Instance.TimeRuler.BeginMark( 1, drawPostProcessMark, Color.YellowGreen );
      Texture2D glow = PostProcessor.Glow( scene, mask );
      //Texture2D motionBlur = PostProcessor.MotionBlur( scene, mask );
      GameCore.Instance.TimeRuler.EndMark( 1, drawPostProcessMark );

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

      device.RenderState.AlphaTestEnable = false;
      device.RenderState.DepthBufferEnable = true;
      device.RenderState.DepthBufferWriteEnable = true;

      // If the game is transitioning on or off, fade it out to black.
      if ( TransitionPosition > 0 && !GameOver )
        ScreenManager.FadeBackBufferToBlack( 255 - TransitionAlpha );
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
      Vector2 position = new Vector2( 20, 20 );
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
      FirstRow = lastRowY;
      Boundary boundary = new Boundary( this, leftBoundX, -leftBoundX, FirstRow, rowSpacing );
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
          ObjectTable.Add( Powerup.CreatePowerup( blockPos + new Vector2( 0, 1f ), PowerupType.Shake ) );

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

      PlayerAI.AddRow( blockPos.Y, curPattern );

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
            ObjectTable.Add( new Player( this, i, (PlayerIndex)i, avatar, shelf.GetPlayerPos( i ), 0 ) );
          }
        }
        else if ( i < initSlotInfo.Length && initSlotInfo[i].Avatar != null )
        {
          initSlotInfo[i].Avatar.Scale = Player.Size;
          ObjectTable.Add( new Player( this, i, initSlotInfo[i].Player, initSlotInfo[i].Avatar,
                                       shelf.GetPlayerPos( i ), initSlotInfo[i].ID ) );
        }
      }
    }

    private void UpdateCamera( float elapsed )
    {
      if ( GameOver )
        UpdateWinCamera( elapsed );
      else
        UpdateCameraScroll( elapsed );
    }

    private void UpdateCameraScroll( float elapsed )
    {
      if ( elapsed == 0f ) return;

      float scrollLine = .2f * FloorBlock.BirthLine;  // camera will be pulled by a spring
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

    private void UpdateWinCamera( float elapsed )
    {
      if ( elapsed > 1f / 30f )
        elapsed = 1f / 30f;

      float u = winnerSpring.GetSource()[0];
      Camera.Target = new Vector3( winner.BoundingCircle.Position, 0 ) + winnerCameraOffset * u;
      Camera.Position = Camera.Target + Vector3.UnitZ * MathHelper.Lerp( cameraDistance, winCameraDistance, 1 - u );

      winnerSpring.Update( elapsed );
    }

    private void TransitionToScoreboard()
    {
      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();

      Player[] playersInPlace = new Player[players.Count];

      for ( int i = 0; i < players.Count; ++i )
        playersInPlace[i] = players.First( p => p.PodiumPlace == ( i + 1 ) );

      // if we add the players in the same loop, the number of wins changes,
      // which is considered when sorting the players.
      for ( int i = 0; i < players.Count; ++i )
      {
        Player player = playersInPlace[i];
        SignedInGamer gamer = (int)player.PlayerIndex < 0 ? null : SignedInGamer.SignedInGamers[player.PlayerIndex];
        scoreboardMenuScreen.SetPlayer( i, player.PlayerNumber, player.Avatar, gamer,
                                        player.HUD.TotalScore, player.ID );
      }

      ScreenManager.AddScreen( scoreboardMenuScreen, null );
    }

    #endregion
  }
}
