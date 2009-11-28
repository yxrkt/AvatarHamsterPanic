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

namespace GameObjects
{
  class Player : GameObject
  {
    public static float Scale { get; private set; }

    private ContentManager content = null;

    static Player()
    {
      Scale = 1.3f;
    }

    public Player( GameplayScreen screen, Vector2 pos, ContentManager content )
      : base( screen )
    {
      AvatarDescription.CreateRandom();
      this.content = content;
      WheelModel = this.content.Load<Model>( "wheel" );

      AvatarDescription avatar = null;
      if ( Gamer.SignedInGamers[PlayerIndex.One] != null )
        avatar = Gamer.SignedInGamers[PlayerIndex.One].Avatar;
      else
        avatar = AvatarDescription.CreateRandom();

      CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", this.content );
      Avatar = new Avatar( avatar, data, .45f * Scale, Vector2.UnitX, pos );
      BoundingCircle = new PhysCircle( Scale / 2f, pos, 10f );
    }

    ~Player()
    {
      BoundingCircle.Release();
    }

    public PhysCircle BoundingCircle { get; private set; }
    public Model WheelModel { get; private set; }
    public Avatar Avatar { get; set; }

    public void GetWheelTransform( out Matrix transform )
    {
      Matrix matTrans, matRot, matScale;
      Matrix.CreateTranslation( BoundingCircle.Position.X, BoundingCircle.Position.Y, 0f, out matTrans );
      Matrix.CreateRotationZ( BoundingCircle.Angle, out matRot );
      Matrix.CreateScale( Scale, out matScale );

      Matrix.Multiply( ref matScale, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public override void Update( GameTime gameTime )
    {
      // update the avatar's position and orientation
      Avatar.Position = new Vector3( BoundingCircle.Position.X, BoundingCircle.Position.Y - Scale / 2.5f, 0f );
      Avatar.Direction = new Vector3( BoundingCircle.AngularVelocity < 0f ? 1f : -1f, 0f, 0f );

      double absVelX = Math.Abs( (double)BoundingCircle.AngularVelocity );

      // update avatar's animation
      double walkThresh = 4.0;
      double animScaleFactor = .10;

      if ( absVelX <= walkThresh )
      {
        animScaleFactor = .8;
        CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", content );
        Avatar.SetAnimation( data );
      }
      else
      {
        CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Run", content );
        Avatar.SetAnimation( data );
      }

      double animScale = animScaleFactor * absVelX;
      Avatar.Update( TimeSpan.FromSeconds( animScale * gameTime.ElapsedGameTime.TotalSeconds ), true );

      // update transforms for wheel
      Matrix transform;

      foreach ( ModelMesh mesh in WheelModel.Meshes )
      {
        foreach ( BasicEffect effect in mesh.Effects )
        {
          GetWheelTransform( out transform );
          effect.World = transform;
        }
      }
    }

    public override void Draw()
    {
      // draw wheel
      foreach ( ModelMesh mesh in WheelModel.Meshes )
      {
        foreach ( BasicEffect effect in mesh.Effects )
        {
          effect.EnableDefaultLighting();
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
  }
}