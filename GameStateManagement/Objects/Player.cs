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

namespace GameObjects
{
  class Player : AutoContain
  {
    public static float Scale { get; private set; }
    public static List<AutoContain> AllPlayers { get { return AutoContain.AllObjects[typeof( Player )]; } }

    private ContentManager content = null;

    static Player()
    {
      Scale = 1.3f;
    }

    public Player( Vector2 pos, ContentManager content )
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
      Avatar = new Avatar( avatar, data, .55f * Scale, Vector2.UnitX, pos );
      Avatar.Content = this.content;
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

    public void Update( GameTime gameTime )
    {
      // update the avatar's position and orientation
      Avatar.Position = new Vector3( BoundingCircle.Position.X, BoundingCircle.Position.Y - Scale / 2f, 0f );
      Avatar.Direction = new Vector3( BoundingCircle.Velocity.X > 0f ? 1f : -1f, 0f, 0f );

      double absVelX = Math.Abs( (double)BoundingCircle.Velocity.X );

      // update avatar's animation
      double walkThresh = 4.0;
      double animScaleFactor = .20;

      if ( absVelX <= walkThresh )
      {
        animScaleFactor = 1.0;
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
    }
  }
}