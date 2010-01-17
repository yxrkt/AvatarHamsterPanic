const float PI = 3.14159265f;

float Boost = 1;
bool Boosting = false;
float Time = 0;

float4 PixelShaderFunction( float2 texCoord : TEXCOORD0 ) : COLOR0
{
  float4 color = float4( 0, 0, 0, 1 );
  if ( texCoord.x <= Boost )
  {
    color = float4( .5 * ( 1 - texCoord.x ), .5 * ( texCoord.x ), 0, 1 );
    color += ( ( .5 + .5 * sin( .25 * 2 * PI * Time ) ) * float4( .2, .2, 0, 0 ) );
    if ( Boosting )
      color.x += .5;
  }
  return color;
}

technique Meter
{
  pass
  {
    PixelShader = compile ps_3_0 PixelShaderFunction();
  }
}
