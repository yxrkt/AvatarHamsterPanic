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
  public class PixieParticleSystem : ParticleSystem
  {
    public PixieParticleSystem( Game game, ContentManager content )
      : base( game, content )
    { }


    protected override void InitializeSettings( ParticleSettings settings )
    {
      settings.TextureName = "pixieParticle";

      settings.MaxParticles = 200;

      settings.Duration = TimeSpan.FromSeconds( 1.25f );
      settings.DurationRandomness = 1;

      settings.MinHorizontalVelocity = 0;
      settings.MaxHorizontalVelocity = 0;

      settings.MinVerticalVelocity = 0;
      settings.MaxVerticalVelocity = 0;

      settings.EndVelocity = 1;

      settings.MinRotateSpeed = -2;
      settings.MaxRotateSpeed = 2;

      settings.FadePower = 10;

      settings.MinStartSize = 0f;
      settings.MaxStartSize = .1f;

      settings.MinMidSize = .1f;
      settings.MaxMidSize = .35f;

      settings.MinEndSize = 0;
      settings.MaxEndSize = 0;
    }
  }
}
