using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using CustomAvatarAnimationFramework;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Input;
using CustomModelSample;
using Particle3DSample;
using Utilities;
using MathLibrary;
using System.Diagnostics;
using Menu;
using Graphics;
using Audio;
using System.Collections.ObjectModel;

namespace AvatarHamsterPanic.Objects
{
  class Player : GameObject, IAudioEmitter
  {
    static readonly float particleCoordU = (float)Math.Cos( MathHelper.ToRadians( 15 ) );
    static readonly float particleCoordV = (float)Math.Sin( MathHelper.ToRadians( 15 ) );

    public static readonly float Size = 1.4f;

    static readonly float baseMass = 10f;
    static readonly float density = baseMass / Geometry.SphereVolume( Size / 2 );
    static readonly float respawnDuration = 1f;
    static readonly float shrinkDuration = 10f;
    static readonly float shrinkSize = .35f;
    //static readonly float squashSize;
    static readonly float crushDuration = .75f;
    static readonly float crushMass = 20f;
    static readonly float seizureDuration = .5f;
    static readonly float lightningStunDuration = 1f;

    static readonly string pvpSound = "plasticHit";
    static readonly string pvBlockSound = "ballVBlock";
    static readonly string pvCageSound = "ballVCage";
    static readonly string laserShotSound = "laserShot";

    static Dictionary<string, uint> playerIDs = new Dictionary<string, uint>( 4 );
    static uint nextID = 1;

    float jumpRegistered;
    const float jumpTimeout = .125f;
    GameTime lastGameTime = new GameTime();
    float lastCollision;
    float shrinkBegin;
    float crushBegin;
    float seizureBegin;
    Collision seizureCollision;
    float lightningStunBegin;
    int lightningStunFrame;
    VertexDeclaration vertexDeclaration;
    CustomAvatarAnimationData walkAnim;
    CustomAvatarAnimationData runAnim;
    Vector3 soundPosition;
    Vector3 soundVelocity;
    PlayerAI playerAI;
    AvatarAnimationPreset standAnim;

    static Random random = new Random();

    public float Scale { get; private set; }
    public SpringInterpolater ScaleSpring { get; private set; }
    public bool Boosting { get; private set; }
    public float BoostBurnRate { get; set; }
    public float BoostRechargeRate { get; set; }
    public uint ID { get; private set; }
    public int PlayerNumber { get; private set; }
    public PlayerIndex PlayerIndex { get; private set; }
    public PhysCircle BoundingCircle { get; private set; }
    public CustomModel WheelModel { get; private set; }
    public Avatar Avatar { get; set; }
    public double RespawnTime { get; private set; }
    public bool Respawning { get { return RespawnTime < respawnDuration; } }
    public PlayerHUD HUD { get; private set; }
    public Powerup Powerup { get; set; }
    public float DeathLine { get; private set; }
    public bool Crushing { get { return crushBegin != 0; } }
    public bool Seizuring { get { return seizureBegin != 0; } }
    public PlayerWinState WinState { get; set; }

    public Player( GameplayScreen screen, int playerNumber, PlayerIndex playerIndex, Avatar avatar, Vector2 pos, uint id )
      : base( screen )
    {
      WheelModel = screen.Content.Load<CustomModel>( "Models/hamsterBall" );
      foreach ( CustomModelSample.CustomModel.ModelPart part in WheelModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( new Vector4( .8f, .7f, 1f, .3f ) );
        part.Effect.Parameters["SpecularPower"].SetValue( 400 );
        part.Effect.Parameters["Mask"].SetValue( MaskHelper.MotionBlur( 1 ) );
      }

      DrawOrder = 3;

      WinState = PlayerWinState.None;

      soundPosition = Vector3.Zero;
      soundVelocity = Vector3.Zero;

      float depth = screen.Camera.Position.Z;
      DeathLine = depth * (float)Math.Tan( screen.Camera.Fov / 2f );

      RespawnTime = float.MaxValue;

      shrinkBegin = 0;
      Scale = 1f;
      ScaleSpring = new SpringInterpolater( 1, 200, SpringInterpolater.GetCriticalDamping( 200 ) );
      ScaleSpring.Active = true;
      ScaleSpring.SetSource( Scale );
      ScaleSpring.SetDest( Scale );

      PlayerIndex = playerIndex;
      PlayerNumber = playerNumber;
      BoostBurnRate = 2f;
      BoostRechargeRate = .125f;

      Avatar = avatar;
      BoundingCircle = new PhysCircle( Size / 2f, pos, 10f );
      BoundingCircle.Parent = this;
      BoundingCircle.Elasticity = .4f;
      BoundingCircle.Friction = .5f;
      BoundingCircle.Collided += HandleCollision;
      BoundingCircle.Responded += HandleCollisionResponse;
      screen.PhysicsSpace.AddBody( BoundingCircle );

      walkAnim = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", Screen.Content );
      runAnim  = CustomAvatarAnimationData.GetAvatarAnimationData( "Run", Screen.Content );

      // pre-load animations for podium screen
      avatar.SetAnimation( AvatarAnimationPreset.Celebrate );
      avatar.SetAnimation( AvatarAnimationPreset.Clap );
      avatar.SetAnimation( AvatarAnimationPreset.FemaleAngry );
      avatar.SetAnimation( AvatarAnimationPreset.MaleCry );

      standAnim = (AvatarAnimationPreset)( (int)AvatarAnimationPreset.Stand0 + random.Next( 8 ) );
      avatar.SetAnimation( standAnim );

      if ( playerIndex >= PlayerIndex.One )
      {
        HUD = new PlayerHUD( this, SignedInGamer.SignedInGamers[playerIndex] );
      }
      else
      {
        HUD = new PlayerHUD( this, null );
        playerAI = new PlayerAI( this );
      }

      vertexDeclaration = new VertexDeclaration( screen.ScreenManager.GraphicsDevice, 
                                                 VertexPositionNormalTexture.VertexElements );

      SetID( id );
    }

    public void GetWheelTransform( out Matrix transform )
    {
      Matrix matTrans, matRot, matScale;
      Matrix.CreateTranslation( BoundingCircle.Position.X, BoundingCircle.Position.Y, 0f, out matTrans );
      Matrix.CreateRotationZ( BoundingCircle.Angle, out matRot );
      Matrix.CreateScale( Size * Scale, out matScale );

      Matrix.Multiply( ref matScale, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    private void SetID( uint id )
    {
      if ( id == 0 )
      {
        if ( PlayerIndex >= PlayerIndex.One )
        {
          string gamertag = SignedInGamer.SignedInGamers[PlayerIndex].Gamertag;
          if ( playerIDs.ContainsKey( gamertag ) )
          {
            ID = playerIDs[gamertag];
          }
          else
          {
            ID = nextID++;
            playerIDs.Add( gamertag, ID );
          }
        }
        else
        {
          ID = nextID++;
        }
      }
      else
      {
        ID = id;
      }
    }

    private bool HandleCollision( Collision result )
    {
      //Player playerB = result.BodyB.Parent as Player;
      //if ( playerB != null )
      //{
      //  // squash him if he's tiny and touching another object
      //  if ( playerB.Scale < squashSize * Size )//&& result.BodyB.Touching != result.BodyA )
      //  {
      //    Debug.WriteLine( "Squash!" );
      //  }
      //}

      // play collision sound
      Player playerB = result.BodyB.Parent as Player;
      if ( playerB != null )
      {
        float relVelMag = ( result.BodyA.Velocity - result.BodyB.Velocity ).Length();
        if ( relVelMag > 1f )
        {
          float volume = Math.Min( .75f, relVelMag / 6f );
          soundPosition = new Vector3( result.Intersection, 0 );
          Screen.AudioManager.Play3DCue( pvpSound, this, volume );
        }
      }

      return true;
    }

    private bool HandleCollisionResponse( Collision result )
    {
      PhysCircle circle = BoundingCircle;

      // keep track of last time of collision (for jumping)
      lastCollision = (float)lastGameTime.TotalGameTime.TotalSeconds;

      // set emitter position
      SparkParticleSystem sparkSystem = Screen.SparkParticleSystem;

      string sound = null;
      if ( result.BodyB.Parent is FloorBlock )
        sound = pvBlockSound;
      if ( result.BodyB.Parent is Boundary || result.BodyB.Parent is Shelves )
        sound = pvCageSound;

      if ( sound != null )
      {
        float impulse = result.BodyA.LastImpulse.Length();
        if ( impulse > .5f )
        {
          float volume = Math.Min( 1f, impulse / ( 5 * BoundingCircle.Mass ) );
          soundPosition = new Vector3( result.Intersection, 0 );
          Screen.AudioManager.Play3DCue( sound, this, volume );
        }
      }

      //// play collision sound
      //Player playerB = result.BodyB.Parent as Player;
      //if ( playerB != null )
      //{
      //  float impulseMag = result.BodyB.LastImpulse.Length();
      //  //if ( impulseMag > .5f )
      //  {
      //    float volume = Math.Min( .85f, impulseMag / 30f );
      //    soundPosition = new Vector3( result.Intersection, 0 );
      //    Screen.AudioManager.Play3DCue( plasticHitSound, this, volume );
      //  }
      //}

      Vector3 position = new Vector3( result.Intersection, 0f );
      //emitter.Position = position;

      // spit some particles
      Vector2 r = Vector2.Normalize( result.Intersection - result.BodyA.Position );
      Vector2 vp = circle.AngularVelocity * circle.Radius * new Vector2( -r.Y, r.X );
      Vector2 dir = circle.Velocity;

      Vector2 vpn = Vector2.Normalize( vp );
      Vector3 direction = new Vector3( particleCoordU * vpn + particleCoordV * -r, 0f );
      //factory.Direction = new Vector3( particleCoordU * vpn + particleCoordV * -r, 0f );

      Vector2 sum = vp + dir;
      if ( sum != Vector2.Zero )
      {
        float sumLength = sum.Length();
        if ( sumLength > .25f )
        {
          float coneAngle = MathHelper.ToRadians( 30 );
          Vector3 velocity = random.NextConeDirection( random.NextFloat( 2, 4 ) * direction, coneAngle );
          sparkSystem.AddParticle( position, velocity );
        }
      }

      return true;
    }

    public void Shrink()
    {
      shrinkBegin = (float)lastGameTime.TotalGameTime.TotalSeconds;
      ScaleSpring.SetDest( shrinkSize * Size );
    }

    public void Crush()
    {
      crushBegin = (float)lastGameTime.TotalGameTime.TotalSeconds;
      BoundingCircle.Velocity.Y -= 3f;
      BoundingCircle.Mass = crushMass;
    }

    public void Laser()
    {
      Vector2 offset   = new Vector2( 1.5f * BoundingCircle.Radius, 0 );
      Vector2 leftPos  = BoundingCircle.Position - offset;
      Vector2 rightPos = BoundingCircle.Position + offset;

      LaserBeam leftLaser = LaserBeam.CreateBeam( leftPos, Vector2.Zero, this, true );
      Screen.AudioManager.Play3DCue( laserShotSound, leftLaser, 1 );
      Screen.ObjectTable.Add( leftLaser );

      LaserBeam rightLaser = LaserBeam.CreateBeam( rightPos, Vector2.Zero, this, false );
      Screen.AudioManager.Play3DCue( laserShotSound, rightLaser, 1 );
      Screen.ObjectTable.Add( rightLaser );
    }

    public void TakeLaserUpAss( Collision result )
    {
      if ( Respawning ) return;

      seizureBegin = (float)lastGameTime.TotalGameTime.TotalSeconds;
      seizureCollision = result;
      BoundingCircle.Flags |= BodyFlags.Anchored;
      BoundingCircle.Velocity = Vector2.Zero;
      BoundingCircle.AngularVelocity = 0;
    }

    public void GetStunnedByLightning()
    {
      if ( Respawning ) return;

      lightningStunBegin = (float)lastGameTime.TotalGameTime.TotalSeconds;
      BoundingCircle.Flags = BodyFlags.Anchored;
      BoundingCircle.Velocity = Vector2.Zero;
      BoundingCircle.AngularVelocity = 0;
    }

    private void UpdateScale( float elapsed )
    {
      float currentScale = ScaleSpring.GetSource()[0];
      if ( Scale != currentScale )
      {
        Scale = currentScale;
        float radius = Size * currentScale / 2f;
        BoundingCircle.Radius = radius;
        float volume = Geometry.SphereVolume( radius );
        BoundingCircle.Mass = density * volume;
        BoundingCircle.MomentOfInertia = .5f * BoundingCircle.Mass * ( radius * radius );
      }

      ScaleSpring.Update( elapsed );
    }

    private void UpdatePowerupEffects( float totalTime )
    {
      // shrink
      if ( shrinkBegin != 0 )
      {
        if ( totalTime - shrinkBegin > shrinkDuration )
        {
          shrinkBegin = 0;
          ScaleSpring.SetDest( 1 );
        }
      }

      // crush
      if ( crushBegin != 0 )
      {
        if ( totalTime - crushBegin > crushDuration )
        {
          crushBegin = 0;
          BoundingCircle.Mass = baseMass;
        }
      }

      // laser
      if ( seizureBegin != 0 )
      {
        if ( totalTime - seizureBegin > seizureDuration )
        {
          BoundingCircle.Velocity = ( 200f / BoundingCircle.Mass ) * -seizureCollision.Normal;
          BoundingCircle.Flags = BodyFlags.None;
          seizureBegin = 0;
        }
        else
        {
          // shake player
          float quake = .05f;
          BoundingCircle.Position += new Vector2( random.NextFloat( -quake, quake ), random.NextFloat( -quake, quake ) );
        }
      }

      // lightning
      if ( lightningStunBegin != 0 )
      {
        if ( totalTime - lightningStunBegin > lightningStunDuration )
        {
          BoundingCircle.Flags = BodyFlags.None;
          lightningStunBegin = 0;
        }
        else
        {
          if ( lightningStunFrame++ % 3 == 0 )
            BoundingCircle.Angle += ( ( lightningStunFrame % 2 ) == 0 ? -.1f : .1f );
        }
      }
    }

    private void UpdatePlace()
    {
      ReadOnlyCollection<Player> players = Screen.ObjectTable.GetObjects<Player>();
      int place = 1;
      int nPlayers = players.Count;
      for ( int i = 0; i < nPlayers; ++i )
      {
        if ( players[i] == this ) continue;

        if ( players[i].HUD.Score > HUD.Score ||
             players[i].HUD.Score == HUD.Score && players[i].PlayerNumber < PlayerNumber )
        {
          place++;
        }
      }

      HUD.Place = place;
    }

    private void ClearPowerupEffects()
    {
      //shrinkBegin = 0;
      crushBegin = 0;
      seizureBegin = 0;
      lightningStunBegin = 0;

      //ScaleSpring.SetDest( 1 );

      if ( BoundingCircle.Flags.HasFlags( BodyFlags.Anchored ) )
      {
        BoundingCircle.Flags = BodyFlags.None;
        BoundingCircle.Velocity = Vector2.Zero;
        BoundingCircle.AngularVelocity = 0;
      }
    }

    public override void Update( GameTime gameTime )
    {
      lastGameTime = gameTime;

      UpdatePowerupEffects( (float)gameTime.TotalGameTime.TotalSeconds );

      UpdateScale( (float)gameTime.ElapsedGameTime.TotalSeconds );

      UpdatePlace();

      UpdateAvatar( gameTime );
      HUD.Update( gameTime );

      if ( !Respawning )
      {
        // check if player should be pwnt
        float deathLine = Screen.Camera.Position.Y + DeathLine - Size * Scale / 2f;
        if ( BoundingCircle.Position.Y >= deathLine && !Crushing )
        {
          ClearPowerupEffects();
          RespawnTime = 0f;
          if ( !Screen.GameOver )
            HUD.AddPoints( -5 );
          BoundingCircle.Velocity.Y = Math.Min( BoundingCircle.Velocity.Y, Screen.CameraScrollSpeed );
        }
      }
      else
      {
        double elapsed = gameTime.ElapsedGameTime.TotalSeconds;
        RespawnTime += elapsed;
      }
    }

    public void HandleInput( InputState input )
    {
      PlayerInput playerInput;
      GetPlayerInput( PlayerIndex, input, out playerInput );

      PhysCircle circle = BoundingCircle;

      // powerups
      PlayerIndex playerIndex = PlayerIndex;
      if ( playerInput.ButtonXHit && Powerup != null && Powerup.Type != PowerupType.GoldenShake )
        Powerup.Use();

      // movement
      float forceY = 0f;
      float forceX = 0f;
      float maxVelX = 4f;

      Boosting = false;
      if ( playerInput.LeftTrigger != 0f )
      {
        Boosting = true;
        forceX = -200f;
        maxVelX = 6f;
      }
      if ( playerInput.RightTrigger != 0f )
      {
        Boosting = true;
        if ( forceX != 0f )
        {
          forceX = 0f;
          forceY = 40f;
          maxVelX = 4f;
        }
        else
        {
          forceX = 200f;
          maxVelX = 6f;
        }
      }

      float maxAngVel = MathHelper.TwoPi;

      float torqueScale = -100f;
      float torque = torqueScale * playerInput.LeftStick.X;

      float elapsed = (float)lastGameTime.ElapsedGameTime.TotalSeconds;

      // torque
      if ( circle.AngularVelocity < 0f && torque < 0f )
      {
        float reqTorque = PhysBody.GetForceRequired( -maxAngVel, circle.AngularVelocity,
                                                     circle.Torque, circle.MomentOfInertia, elapsed );
        torque = Math.Max( torque, reqTorque );
      }
      else if ( circle.AngularVelocity > 0f && torque > 0f )
      {
        float reqTorque = PhysBody.GetForceRequired( maxAngVel, circle.AngularVelocity,
                                                     circle.Torque, circle.MomentOfInertia, elapsed );
        torque = Math.Min( torque, reqTorque );
      }
      circle.Torque += torque;

      // linear force
      if ( Boosting )
      {
        if ( circle.Velocity.X < 0f && forceX < 0f )
          forceX = Math.Max( forceX, PhysBody.GetForceRequired( -maxVelX, circle.Velocity.X, circle.Force.X, circle.Mass, elapsed ) );
        else if ( circle.Velocity.X > 0f && forceX > 0f )
          forceX = Math.Min( forceX, PhysBody.GetForceRequired( maxVelX, circle.Velocity.X, circle.Force.X, circle.Mass, elapsed ) );

        float maxBurn = BoostBurnRate * elapsed;
        float burn = Math.Min( HUD.Boost, maxBurn );
        HUD.Boost -= burn;

        circle.Force += ( burn / maxBurn ) * new Vector2( forceX, forceY );
      }
      else
      {
        HUD.Boost = MathHelper.Clamp( HUD.Boost + BoostRechargeRate * elapsed, 0f, 1f );
      }

      // jumping
      float totalTime = (float)lastGameTime.TotalGameTime.TotalSeconds;
      if ( playerInput.ButtonAHit )
      {
        if ( totalTime - lastCollision < jumpTimeout )
          circle.Velocity += 2f * circle.TouchNormal;
        else
          jumpRegistered = totalTime;
      }

      if ( jumpRegistered != 0f )
      {
        if ( circle.Touching != null )
        {
          circle.Velocity += 2f * circle.TouchNormal;
          jumpRegistered = 0f;
        }
        else if ( totalTime - jumpRegistered > jumpTimeout )
        {
          jumpRegistered = 0f;
        }
      }
    }

    private void GetPlayerInput( PlayerIndex playerIndex, InputState input, out PlayerInput playerInput )
    {
      if ( playerIndex < PlayerIndex.One )
      {
        playerAI.GetInput( out playerInput );
      }
      else
      {
        playerInput.ButtonAHit = input.IsNewButtonPress( Buttons.A, playerIndex, out playerIndex );
        playerInput.ButtonXHit = input.IsNewButtonPress( Buttons.X, playerIndex, out playerIndex );

        GamePadState gamePadState = input.CurrentGamePadStates[(int)playerIndex];

        playerInput.LeftTrigger = gamePadState.Triggers.Left;
        playerInput.RightTrigger = gamePadState.Triggers.Right;
        playerInput.LeftStick = gamePadState.ThumbSticks.Left;
      }
    }

    public override void Draw()
    {
      if ( Respawning && ( (int)( RespawnTime * 16f ) % 2 ) == 0 )
        return;

      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = graphics.RenderState;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      renderState.AlphaBlendEnable = false;

      // draw avatar first, so the ball doesn't cover it
      Avatar.Renderer.View = Screen.View;
      Avatar.Renderer.Projection = Screen.Projection;

      Matrix matRot = Matrix.CreateWorld( Vector3.Zero, Avatar.Direction, Screen.Camera.Up );
      Matrix matTrans = Matrix.CreateTranslation( Avatar.Position );
      Avatar.Renderer.World = Matrix.CreateScale( .5f * Size * Scale ) * matRot * matTrans;
      Avatar.Renderer.Draw( Avatar.BoneTransforms, Avatar.Expression );

      // draw the ball
      Matrix transform;
      GetWheelTransform( out transform );

      renderState.AlphaBlendEnable = true;
      renderState.AlphaSourceBlend = Blend.SourceAlpha;
      renderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;

      renderState.CullMode = CullMode.CullClockwiseFace;
      WheelModel.Draw( Screen.Camera.Position, transform, Screen.View, Screen.Projection );

      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      WheelModel.Draw( Screen.Camera.Position, transform, Screen.View, Screen.Projection );
    }

    private void UpdateAvatar( GameTime gameTime )
    {
      PhysCircle circle = BoundingCircle;
      Avatar.Position = new Vector3( circle.Position.X, circle.Position.Y - ( Scale * Size ) / 2.3f, 0f );

      double absAngVel = Math.Abs( (double)circle.AngularVelocity );

      // update avatar's animation
      double idleThresh = .1;
      double walkThresh = 4.0;
      double animScaleFactor = .20;

      if ( absAngVel <= idleThresh )
      {
        Avatar.SetAnimation( standAnim );
        Avatar.Update( gameTime.ElapsedGameTime, true );
      }
      else
      {
        Avatar.Direction = new Vector3( BoundingCircle.AngularVelocity < 0f ? 1f : -1f, 0f, 0f );
        if ( absAngVel <= walkThresh )
        {
          animScaleFactor = 1.0;
          Avatar.SetAnimation( walkAnim );
        }
        else
        {
          Avatar.SetAnimation( runAnim );
        }
        double animScale = animScaleFactor * absAngVel;
        Avatar.Update( TimeSpan.FromSeconds( animScale * gameTime.ElapsedGameTime.TotalSeconds ), true );
      }
    }

    #region IAudioEmitter Members

    public Vector3 Position
    {
      get { return soundPosition; }
    }

    public Vector3 Forward
    {
      get { return Vector3.Forward; }
    }

    public Vector3 Up
    {
      get { return Screen.Camera.Up; }
    }

    public Vector3 Velocity
    {
      get { return soundVelocity; }
    }

    #endregion
  }

  enum PlayerWinState
  {
    None,
    Lose,
    Win,
  }

  struct PlayerInput
  {
    public bool ButtonAHit;
    public bool ButtonXHit;
    public float LeftTrigger;
    public float RightTrigger;
    public Vector2 LeftStick;
  }

  class PlayerAI
  {
    #region Static Members

    static readonly SortedList<float, int> rows = new SortedList<float, int>( 6, new DescendingComparer<float>() );

    public static void AddRow( float height, int pattern )
    {
      rows.Add( height, pattern );
      while ( rows.Count > 5 )
        rows.Remove( rows.First().Key );
    }

    //public static void RemoveRow( float height )
    //{
    //  rows.Remove( height );
    //}

    public static void RemoveAllRows()
    {
      rows.Clear();
    }

    #endregion

    #region Instance Memebers

    Player player;
    float rightStart;

    public PlayerAI( Player player )
    {
      this.player = player;
      rightStart = ( FloorBlock.Size * GameplayScreen.BlocksPerRow - FloorBlock.Size ) / 2f;
    }

    public void GetInput( out PlayerInput playerInput )
    {
      //AI SUPERBRAIN GOES HERE
      playerInput.ButtonAHit = false;
      playerInput.ButtonXHit = false;
      playerInput.LeftTrigger = 0;
      playerInput.RightTrigger = 0;
      playerInput.LeftStick = Vector2.Zero;

      // No stage data yet
      if ( rows.Count == 0 || !player.Screen.CameraIsScrolling )
        return;

      // MOVEMENT
      //if falling through row
      //  try to align self with hole
      //else if row has been visible for x seconds
      //  find nearest hole
      //    set target to hole

      PhysCircle circle = player.BoundingCircle;

      // determine if falling through hole
      float rowsTraveled = ( player.Screen.FirstRow - circle.Position.Y ) / player.Screen.RowSpacing;
      float remainder = rowsTraveled - (float)Math.Floor( rowsTraveled );
      if ( remainder > .5f )
        remainder = 1f - remainder;
      float holeDist = ( FloorBlock.Height / 2 + circle.Radius ) / player.Screen.RowSpacing;

      if ( remainder < holeDist )
      {
        if ( circle.Touching != null )
        {
          // move to middle of hole
        }
      }
      else
      {
        foreach ( KeyValuePair<float, int> row in rows )
        {
          // TODO: Reaction time delay
          if ( row.Key < circle.Position.Y )
          {
            Vector2 hole = GetNearestHole( row );
            if ( circle.Position.X < hole.X )
              playerInput.LeftStick.X = 1;
            else if ( circle.Position.X > hole.X )
              playerInput.LeftStick.X = -1;
            break;
          }
        }
      }
    }

    private Vector2 GetNearestHole( KeyValuePair<float, int> row )
    {
      float bestDist = float.MaxValue;
      float bestX = 0;

      int pattern = row.Value;
      float playerXPos = player.BoundingCircle.Position.X;

      float x = -rightStart;
      for ( int i = 0; i < GameplayScreen.BlocksPerRow; ++i )
      {
        if ( ( pattern & ( 1 << i ) ) == 0 )
        {
          float dist = Math.Abs( playerXPos - x );
          if ( dist < bestDist )
          {
            bestX = x;
            bestDist = dist;
          }
        }

        x += FloorBlock.Size;
      }

      return new Vector2( bestX, row.Key );
    }

    #endregion
  }
}