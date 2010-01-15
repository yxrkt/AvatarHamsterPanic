/*
 * Warp effect used for warping out player baskets/cages
 */

float time = 0.f;
float4x4 matWorldViewProj;


void VS_Warp( inout float4 pos : POSITION )
{
    pos = mul( pos, matWorldViewProj );
}

float4 PS_Warp() : COLOR
{
    return float4( 1.0, 1.0, 1.0, 1.0 - saturate( time ) );
}

technique Warp
{
    pass pass0
    {
        VertexShader = compile vs_3_0 VS_Warp();
        PixelShader  = compile ps_3_0 PS_Warp();
    }
}
