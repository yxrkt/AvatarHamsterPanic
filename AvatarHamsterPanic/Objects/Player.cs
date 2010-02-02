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

namespace AvatarHamsterPanic.Objects
{
  class Player : GameObject
  {
    static readonly float particleCoordU = (float)Math.Cos( MathHelper.ToRadians( 15 ) );
    static readonly float particleCoordV = (float)Math.Sin( MathHelper.ToRadians( 15 ) );

    float jumpRegistered;
    const float jumpTimeout = .125f;
    GameTime lastGameTime = new GameTime();
    float lastCollision;
    VertexDeclaration vertexDeclaration;
    CustomAvatarAnimationData walkAnim;
    CustomAvatarAnimationData runAnim;

    public static float Size { get; private set; }
    public static float DeathLine { get; set; }
    public static double RespawnLength { get; private set; }

    static Random random = new Random();

    public float Scale { get; private set; }
    public bool Boosting { get; private set; }
    public float BoostBurnRate { get; set; }
    public float BoostRechargeRate { get; set; }
    public int PlayerNumber { get; private set; }
    public PlayerIndex PlayerIndex { get; private set; }
    public PhysCircle BoundingCircle { get; private set; }
    public CustomModel WheelModel { get; private set; }
    public Avatar Avatar { get; set; }
    public double RespawnTime { get; private set; }
    public bool Respawning { get { return RespawnTime < RespawnLength; } }
    public PlayerHUD HUD { get; private set; }
    public Powerup Powerup { get; set; }

    static Player()
    {
      Size = 1.4f;
      DeathLine = 3.5f;
      RespawnLength = 1f;
    }

    public Player( GameplayScreen screen, int playerNumber, PlayerIndex playerIndex, Avatar avatar, Vector2 pos )
      : base( screen )
    {
      WheelModel = screen.Content.Load<CustomModel>( "Models/hamsterBall" );
      foreach ( CustomModelSample.CustomModel.ModelPart part in WheelModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( new Vector4( .8f, .7f, 1f, .3f ) );
        part.Effect.Parameters["SpecularPower"].SetValue( 400 );
      }
      DrawOrder = 3;

      float depth = screen.Camera.Position.Z - Size / 2;
      DeathLine = depth * (float)Math.Tan( screen.Camera.Fov / 2f );

      RespawnTime = float.MaxValue;

      Scale = 1f;
      PlayerIndex = playerIndex;
      PlayerNumber = playerNumber;
      BoostBurnRate = 2f;
      BoostRechargeRate = .125f;

      Avatar = avatar;
      BoundingCircle = new PhysCircle( Size / 2f, pos, 10f );
      BoundingCircle.Parent = this;
      BoundingCircle.Elasticity = .4f;
      BoundingCircle.Friction = .5f;
      BoundingCircle.Responded += HandleCollisionResponse;

      walkAnim = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", Screen.Content );
      runAnim  = CustomAvatarAnimationData.GetAvatarAnimationData( "Run", Screen.Content );

      if ( playerIndex >= PlayerIndex.One )
        HUD = new PlayerHUD( this, SignedInGamer.SignedInGamers[playerIndex] );
      else
        HUD = new PlayerHUD( this, null );

      vertexDeclaration = new VertexDeclaration( screen.ScreenManager.GraphicsDevice, 
                                                 VertexPositionNormalTexture.VertexElements );
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

    private bool HandleCollisionResponse( CollisResult data )
    {
      PhysCircle circle = BoundingCircle;

      // keep track of last time of collision (for jumping)
      lastCollision = (float)lastGameTime.TotalGameTime.TotalSeconds;

      // set emitter position
      SparkParticleSystem sparkSystem = Screen.SparkParticleSystem;

      Vector3 position = new Vector3( data.Intersection, 0f );
      //emitter.Position = position;

      // spit some particles
      Vector2 r = Vector2.Normalize( data.Intersection - data.BodyA.Position );
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

    public override void Update( GameTime gameTime )
    {
      lastGameTime = gameTime;

      UpdateAvatar( gameTime );
      HUD.Update( gameTime );

      if ( !Respawning )
      {
        // check if player should be pwnt
        if ( BoundingCircle.Position.Y >= Screen.Camera.Position.Y + DeathLine - Size * Scale / 2f )
        {
          RespawnTime = 0f;
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
      if ( PlayerIndex < PlayerIndex.One ) return;

      PhysCircle circle = BoundingCircle;
      GamePadState gamePadState = input.CurrentGamePadStates[(int)PlayerIndex];

      //// == testing garbage ==
      //PlayerIndex trash;
      //if ( input.IsNewButtonPress( Buttons.B, null, out trash ) )
      //  HUD.Place = ( HUD.Place ) % 4 + 1;

      //PlayerIndex playerIndex = PlayerIndex;
      //if ( input.IsNewButtonPress( Buttons.X, playerIndex, out playerIndex ) )
      //{
      //  Scale = .5f;
      //  BoundingCircle.Radius = ( Scale * Size ) / 2f;
      //}
      //else if ( input.IsNewButtonPress( Buttons.Y, playerIndex, out playerIndex ) )
      //{
      //  Scale = 1f;
      //  BoundingCircle.Radius = Size / 2f;
      //}
      //// == testing garbage ==

      // powerups
      PlayerIndex playerIndex = PlayerIndex;
      if ( input.IsNewButtonPress( Buttons.X, playerIndex, out playerIndex ) )
      {
        if ( Powerup != null )
          Powerup.Use();
      }

      // movement
      float forceY = 0f;
      float forceX = 0f;
      float maxVelX = 4f;

      Boosting = false;
      if ( gamePadState.Triggers.Left != 0f )
      {
        Boosting = true;
        forceX = -200f;
        maxVelX = 6f;
      }
      if ( gamePadState.Triggers.Right != 0f )
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

      Vector2 leftStick = gamePadState.ThumbSticks.Left;
      float torqueScale = -100f;
      float torque = torqueScale * leftStick.X;

      float elapsed = (float)lastGameTime.ElapsedGameTime.TotalSeconds;//1f / 60f;

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
      PlayerIndex ret;
      if ( input.IsNewButtonPress( Buttons.A, PlayerIndex, out ret ) )
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
        Avatar.SetAnimation( AvatarAnimationPreset.Celebrate );
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
  }
}