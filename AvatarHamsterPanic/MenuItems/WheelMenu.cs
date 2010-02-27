using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using CustomModelSample;
using Graphics;
using Microsoft.Xna.Framework.Graphics;
using MathLibrary;
using System.Diagnostics;

namespace Menu
{
  class WheelMenu : MenuItem
  {
    CustomModel wheelModel;

    Camera camera;
    float wheelScale;
    Matrix scaleMatrix;
    float angle;
    float lastX;

    SpringInterpolater rotateSpring;
    float angleStep;

    // The wheel entries
    List<WheelMenuEntry> entries = new List<WheelMenuEntry>( 4 );

    // The distance from the rim of the wheel the entries sit.
    public const float EntryOffset = .25f;

    // These are the scales for entries when they are idle or active.
    public const float EntryIdleSize = .2f;
    public const float EntryActiveScale = 1.75f;

    // The effect used to render the entries
    Effect entryEffect;
    public Effect EntryEffect { get { return entryEffect; } }
    public EffectParameter EntryDiffuseEffectParameter { get { return entryDiffuseEffectParameter; } }

    EffectParameter entryWorldEffectParameter;
    EffectParameter entryViewEffectParameter;
    EffectParameter entryProjectionEffectParameter;
    EffectParameter entryDiffuseEffectParameter;

    // The radius of the wheel.
    public float Radius { get { return wheelScale / 2; } }

    public WheelMenu( MenuScreen screen, Camera camera, float wheelScale, 
                      float screenScale, float startX, float activeX, float finishX, float y )
      : base( screen, Vector2.Zero )
    {
      this.wheelScale = wheelScale;
      scaleMatrix = Matrix.CreateScale( wheelScale );
      this.camera = camera;
      angle = MathHelper.PiOver4;

      TransitionOnPosition = new Vector2( startX, y );
      Position = new Vector2( activeX, y );
      TransitionOffPosition = new Vector2( finishX, y );

      rotateSpring = new SpringInterpolater( 1, 50, SpringInterpolater.GetCriticalDamping( 50 ) );

      lastX = startX;

      ContentManager content = screen.ScreenManager.Game.Content;

      entryEffect = content.Load<Effect>( "Effects/basic" ).Clone( Screen.ScreenManager.GraphicsDevice );
      entryEffect.CurrentTechnique = entryEffect.Techniques["DiffuseColor"];
      entryEffect.Parameters["LightingEnabled"].SetValue( false );
      entryWorldEffectParameter = entryEffect.Parameters["World"];
      entryViewEffectParameter = entryEffect.Parameters["View"];
      entryProjectionEffectParameter = entryEffect.Parameters["Projection"];
      entryDiffuseEffectParameter = entryEffect.Parameters["DiffuseMap"];

      wheelModel = content.Load<CustomModel>( "Models/hamsterWheel" );
      foreach ( CustomModel.ModelPart part in wheelModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( new Color( Color.RoyalBlue, 0 ).ToVector4() );
      }
    }

    public void AddEntry( WheelMenuEntry entry )
    {
      entries.Add( entry );
    }

    public void ConfigureEntries()
    {
      angleStep = -MathHelper.TwoPi / entries.Count;
      float angle = 0;
      foreach ( WheelMenuEntry entry in entries )
      {
        entry.Active = false;
        entry.Angle = angle;
        angle += angleStep;
      }
      entries.First().Active = true;
    }

    public override void Update( GameTime gameTime )
    {
      if ( Screen.ScreenState == ScreenState.Active && entries.Count != 0 && entries[0].Collapsed )
      {
        foreach ( WheelMenuEntry entry in entries )
          entry.Collapsed = false;
        entries[0].Active = true;

        rotateSpring.SetSource( angle );
        rotateSpring.SetDest( angle );
        rotateSpring.Active = true;
      }
      else if ( Screen.ScreenState != ScreenState.Active && entries.Count != 0 && !entries[0].Collapsed )
      {
        foreach ( WheelMenuEntry entry in entries )
          entry.Collapsed = true;
        rotateSpring.Active = false;
      }

      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      if ( elapsed > 1f / 60f )
        elapsed = 1f / 60f;

      foreach ( WheelMenuEntry entry in entries )
        entry.Update( elapsed );

      if ( rotateSpring.Active )
      {
        float lastAngle = angle;
        angle = rotateSpring.GetSource()[0];
        float angleDelta = angle - lastAngle;
        foreach ( WheelMenuEntry entry in entries )
          entry.Angle = MathHelper.WrapAngle( entry.Angle + angleDelta );
      }

      rotateSpring.Update( elapsed );

      base.Update( gameTime );
    }

    public void RotateCCW()
    {
      if ( rotateSpring.Active )
        rotateSpring.SetDest( rotateSpring.GetDest()[0] - angleStep );
      for ( int i = 0; i < entries.Count; ++i )
      {
        WheelMenuEntry entry = entries[i];
        if ( entry.Active )
        {
          entry.Active = false;
          entries[( i + 1 ) % entries.Count].Active = true;
          break;
        }
      }
    }

    public void RotateCW()
    {
      if ( rotateSpring.Active )
        rotateSpring.SetDest( rotateSpring.GetDest()[0] + angleStep );
      for ( int i = 0; i < entries.Count; ++i )
      {
        WheelMenuEntry entry = entries[i];
        if ( entry.Active )
        {
          entry.Active = false;
          entries[( i + entries.Count - 1 ) % entries.Count].Active = true;
          break;
        }
      }
    }

    protected internal override void OnSelect( PlayerIndex playerIndex )
    {
      foreach ( WheelMenuEntry entry in entries )
      {
        if ( entry.Active )
        {
          entry.OnSelect( playerIndex );
          break;
        }
      }
    }

    public override void Draw( GameTime gameTime )
    {
      Matrix view = camera.GetViewMatrix();
      Matrix projection = camera.GetProjectionMatrix();

      // draw wheel model
      Screen.ScreenManager.GraphicsDevice.RenderState.DepthBufferEnable = true;
      Matrix world = scaleMatrix * Matrix.CreateRotationZ( angle ) * Matrix.CreateTranslation( curPos.X, curPos.Y, 0 );
      wheelModel.Draw( camera.Position, world, view, projection );

      //draw each entry
      entryViewEffectParameter.SetValue( view );
      entryProjectionEffectParameter.SetValue( projection );

      foreach ( WheelMenuEntry entry in entries )
      {
        world = Matrix.CreateRotationZ( entry.Angle ) * Matrix.CreateTranslation( curPos.X, curPos.Y, 0 );
        entryWorldEffectParameter.SetValue( world );
        entry.Draw();
      }
    }

    public override void UpdateTransition( float transitionPosition, ScreenState state )
    {
      if ( state == ScreenState.Active ) return;

      float t = ( transitionPosition * transitionPosition );

      foreach ( CustomModel.ModelPart part in wheelModel.ModelParts )
      {
        Vector4 color = part.EffectParamColor.GetValueVector4();
        color.W = 1 - t;
        part.EffectParamColor.SetValue( color );
      }

      if ( state == ScreenState.TransitionOn )
        curPos = Position + t * ( TransitionOnPosition - Position );
      else if ( state == ScreenState.TransitionOff )
        curPos = Position + t * ( TransitionOffPosition - Position );

      // update wheel's rotation
      angle -= ( curPos.X - lastX ) / ( wheelScale / 2 );

      lastX = curPos.X;
    }
  }
}