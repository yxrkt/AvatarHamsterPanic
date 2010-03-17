#region File Description
//-----------------------------------------------------------------------------
// ExplosionParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Graphics;
#endregion

namespace Particle3DSample
{
  /// <summary>
  /// Custom particle system for creating the fiery part of the explosions.
  /// </summary>
  public class SparkParticleSystem : ParticleSystem
  {
    public SparkParticleSystem( Game game, ContentManager content )
      : base( game, content )
    { }


    protected override void InitializeSettings( ParticleSettings settings )
    {
      settings.TextureName = "sparkParticle";

      settings.MaxParticles = 200;

      settings.Duration = TimeSpan.FromSeconds( 1 );
      settings.DurationRandomness = 0;

      settings.MinHorizontalVelocity = 0;
      settings.MaxHorizontalVelocity = 0;

      settings.MinVerticalVelocity = 0;
      settings.MaxVerticalVelocity = 0;

      settings.EndVelocity = 0;

      settings.AlignWithVelocity = true;

      settings.StretchVelocity = 2f;
      settings.StretchFactor = 1f;

      settings.FadePower = 0;

      settings.MinStartSize = .01f;
      settings.MaxStartSize = .03f;

      settings.MinEndSize = .01f;
      settings.MaxEndSize = .03f;

      settings.Mask = MaskHelper.Glow( 1 );
    }
  }
}
