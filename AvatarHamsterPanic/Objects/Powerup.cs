using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Physics;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Utilities;
using Microsoft.Xna.Framework.Content;
using CustomModelSample;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Menu;
using MathLibrary;
using Graphics;
using Audio;

namespace AvatarHamsterPanic.Objects
{
  enum PowerupType
  {
    Shake,
    GoldenShake,
    Hammer,
    Laser,
    Shrimp,
    Lightning
  }

  class Powerup : GameObject, IAudioEmitter
  {
    // event delegates
    private delegate void UpdateMethod( GameTime gameTime );
    private delegate void ActivateMethod();

    Matrix world;
    Player owner;
    bool alive;
    //float rotateTime;
    float nonTubeTime;
    float rotationAngle;

    static VertexDeclaration vertexDeclaration;
    static GameplayScreen screen;
    static CustomModel shakeModel;
    static CustomModel goldShakeModel;
    static CustomModel shrimpModel;
    static CustomModel hammerModel;
    static CustomModel laserModel;
    static CustomModel boltModel;

    static Dictionary<CustomModel, Matrix> initialTransforms = 
      new Dictionary<CustomModel, Matrix>( 4 );

    static Powerup[] pool;


    event UpdateMethod UpdateSelf;
    event ActivateMethod Activate;

    public PhysCircle Body { get; private set; }
    public CustomModel Model { get; private set; }
    public float Size { get; private set; }
    public PowerupType Type { get; private set; }
    public float CollectedAt { get; private set; }

    bool inTube;
    Vector2 exitTubePos;
    public bool InTube 
    {
      get { return inTube; }
      set
      {
        inTube = value;
        if ( !inTube )
        {
          exitTubePos = Body.Position;
          GameCore.Instance.AudioManager.Play3DCue( "tubePop", this, 1 );
          GameCore.Instance.AudioManager.Play3DCue( "twinkle", this, 1 );
          if ( Type == PowerupType.GoldenShake )
            Screen.ShakeIsOut = true;
        }
      }
    }
    public SpringInterpolater Oscillator { get; private set; }
    public SpringInterpolater SizeSpring { get; private set; }
    public SpringInterpolater LockToPlayerSpring { get; private set; }
    public SpringInterpolater RotationSpring { get; private set; }

    public static float DeathLine { get; private set; }

    static Random rand = new Random();

    public static void Initialize( GameplayScreen screen )
    {
      Powerup.screen = screen;
      GraphicsDevice device = screen.ScreenManager.GraphicsDevice;
      vertexDeclaration = new VertexDeclaration( device, VertexPositionColor.VertexElements );

      ContentManager content = screen.Content;

      shakeModel = content.Load<CustomModel>( "Models/milkshake" );
      InitializeShakeColors();

      goldShakeModel = content.Load<CustomModel>( "Models/goldshake" );
      foreach ( CustomModel.ModelPart part in goldShakeModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.EffectParamColor.SetValue( ColorHelper.ColorFromUintRgb( 0xCFB53B ).ToVector4() );
        part.Effect.Parameters["Mask"].SetValue( MaskHelper.Glow( .85f ) );
      }

      shrimpModel = content.Load<CustomModel>( "Models/shrimp" );
      foreach ( CustomModel.ModelPart part in shrimpModel.ModelParts )
        part.Effect.CurrentTechnique = part.Effect.Techniques["DiffuseColor"];
      
      hammerModel = content.Load<CustomModel>( "Models/hammer" );
      
      laserModel = content.Load<CustomModel>( "Models/gun" );
      foreach ( CustomModel.ModelPart part in laserModel.ModelParts )
        part.Effect.CurrentTechnique = part.Effect.Techniques["DiffuseColor"];
      
      boltModel = content.Load<CustomModel>( "Models/bolt" );
      foreach ( CustomModel.ModelPart part in boltModel.ModelParts )
        part.Effect.CurrentTechnique = part.Effect.Techniques["DiffuseColor"];

      initialTransforms.Add( shakeModel, Matrix.CreateRotationZ( MathHelper.ToRadians( 15 ) ) );
      initialTransforms.Add( goldShakeModel, Matrix.CreateRotationZ( MathHelper.ToRadians( 15 ) ) );
      initialTransforms.Add( shrimpModel, Matrix.CreateRotationY( MathHelper.PiOver2 ) );
      initialTransforms.Add( hammerModel, Matrix.CreateRotationZ( MathHelper.ToRadians( 30 ) ) );
      initialTransforms.Add( laserModel, Matrix.CreateRotationZ( MathHelper.ToRadians( -20 ) ) );
      initialTransforms.Add( boltModel, Matrix.Identity );

      float maxPowerupSize = 2f;
      Camera camera = screen.Camera;
      float dist = camera.Position.Z + maxPowerupSize / 2f;
      float height = dist * (float)Math.Tan( camera.Fov / 2f );
      DeathLine = height + maxPowerupSize / 2f;

      const int poolSize = 20;
      pool = new Powerup[poolSize];
      for ( int i = 0; i < poolSize; ++i )
        pool[i] = new Powerup( screen );
    }

    private static void InitializeShakeColors()
    {
      Vector4[] colors = new Vector4[]
      {
        new Color( 0x5C, 0x44, 0x80, 0xFF ).ToVector4(), // straw
        new Color( 0xFF, 0xC2, 0xD1, 0xFF ).ToVector4(), // shake
        new Color( 0xD7, 0xD7, 0xD7, 0xFF ).ToVector4(), // cream
        new Color( 0x80, 0x02, 0x0E, 0xFF ).ToVector4(), // cherry
        new Color( 0x80, 0x02, 0x0E, 0xFF ).ToVector4(), // stem
        new Color( 0xEC, 0xEC, 0xF8, 0x50 ).ToVector4(), // glass
      };

      int i = 0;
      foreach ( CustomModel.ModelPart part in shakeModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( colors[i++] );
      }
    }

    public static Powerup CreateRandomPowerup( Vector2 pos, bool includeGoldenShake )
    {
      if ( includeGoldenShake )
        return CreatePowerup( pos, PowerupType.GoldenShake );
      PowerupType min = includeGoldenShake ? PowerupType.GoldenShake : PowerupType.Hammer;
      return CreatePowerup( pos, (PowerupType)rand.Next( (int)min, (int)PowerupType.Lightning + 1 ) );
    }

    public static Powerup CreatePowerup( Vector2 pos, PowerupType type )
    {
      foreach ( Powerup powerup in pool )
      {
        if ( !powerup.alive )
        {
          powerup.Initialize( pos, type );
          return powerup;
        }
      }

      return null;
    }

    private void Initialize( Vector2 pos, PowerupType type )
    {
      Type = type;
      Body.Position = pos;
      Body.ClearEvents();
      Screen.PhysicsSpace.AddBody( Body );
      UpdateSelf = null;
      Activate = null;
      alive = true;
      owner = null;
      //rotateTime = 0f;
      nonTubeTime = 0f;
      rotationAngle = 0f;

      switch ( type )
      {
        case PowerupType.Shake:
          Size = 1f;
          Model = shakeModel;
          UpdateSelf += Rotate;
          Body.Collided += HandleShakeCollision;
          break;
        case PowerupType.GoldenShake:
          Size = .8f;
          Model = goldShakeModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Body.Collided += HandleGoldenShakeCollision;
          break;
        case PowerupType.Hammer:
          Size = .8f;
          Model = hammerModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Activate += ActivateCrush;
          Body.Collided += HandlePowerupCollision;
          break;
        case PowerupType.Laser:
          Size = .8f;
          Model = laserModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Activate += ActivateLaser;
          Body.Collided += HandlePowerupCollision;
          break;
        case PowerupType.Shrimp:
          Size = .8f;
          Model = shrimpModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Activate += ActivateShrink;
          Body.Collided += HandlePowerupCollision;
          break;
        case PowerupType.Lightning:
          Size = .8f;
          Model = boltModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Activate += ActivateLightning;
          Body.Collided += HandlePowerupCollision;
          break;
        default:
          throw new ArgumentOutOfRangeException( "powerup" );
      }

      Body.Radius = Size / 2f;
    }

    private Powerup( GameplayScreen screen )
      : base( screen )
    {
      Body = new PhysCircle( 1f, Vector2.Zero, 1f );
      Body.Flags = BodyFlags.Anchored | BodyFlags.Ghost;
      Body.Parent = this;

      Oscillator = new SpringInterpolater( 1, 10, 0 );
      SizeSpring = new SpringInterpolater( 1, 200, .15f * SpringInterpolater.GetCriticalDamping( 200 ) );
      LockToPlayerSpring = new SpringInterpolater( 2, 100, SpringInterpolater.GetCriticalDamping( 100 ) );
      RotationSpring = new SpringInterpolater( 1, 15, SpringInterpolater.GetCriticalDamping( 15 ) );
      Oscillator.Active = true;
      SizeSpring.Active = true;
      LockToPlayerSpring.Active = true;
      RotationSpring.Active = true;

      DrawOrder = 6;
    }

    public override void Update( GameTime gameTime )
    {
      if ( !InTube && owner == null && Body.Position.Y > Screen.Camera.Position.Y + DeathLine )
        Die();
      else if ( UpdateSelf != null )
        UpdateSelf( gameTime );

      world = initialTransforms[Model] * Matrix.CreateScale( Size ) * Matrix.CreateRotationY( rotationAngle );
      world *= Matrix.CreateTranslation( Body.Position.X, Body.Position.Y, 0f );
    }

    public void Use()
    {
      if ( owner != null )
        owner.Powerup = null;
      else
        return;

      if ( Activate != null )
        Activate();
      UpdateSelf += ShrivelUpAndDie;
      SizeSpring.SetDest( 0 );
      SizeSpring.SetSource( SizeSpring.GetSource()[0] * 1.25f );
      SizeSpring.B = SpringInterpolater.GetCriticalDamping( SizeSpring.K );
      owner.Powerup = null;
    }

    private void Die()
    {
      if ( Type == PowerupType.GoldenShake )
        Screen.ShakeIsOut = false;
      Screen.PhysicsSpace.RemoveBody( Body );
      Screen.ObjectTable.MoveToTrash( this );
      alive = false;
    }

    private void Rotate( GameTime gameTime )
    {
      rotationAngle = .25f * MathHelper.TwoPi * /*/rotateTime/*/Screen.AccumulatedTime/**/;
      //rotateTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    private void SineWave( GameTime gameTime )
    {
      if ( !InTube )
      {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Oscillator.Update( elapsed );
        SizeSpring.Update( elapsed );

        // leave some pixie dust behind
        for ( int i = 0; i < 3; ++i )
        {
          Vector3 offset = .75f * Body.Radius * Vector3.Normalize( rand.NextVector3() );
          Vector3 position = new Vector3( Body.Position, 0 );
          Screen.PinkPixieParticleSystem.AddParticle( position + offset, -offset );
        }

        float horizontalSpeed = 1.75f * Math.Sign( -exitTubePos.X );
        float horizontalDist = horizontalSpeed * nonTubeTime;
        if ( Math.Abs( horizontalDist ) > .85f * Math.Abs( 2 * exitTubePos.X ) )
          Oscillator.SetDest( exitTubePos.Y + 2.5f );
        Body.Position.X = exitTubePos.X + horizontalDist;
        Body.Position.Y = Oscillator.GetSource()[0];

        Size = SizeSpring.GetSource()[0];

        nonTubeTime += elapsed;
      }
    }

    private void LockToPlayer( GameTime gameTime )
    {
      // update position
      float[] sourcePos = LockToPlayerSpring.GetSource();
      Vector2 relativePos = new Vector2( sourcePos[0], sourcePos[1] );
      Body.Position = owner.BoundingCircle.Position + relativePos;

      // update scale
      Size = SizeSpring.GetSource()[0];

      if ( owner.Powerup != this )
        return;

      // update angle
      if ( RotationSpring.Active )
        rotationAngle = RotationSpring.GetSource()[0];

      // scale up the size if the powerup has reached its destination position
      float[] destPos = LockToPlayerSpring.GetDest();
      if ( SizeSpring.GetDest()[0] != .5f )
      {
        if ( Vector2.DistanceSquared( relativePos, new Vector2( destPos[0], destPos[1] ) ) < ( .125f * .125f ) )
        {
          SizeSpring.SetDest( .5f );
          RotationSpring.SetSource( MathHelper.WrapAngle( rotationAngle ) );
          RotationSpring.SetDest( MathHelper.TwoPi * 3f );
          RotationSpring.Active = true;
        }
      }

      // update springs
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      LockToPlayerSpring.Update( elapsed );
      SizeSpring.Update( elapsed );
      RotationSpring.Update( elapsed );
    }

    private void ShrivelUpAndDie( GameTime gameTime )
    {
      if ( Size < .125f )
      {
        Die();
        return;
      }

      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      SizeSpring.Update( elapsed );
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      device.VertexDeclaration = vertexDeclaration;
      renderState.AlphaBlendEnable = true;
      renderState.DepthBufferEnable = true;

      ////yeah...
      //if ( Model == shakeModel )
      //{
      //  renderState.CullMode = CullMode.CullClockwiseFace;
      //  Model.Draw( Screen.Camera.Position, world, Screen.View, Screen.Projection );
      //}

      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      Model.Draw( Screen.Camera.Position, world, Screen.View, Screen.Projection );
    }

    public bool HandleShakeCollision( Collision result )
    {
      if ( owner == null && result.BodyB.Parent is Player )
      {
        owner = (Player)result.BodyB.Parent;
        if ( !Screen.GameOver )
          owner.HUD.AddPoints( 1 );

        Screen.PhysicsSpace.RemoveBody( result.BodyA );
        Screen.ObjectTable.MoveToTrash( this );
        alive = false;

        owner.Position = new Vector3( owner.BoundingCircle.Position, 0 );
        GameCore.Instance.AudioManager.Play3DCue( "collect", owner, .8f );

        // make sparkle particles
        PixieParticleSystem system = Screen.PixieParticleSystem;
        for ( int i = 0; i < 20; ++i )
        {
          Vector3 pos = rand.NextVector3();
          if ( pos != Vector3.Zero )
          {
            pos.Normalize();
            pos *= ( 1.25f * Body.Radius * (float)rand.NextDouble() );
          }
          system.AddParticle( new Vector3( Body.Position, Player.Size ) + pos, Vector3.Zero/*.5f * pos*/ );
        }
      }

      return true;
    }

    public bool HandleGoldenShakeCollision( Collision result )
    {
      if ( owner != null ) return true; // maybe have the powerup avoid the player?

      Player player = result.BodyB.Parent as Player;
      if ( player != null && player.Powerup == null )
      {
        UpdateSelf -= SineWave;
        UpdateSelf += LockToPlayer;

        owner = player;
        LockToPlayerSpring.SetSource( Body.Position - player.BoundingCircle.Position );
        LockToPlayerSpring.SetDest( new Vector2( 0f, .75f * Player.Size ) );
        SizeSpring.SetDest( .3f );
        RotationSpring.Active = false;

        player.HUD.AddPoints( 7 );

        player.Powerup = this;

        owner.Position = new Vector3( owner.BoundingCircle.Position, 0 );
        GameCore.Instance.AudioManager.Play3DCue( "collect", owner, 1f );

        GameplayScreen.Instance.EndGame();
      }

      return true;
    }

    public bool HandlePowerupCollision( Collision result )
    {
      if ( owner != null ) return true; // maybe have the powerup avoid the player?

      Player player = result.BodyB.Parent as Player;
      if ( player != null && player.Powerup == null )
      {
        UpdateSelf -= SineWave;
        UpdateSelf += LockToPlayer;

        owner = player;
        LockToPlayerSpring.SetSource( Body.Position - player.BoundingCircle.Position );
        LockToPlayerSpring.SetDest( new Vector2( 0f, .75f * Player.Size ) );
        SizeSpring.SetDest( .3f );
        RotationSpring.Active = false;

        CollectedAt = screen.AccumulatedTime;

        player.Powerup = this;

        owner.Position = new Vector3( owner.BoundingCircle.Position, 0 );
        GameCore.Instance.AudioManager.Play3DCue( "sillySpin", owner, 1f );
      }

      return true;
    }

    public void ActivateShrink()
    {
      ReadOnlyCollection<Player> players = Screen.ObjectTable.GetObjects<Player>();
      for ( int i = 0; i < players.Count; ++i )
      {
        Player player = players[i];
        if ( player != owner )
        {
          player.Shrink();
          player.Position = new Vector3( player.BoundingCircle.Position, 0 );
          GameCore.Instance.AudioManager.Play3DCue( "shrimpDown", player, 1f );
        }
      }
    }

    public void ActivateCrush()
    {
      owner.Position = new Vector3( owner.BoundingCircle.Position, 0 );
      GameCore.Instance.AudioManager.Play3DCue( "hammerFlame", owner, 1f );
      owner.Crush();
    }

    public void ActivateLaser()
    {
      owner.Laser();
    }

    public void ActivateLightning()
    {
      ReadOnlyCollection<Player> players = Screen.ObjectTable.GetObjects<Player>();
      for ( int i = 0; i < players.Count; ++i )
      {
        if ( players[i] != owner )
          players[i].GetStunnedByLightning( owner );
      }

      if ( players.Count > 1 )
        GameCore.Instance.AudioManager.Play2DCue( "lightning", 1f );
    }

    #region IAudioEmitter Members

    public Vector3 Position
    {
      get { return new Vector3( Body.Position, 0 ); }
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
      get { return Vector3.Zero; }
    }

    #endregion
  }
}