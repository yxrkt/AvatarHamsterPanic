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
using Microsoft.Xna.Framework.Audio;

namespace AvatarHamsterPanic.Objects
{
  class Player : GameObject, IAudioEmitter
  {
    #region Static Fields

    static readonly float particleCoordU = (float)Math.Cos( MathHelper.ToRadians( 15 ) );
    static readonly float particleCoordV = (float)Math.Sin( MathHelper.ToRadians( 15 ) );

    public static readonly float Size = 1.4f;

    static readonly float baseMass = 10f;
    static readonly float density = baseMass / Geometry.SphereVolume( Size / 2 );
    static readonly float respawnDuration = 1.25f;
    static readonly float shrinkDuration = 10f;
    static readonly float shrinkSize = .35f;
    //static readonly float squashSize;
    static readonly float crushDuration = .6f;
    static readonly float crushMass = 20f;
    static readonly float seizureDuration = .5f;
    static readonly float lightningStunDuration = 1f;
    static readonly float boostLingerDuration = 1f;
    static readonly float boostRumbleStrength = .17f;

    static readonly string pvpSound = "plasticHit";
    static readonly string pvBlockSound = "ballVBlock";
    static readonly string pvCageSound = "ballVCage";
    static readonly string laserShotSound = "laserShot";
    static readonly string volumeVariable = "Volume";

    static Dictionary<string, uint> playerIDs = new Dictionary<string, uint>( 4 );
    static uint nextID = 1;

    static Random random = new Random();

    #endregion

    #region Fields

    //float jumpRegistered;
    const float jumpTimeout = .125f;
    GameTime lastGameTime = new GameTime();
    float lastCollision;
    //float lastJump;
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
    Cue boosterSound;
    float boostCutoff;
    List<Lightning> lightnings = new List<Lightning>( 4 );
    CircularGlow glow;
    SpringInterpolater glowSpring;

    #endregion

    #region Properties

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
    public PlayerTag Tag { get; private set; }
    public Powerup Powerup { get; set; }
    public float DeathLine { get; private set; }
    public bool Crushing { get { return crushBegin != 0; } }
    public bool Seizuring { get { return seizureBegin != 0; } }
    public PlayerWinState WinState { get; set; }
    public int PodiumPlace
    {
      get
      {
        ReadOnlyCollection<Player> players = Screen.ObjectTable.GetObjects<Player>();

        int place = 1;
        int playerIndex = -1;
        for ( int i = 0; i < 4; ++i )
        {
          if ( !Screen.Slots[i].Player.IsPlayer() ) continue;
          playerIndex++;

          Player player = players[playerIndex];

          if ( player.HUD.TotalScore < HUD.TotalScore || i == PlayerNumber ) continue;

          if ( player.HUD.TotalScore == HUD.TotalScore )
          {
            if ( Powerup != null && Powerup.Type == PowerupType.GoldenShake )
              continue;

            if ( player.Powerup != null && player.Powerup.Type == PowerupType.GoldenShake )
            {
              place++;
              continue;
            }

            int myWins = 0;
            if ( GameCore.Instance.PlayerWins.ContainsKey( ID ) )
              myWins = GameCore.Instance.PlayerWins[ID];

            int hisWins = 0;
            if ( GameCore.Instance.PlayerWins.ContainsKey( player.ID ) )
              hisWins = GameCore.Instance.PlayerWins[player.ID];

            if ( myWins < hisWins || myWins == hisWins && PlayerNumber < player.PlayerNumber )
              continue;
          }

          place++;
        }

        return place;
      }
    }

    #endregion

    #region Initialization

    public Player( GameplayScreen screen, int playerNumber, PlayerIndex playerIndex, Avatar avatar, Vector2 pos, uint id )
      : base( screen )
    {
      WheelModel = screen.Content.Load<CustomModel>( "Models/hamsterBall" );
      foreach ( CustomModelSample.CustomModel.ModelPart part in WheelModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( new Vector4( .8f, .7f, 1f, .225f ) );
        part.Effect.Parameters["SpecularPower"].SetValue( 400 );
        part.Effect.Parameters["Mask"].SetValue( MaskHelper.MotionBlur( 1 ) );
      }

      DrawOrder = 8;

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
      BoostBurnRate = 1f;
      BoostRechargeRate = .25f;

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

      boosterSound = GameCore.Instance.AudioManager.Play2DCue( "booster", 1f );
      boosterSound.Pause();

      glow = new CircularGlow( new Vector3( BoundingCircle.Position, 0 ), Color.OrangeRed, Size );
      glow.Player = this;
      screen.ObjectTable.Add( glow );

      glowSpring = new SpringInterpolater( 1, 500, .75f * SpringInterpolater.GetCriticalDamping( 500 ) );
      glowSpring.Active = true;
      glowSpring.SetSource( 0 );
      glowSpring.SetDest( 0 );

      Tag = new PlayerTag( this, screen.Content.Load<SpriteFont>( "Fonts/playerTagFont" ) );

      SetID( id );
    }

    public void OnDestruct()
    {
      boosterSound.Dispose();
      boosterSound = null;
      foreach ( Lightning lightning in lightnings )
        Screen.ObjectTable.MoveToTrash( lightning );
      lightnings.Clear();
    }

    #endregion

    #region Update and Draw

    public override void Update( GameTime gameTime )
    {
      lastGameTime = gameTime;

      UpdatePowerupEffects( Screen.AccumulatedTime );

      UpdateScale( (float)gameTime.ElapsedGameTime.TotalSeconds );

      UpdatePlace();

      UpdateAvatar( gameTime );
      Tag.Update( gameTime );
      HUD.Update( gameTime );

      UpdateBoostSound();
      UpdateGlowEffect();

      if ( !Screen.CameraIsScrolling ) return;

      float deathLine = Screen.Camera.Position.Y + DeathLine + Size * Scale / 2f;
      bool hittingLine = BoundingCircle.Position.Y >= deathLine;
      if ( !Respawning )
      {
        // check if player should be pwnt
        if ( hittingLine && !Crushing )
        {
          ClearPowerupEffects();
          RespawnTime = 0f;
          if ( !Screen.GameOver )
            HUD.AddPoints( -3 );
          BoundingCircle.Velocity.Y = Math.Min( BoundingCircle.Velocity.Y, 1.5f * Screen.CameraScrollSpeed );
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.RumbleLow( PlayerIndex, .25f, .5f );
        }
      }
      else
      {
        double elapsed = gameTime.ElapsedGameTime.TotalSeconds;
        if ( !( hittingLine && RespawnTime + elapsed > respawnDuration ) )
          RespawnTime += elapsed;
      }
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

    private void UpdateBoostSound()
    {
      if ( boosterSound != null )
      {
        if ( Boosting && HUD.Boost != 0 && ( boosterSound.IsPaused || boostCutoff != 0 ) )
        {
          boostCutoff = 0;
          boosterSound.SetVariable( volumeVariable, XACTHelper.GetDecibels( 1 ) );
          boosterSound.Resume();
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.TurnOnHigh( PlayerIndex, boostRumbleStrength );
        }
        else if ( ( !Boosting || HUD.Boost == 0 ) && !boosterSound.IsPaused && boostCutoff == 0 )
        {
          boostCutoff = Screen.AccumulatedTime;
        }
      }
      if ( boostCutoff != 0 )
      {
        float t = ( Screen.AccumulatedTime - boostCutoff ) / boostLingerDuration;
        if ( t < 1 )
        {
          boosterSound.SetVariable( volumeVariable, XACTHelper.GetDecibels( 1 - t ) );
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.TurnOnHigh( PlayerIndex, boostRumbleStrength * ( 1 - t ) );
        }
        else
        {
          boosterSound.Pause();
          boostCutoff = 0;
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.TurnOffHigh( PlayerIndex );
        }
      }
    }

    private void UpdateGlowEffect()
    {
      if ( Crushing )
      {
        if ( glowSpring.GetDest()[0] != 1f )
          glowSpring.SetDest( 1f );
        glow.Color.R = 255;
        glow.Color.G = 80;
        glow.Color.B = 0;
      }
      else if ( Boosting && HUD.Boost > 0 )
      {
        if ( glowSpring.GetDest()[0] != 1f )
          glowSpring.SetDest( 1f );
        glow.Color.R = 0;
        glow.Color.G = 225;
        glow.Color.B = 255;
      }
      else if ( Respawning )
      {
        if ( glowSpring.GetDest()[0] != 1f )
          glowSpring.SetDest( 1f );
        glow.Color.R = 255;
        glow.Color.G = 255;
        glow.Color.B = 255;
      }
      else
      {
        glowSpring.SetDest( 0 );
      }

      float glowSize = glowSpring.GetSource()[0];
      if ( glowSize < 0 )
      {
        glowSize = 0;
        glowSpring.SetSource( 0 );
      }

      if ( Respawning && ( (int)( RespawnTime * 16f ) % 2 ) == 0 )
        glowSize = 0;

      glow.Color.A = (byte)( glowSize * 255f + .5f );

      //if ( glowSpring.GetDest()[0] == 0 )
      //{
      //  glowSpring.K = 100;
      //  glowSpring.B = SpringInterpolater.GetCriticalDamping( 100 );
      //}
      //else
      //{
      //  glowSpring.K = 500;
      //  glowSpring.B = .5f * SpringInterpolater.GetCriticalDamping( 500 );
      //}

      glowSpring.Update( (float)lastGameTime.ElapsedGameTime.TotalSeconds );
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
          soundPosition = new Vector3( BoundingCircle.Position, 0 );
          GameCore.Instance.AudioManager.Play3DCue( "shrimpUp", this, 1f );
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
          foreach ( Lightning lightning in lightnings )
            Screen.ObjectTable.MoveToTrash( lightning );
          lightnings.Clear();
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

        if ( players[i].HUD.Score > HUD.Score )
          place++;
      }

      HUD.Place = place;
    }

    public void HandleInput( InputState input )
    {
      PlayerInput playerInput;
      GetPlayerInput( PlayerIndex, input, out playerInput );

      PhysCircle circle = BoundingCircle;

      // powerups
      PlayerIndex playerIndex = PlayerIndex;
      if ( ( playerInput.ButtonYHit ) &&
           Powerup != null && Powerup.Type != PowerupType.GoldenShake )
      {
        if ( !Screen.GameOver )
          Powerup.Use();
      }

      // movement
      float forceY = 0f;
      float forceX = 0f;
      float maxVelX = 4f;

      bool leftHeld = ( playerInput.LeftStick.X < 0 || playerInput.LeftDpad ) &&
                      !( playerInput.LeftStick.X > 0 || playerInput.RightDpad );
      bool rightHeld = ( playerInput.LeftStick.X > 0 || playerInput.RightDpad ) &&
                       !( playerInput.LeftStick.X < 0 || playerInput.LeftDpad );

      Boosting = false;
      if ( !Screen.GameOver )
      {
        if ( playerInput.LeftTrigger != 0f || playerInput.LeftBumper ||
             ( playerInput.ButtonBDown && leftHeld ) ||
             ( playerInput.ButtonBDown && !rightHeld && circle.Velocity.X < 0 ) )
        {
          Boosting = true;
          forceX = -20f * BoundingCircle.Mass;
          maxVelX = 6f;
        }
        else if ( playerInput.RightTrigger != 0f || playerInput.RightBumper ||
             ( playerInput.ButtonBDown && rightHeld ) ||
             ( playerInput.ButtonBDown && !leftHeld && circle.Velocity.X > 0 ) )
        {
          Boosting = true;
          forceX = 20f * BoundingCircle.Mass;
          maxVelX = 6f;
        }
      }

      float maxAngVel = MathHelper.TwoPi;

      float torqueScale = -100f;
      float torque = torqueScale * playerInput.LeftStick.X;
      if ( playerInput.LeftDpad )
        torque = -torqueScale;
      else if ( playerInput.RightDpad )
        torque = torqueScale;

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
        {
          forceX = Math.Max( forceX, PhysBody.GetForceRequired( -maxVelX, circle.Velocity.X,
                                                                circle.Force.X, circle.Mass, elapsed ) );
        }
        else if ( circle.Velocity.X > 0f && forceX > 0f )
        {
          forceX = Math.Min( forceX, PhysBody.GetForceRequired( maxVelX, circle.Velocity.X,
                                                                circle.Force.X, circle.Mass, elapsed ) );
        }

        float maxBurn = BoostBurnRate * elapsed;
        float burn = Math.Min( HUD.Boost, maxBurn );
        HUD.Boost -= burn;

        circle.Force += ( burn / maxBurn ) * new Vector2( forceX, forceY );
      }
      else
      {
        HUD.Boost = MathHelper.Clamp( HUD.Boost + BoostRechargeRate * elapsed, 0f, 1f );

        if ( playerInput.LeftStick.Y < -.75f || playerInput.DownDpad )
        {
          //circle.Force += 1.0f * Screen.PhysicsSpace.Gravity;
        }
      }

      /*/// jumping
      float totalTime = Screen.AccumulatedTime;
      if ( playerInput.ButtonAHit )
      {
        if ( lastJump != lastCollision && totalTime - lastCollision < jumpTimeout )
        {
          Jump( circle );
          lastJump = lastCollision;
        }
        else
        {
          jumpRegistered = totalTime;
        }
      }

      if ( jumpRegistered != 0f )
      {
        if ( circle.Touching != null )
        {
          Jump( circle );
          lastJump = lastCollision;
          jumpRegistered = 0f;
        }
        else if ( totalTime - jumpRegistered > jumpTimeout )
        {
          jumpRegistered = 0f;
        }
      }
      /*/
      // updward boosting
      if ( playerInput.ButtonADown && HUD.Boost != 0 && !Screen.GameOver )
      {
        float maxBurn = BoostBurnRate * elapsed;
        float burn = Math.Min( HUD.Boost, maxBurn );
        HUD.Boost -= burn;

        Boosting = true;
        float boostScale = burn / maxBurn;
        circle.Force += 2f * circle.Mass * -Screen.PhysicsSpace.Gravity * boostScale;
      }
      /**/
    }

    public override void Draw()
    {
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

    #endregion

    #region Public Methods

    public void GetWheelTransform( out Matrix transform )
    {
      Matrix matTrans, matRot, matScale;
      Matrix.CreateTranslation( BoundingCircle.Position.X, BoundingCircle.Position.Y, 0f, out matTrans );
      Matrix.CreateRotationZ( BoundingCircle.Angle, out matRot );
      Matrix.CreateScale( Size * Scale, out matScale );

      Matrix.Multiply( ref matScale, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public void Shrink()
    {
      shrinkBegin = Screen.AccumulatedTime;
      ScaleSpring.SetDest( shrinkSize * Size );
    }

    public void Crush()
    {
      crushBegin = Screen.AccumulatedTime;
      BoundingCircle.Velocity.Y -= 3f;
      BoundingCircle.Mass = crushMass;
    }

    public void Laser()
    {
      Vector2 offset = new Vector2( 1.5f * BoundingCircle.Radius, 0 );
      Vector2 leftPos = BoundingCircle.Position - offset;
      Vector2 rightPos = BoundingCircle.Position + offset;

      LaserBeam leftLaser = LaserBeam.CreateBeam( leftPos, Vector2.Zero, this, true );
      GameCore.Instance.AudioManager.Play3DCue( laserShotSound, leftLaser, 1 );
      Screen.ObjectTable.Add( leftLaser );

      LaserBeam rightLaser = LaserBeam.CreateBeam( rightPos, Vector2.Zero, this, false );
      GameCore.Instance.AudioManager.Play3DCue( laserShotSound, rightLaser, 1 );
      Screen.ObjectTable.Add( rightLaser );
    }

    public void TakeLaserUpAss( Collision result )
    {
      if ( Respawning ) return;

      seizureBegin = Screen.AccumulatedTime;
      seizureCollision = result;
      BoundingCircle.Flags |= BodyFlags.Anchored;
      BoundingCircle.Velocity = Vector2.Zero;
      BoundingCircle.AngularVelocity = 0;

      if ( PlayerIndex.IsHuman() )
        GameCore.Instance.Rumble.RumbleHigh( PlayerIndex, .4f, seizureDuration );
    }

    public void GetStunnedByLightning( Player attackingPlayer )
    {
      if ( Respawning ) return;

      lightningStunBegin = Screen.AccumulatedTime;
      BoundingCircle.Flags = BodyFlags.Anchored;
      BoundingCircle.Velocity = Vector2.Zero;
      BoundingCircle.AngularVelocity = 0;

      Lightning lightning = new Lightning( 5, Vector3.Zero, Vector3.Zero );
      lightning.LinkToPlayers( attackingPlayer, this );
      lightnings.Add( lightning );
      Screen.ObjectTable.Add( lightning );

      if ( PlayerIndex.IsHuman() )
        GameCore.Instance.Rumble.RumbleLow( PlayerIndex, .3f, lightningStunDuration );
    }

    #endregion

    #region Private Helpers

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
        if ( relVelMag > 3f )
        {
          float volume = Math.Min( .75f, relVelMag / 100f );
          soundPosition = new Vector3( result.Intersection, 0 );
          GameCore.Instance.AudioManager.Play3DCue( pvpSound, this, volume );
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.RumbleHigh( PlayerIndex, .2f, .15f );
        }
      }

      return true;
    }

    private bool HandleCollisionResponse( Collision result )
    {
      PhysCircle circle = BoundingCircle;

      // keep track of last time of collision (for jumping)
      lastCollision = Screen.AccumulatedTime;

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
        if ( impulse > 10f )
        {
          float volume = Math.Min( 1f, impulse / ( 100 * BoundingCircle.Mass ) );
          soundPosition = new Vector3( result.Intersection, 0 );
          GameCore.Instance.AudioManager.Play3DCue( sound, this, volume );
          if ( PlayerIndex.IsHuman() )
            GameCore.Instance.Rumble.RumbleHigh( PlayerIndex, .2f, .15f );
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

    private void ClearPowerupEffects()
    {
      //shrinkBegin = 0;
      crushBegin = 0;
      seizureBegin = 0;
      lightningStunBegin = 0;
      foreach ( Lightning lightning in lightnings )
        Screen.ObjectTable.MoveToTrash( lightning );
      lightnings.Clear();

      //ScaleSpring.SetDest( 1 );

      if ( BoundingCircle.Flags.HasFlags( BodyFlags.Anchored ) )
      {
        BoundingCircle.Flags = BodyFlags.None;
        BoundingCircle.Velocity = Vector2.Zero;
        BoundingCircle.AngularVelocity = 0;
      }
    }

    private void Jump( PhysCircle circle )
    {
      circle.Velocity += 2f * circle.TouchNormal;
      glowSpring.SetSource( 1f );
      glow.Color.R = 255;
      glow.Color.G = 225;
      glow.Color.B = 0;
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
        playerInput.ButtonBHit = input.IsNewButtonPress( Buttons.B, playerIndex, out playerIndex );
        playerInput.ButtonXHit = input.IsNewButtonPress( Buttons.X, playerIndex, out playerIndex );
        playerInput.ButtonYHit = input.IsNewButtonPress( Buttons.Y, playerIndex, out playerIndex );
        playerInput.ButtonADown = input.CurrentGamePadStates[(int)playerIndex].IsButtonDown( Buttons.A );
        playerInput.ButtonBDown = input.CurrentGamePadStates[(int)playerIndex].IsButtonDown( Buttons.B );
        if ( !playerInput.ButtonBDown )
          playerInput.ButtonBDown = input.CurrentGamePadStates[(int)playerIndex].IsButtonDown( Buttons.X );
        
        GamePadState gamePadState = input.CurrentGamePadStates[(int)playerIndex];

        playerInput.LeftTrigger = gamePadState.Triggers.Left;
        playerInput.RightTrigger = gamePadState.Triggers.Right;
        playerInput.LeftBumper = gamePadState.IsButtonDown( Buttons.LeftShoulder );
        playerInput.RightBumper = gamePadState.IsButtonDown( Buttons.RightShoulder );
        playerInput.LeftStick = gamePadState.ThumbSticks.Left;
        playerInput.LeftDpad = gamePadState.DPad.Left == ButtonState.Pressed;
        playerInput.RightDpad = gamePadState.DPad.Right == ButtonState.Pressed;
        playerInput.DownDpad = gamePadState.DPad.Down == ButtonState.Pressed;
      }
    }

    #endregion

    #region IAudioEmitter Members

    public Vector3 Position
    {
      get { return soundPosition; }
      set { soundPosition = value; }
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
    public bool ButtonADown;
    public bool ButtonBHit;
    public bool ButtonBDown;
    public bool ButtonXHit;
    public bool ButtonYHit;
    public float LeftTrigger;
    public float RightTrigger;
    public Vector2 LeftStick;
    public bool LeftDpad;
    public bool RightDpad;
    public bool LeftBumper;
    public bool RightBumper;
    public bool DownDpad;
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

    static readonly float minimumWaitTime = 1f;
    static readonly float hammerSpookThreshold = 1f;
    static readonly float hammerSpeedThreshold = -3f;
    static readonly float lightningAttackThreshold = 2.5f;
    static readonly float lightningDefenseThreshold = .5f;
    static readonly float shrimpThreshold = 2f * 2f;

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
      playerInput.ButtonADown = false;
      playerInput.ButtonBHit = false;
      playerInput.ButtonBDown = false;
      playerInput.ButtonXHit = false;
      playerInput.ButtonYHit = ShouldUsePowerup;
      playerInput.LeftTrigger = 0;
      playerInput.RightTrigger = 0;
      playerInput.LeftStick = Vector2.Zero;
      playerInput.LeftDpad = false;
      playerInput.DownDpad = false;
      playerInput.RightDpad = false;
      playerInput.LeftBumper = false;
      playerInput.RightBumper = false;

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
          playerInput.DownDpad = true;
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

    private bool ShouldUsePowerup
    {
      get
      {
        if ( player.Powerup == null )
          return false;

        if ( player.Screen.AccumulatedTime - player.Powerup.CollectedAt < minimumWaitTime )
          return false;

        if ( player.Screen.ShakeIsOut )
          return true;

        float deathLine = player.DeathLine + player.Screen.Camera.Position.Y 
                                           - Player.Size * player.Scale / 2f;
        PhysCircle circle = player.BoundingCircle;
        ReadOnlyCollection<Player> players = player.Screen.ObjectTable.GetObjects<Player>();

        switch ( player.Powerup.Type )
        {
          case PowerupType.Hammer:
            if ( deathLine - circle.Position.Y < hammerSpookThreshold )
              return true;
            if ( circle.Velocity.Y < hammerSpeedThreshold && !player.Respawning )
              return true;
            break;
          case PowerupType.Laser:
            float shootRange = circle.Radius / 2;
            for ( int i = 0; i < players.Count; ++i )
            {
              if ( i == player.PlayerNumber || players[i].Respawning ) continue;
              if ( Math.Abs( circle.Position.Y - players[i].BoundingCircle.Position.Y ) < shootRange )
                return true;
            }
            break;
          case PowerupType.Lightning:
            float springLine = player.Screen.Camera.Position.Y + FloorBlock.BirthLine * .2f;
            for ( int i = 0; i < players.Count; ++i )
            {
              float distToDeath = deathLine - players[i].BoundingCircle.Position.Y;
              if ( !players[i].Respawning && distToDeath < lightningAttackThreshold )
                return true;
              if ( players[i].BoundingCircle.Position.Y - springLine < lightningDefenseThreshold )
                return true;
            }

            break;
          case PowerupType.Shrimp:
            for ( int i = 0; i < players.Count; ++i )
            {
              Vector2 enemyPos = players[i].BoundingCircle.Position;
              float distSquared = Vector2.DistanceSquared( enemyPos, circle.Position );
              if ( distSquared < shrimpThreshold )
                return true;
            }
            break;
        }

        return false;
      }
    }

    #endregion
  }
}