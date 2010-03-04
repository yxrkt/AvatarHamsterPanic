float4x4 WorldViewProjection;


struct VertexShaderInput
{
  float4 Position : POSITION0;
  float4 Color : COLOR0;
};

struct VertexShaderOutput
{
  float4 Position : POSITION0;
  float4 Color : COLOR0;
};

struct PixelShaderOutput
{
  float4 Color : COLOR0;
  float4 Mask  : COLOR1;
};

VertexShaderOutput VertexShader( VertexShaderInput input )
{
  VertexShaderOutput output;
  
  output.Position = mul( input.Position, WorldViewProjection );
  output.Color = input.Color;

  return output;
}

PixelShaderOutput PixelShader( VertexShaderOutput input )
{
  PixelShaderOutput output;
  
  output.Color = input.Color;
  output.Mask  = float4( 1, 0, 0, 1 );
  
  return output;
}

technique Technique1
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 VertexShader();
    PixelShader = compile ps_3_0 PixelShader();
  }
}
