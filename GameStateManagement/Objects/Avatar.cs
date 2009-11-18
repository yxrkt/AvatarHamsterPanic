using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CustomAvatarAnimationFramework;

namespace GameObjects
{
  class Avatar
  {
    public AvatarRenderer Renderer { get; set; }
    public float Scale { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Position { get; set; }
    public float BlendTime { get; set; }
    public ReadOnlyCollection<Matrix> BoneTransforms { get; private set; }
    public AvatarExpression Expression { get { return currentAnimation.Expression; } }
    public ContentManager Content { get; set; }

    private AvatarAnimationWrapper currentAnimation = null;
    private AvatarAnimationWrapper targetAnimation = null;
    private Matrix[] avatarBones = new Matrix[AvatarRenderer.BoneCount];
    private float blendTimeCurrent = 0f;

    public Avatar( AvatarDescription body, CustomAvatarAnimationData anim, float scale, Vector2 dir, Vector2 pos )
      : this( body, new AvatarAnimationWrapper( anim ), scale, dir, pos )
    {
    }

    public Avatar( AvatarDescription body, AvatarAnimationPreset anim, float scale, Vector2 dir, Vector2 pos )
      : this( body, new AvatarAnimationWrapper( anim ), scale, dir, pos )
    {
    }

    private Avatar( AvatarDescription body, AvatarAnimationWrapper anim, float scale, Vector2 dir, Vector2 pos )
    {
      Scale = scale;
      Direction = new Vector3( dir, 0f );
      Position = new Vector3( pos, 0f );
      BlendTime = .25f;

      Renderer = new AvatarRenderer( body );
      currentAnimation = anim;

      BoneTransforms = new ReadOnlyCollection<Matrix>( avatarBones );
    }

    public void Update( TimeSpan time, bool loop )
    {
      currentAnimation.Update( time, loop );

      if ( targetAnimation == null )
      {
        currentAnimation.BoneTransforms.CopyTo( avatarBones, 0 );
      }
      else
      {
        targetAnimation.Update( time, loop );

        ReadOnlyCollection<Matrix> currentBoneTransforms = currentAnimation.BoneTransforms;
        ReadOnlyCollection<Matrix> targetBoneTransforms = targetAnimation.BoneTransforms;

        float elapsed = (float)time.TotalSeconds;
        blendTimeCurrent += elapsed;

        float t = blendTimeCurrent / BlendTime;

        if ( t >= 1f )
        {
          currentAnimation = targetAnimation;
          targetAnimation = null;
          t = 1f;
        }

        Quaternion currentRotation, targetRotation, finalRotation;
        Vector3 currentPosition, targetPosition, finalPosition;

        int nBones = avatarBones.Length;
        for ( int i = 0; i < nBones; ++i )
        {
          currentRotation = Quaternion.CreateFromRotationMatrix( currentBoneTransforms[i] );
          targetRotation = Quaternion.CreateFromRotationMatrix( targetBoneTransforms[i] );
          Quaternion.Slerp( ref currentRotation, ref targetRotation, t, out finalRotation );

          currentPosition = currentBoneTransforms[i].Translation;
          targetPosition = targetBoneTransforms[i].Translation;
          Vector3.Lerp( ref currentPosition, ref targetPosition, t, out finalPosition );

          avatarBones[i] = Matrix.CreateFromQuaternion( finalRotation ) *
                           Matrix.CreateTranslation( finalPosition );
        }
      }
    }

    public void SetAnimation( AvatarAnimationPreset preset )
    {
      AvatarAnimationWrapper animation = new AvatarAnimationWrapper( preset );
      if ( animation != currentAnimation && animation != targetAnimation )
      {
        targetAnimation = animation;
        targetAnimation.CurrentPosition = TimeSpan.Zero;
        blendTimeCurrent = 0f;
      }
    }

    public void SetAnimation( CustomAvatarAnimationData data )
    {
      AvatarAnimationWrapper animation = new AvatarAnimationWrapper( data );
      if ( animation != currentAnimation && animation != targetAnimation )
      {
        targetAnimation = animation;
        targetAnimation.CurrentPosition = TimeSpan.Zero;
        blendTimeCurrent = 0f;
      }
    }
  }
}
