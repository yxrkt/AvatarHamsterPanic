float4x4 World;
float4x4 View;
float4x4 Projection;

float3 Eye;
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

struct VS_BASIC_IN
{
    float4 Position  : POSITION;
    float2 TextureUV : TEXCOORD0;
    float3 Normal    : NORMAL;
};

struct VS_BASIC_OUT
{
    float4 Position  : POSITION;
    float2 TextureUV : TEXCOORD0;
    float3 Normal    : TEXCOORD2;
    float3 ViewDir   : TEXCOORD3;
};

struct VS_NORMALMAP_IN
{
    float4 Position  : POSITION;
    float2 TextureUV : TEXCOORD;
    float3 Normal    : NORMAL;
    float3 Tangent   : TANGENT;
    float3 Binormal  : BINORMAL;
};

struct VS_NORMALMAP_OUT
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

VS_BASIC_OUT VS_Basic( VS_BASIC_IN params )
{
    VS_BASIC_OUT Out = (VS_BASIC_OUT)0;
    
    float4 worldPosition = mul( params.Position, World );
    float4 viewPosition  = mul( worldPosition, View );
    Out.Position = mul( viewPosition, Projection );
    
    Out.TextureUV = params.TextureUV;
    
    Out.Normal = mul( params.Normal, World );
    
    Out.ViewDir = Eye - worldPosition;
    
    return Out;
}

float4 PS_Basic( VS_BASIC_OUT params ) : COLOR
{
    float3 normal  = normalize( params.Normal );
    float3 viewDir = normalize( params.ViewDir );
    
    float3 I = ComputePerPixelLights( viewDir, normal, DirLight0Direction, DirLight1Direction, DirLight2Direction );
    
    float4 color = tex2D( DiffuseMapSampler, params.TextureUV ) * float4( I, 1 );
    
    return color;
}

VS_NORMALMAP_OUT VS_NormalMap( VS_NORMALMAP_IN params )
{
    VS_NORMALMAP_OUT Out = (VS_NORMALMAP_OUT)0;
    
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

float4 PS_NormalMap( VS_NORMALMAP_OUT params ) : COLOR
{
    /**/
    //float3 normal  = normalize( params.Normal );
    float3 viewDir = normalize( params.ViewDir );
    float3 lightDir0 = normalize( params.LightDir0 );
    float3 lightDir1 = normalize( params.LightDir1 );
    float3 lightDir2 = normalize( params.LightDir2 );
    
    float3 normal = ( 2.0 * ( tex2D( NormalMapSampler, params.TextureUV ) ) ) - 1.0;
    
    float3 I = ComputePerPixelLights( viewDir, normal, lightDir0, lightDir1, lightDir2 );
    
    float4 color = tex2D( DiffuseMapSampler, params.TextureUV ) * float4( I, 1 );
    
    return color;
    /*/
	// Get the color from ColorMapSampler using the texture coordinates in TextureUV.
	float4 Color = tex2D( DiffuseMapSampler, params.TextureUV );
	
	float3 L = normalize( params.LightDir0 );
	float3 V = normalize( params.ViewDir );

	// Get the Color of the normal. The color describes the direction of the normal vector
	// and make it range from 0 to 1.
	float3 N = ( 2.0 * ( tex2D( NormalMapSampler, params.TextureUV ) ) ) - 1.0;

	// diffuse
	float D = saturate( dot( N, L ) );

	// reflection
	float3 R = normalize( 2 * D * N - L );

	// specular
	float S = pow( saturate( dot( R, V ) ), 2 );

	// calculate light ( ambient + diffuse + specular )
	const float4 Ambient = float4( 0.0, 0.0, 0.0, 1.0 );
	return Color * Ambient + Color * D + Color * S;
	/**/
}


technique Basic
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS_Basic();
        PixelShader  = compile ps_3_0 PS_Basic();
    }
}


technique NormalMap
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS_NormalMap();
        PixelShader  = compile ps_3_0 PS_NormalMap();
    }
}
