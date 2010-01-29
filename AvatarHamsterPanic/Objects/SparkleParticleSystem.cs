#region File Description
//-----------------------------------------------------------------------------
// SmokePlumeParticleSystem.cs
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
  /// Custom particle system for creating sparkle feedback.
  /// </summary>
  public class SparkleParticleSystem : ParticleSystem
  {
    public SparkleParticleSystem( Game game, ContentManager content )
      : base( game, content )
    { }


    protected override void InitializeSettings( ParticleSettings settings )
    {
      settings.TextureName = "Textures/particleSparkle";

      settings.MaxParticles = 300;

      settings.Duration = TimeSpan.FromSeconds( 1 );
      settings.DurationRandomness = .5f;

      settings.MinHorizontalVelocity = 0;
      settings.MaxHorizontalVelocity = 0;

      settings.MinVerticalVelocity = 0;
      settings.MaxVerticalVelocity = 0;

      settings.EndVelocity = 0f;

      settings.MinRotateSpeed = -2;
      settings.MaxRotateSpeed = 2;

      settings.MinStartSize = .1f;
      settings.MaxStartSize = .4f;

      settings.MinEndSize = 0;
      settings.MaxEndSize = 0;

      settings.MinColor = Color.Gold;
      settings.MaxColor = Color.Gold;
    }
  }
}
