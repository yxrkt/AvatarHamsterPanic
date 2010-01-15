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
using GameStateManagement;
using Microsoft.Xna.Framework.Input;

namespace GameObjects
{
  class Player : GameObject
  {
    static float uCoord = (float)Math.Cos( MathHelper.ToRadians( 15 ) );
    static float vCoord = (float)Math.Sin( MathHelper.ToRadians( 15 ) );

    ParticleEmitter emitter;
    GamerProfile gamerProfile;

    public static float Scale { get; private set; }
    public static float DeathLine { get; set; }
    public static double RespawnLength { get; private set; }

    public PlayerIndex PlayerIndex { get; set; }
    public PhysCircle BoundingCircle { get; private set; }
    public Model WheelModel { get; private set; }
    public Avatar Avatar { get; set; }
    public double RespawnTime { get; private set; }
    public bool Respawning { get { return RespawnTime < RespawnLength; } }
    public SignedInGamer Gamer { get; private set; }

    static Player()
    {
      Scale = 1.3f;
      DeathLine = 3.5f;
      RespawnLength = 1f;
    }

    public Player( GameplayScreen screen, PlayerIndex playerIndex, Avatar avatar, Vector2 pos )
      : base( screen )
    {
      WheelModel = Screen.Content.Load<Model>( "wheel" );

      RespawnTime = float.MaxValue;

      PlayerIndex = playerIndex;
      if ( playerIndex >= PlayerIndex.One )
      {
        Gamer = SignedInGamer.SignedInGamers[playerIndex];
        gamerProfile = Gamer.GetProfile();
      }

      Avatar = avatar;
      BoundingCircle = new PhysCircle( Scale / 2f, pos, 10f );
      BoundingCircle.Parent = this;
      BoundingCircle.Elasticity = .4f;
      BoundingCircle.Friction = .5f;
      BoundingCircle.Collided += KillBlockIfPwnt;
      BoundingCircle.Responded += new PhysBody.CollisionEvent( HandleCollisionResponse );

      Texture2D particleTex = screen.Content.Load<Texture2D>( "particleRound" );
      ParticleConeFactory factory = new ParticleConeFactory( Vector3.Up, MathHelper.ToRadians( 30f ), 2f, 4f, 
                                                             .5f, .75f, .03f, .03f, 2.5f, Color.White, 3 );
      emitter = new ParticleEmitter( screen, Vector3.Zero, factory, particleTex );
      screen.ObjectTable.Add( emitter );
    }

    public void GetWheelTransform( out Matrix transform )
    {
      Matrix matTrans, matRot, matScale;
      Matrix.CreateTranslation( BoundingCircle.Position.X, BoundingCircle.Position.Y, 0f, out matTrans );
      Matrix.CreateRotationZ( BoundingCircle.Angle, out matRot );
      Matrix.CreateScale( Scale, out matScale );

      Matrix.Multiply( ref matScale, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    private bool KillBlockIfPwnt( PhysBody collider, CollisResult data )
    {
      if ( Respawning )
      {
        if ( data.Object.Parent is FloorBlock )
        {
          FloorBlock block = (FloorBlock)data.Object.Parent;

          // make sure this only happens once
          if ( ( block.BoundingPolygon.Flags & PhysBodyFlags.Ghost ) == 0 )
          {
            // remove the block
            block.BoundingPolygon.Flags |= PhysBodyFlags.Ghost;
            block.BoundingPolygon.Release();
            Screen.ObjectTable.MoveToTrash( block );

            // add the exploding block particle system
            Vector3 position = new Vector3( block.BoundingPolygon.Position, 0f );
            ModelMeshCollection meshes = Screen.Content.Load<Model>( "block_broken" ).Meshes;
            Screen.ObjectTable.Add( new MeshClusterExplosion( Screen, position, meshes ) );

            return false;
          }
        }
      }
      return true;
    }

    private bool HandleCollisionResponse( PhysBody collider, CollisResult data )
    {
      PhysCircle circle = BoundingCircle;

      // set emitter direction
      ParticleConeFactory factory = (ParticleConeFactory)emitter.Factory;

      // set emitter position
      Vector3 position = new Vector3( data.Intersection, FloorBlock.Size / 2f - .2f );
      emitter.Position = position;

      // spit some particles
      Vector2 r = Vector2.Normalize( data.Intersection - collider.Position );
      Vector2 vp = circle.AngularVelocity * circle.Radius * new Vector2( -r.Y, r.X );
      Vector2 dir = circle.Velocity;

      Vector2 vpn = Vector2.Normalize( vp );
      factory.Direction = new Vector3( uCoord * vpn + vCoord * -r, 0f );

      Vector2 sum = vp + dir;
      if ( sum != Vector2.Zero )
      {
        float sumLength = sum.Length();
        if ( sumLength > .05f )
        {
          float numToSpit = .5f * sumLength;
          emitter.Position += new Vector3( ( vpn * .1f ), 0f );
          emitter.Spit( numToSpit );
          position.Z = -position.Z;
          emitter.Position = position;
          emitter.Spit( numToSpit );
        }
      }

      return true;
    }

    public override void Update( GameTime gameTime )
    {
      UpdateAvatar( gameTime );

      Vector2 pos = BoundingCircle.Position;

      if ( !Respawning )
      {
        // check if player should be pwnt
        if ( pos.Y >= Screen.Camera.Position.Y + DeathLine )
        {
          RespawnTime = 0f;
          BoundingCircle.Velocity += new Vector2( 0f, -3f );
        }
      }
      else
      {
        // something something something darkside
        RespawnTime += gameTime.ElapsedGameTime.TotalSeconds;
      }
    }

    public void HandleInput( InputState input )
    {
      if ( PlayerIndex < PlayerIndex.One ) return;

      PhysCircle circle = BoundingCircle;
      GamePadState gamePadState = input.CurrentGamePadStates[(int)PlayerIndex];

      float forceY = 0f;
      float forceX = 0f;
      float maxVelX = 4f;

      if ( gamePadState.Triggers.Left != 0f )
      {
        forceX = -200f;
        maxVelX = 8f;
      }
      if ( gamePadState.Triggers.Right != 0f )
      {
        if ( forceX != 0f )
        {
          forceX = 0f;
          forceY = 40f;
          maxVelX = 4f;
        }
        else
        {
          forceX = 200f;
          maxVelX = 8f;
        }
      }

      float maxAngVel = MathHelper.TwoPi;

      Vector2 leftStick = gamePadState.ThumbSticks.Left;
      float torqueScale = -100f;
      float torque = torqueScale * leftStick.X;

      // torque
      if ( circle.AngularVelocity < 0f && torque < 0f )
      {
        float reqTorque = PhysBody.GetForceRequired( -maxAngVel, circle.AngularVelocity,
                                                     circle.Torque, circle.MomentOfIntertia, 1f / 60f );
        torque = Math.Max( torque, reqTorque );
      }
      else if ( circle.AngularVelocity > 0f && torque > 0f )
      {
        float reqTorque = PhysBody.GetForceRequired( maxAngVel, circle.AngularVelocity,
                                                     circle.Torque, circle.MomentOfIntertia, 1f / 60f );
        torque = Math.Min( torque, reqTorque );
      }
      circle.Torque += torque;

      // linear force
      if ( circle.Velocity.X < 0f && forceX < 0f )
        forceX = Math.Max( forceX, PhysBody.GetForceRequired( -maxVelX, circle.Velocity.X, circle.Force.X, circle.Mass, 1f / 60f ) );
      else if ( circle.Velocity.X > 0f && forceX > 0f )
        forceX = Math.Min( forceX, PhysBody.GetForceRequired( maxVelX, circle.Velocity.X, circle.Force.X, circle.Mass, 1f / 60f ) );
      circle.Force += new Vector2( forceX, forceY );
    }

    public override void Draw()
    {
      if ( Respawning && ( (int)Math.Floor( RespawnTime * 16f ) % 2 ) == 0 )
        return;

      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = new VertexDeclaration( graphics, VertexPositionNormalTexture.VertexElements );
      SetRenderState( graphics.RenderState );

      Matrix transform;

      // draw wheel
      foreach ( ModelMesh mesh in WheelModel.Meshes )
      {
        foreach ( BasicEffect effect in mesh.Effects )
        {
          effect.EnableDefaultLighting();

          GetWheelTransform( out transform );
          effect.World = transform;
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
        }

        mesh.Draw();
      }

      // draw avatar
      Avatar.Renderer.View = Screen.View;
      Avatar.Renderer.Projection = Screen.Projection;

      Matrix matRot = Matrix.CreateWorld( Vector3.Zero, Avatar.Direction, Screen.Camera.Up );
      Matrix matTrans = Matrix.CreateTranslation( Avatar.Position );
      Avatar.Renderer.World = Matrix.CreateScale( Avatar.Scale ) * matRot * matTrans;
      Avatar.Renderer.Draw( Avatar.BoneTransforms, Avatar.Expression );
    }

    public void DrawHUD( SpriteBatch spriteBatch )
    {
      Rectangle rect = new Rectangle( 100 * ( (int)PlayerIndex + 1 ), 400, 64, 64 );
      spriteBatch.Draw( gamerProfile.GamerPicture, rect, Color.White );
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.CullCounterClockwiseFace;

      renderState.AlphaBlendEnable = false;

      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }

    private void UpdateAvatar( GameTime gameTime )
    {
      Avatar.Position = new Vector3( BoundingCircle.Position.X, BoundingCircle.Position.Y - Scale / 2.5f, 0f );

      double absAngVel = Math.Abs( (double)BoundingCircle.AngularVelocity );

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
          CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", Screen.Content );
          Avatar.SetAnimation( data );
        }
        else
        {
          CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Run", Screen.Content );
          Avatar.SetAnimation( data );
        }
        double animScale = animScaleFactor * absAngVel;
        Avatar.Update( TimeSpan.FromSeconds( animScale * gameTime.ElapsedGameTime.TotalSeconds ), true );
      }
    }
  }
}