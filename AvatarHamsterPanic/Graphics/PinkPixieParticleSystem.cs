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
#endregion

namespace Particle3DSample
{
  /// <summary>
  /// Custom particle system for creating the fiery part of the explosions.
  /// </summary>
  public class PinkPixieParticleSystem : ParticleSystem
  {
    public PinkPixieParticleSystem( Game game, ContentManager content )
      : base( game, content )
    { }


    protected override void InitializeSettings( ParticleSettings settings )
    {
      settings.TextureName = "pixieParticle";

      settings.MaxParticles = 1000;

      settings.Duration = TimeSpan.FromSeconds( 1.75f );
      settings.DurationRandomness = 0;

      settings.MinHorizontalVelocity = 0;
      settings.MaxHorizontalVelocity = 0;

      settings.MinVerticalVelocity = 0;
      settings.MaxVerticalVelocity = 0;

      settings.Colors = new Vector3[]
      {
        Color.Pink.ToVector3(),
        new Color( 0xFF, 0xDB, 0x69 ).ToVector3(), // soft yellow
        Color.Turquoise.ToVector3()
      };

      settings.EmitterVelocitySensitivity = .5f;

      settings.EndVelocity = 1;

      settings.MinRotateSpeed = -1;
      settings.MaxRotateSpeed = 1;

      settings.FadePower = 10;

      settings.MinStartSize = 0f;
      settings.MaxStartSize = .05f;

      settings.MinMidSize = .075f;
      settings.MaxMidSize = .25f;

      settings.MinEndSize = 0;
      settings.MaxEndSize = 0;
    }
  }
}
