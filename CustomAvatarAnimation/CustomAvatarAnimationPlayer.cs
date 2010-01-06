#region File Description
//-----------------------------------------------------------------------------
// CustomAvatarAnimationPlayer.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Content;
#endregion

namespace CustomAvatarAnimationFramework
{
    /// <summary>
    /// This type implements an animation at runtime, including the 
    /// current state and updating that state for time.
    /// </summary>
    public class CustomAvatarAnimation : CustomAvatarAnimationData
    {
        #region Current Animation State

        /// <summary>
        /// The current keyframe in the animation.
        /// </summary>
        private int currentKeyframe = 0;

        /// <summary>
        /// The current temporal position in the animation.
        /// </summary>
        private TimeSpan currentPosition = TimeSpan.Zero;

        /// <summary>
        /// The current temporal position in the animation.
        /// </summary>
        public TimeSpan CurrentPosition
        {
            get
            {
                return currentPosition;
            }
            set
            {
                currentPosition = value;

                // Set the current keyframe to 0 since we don't know where we are
                // in the list of keyframes. The next update will set the correct 
                // keyframe.
                currentKeyframe = 0;

                // update the animation for the new position, 
                // elapsing zero additional time
                Update(TimeSpan.Zero, false);
            }
        }

        /// <summary>
        /// The current expression in the animation.
        /// </summary>
        public AvatarExpression Expression { get; private set; }

        /// <summary>
        /// The current position of the bones as the current time in the animation.
        /// </summary>
        Matrix[] avatarBoneTransforms = new Matrix[AvatarRenderer.BoneCount];

        /// <summary>
        /// The current position of the bones as the current time in the animation.
        /// </summary>
        public ReadOnlyCollection<Matrix> BoneTransforms { get; private set; }

        #endregion


        #region Initialization

        /// <summary>
        /// Constructs a new CustomAvatarAnimationPlayer object.
        /// </summary>
        /// <param name="name">The name of the animation.</param>
        /// <param name="length">The length of the animation.</param>
        /// <param name="keyframes">The keyframes in the animation.</param>
        public CustomAvatarAnimation(string name, TimeSpan length,
                                           List<AvatarKeyFrame> keyframes) :
            base(name, length, keyframes)
        {
            // Reset the current bone transforms
            for (int i = 0; i < AvatarRenderer.BoneCount; i++)
                avatarBoneTransforms[i] = Matrix.Identity;

            BoneTransforms = new ReadOnlyCollection<Matrix>( avatarBoneTransforms );

            Expression = new AvatarExpression();

            // Update the current bone transforms to the first position in the animation
            Update(TimeSpan.Zero, false);
        }

        #endregion


        #region Updating

        /// <summary>
        /// Updates the current position of the animation.
        /// </summary>
        /// <param name="timeSpan">The elapsed time since the last update.</param>
        /// <param name="loop">If true, the animation will loop.</param>
        public void Update(TimeSpan timeSpan, bool loop)
        {
            // Add the elapsed time to the current time.
            currentPosition += timeSpan;

            // Check current time against the length
            if (currentPosition > Length)
            {
                if (loop)
                {
                    // Find the right time in the new loop iteration
                    while (currentPosition > Length)
                    {
                        currentPosition -= Length;
                    }
                    // Set the keyframe to 0.
                    currentKeyframe = 0;
                }
                else
                {
                    // If the animation is not looping, 
                    // then set the time to the end of the animation.
                    currentPosition = Length;
                }
            }
            // Check to see if we are less than zero
            else if (currentPosition < TimeSpan.Zero)
            {
                if (loop)
                {
                    // If the animation is looping, 
                    // then find the right time in the new loop iteration
                    while (currentPosition < TimeSpan.Zero)
                    {
                        currentPosition += Length;
                    }
                    // Set the keyframe to the last keyframe
                    currentKeyframe = Keyframes.Count - 1;
                }
                else
                {
                    // If the animation is not looping, 
                    // then set the time to the beginning of the animation.
                    currentPosition = TimeSpan.Zero;
                }
            }

            // Update the bone transforms based on the current time.
            UpdateBoneTransforms(timeSpan >= TimeSpan.Zero);
        }


        /// <summary>
        /// Updates the transforms with the correct keyframes based on the current time.
        /// </summary>
        /// <param name="playingForward">
        /// If true, the animation is playing forward; otherwise, it is playing backwards
        /// </param>
        private void UpdateBoneTransforms(bool playingForward)
        {
            if (playingForward)
            {
                while (currentKeyframe < Keyframes.Count)
                {
                    // Get the current keyframe
                    AvatarKeyFrame keyframe = Keyframes[currentKeyframe];

                    // Stop when we've read up to the current time.
                    if (keyframe.Time > currentPosition)
                        break;

                    // Apply the current keyframe's transform to the bone array.
                    avatarBoneTransforms[keyframe.Bone] = keyframe.Transform;

                    // Move the current keyframe forward.
                    currentKeyframe++;
                }
            }
            else
            {
                while (currentKeyframe >= 0)
                {
                    // Get the current keyframe
                    AvatarKeyFrame keyframe = Keyframes[currentKeyframe];

                    // Stop when we've read back to the current time.
                    if (keyframe.Time < currentPosition)
                        break;

                    // Apply the current keyframe's transform to the bone array.
                    avatarBoneTransforms[keyframe.Bone] = keyframe.Transform;

                    // Move the current keyframe backwards.
                    currentKeyframe--;
                }
            }
        }

        #endregion
    }

    public class AvatarAnimationWrapper
    {
      /// <summary>
      /// The current standard animation.
      /// </summary>
      private AvatarAnimation animation = null;

      /// <summary>
      /// The current custom animation.
      /// </summary>
      private CustomAvatarAnimation customAnimation = null;

      private AvatarAnimationPreset standardID = (AvatarAnimationPreset)( -1 );
      private string customID = "";

      /// <summary>
      /// Gets the expression of the related animation at the current time position.
      /// </summary>
      public AvatarExpression Expression
      {
        get
        {
          if ( animation != null )
            return animation.Expression;
          return customAnimation.Expression;
        }
      }

      /// <summary>
      /// Gets the bone transform matrices.
      /// </summary>
      public ReadOnlyCollection<Matrix> BoneTransforms
      {
        get
        {
          if ( animation != null )
            return animation.BoneTransforms;
          return customAnimation.BoneTransforms;
        }
      }

      /// <summary>
      /// Gets or sets the current time position in the animation.
      /// </summary>
      public TimeSpan CurrentPosition
      {
        get
        {
          if ( animation != null )
            return animation.CurrentPosition;
          return customAnimation.CurrentPosition;
        }
        set
        {
          if ( animation != null )
            animation.CurrentPosition = value;
          else
            customAnimation.CurrentPosition = value;
        }
      }

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="preset">The specified standard animation.</param>
      public AvatarAnimationWrapper( AvatarAnimationPreset preset )
      {
        customAnimation = null;
        animation = new AvatarAnimation( preset );
        animation.CurrentPosition = TimeSpan.Zero;
        standardID = preset;
      }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="data">The asset data for the custom animation.</param>
      public AvatarAnimationWrapper( CustomAvatarAnimationData data )
      {
        animation = null;
        customAnimation = new CustomAvatarAnimation( data.Name, data.Length, data.Keyframes );
        customAnimation.CurrentPosition = TimeSpan.Zero;
        customID = data.Name;
      }

      /// <summary>
      /// Updates the current time position of the avatar animation.
      /// </summary>
      /// <param name="elapsedAnimationTime">Elapsed time since the last animation frame.</param>
      /// <param name="loop">true if the animation playback is to be looped; otherwise, false.</param>        
      public void Update( TimeSpan elapsedAnimationTime, bool loop )
      {
        if ( animation != null )
          animation.Update( elapsedAnimationTime, loop );
        else if ( customAnimation != null )
          customAnimation.Update( elapsedAnimationTime, loop );
      }

      /// <summary>
      /// Checks if the animation matches a preset.
      /// </summary>
      /// <param name="preset">The animation preset.</param>
      /// <returns>True if the preset matches the type of the animation.</returns>
      public bool IsOfType( AvatarAnimationPreset preset )
      {
        return ( preset == standardID );
      }

      /// <summary>
      /// Checks if the animation matches a cusom animation.
      /// </summary>
      /// <param name="customAnimation">The name of the custom animation.</param>
      /// <returns>True if the name matches the animation.</returns>
      public bool IsOfType( string customAnimation )
      {
        return ( customAnimation == customID );
      }

      /// <summary>
      /// Determines whether the specified System.Object is equal to the current System.Object.
      /// </summary>
      /// <param name="obj">The System.Object to compare with the current System.Object.</param>
      /// <returns>true if the specified System.Object is equal to the current System.Object; otherwise, false.</returns>
      public override bool Equals( object obj )
      {
        AvatarAnimationWrapper wrapperObj = (AvatarAnimationWrapper)obj;
        return ( ( standardID == wrapperObj.standardID ) && ( customID == wrapperObj.customID ) );
      }

      public static bool operator ==( AvatarAnimationWrapper lhs, AvatarAnimationWrapper rhs )
      {
        return AvatarAnimationWrapper.Equals( lhs, rhs );
      }

      public static bool operator !=( AvatarAnimationWrapper lhs, AvatarAnimationWrapper rhs )
      {
        return !( lhs == rhs );
      }

      public override int GetHashCode()
      {
        return base.GetHashCode();
      }
    }
}
