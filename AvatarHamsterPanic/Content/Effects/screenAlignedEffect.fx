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

TextureSampler( Texture, TextureSampler, Linear, Clamp );
int ScreenWidth;
int ScreenHeight;
float4 Tint = { 1, 1, 1, 1 };

struct ColorVertexShaderInput
{
  float4 Position : POSITION0;
  float4 Color : COLOR0;
};

struct ColorVertexShaderOutput
{
  float4 Position : POSITION0;
  float4 Color : COLOR0;
};

struct TextureVertexShaderInput
{
  float4 Position : POSITION0;
  float2 TexCoord : TEXCOORD0;
};

struct TextureVertexShaderOutput
{
  float4 Position : POSITION0;
  float2 TexCoord : TEXCOORD0;
};

ColorVertexShaderOutput ColorVertexShader( ColorVertexShaderInput input )
{
  ColorVertexShaderOutput output;
  
  float x = 2.0 * ( input.Position.x / (float)ScreenWidth  ) - 1.0;
  float y = 2.0 * ( input.Position.y / (float)ScreenHeight ) - 1.0;
  output.Position = float4( x, -y, 0, 1 );
  
  output.Color = input.Color;

  return output;
}

TextureVertexShaderOutput TextureVertexShader( TextureVertexShaderInput input )
{
  TextureVertexShaderOutput output;
  
  float x = 2.0 * ( input.Position.x / (float)ScreenWidth  ) - 1.0;
  float y = 2.0 * ( input.Position.y / (float)ScreenHeight ) - 1.0;
  output.Position = float4( x, -y, 0, 1 );
  
  output.TexCoord = input.TexCoord;
  
  return output;
}

float4 ColorPixelShader( ColorVertexShaderOutput input ) : COLOR0
{
  return input.Color * Tint;
}

float4 TexturePixelShader( TextureVertexShaderOutput input ) : COLOR0
{
  return tex2D( TextureSampler, input.TexCoord ) * Tint;
}

technique Color
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 ColorVertexShader();
    PixelShader  = compile ps_3_0 ColorPixelShader();
  }
}

technique Texture
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 TextureVertexShader();
    PixelShader  = compile ps_3_0 TexturePixelShader();
  }
}
