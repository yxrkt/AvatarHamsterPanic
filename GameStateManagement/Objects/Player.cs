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
  class Player
  {
    public static float Scale { get; private set; }

    private int m_id = 0;
    private PhysCircle m_circle = null;
    private Avatar m_avatar = null;
    private Model m_wheel = null;
    private float m_lastVelX = 0f;
    private ContentManager m_content = null;

    static Player()
    {
      Scale = 1.3f;
    }

    public Player( Vector2 pos, ContentManager content )
    {
      AvatarDescription.CreateRandom();
      m_content = content;
      m_wheel = m_content.Load<Model>( "wheel" );

      AvatarDescription avatar = null;
      if ( Gamer.SignedInGamers[PlayerIndex.One] != null )
        avatar = Gamer.SignedInGamers[PlayerIndex.One].Avatar;
      else
        avatar = AvatarDescription.CreateRandom();

      CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", content );
      m_avatar = new Avatar( avatar, data, .55f * Scale, Vector2.UnitX, pos );
      m_avatar.Content = m_content;
      m_circle = new PhysCircle( Scale / 2f, pos, 10f );
    }

    ~Player()
    {
      m_circle.Release();
    }

    public int ID { get { return m_id; } }
    public PhysCircle BoundingCircle { get { return m_circle; } }
    public Model WheelModel { get { return m_wheel; } }
    public Avatar Avatar { get { return m_avatar; } set { m_avatar = value; } }

    public void GetWheelTransform( out Matrix transform )
    {
      Matrix matTrans, matRot, matScale;
      Matrix.CreateTranslation( m_circle.Position.X, m_circle.Position.Y, 0f, out matTrans );
      Matrix.CreateRotationZ( m_circle.Angle, out matRot );
      Matrix.CreateScale( Scale, out matScale );

      Matrix.Multiply( ref matScale, ref matRot, out transform );
      Matrix.Multiply( ref transform, ref matTrans, out transform );
    }

    public void Update( GameTime gameTime )
    {
      // update the avatar's position and orientation
      Avatar.Position = new Vector3( m_circle.Position.X, m_circle.Position.Y - Scale / 2f, 0f );
      Avatar.Direction = new Vector3( m_circle.Velocity.X > 0f ? 1f : -1f, 0f, 0f );

      double absVelX = Math.Abs( (double)m_circle.Velocity.X );

      // update avatar's animation
      double walkThresh = 4.0;
      double animScaleFactor = .20;

      if ( absVelX <= walkThresh )
      {
        animScaleFactor = 1.0;
        CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Walk", m_content );
        m_avatar.SetAnimation( data );
      }
      else
      {
        CustomAvatarAnimationData data = CustomAvatarAnimationData.GetAvatarAnimationData( "Run", m_content );
        m_avatar.SetAnimation( data );
      }

      double animScale = animScaleFactor * absVelX;
      Avatar.Update( TimeSpan.FromSeconds( animScale * gameTime.ElapsedGameTime.TotalSeconds ), true );

      m_lastVelX = (float)absVelX;
    }
  }
}