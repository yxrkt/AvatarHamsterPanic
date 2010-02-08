using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace AvatarHamsterPanic
{
  public static class MaskHelper
  {
    public static Vector4 Glow( float intensity )
    {
      return new Vector4( intensity, 0, 0, 1f );
    }

    public static Vector4 MotionBlur( float intensity )
    {
      return new Vector4( 0, intensity, 0, 1f );
    }
  }

  public static class PostProcessor
  {
    static GraphicsDevice device;
    static SpriteBatch spriteBatch;
    
    // screen aligned quad
    static VertexPositionTexture[] quadVertices;
    static VertexDeclaration vertexDeclaration;
    static VertexBuffer vertexBuffer;
    static Rectangle screenRectangle;

    // glow
    const int blurSampleRadius = 2; // number of samples that will be taken in each direction

    static Effect glowEffect;
    static EffectParameter glowSceneParameter;
    static EffectParameter glowMaskParameter;
    static EffectTechnique glowExtractTechnique;
    static EffectTechnique glowHBlurTechnique;
    static EffectTechnique glowVBlurTechnique;
    static RenderTarget2D glowExtractRenderTarget;
    static RenderTarget2D glowHBlurRenderTarget;
    static RenderTarget2D glowVBlurRenderTarget;
    static float hStep, vStep;
    static float[] hWeights, vWeights;

    // motion blur
    static Effect motionBlurEffect;
    static EffectParameter motionBlurSceneParameter;
    static EffectParameter motionBlurMaskParameter;
    static EffectParameter motionBlurLastFrameParameter;
    static RenderTarget2D motionBlurRenderTarget;
    static Texture2D motionBlurLastFrame;

    public static void Initialize( GraphicsDevice device, SpriteBatch spriteBatch, ContentManager content )
    {
      PostProcessor.device = device;
      PostProcessor.spriteBatch = spriteBatch;

      PresentationParameters pars = device.PresentationParameters;

      int downScale = 4;

      screenRectangle = new Rectangle( 0, 0, pars.BackBufferWidth, pars.BackBufferHeight );

      // screen aligned quad
      quadVertices = new VertexPositionTexture[4];
      vertexDeclaration = new VertexDeclaration( device, VertexPositionTexture.VertexElements );
      vertexBuffer = new VertexBuffer( device, typeof( VertexPositionTexture ), 4, BufferUsage.None );

      quadVertices[0].Position = new Vector3( screenRectangle.Left,  screenRectangle.Top,    0 );
      quadVertices[1].Position = new Vector3( screenRectangle.Right, screenRectangle.Top,    0 );
      quadVertices[2].Position = new Vector3( screenRectangle.Right, screenRectangle.Bottom, 0 );
      quadVertices[3].Position = new Vector3( screenRectangle.Left,  screenRectangle.Bottom, 0 );

      quadVertices[0].TextureCoordinate = new Vector2( 0, 0 );
      quadVertices[1].TextureCoordinate = new Vector2( 1, 0 );
      quadVertices[2].TextureCoordinate = new Vector2( 1, 1 );
      quadVertices[3].TextureCoordinate = new Vector2( 0, 1 );

      vertexBuffer.SetData( quadVertices );


      // glow
      glowExtractRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth / downScale,
                                                    pars.BackBufferHeight / downScale, 1, pars.BackBufferFormat );
      glowHBlurRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth / downScale,
                                                  pars.BackBufferHeight / downScale, 1, pars.BackBufferFormat );
      glowVBlurRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth / downScale,
                                                  pars.BackBufferHeight / downScale, 1, pars.BackBufferFormat );

      float blurRadius = 2f;

      hStep = ( blurRadius / (float)blurSampleRadius ) * ( 1f / (float)( pars.BackBufferWidth / downScale ) );
      vStep = ( blurRadius / (float)blurSampleRadius ) * ( 1f / (float)( pars.BackBufferHeight / downScale ) );

      hWeights = new float[blurSampleRadius + 1];
      vWeights = new float[blurSampleRadius + 1];

      for ( int i = 0; i <= blurSampleRadius; ++i )
      {
        hWeights[i] = GaussWeight( i * hStep, blurRadius );
        vWeights[i] = GaussWeight( i * vStep, blurRadius );
      }

      float hSum = hWeights.Sum( w => w != hWeights[0] ? 2 * w : w );
      float vSum = vWeights.Sum( w => w != vWeights[0] ? 2 * w : w );

      for ( int i = 0; i <= blurSampleRadius; ++i )
      {
        hWeights[i] /= hSum;
        vWeights[i] /= vSum;
      }

      glowEffect = content.Load<Effect>( "Effects/glowEffect" );

      glowExtractTechnique = glowEffect.Techniques["ExtractElementsToGlow"];
      glowHBlurTechnique = glowEffect.Techniques["HorizontalBlur"];
      glowVBlurTechnique = glowEffect.Techniques["VerticalBlur"];

      glowEffect.Parameters["HWeights"].SetValue( hWeights );
      glowEffect.Parameters["VWeights"].SetValue( vWeights );
      glowEffect.Parameters["HStep"].SetValue( hStep );
      glowEffect.Parameters["VStep"].SetValue( vStep );
      glowEffect.Parameters["ScreenWidth"].SetValue( screenRectangle.Right );
      glowEffect.Parameters["ScreenHeight"].SetValue( screenRectangle.Bottom );

      glowSceneParameter = glowEffect.Parameters["SceneTexture"];
      glowMaskParameter = glowEffect.Parameters["MaskTexture"];


      // motion blur
      motionBlurEffect = content.Load<Effect>( "Effects/motionBlurEffect" );
      motionBlurEffect.CurrentTechnique = motionBlurEffect.Techniques[0];
      motionBlurEffect.Parameters["ScreenWidth"].SetValue( screenRectangle.Right );
      motionBlurEffect.Parameters["ScreenHeight"].SetValue( screenRectangle.Bottom );
      motionBlurSceneParameter = motionBlurEffect.Parameters["SceneTexture"];
      motionBlurMaskParameter = motionBlurEffect.Parameters["MaskTexture"];
      motionBlurLastFrameParameter = motionBlurEffect.Parameters["LastFrameTexture"];
      motionBlurRenderTarget = new RenderTarget2D( device, pars.BackBufferWidth, 
                                                   pars.BackBufferHeight, 1, pars.BackBufferFormat );
      motionBlurLastFrame = new Texture2D( device, pars.BackBufferWidth, pars.BackBufferHeight, 1,
                                           TextureUsage.None, pars.BackBufferFormat );
    }

    private static float GaussWeight( float dist, float radius )
    {
      float r2 = radius * radius;
      return ( 1f / (float)Math.Sqrt( MathHelper.TwoPi * r2 ) ) * (float)Math.Exp( -dist * dist / ( 2 * r2 ) );
    }

    public static Texture2D Glow( Texture2D scene, Texture2D mask )
    {
      device.VertexDeclaration = vertexDeclaration;
      device.Vertices[0].SetSource( vertexBuffer, 0, VertexPositionTexture.SizeInBytes );

      device.RenderState.AlphaBlendEnable = false;
      device.RenderState.AlphaTestEnable = false;

      // extract elements to glow
      device.SetRenderTarget( 0, glowExtractRenderTarget );
      device.Clear( Color.TransparentBlack );

      glowSceneParameter.SetValue( scene );
      glowMaskParameter.SetValue( mask );

      glowEffect.CurrentTechnique = glowExtractTechnique;
      glowEffect.Begin();
      glowEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawPrimitives( PrimitiveType.TriangleFan, 0, 2 );
      glowEffect.CurrentTechnique.Passes[0].End();
      glowEffect.End();

      //device.SetRenderTarget( 0, null );
      //return glowExtractRenderTarget.GetTexture();

      device.SetRenderTarget( 0, glowHBlurRenderTarget );
      device.Clear( Color.TransparentBlack );

      // blur horzontally
      glowEffect.CurrentTechnique = glowHBlurTechnique;
      glowSceneParameter.SetValue( glowExtractRenderTarget.GetTexture() );
      glowEffect.Begin();
      glowEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawPrimitives( PrimitiveType.TriangleFan, 0, 2 );
      glowEffect.CurrentTechnique.Passes[0].End();
      glowEffect.End();

      device.SetRenderTarget( 0, glowVBlurRenderTarget );
      device.Clear( Color.TransparentBlack );

      // blur vertically
      glowEffect.CurrentTechnique = glowVBlurTechnique;
      glowSceneParameter.SetValue( glowHBlurRenderTarget.GetTexture() );
      glowEffect.Begin();
      glowEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawPrimitives( PrimitiveType.TriangleFan, 0, 2 );
      glowEffect.CurrentTechnique.Passes[0].End();
      glowEffect.End();

      device.SetRenderTarget( 0, null );

      device.RenderState.AlphaBlendEnable = true;
      device.RenderState.AlphaTestEnable = true;

      return glowVBlurRenderTarget.GetTexture();
    }

    public static Texture2D MotionBlur( Texture2D scene, Texture2D mask )
    {
      device.VertexDeclaration = vertexDeclaration;
      device.Vertices[0].SetSource( vertexBuffer, 0, VertexPositionTexture.SizeInBytes );

      device.SetRenderTarget( 0, motionBlurRenderTarget );
      device.Clear( Color.TransparentBlack );

      //spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None );
      //spriteBatch.Draw( motionBlurLastFrame, screenRectangle, new Color( Color.White, 255 ) );
      //spriteBatch.End();

      //device.RenderState.AlphaBlendEnable = false;
      //device.RenderState.AlphaTestEnable = false;

      motionBlurSceneParameter.SetValue( scene );
      motionBlurMaskParameter.SetValue( mask );
      motionBlurLastFrameParameter.SetValue( motionBlurLastFrame );

      motionBlurEffect.Begin();
      foreach ( EffectPass pass in motionBlurEffect.CurrentTechnique.Passes )
      {
        pass.Begin();
        device.DrawPrimitives( PrimitiveType.TriangleFan, 0, 2 );
        pass.End();
      }
      motionBlurEffect.End();

      device.SetRenderTarget( 0, null );

      motionBlurLastFrame = motionBlurRenderTarget.GetTexture();

      return motionBlurLastFrame;
    }
  }
}