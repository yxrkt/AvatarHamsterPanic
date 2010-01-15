float4x4 World;
float4x4 View;
float4x4 Projection;

float4 Color = float4( 1, 1, 1, 1 );

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

TextureSampler( Diffuse, DiffuseSampler, Linear, Clamp );

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction( VertexShaderInput input )
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float4 worldPosition = mul( input.Position, World );
    float4 viewPosition = mul( worldPosition, View );
    output.Position = mul( viewPosition, Projection );
    
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PixelShaderFunction( VertexShaderOutput input ) : COLOR0
{
	//return float4( 1, 1, 1, 1 );
    return ( Color * tex2D( DiffuseSampler, input.TexCoord ) );
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader  = compile ps_3_0 PixelShaderFunction();
    }
}
