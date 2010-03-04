float4x4 Transform;

texture Texture;
sampler TextureSampler = 
sampler_state
{
  Texture = (Texture);
  
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
};


struct VertexShaderInput
{
  float4 Position : POSITION;
  float2 TexCoord : TEXCOORD0;
  float4 Color    : COLOR0;
};

struct VertexShaderOutput
{
  float4 Position : POSITION;
  float2 TexCoord : TEXCOORD0;
  float4 Color    : COLOR0;
};


VertexShaderOutput VertexShader( VertexShaderInput input )
{
  VertexShaderOutput output;
  
  output.Position = mul( input.Position, Transform );
  output.TexCoord = input.TexCoord;
  output.Color    = input.Color;
  
  return output;
}

float4 PixelShader( float2 texCoord : TEXCOORD0, float4 color : COLOR0 ) : COLOR0
{
  float4 output = tex2D( TextureSampler, texCoord );
  if ( output.r != 0 && output.g != 0 && output.b != 0 )
  {
    output.rgb = saturate( output.rgb + color.rgb );
    output.a *= color.a;
  }
  return output;
}

technique Screen
{
  pass Pass1
  {
    PixelShader = compile ps_3_0 PixelShader();
  }
}

technique World
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader  = compile ps_3_0 PixelShader();
  }
}
