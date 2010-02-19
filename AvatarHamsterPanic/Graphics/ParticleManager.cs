using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics
{
  public sealed class ParticleManager : DrawableGameComponent
  {
    ContentManager content;
    Matrix viewMatrix, projectionMatrix;
    Vector3 eye;

    Effect modelEffect;
    EffectParameter modelEffectParamWorld;
    EffectParameter modelEffectParamView;
    EffectParameter modelEffectParamProjection;
    EffectParameter modelEffectParamEye;
    EffectParameter modelEffectParamColor;

    VertexDeclaration modelVertexDeclaration;

    List<ModelExplosion> modelExplosions;

    public ParticleManager( Game game, ContentManager content )
      : base( game )
    {
      this.content = content;
      modelExplosions = new List<ModelExplosion>( 10 );
    }

    public override void Initialize()
    {
      base.Initialize();

      modelVertexDeclaration = new VertexDeclaration( GraphicsDevice, 
                                                      VertexPositionNormalTexture.VertexElements );
    }

    protected override void LoadContent()
    {
      LoadModelEffect();
    }

    public void Unload()
    {
      foreach ( ModelExplosion explosion in modelExplosions )
        explosion.Invalidate();
    }

    void LoadModelEffect()
    {
      modelEffect = content.Load<Effect>( "Effects/basic" ).Clone( GraphicsDevice );
      modelEffect.CurrentTechnique = modelEffect.Techniques["Color"];
      modelEffectParamWorld = modelEffect.Parameters["World"];
      modelEffectParamView = modelEffect.Parameters["View"];
      modelEffectParamProjection = modelEffect.Parameters["Projection"];
      modelEffectParamEye = modelEffect.Parameters["Eye"];
      modelEffectParamColor = modelEffect.Parameters["Color"];
    }

    public void Add( ModelExplosion explosion )
    {
      explosion.Manager = this;
      modelExplosions.Add( explosion );
    }

    public override void Update( GameTime gameTime )
    {
      foreach ( ModelExplosion explosion in modelExplosions )
        explosion.Update( gameTime );

      modelExplosions.RemoveAll( e => !e.Valid );
    }

    public void SetCamera( Vector3 eye, Matrix viewMatrix, Matrix projectionMatrix )
    {
      this.eye = eye;
      this.viewMatrix = viewMatrix;
      this.projectionMatrix = projectionMatrix;
    }

    public override void Draw( GameTime gameTime )
    {
      RenderState renderState = Game.GraphicsDevice.RenderState;

      renderState.AlphaBlendEnable = true;
      renderState.AlphaSourceBlend = Blend.SourceAlpha;
      renderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;

      if ( modelExplosions.Count > 0 )
        DrawModelExplosions();
    }

    private void DrawModelExplosions()
    {
      GraphicsDevice device = GraphicsDevice;
      device.VertexDeclaration = modelVertexDeclaration;

      modelEffectParamEye.SetValue( eye );
      modelEffectParamView.SetValue( viewMatrix );
      modelEffectParamProjection.SetValue( projectionMatrix );

      modelEffect.Begin();

      EffectPassCollection passes = modelEffect.CurrentTechnique.Passes;
      for ( int i = 0; i < passes.Count; ++i )
      {
        EffectPass pass = passes[i];

        pass.Begin();

        foreach ( ModelExplosion explosion in modelExplosions )
          explosion.Draw( device, modelEffect, modelEffectParamWorld, modelEffectParamColor );

        pass.End();
      }

      modelEffect.End();
    }
  }
}