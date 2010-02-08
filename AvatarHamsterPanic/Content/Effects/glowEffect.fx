// ----------------------------------------------------------------------------
// Glow post-processing effect
// ----------------------------------------------------------------------------

#define SAMPLE_RADIUS 2

#define TextureSampler( texName, samplerName, filter, clampOrWrap) \
texture texName; \
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

int ScreenWidth, ScreenHeight;
float HWeights[SAMPLE_RADIUS + 1];
float VWeights[SAMPLE_RADIUS + 1];
float HStep;
float VStep;

TextureSampler( SceneTexture, SceneSampler, Linear, Clamp );
TextureSampler( MaskTexture, MaskSampler, Linear, Clamp );

struct Input
{
  float2 texCoord : TEXCOORD0;
};

void VertexShader( inout float4 Position : POSITION, inout float2 TexCoord : TEXCOORD0 )
{
  float x = 2.0 * ( Position.x / (float)ScreenWidth  ) - 1.0;
  float y = 2.0 * ( Position.y / (float)ScreenHeight ) - 1.0;
  Position = float4( x, -y, 0, 1 );
  
  TexCoord = TexCoord;
}

float4 ExtractElementsToGlow( Input input ) : COLOR0
{
  float4 output = 0;
  float intensity = tex2D( MaskSampler, input.texCoord ).r;
  if ( intensity != 0 )
    output = tex2D( SceneSampler, input.texCoord ) * intensity;
  return output;
}

float4 HorizontalBlur( Input input ) : COLOR0
{
  float4 output = 0;
  
  float v = input.texCoord.y;
  float lPos = input.texCoord.x;
  float rPos = input.texCoord.x;
  
  output += HWeights[0] * tex2D( SceneSampler, float2( lPos, v ) );
  
  for ( int i = 1; i <= SAMPLE_RADIUS; ++i )
  {
    lPos -= HStep;
    rPos += HStep;
  
    output += HWeights[i] * tex2D( SceneSampler, float2( lPos, v ) );
    output += HWeights[i] * tex2D( SceneSampler, float2( rPos, v ) );
  }
  
  return output;
}

float4 VerticalBlur( Input input ) : COLOR0
{
  float4 output = 0;
  
  float u = input.texCoord.x;
  float tPos = input.texCoord.y;
  float bPos = input.texCoord.y;
  
  output += VWeights[0] * tex2D( SceneSampler, float2( u, tPos ) );
  
  for ( int i = 1; i <= SAMPLE_RADIUS; ++i )
  {
    tPos -= VStep;
    bPos += VStep;
  
    output += VWeights[i] * tex2D( SceneSampler, float2( u, tPos ) );
    output += VWeights[i] * tex2D( SceneSampler, float2( u, bPos ) );
  }
  
  output.rgb = normalize( output.rgb );
  
  return output;
}

technique ExtractElementsToGlow
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 ExtractElementsToGlow();
  }
}

technique HorizontalBlur
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 HorizontalBlur();
  }
}

technique VerticalBlur
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 VerticalBlur();
  }
}
