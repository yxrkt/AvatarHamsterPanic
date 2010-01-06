#region File Description
//-----------------------------------------------------------------------------
// AvatarKeyframe.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#endregion

namespace CustomAvatarAnimationFramework
{
    /// <summary>
    /// Describes the position of a single bone at a single point in time.
    /// </summary>
    public class AvatarKeyFrame
    {
        /// <summary>
        /// The index of the target bone that is animated by this keyframe.
        /// </summary>
        [ContentSerializer]
        public int Bone { get; private set; }

        /// <summary>
        /// The time offset from the start of the animation to this keyframe.
        /// </summary>
        [ContentSerializer]
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// The bone transform for this keyframe.
        /// </summary>
        [ContentSerializer]
        public Matrix Transform { get; private set; }

        #region Initialization


        /// <summary>
        /// Private constructor for use by the XNB deserializer.
        /// </summary>
        private AvatarKeyFrame() { }


        /// <summary>
        /// Constructs a new AvatarKeyFrame object.
        /// </summary>
        public AvatarKeyFrame(int bone, TimeSpan time, Matrix transform)
        {
            Bone = bone;
            Time = time;
            Transform = transform;
        }

        #endregion
    }
}
