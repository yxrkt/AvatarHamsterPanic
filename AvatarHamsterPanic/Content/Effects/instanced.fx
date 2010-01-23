float4x4 World;
float4x4 View;
float4x4 Projection;

float3 Eye;
float4 Color = { 1, 1, 1, 1 };
float3 AmbientLight = { 0.05333332, 0.09882354, 0.1819608 };
float3 DirLight0Direction = { -0.5265408, -0.5735765, -0.6275069 };
float3 DirLight0Diffuse   = { 1, 0.9607844, 0.8078432 };
float3 DirLight0Specular  = { 1, 0.9607844, 0.8078432 };
float3 DirLight1Direction = { 0.7198464, 0.3420201, 0.6040227 };
float3 DirLight1Diffuse   = { 0.9647059, 0.7607844, 0.4078432 };
float3 DirLight1Specular  = { 0, 0, 0 };
float3 DirLight2Direction = { 0.4545195, -0.7660444, 0.4545195 };
float3 DirLight2Diffuse   = { 0.3231373, 0.3607844, 0.3937255 };
float3 DirLight2Specular  = { 0.3231373, 0.3607844, 0.3937255 };
float3 SpecularColor   = { 0.7215686, 0.6078432, 0.8980393 };
float  SpecularPower   = 16.0;

float4x4 InstanceTransforms[59];
int VertexCount;

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

TextureSampler( DiffuseMap, DiffuseMapSampler, Linear, Clamp );
TextureSampler( NormalMap, NormalMapSampler, Linear, Clamp );

struct ColorIn
{
  float4 Position : POSITION;
  float3 Normal   : NORMAL;
  float4 Color    : COLOR0;
};

struct ColorOut
{
  float4 Position : POSITION;
  float3 Normal   : TEXCOORD2;
  float3 ViewDir  : TEXCOORD3;
  float4 Color    : COLOR0;
};

struct DiffuseColorIn
{
  float4 Position  : POSITION;
  float2 TextureUV : TEXCOORD0;
  float3 Normal    : NORMAL;
};

struct DiffuseColorOut
{
  float4 Position  : POSITION;
  float2 TextureUV : TEXCOORD0;
  float3 Normal    : TEXCOORD2;
  float3 ViewDir   : TEXCOORD3;
};

struct NormalDiffuseColorIn
{
  float4 Position  : POSITION;
  float2 TextureUV : TEXCOORD;
  float3 Normal    : NORMAL;
  float3 Tangent   : TANGENT;
  float3 Binormal  : BINORMAL;
};

struct NormalDiffuseColorOut
{
  float4 Position  : POSITION;
  float2 TextureUV : TEXCOORD0;
  float3 ViewDir   : TEXCOORD3;
  float3 LightDir0 : TEXCOORD4;
  float3 LightDir1 : TEXCOORD5;
  float3 LightDir2 : TEXCOORD6;
};

float3 ComputePerPixelLights( float3 E, float3 N, float3 LightDir0, float3 LightDir1, float3 LightDir2 )
{
  float3 diffuse  = AmbientLight;
  float3 specular = 0;
  
  // Light0
  float3 L = -LightDir0;
  float3 H = normalize( E + L );
  float dt = max( 0, dot( L, N ) );
  diffuse += DirLight0Diffuse * dt;
  if ( dt != 0 )
	specular += DirLight0Specular * pow( max( 0, dot( H, N ) ), SpecularPower );

  // Light1
  L = -LightDir1;
  H = normalize( E + L );
  dt = max( 0, dot( L, N ) );
  diffuse += DirLight1Diffuse * dt;
  if ( dt != 0 )
	specular += DirLight1Specular * pow( max( 0, dot( H, N ) ), SpecularPower );
	
  // Light2
  L = -LightDir2;
  H = normalize( E + L );
  dt = max( 0, dot( L, N ) );
  diffuse += DirLight2Diffuse * dt;
  if ( dt != 0 )
	specular += DirLight2Specular * pow( max( 0, dot( H, N ) ), SpecularPower );

  return diffuse + specular * SpecularColor;
}

ColorOut VS_Color( ColorIn params )
{
  ColorOut Out = (ColorOut)0;
  
  float4 worldPosition = mul( params.Position, World );
  float4 viewPosition  = mul( worldPosition, View );
  Out.Position = mul( viewPosition, Projection );
  
  Out.Normal = mul( params.Normal, World );
  
  Out.ViewDir = Eye - worldPosition;
  
  Out.Color = params.Color;
  
  return Out;
}

ColorOut VS_VFetchColor( int index : INDEX )
{
  int vertex   = ( index + .5 ) % VertexCount;
  int instance = ( index + .5 ) / VertexCount;

  float4 position;
  float4 normal;
  float4 color;

  asm
  {
    vfetch position, vertex, position0
    vfetch normal,   vertex, normal0
    vfetch color,    vertex, color0
  };
  
  ColorIn In;
  In.Position  = position;
  In.Normal    = normal;
  In.Color     = color;
  
  World = InstanceTransforms[instance];
  
  return VS_Color( In );
}

float4 PS_ColorDefault( ColorOut params ) : COLOR
{
  float3 normal  = normalize( params.Normal );
  float3 viewDir = normalize( params.ViewDir );
  
  float3 I = ComputePerPixelLights( viewDir, normal, DirLight0Direction, DirLight1Direction, DirLight2Direction );
  
  float4 color = float4( I, 1 ) * Color * params.Color;
  
  return color;
}

float4 PS_Color( ColorOut params ) : COLOR
{
  float3 normal  = normalize( params.Normal );
  float3 viewDir = normalize( params.ViewDir );
  
  float3 I = ComputePerPixelLights( viewDir, normal, DirLight0Direction, DirLight1Direction, DirLight2Direction );
  
  float4 color = float4( I, 1 ) * Color;
  
  return color;
}

DiffuseColorOut VS_DiffuseColor( DiffuseColorIn params )
{
  DiffuseColorOut Out = (DiffuseColorOut)0;
  
  float4 worldPosition = mul( params.Position, World );
  float4 viewPosition  = mul( worldPosition, View );
  Out.Position = mul( viewPosition, Projection );
  
  Out.TextureUV = params.TextureUV;
  
  Out.Normal = mul( params.Normal, World );
  
  Out.ViewDir = Eye - worldPosition;
  
  return Out;
}

DiffuseColorOut VS_VFetchDiffuseColor( int index : INDEX )
{
  int vertex   = ( index + .5 ) % VertexCount;
  int instance = ( index + .5 ) / VertexCount;

  float4 position;
  float4 normal;
  float4 texcoord;

  asm
  {
    vfetch position, vertex, position0
    vfetch normal,   vertex, normal0
    vfetch texcoord, vertex, texcoord0
  };
  
  DiffuseColorIn In;
  In.Position  = position;
  In.Normal    = normal;
  In.TextureUV = texcoord;
  
  World = InstanceTransforms[instance];
  
  return VS_DiffuseColor( In );
}

float4 PS_DiffuseColor( DiffuseColorOut params ) : COLOR
{
  float3 normal  = normalize( params.Normal );
  float3 viewDir = normalize( params.ViewDir );
  
  float3 I = ComputePerPixelLights( viewDir, normal, DirLight0Direction, DirLight1Direction, DirLight2Direction );
  
  float4 color = tex2D( DiffuseMapSampler, params.TextureUV ) * float4( I, 1 ) * Color;
  
  return color;
}

NormalDiffuseColorOut VS_NormalDiffuseColor( NormalDiffuseColorIn params )
{
  NormalDiffuseColorOut Out = (NormalDiffuseColorOut)0;
  
  float4 worldPosition = mul( params.Position, World );
  float4 viewPosition  = mul( worldPosition, View );
  Out.Position = mul( viewPosition, Projection );
  
  float3x3 worldToTangentSpace;
  worldToTangentSpace[0] = mul( normalize( params.Tangent ),  World );
  worldToTangentSpace[1] = mul( normalize( params.Binormal ), World );
  worldToTangentSpace[2] = mul( normalize( params.Normal ),   World );
  
  Out.TextureUV = params.TextureUV;
  
  Out.ViewDir = Eye - worldPosition;
  
  Out.LightDir0 = mul( worldToTangentSpace, float4( DirLight0Direction, 1 ) );
  Out.LightDir1 = mul( worldToTangentSpace, float4( DirLight1Direction, 1 ) );
  Out.LightDir2 = mul( worldToTangentSpace, float4( DirLight2Direction, 1 ) );
  
  Out.ViewDir = mul( worldToTangentSpace, float4( Eye, 0 ) - worldPosition );
  
  return Out;
}

NormalDiffuseColorOut VS_VFetchNormalDiffuseColor( int index : INDEX )
{
  int vertex   = ( index + .5 ) % VertexCount;
  int instance = ( index + .5 ) / VertexCount;

  float4 position;
  float4 normal;
  float4 texcoord;
  float4 tangent;
  float4 binormal;

  asm
  {
    vfetch position, vertex, position0
    vfetch normal,   vertex, normal0
    vfetch texcoord, vertex, texcoord0
    vfetch tangent,  vertex, tangent0
    vfetch binormal, vertex, binormal0
  };
  
  NormalDiffuseColorIn In;
  In.Position  = position;
  In.Normal    = normal;
  In.TextureUV = texcoord;
  In.Tangent   = tangent;
  In.Binormal  = binormal;
  
  World = InstanceTransforms[instance];
  
  return VS_NormalDiffuseColor( In );
}

float4 PS_NormalDiffuseColor( NormalDiffuseColorOut params ) : COLOR
{
  //float3 normal  = normalize( params.Normal );
  float3 viewDir = normalize( params.ViewDir );
  float3 lightDir0 = normalize( params.LightDir0 );
  float3 lightDir1 = normalize( params.LightDir1 );
  float3 lightDir2 = normalize( params.LightDir2 );
  
  float3 normal = ( 2.0 * ( tex2D( NormalMapSampler, params.TextureUV ) ) ) - 1.0;
  
  float3 I = ComputePerPixelLights( viewDir, normal, lightDir0, lightDir1, lightDir2 );
  
  float4 color = tex2D( DiffuseMapSampler, params.TextureUV ) * float4( I, 1 ) * Color;
  
  return color;
}

technique ColorDefault
{
  pass
  {
    VertexShader = compile vs_3_0 VS_VFetchColor();
    PixelShader  = compile ps_3_0 PS_ColorDefault();
  }
}

technique Color
{
  pass
  {
    VertexShader = compile vs_3_0 VS_VFetchColor();
    PixelShader  = compile ps_3_0 PS_Color();
  }
}

technique DiffuseColor
{
  pass
{
    VertexShader = compile vs_3_0 VS_VFetchDiffuseColor();
    PixelShader  = compile ps_3_0 PS_DiffuseColor();
  }
}

technique NormalDiffuseColor
{
  pass
  {
    VertexShader = compile vs_3_0 VS_VFetchNormalDiffuseColor();
    PixelShader  = compile ps_3_0 PS_NormalDiffuseColor();
  }
}
