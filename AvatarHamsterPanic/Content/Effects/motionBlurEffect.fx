// ----------------------------------------------------------------------------
// Motion blur post-processing effect
// ----------------------------------------------------------------------------

#define TextureSampler( texName, samplerName, filter, clampOrWrap) \
uniform extern texture texName; \
sampler samplerName = sampler_state \
{ \
	Texture = (texName); \
	\
	MinFilter = filter; \
	MagFilter = filter; \
	MipFilter = filter; \
	\
	AddressU = clampOrWrap; \
	AddressV = clampOrWrap; \
};

uniform extern int ScreenWidth, ScreenHeight;

TextureSampler( SceneTexture, SceneSampler, Linear, Clamp );
TextureSampler( MaskTexture, MaskSampler, Linear, Clamp );
TextureSampler( LastFrameTexture, LastFrameSampler, Linear, Clamp );

void VertexShader( inout float4 Position : POSITION, inout float2 TexCoord : TEXCOORD0 )
{
  float x = 2.0 * ( Position.x / (float)ScreenWidth  ) - 1.0;
  float y = 2.0 * ( Position.y / (float)ScreenHeight ) - 1.0;
  Position = float4( x, -y, 0, 1 );
  
  TexCoord = TexCoord;
}

float4 PixelShaderLastFrame( float2 texCoord : TEXCOORD0 ) : COLOR0
{
  return float4( tex2D( LastFrameSampler, texCoord ).rgb, 1 );
}

float4 PixelShader( float2 texCoord : TEXCOORD0 ) : COLOR0
{
  float4 output = 0;
  float intensity = tex2D( MaskSampler, texCoord ).g;
  if ( intensity != 0 )
    output = float4( 0, 0, 1, 1 );
    //output = tex2D( SceneSampler, texCoord );
  return output;
}

technique MotionBlur
{
  pass Pass0
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 PixelShaderLastFrame();
  }
  
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 PixelShader();
  }
}
