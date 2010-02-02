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

namespace AvatarHamsterPanic.Objects
{
  enum PowerupType
  {
    ScoreCoin,
    Hammer,
    Laser,
    Shrimp,
    Lightning
  }

  class Powerup : GameObject
  {
    // event delegates
    private delegate void UpdateMethod( GameTime gameTime );

    Matrix world;
    int owner;
    bool alive;
    float rotateTime;
    float nonTubeTime;

    static VertexDeclaration vertexDeclaration;
    static GameplayScreen screen;
    static CustomModel shakeModel;
    static CustomModel shrimpModel;
    static CustomModel hammerModel;
    static CustomModel boltModel;

    static Powerup[] pool;


    event UpdateMethod UpdateSelf;

    public PhysCircle Body { get; private set; }
    public CustomModel Model { get; private set; }
    public float Size { get; private set; }

    bool inTube;
    Vector2 exitTubePos;
    public bool InTube 
    {
      get { return inTube; }
      set { inTube = value; if ( !inTube ) exitTubePos = Body.Position; }
    }
    public SpringInterpolater Oscillator { get; private set; }
    public SpringInterpolater SizeSpring { get; private set; }

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
      shrimpModel = content.Load<CustomModel>( "Models/shrimp" );
      hammerModel = content.Load<CustomModel>( "Models/hammer" );
      boltModel = content.Load<CustomModel>( "Models/bolt" );

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
        new Color( 0xEC, 0xEC, 0xF8, 0x19 ).ToVector4(), // glass
      };

      int i = 0;
      foreach ( CustomModel.ModelPart part in shakeModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( colors[i++] );
      }
    }

    public static Powerup CreateRandomPowerup( Vector2 pos )
    {
      return CreatePowerup( pos, (PowerupType)rand.Next( (int)PowerupType.Hammer, (int)PowerupType.Lightning + 1 ) );
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
      Body.Position = pos;
      Body.Released = false;
      Body.ClearEvents();
      PhysBody.AllBodies.Add( Body );
      UpdateSelf = null;
      alive = true;
      owner = -1;
      rotateTime = 0f;
      nonTubeTime = 0f;

      switch ( type )
      {
        case PowerupType.ScoreCoin:
          Size = .8f;
          Model = shakeModel;
          UpdateSelf += Rotate;
          Body.Collided += HandleCoinCollision;
          break;
        case PowerupType.Hammer:
          Size = .8f;
          Model = hammerModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Body.Collided += HandleCoinCollision;
          break;
        case PowerupType.Laser:
          Size = .8f;
          Model = shakeModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Body.Collided += HandleCoinCollision;
          break;
        case PowerupType.Shrimp:
          Size = .7f;
          Model = shrimpModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Body.Collided += HandleCoinCollision;
          break;
        case PowerupType.Lightning:
          Size = .8f;
          Model = boltModel;
          UpdateSelf += Rotate;
          UpdateSelf += SineWave;
          Body.Collided += HandleCoinCollision;
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
      Body.Flags = PhysBodyFlags.Anchored | PhysBodyFlags.Ghost;
      Body.Parent = this;
      Body.Release();

      Oscillator = new SpringInterpolater( 1, 10, 0 );
      SizeSpring = new SpringInterpolater( 1, 200, .15f * SpringInterpolater.GetCriticalDamping( 200 ) );
      Oscillator.Active = true;
      SizeSpring.Active = true;
    }

    public override void Update( GameTime gameTime )
    {
      if ( !InTube && Body.Position.Y > Screen.Camera.Position.Y + DeathLine )
      {
        Body.Release();
        Screen.ObjectTable.MoveToTrash( this );
        alive = false;
      }
      else if ( UpdateSelf != null )
        UpdateSelf( gameTime );
    }

    private void Rotate( GameTime gameTime )
    {
      float angle = .25f * MathHelper.TwoPi * /*/rotateTime/*/(float)gameTime.TotalGameTime.TotalSeconds/**/;
      world = Matrix.CreateScale( Size ) * Matrix.CreateRotationY( angle );
      world *= Matrix.CreateTranslation( new Vector3( Body.Position, 0f ) );
      rotateTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    private void SineWave( GameTime gameTime )
    {
      if ( !InTube )
      {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Oscillator.Update( elapsed );
        SizeSpring.Update( elapsed );

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

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      device.VertexDeclaration = vertexDeclaration;
      renderState.AlphaBlendEnable = true;
      renderState.DepthBufferEnable = true;

      if ( Model == shakeModel )
      {
        renderState.CullMode = CullMode.CullClockwiseFace;
        Model.Draw( Screen.Camera.Position, world, Screen.View, Screen.Projection );
      }

      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      Model.Draw( Screen.Camera.Position, world, Screen.View, Screen.Projection );
    }

    public bool HandleCoinCollision( CollisResult result )
    {
      if ( owner == -1 && result.BodyB.Parent is Player )
      {
        Player player = (Player)result.BodyB.Parent;
        owner = player.PlayerNumber;
        player.HUD.AddPoints( 1 );

        result.BodyA.Release();
        Screen.ObjectTable.MoveToTrash( this );
        alive = false;

        // make sparkle particles
        SparkleParticleSystem system = Screen.SparkleParticleSystem;
        for ( int i = 0; i < 20; ++i )
        {
          Vector3 pos = rand.NextVector3();
          if ( pos != Vector3.Zero )
          {
            pos.Normalize();
            pos *= ( Body.Radius * (float)rand.NextDouble() );
          }
          system.AddParticle( new Vector3( Body.Position, 0 ) + pos, .5f * pos );
        }
      }

      return true;
    }
  }
}