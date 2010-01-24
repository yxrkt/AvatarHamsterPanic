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
using System.Collections.Generic;
using MathLib;
using Microsoft.Xna.Framework.Audio;
using CustomAvatarAnimationFramework;
using System.Collections.ObjectModel;
using System.Diagnostics;
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// This screen implements the actual game logic.
  /// </summary>
  public class GameplayScreen : GameScreen
  {
    #region Fields and Properties

    public ContentManager Content { get; private set; }
    public Camera Camera { get; private set; }
    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public ObjectTable<GameObject> ObjectTable { get; private set; }
    public float CountdownTime { get; set; }
    public float CountdownEnd { get; set; }
    public Rectangle SafeRect { get; private set; }
    public static string DebugString { get; set; }

    TubeMaze tubeMaze;
    SpriteFont gameFont;
    float lastRowY = 0f;
    float lastCamY = 0f;
    float rowSpacing = 5f * FloorBlock.Size / 2f;
    float stageWidth = FloorBlock.Size * 8f;
    int lastRowPattern = int.MaxValue;
    SlotState[] initSlotInfo;
    bool firstFrame;
    float camScrollSpeed = -1.25f;

    double totalPhysTime = 0;
    int nFrames = 0;
    double avgPhysUpdate = 0;
    double longestPhysUpdate = 0;

    Random random = new Random();

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public GameplayScreen( SlotState[] slots )
    {
      TransitionOnTime = TimeSpan.FromSeconds( 1.5 );
      TransitionOffTime = TimeSpan.FromSeconds( 0.5 );
      initSlotInfo = slots;
    }


    /// <summary>
    /// Load graphics content for the game.
    /// </summary>
    public override void LoadContent()
    {
      if ( Content == null )
        Content = new ContentManager( ScreenManager.Game.Services, "Content" );

      firstFrame = true;
      gameFont = Content.Load<SpriteFont>( "Fonts/gamefont" );
      Content.Load<SpriteFont>( "Fonts/HUDNameFont" );

      // pre-load
      Content.Load<CustomAvatarAnimationData>( "Animations/Walk" );
      Content.Load<CustomAvatarAnimationData>( "Animations/Run" );
      Content.Load<Model>( "Models/wheel" );
      Content.Load<Model>( "Models/block" );
      Content.Load<Model>( "Models/block_broken" );
      Content.Load<Model>( "Models/basket" );
      Content.Load<Effect>( "Effects/warp" );

      // init game stuff
      ObjectTable = new ObjectTable<GameObject>();

      float fov = MathHelper.ToRadians( 30f );
      float aspect = ScreenManager.GraphicsDevice.DisplayMode.AspectRatio;
      Camera = new Camera( fov, aspect, 1f, 100f, new Vector3( 0f, 0f, 20f ), Vector3.Zero );

      FloorBlock.Initialize( Camera );

      InitSafeRectangle();
      InitStage();

      // the background tube-maze
      tubeMaze = new TubeMaze( this, -5f, 2.3f );
      ObjectTable.Add( tubeMaze );

      CountdownTime = 0f;
      CountdownEnd = 3f;

      lastRowY = 0f;
      lastCamY = Camera.Position.Y;
      SpawnRows(); // spawn additional rows before loading screen is over

      // set gravity
      PhysicsManager.Instance.Gravity = new Vector2( 0f, -5.5f );

      //Thread.Sleep( 5000 );
      effect = Content.Load<Effect>( "Effects/instanced" );
      effect.CurrentTechnique = effect.Techniques["DiffuseColor"];
      Texture2D DiffuseMap = Content.Load<Texture2D>( "Textures/glassDiffuse" );
      Texture2D NormalMap = Content.Load<Texture2D>( "Textures/glassNormal" );
      effect.Parameters["DiffuseMap"].SetValue( DiffuseMap );
      effect.Parameters["NormalMap"].SetValue( NormalMap );

      effectParameterWorld = effect.Parameters["World"];
      effectParameterView = effect.Parameters["View"];
      effectParameterProjection = effect.Parameters["Projection"];
      effectParameterEye = effect.Parameters["Eye"];
      effectParameterVertexCount = effect.Parameters["VertexCount"];
      effectParameterTransforms = effect.Parameters["InstanceTransforms"];
      blockModel = Content.Load<Model>( "Models/block" );

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
      // if bodies aren't cleared, objects containing them won't be destroyed
      PhysBody.AllBodies.Clear();
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
        double elapsed = /**/gameTime.ElapsedGameTime.TotalSeconds/*/1.0 / 60.0/**/;

        // Update physics
        long tick = Stopwatch.GetTimestamp();
        PhysicsManager.Instance.Update( elapsed );
        double physElapsed = ( (double)( Stopwatch.GetTimestamp() - tick ) / (double)Stopwatch.Frequency );
        if ( physElapsed > longestPhysUpdate )
          longestPhysUpdate = physElapsed;
        totalPhysTime += physElapsed;
        nFrames++;
        avgPhysUpdate = totalPhysTime / (double)nFrames;

        // avoid scrolling the camera while the countdown is running
        if ( CountdownTime < CountdownEnd )
        {
          if ( CountdownTime >= .75f * CountdownEnd )
          {
            float warpDuration = CountdownEnd - CountdownTime;
            ReadOnlyCollection<Basket> baskets = ObjectTable.GetObjects<Basket>();
            foreach ( Basket basket in baskets )
              basket.WarpOut( warpDuration );
          }

          CountdownTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        else
        {
          UpdateCamera( (float)elapsed );
        }

        SpawnRows();

        // Delete objects
        ObjectTable.EmptyTrash();

        // Update objects
        foreach ( GameObject obj in ObjectTable.AllObjects )
          obj.Update( gameTime );

        Performance.Update( gameTime.ElapsedGameTime );
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
        foreach ( Player player in ObjectTable.GetObjects<Player>() )
          player.HandleInput( input );
      }
    }

    Matrix[] blockTransforms = new Matrix[59];
    Effect effect;
    EffectParameter effectParameterWorld;
    EffectParameter effectParameterView;
    EffectParameter effectParameterProjection;
    EffectParameter effectParameterEye;
    EffectParameter effectParameterVertexCount;
    EffectParameter effectParameterTransforms;
    Model blockModel;


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

      GraphicsDevice device = ScreenManager.GraphicsDevice;
      VertexElement[] elements = VertexPositionNormalTextureTangentBinormal.VertexElements;
      device.VertexDeclaration = new VertexDeclaration( device, elements );

      // draw non-particles
      foreach ( GameObject obj in ObjectTable.AllObjects )
      {
        if ( !( obj is ParticleEmitter ) )
          obj.Draw();
      }

      // DRAW FLOORBLOCKSEN
      ReadOnlyCollection<FloorBlock> blocks = ObjectTable.GetObjects<FloorBlock>();
      int nBlocks = blocks.Count;
      for ( int i = 0; i < nBlocks; ++i )
        blocks[i].GetTransform( out blockTransforms[i] );

      effectParameterEye.SetValue( Camera.Position );
      effectParameterView.SetValue( View );
      effectParameterProjection.SetValue( Projection );
      effectParameterTransforms.SetValue( blockTransforms );

      effect.Begin();
      foreach ( EffectPass pass in effect.CurrentTechnique.Passes )
      {
        pass.Begin();
        foreach ( ModelMesh mesh in blockModel.Meshes )
        {
          foreach ( ModelMeshPart part in mesh.MeshParts )
          {
            device.Vertices[0].SetSource( mesh.VertexBuffer, part.StreamOffset, part.VertexStride );
            device.Indices = mesh.IndexBuffer;

            effectParameterVertexCount.SetValue( part.NumVertices );
            effect.CommitChanges();

            device.DrawIndexedPrimitives( PrimitiveType.TriangleList, 0, 0,
                                          2 * part.NumVertices, 0, part.PrimitiveCount * 2 );
          }
        }
        pass.End();
      }
      effect.End();

      // DRAW FLOORBLOCKSEN

      // draw particles
      ReadOnlyCollection<ParticleEmitter> emitters = ObjectTable.GetObjects<ParticleEmitter>();
      foreach ( ParticleEmitter emitter in emitters )
        emitter.Draw();

      DrawSafeRect( device );

      // 2D elements drawn here
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
      spriteBatch.Begin();

      // player HUDs
      ReadOnlyCollection<Player> players = ObjectTable.GetObjects<Player>();
      foreach ( Player player in players )
        player.HUD.Draw();

      //// debugging stuff for physics
      //string physString = "Physics Debug\n";
      //physString += "Slowest: " + ( 1.0 / longestPhysUpdate ).ToString() + "\n";
      //physString += "Average: " + ( 1.0 / avgPhysUpdate ).ToString() + "\n";
      //spriteBatch.DrawString( gameFont, physString, new Vector2( 100, 100 ), Color.Black );
      //spriteBatch.DrawString( gameFont, debug, new Vector2( 100, 100 ), Color.AntiqueWhite );
      string fps = "FrameRate: " + Performance.FrameRate.ToString();
      spriteBatch.DrawString( gameFont, fps + "\n" + DebugString, new Vector2( 20, 20 ), Color.Black );

      spriteBatch.End();

      // If the game is transitioning on or off, fade it out to black.
      if ( TransitionPosition > 0 )
        ScreenManager.FadeBackBufferToBlack( 255 - TransitionAlpha );

      Performance.CountFrame();
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
      // create side boundaries
      float leftBoundX = -.5f * stageWidth;
      ObjectTable.Add( new Boundary( this,  leftBoundX ) );
      ObjectTable.Add( new Boundary( this, -leftBoundX ) );

      // trap doors and players
      float doorPosY = FloorBlock.DeathLine - 2f * Player.Size;
      float doorPosX = leftBoundX;
      float doorPosXStep = stageWidth / 3f - FloorBlock.Size / 3f;

      Vector2 doorPos = new Vector2( doorPosX, 0f );
      for ( int i = 0; i < 4; ++i )
      {
        if ( initSlotInfo == null )
        {
          if ( i < Gamer.SignedInGamers.Count )
          {
            Vector2 playerPos = doorPos;
            playerPos.X += Basket.Scale / 2f;
            playerPos.Y += Player.Size;
            Avatar avatar = new Avatar( Gamer.SignedInGamers[i].Avatar, AvatarAnimationPreset.Stand0,
                                        Player.Size, Vector3.UnitX, new Vector3( doorPos, 0f ) );
            Player player = new Player( this, i, (PlayerIndex)i, avatar, playerPos );
            ObjectTable.Add( player );
          }
        }
        else if ( i < initSlotInfo.Length && initSlotInfo[i].Avatar != null )
        {
          Vector2 playerPos = doorPos;
          playerPos.X += Basket.Scale / 2f;
          playerPos.Y += Player.Size;
          initSlotInfo[i].Avatar.Scale = Player.Size;
          Player player = new Player( this, i, initSlotInfo[i].Player, initSlotInfo[i].Avatar, playerPos );
          ObjectTable.Add( player );
        }

        ObjectTable.Add( new Basket( this, doorPos ) );

        doorPos.X += doorPosXStep;
      }
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
        if ( random.Next( 10 ) < 7 )
          ObjectTable.Add( Powerup.CreatePowerup( this, blockPos + new Vector2( 0f, 1f ), PowerupType.ScoreCoin ) );

        if ( ( lastRowPattern & ( 1 << i ) ) == 0 )
        {
          ObjectTable.Add( new FloorBlock( this, blockPos ) );
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
          ObjectTable.Add( new FloorBlock( this, blockPos ) );
          curPattern |= ( 1 << ( j - 1 ) );
        }

        blockPos.X += xStep;
      }

      lastRowPattern = curPattern;
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
      foreach ( Player player in players )
      {
        PhysCircle playerCircle = player.BoundingCircle;
        if ( lowestPlayer == null || playerCircle.Position.Y < lowestPlayer.Position.Y )
          lowestPlayer = playerCircle;
      }

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
      else
      {
        Camera.Translate( new Vector3( 0f, camScrollSpeed * (float)elapsed, 0f ) );
      }
    
      lastCamY = begCamPos;
    }

    private void BeginCountdown()
    {
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
