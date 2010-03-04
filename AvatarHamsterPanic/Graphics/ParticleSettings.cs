#region File Description
//-----------------------------------------------------------------------------
// ParticleSettings.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Particle3DSample
{
  /// <summary>
  /// Settings class describes all the tweakable options used
  /// to control the appearance of a particle system.
  /// </summary>
  public class ParticleSettings
  {
    // Name of the texture used by this particle system.
    public string TextureName = null;


    // Maximum number of particles that can be displayed at one time.
    public int MaxParticles = 100;


    // How long these particles will last.
    public TimeSpan Duration = TimeSpan.FromSeconds( 1 );


    // If greater than zero, some particles will last a shorter time than others.
    public float DurationRandomness = 0;


    // Controls how much particles are influenced by the velocity of the object
    // which created them. You can see this in action with the explosion effect,
    // where the flames continue to move in the same direction as the source
    // projectile. The projectile trail particles, on the other hand, set this
    // value very low so they are less affected by the velocity of the projectile.
    public float EmitterVelocitySensitivity = 1;


    // Range of values controlling how much X and Z axis velocity to give each
    // particle. Values for individual particles are randomly chosen from somewhere
    // between these limits.
    public float MinHorizontalVelocity = 0;
    public float MaxHorizontalVelocity = 0;


    // Range of values controlling how much Y axis velocity to give each particle.
    // Values for individual particles are randomly chosen from somewhere between
    // these limits.
    public float MinVerticalVelocity = 0;
    public float MaxVerticalVelocity = 0;


    // Direction and strength of the gravity effect. Note that this can point in any
    // direction, not just down! The fire effect points it upward to make the flames
    // rise, and the smoke plume points it sideways to simulate wind.
    public Vector3 Gravity = Vector3.Zero;


    // Controls how the particle velocity will change over their lifetime. If set
    // to 1, particles will keep going at the same speed as when they were created.
    // If set to 0, particles will come to a complete stop right before they die.
    // Values greater than 1 make the particles speed up over time.
    public float EndVelocity = 1;


    // Color the particles are multiplied by. Alpha is handled seperately.
    public Vector3[] Colors = { Color.White.ToVector3(), Color.White.ToVector3(), Color.White.ToVector3() };


    // Range of values controlling how fast the particles rotate. Values for
    // individual particles are randomly chosen from somewhere between these
    // limits. If both these values are set to 0, the particle system will
    // automatically switch to an alternative shader technique that does not
    // support rotation, and thus requires significantly less GPU power. This
    // means if you don't need the rotation effect, you may get a performance
    // boost from leaving these values at 0.
    public float MinRotateSpeed = 0;
    public float MaxRotateSpeed = 0;

    
    // True if the angle of the particle should be aligned with its velocity.
    public bool AlignWithVelocity = false;


    // Maximum velocity where there is no stretch on the particle. If this value is
    // zero, the particle will never be stretched along the velocity.
    public float StretchVelocity = 0;


    // Linearly scales the velocity stretch.
    public float StretchFactor = 1;


    // The exponent for the power curve controlling the alpha. If 0, the default
    // fade-in fade-out method is used.
    public int FadePower = 0;


    // Range of values controlling how big the particles are when first created.
    // Values for individual particles are randomly chosen from somewhere between
    // these limits.
    public float MinStartSize = 1;
    public float MaxStartSize = 1;


    // Range of values controlling how big particles are at the middle range of
    // their life. Values for indivisual particles are randomly chosen from 
    // somewhere between these limits. If both are left zero, the particle's 
    // size is interpolated between the start and end times.
    public float MinMidSize = 0;
    public float MaxMidSize = 0;


    // Range of values controlling how big particles become at the end of their
    // life. Values for individual particles are randomly chosen from somewhere
    // between these limits.
    public float MinEndSize = 1;
    public float MaxEndSize = 1;


    // Alpha blending settings.
    public Blend SourceBlend = Blend.SourceAlpha;
    public Blend DestinationBlend = Blend.InverseSourceAlpha;
  }
}
