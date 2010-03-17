//-----------------------------------------------------------------------------
// ParticleEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


// Camera parameters.
float4x4 View;
float4x4 Projection;
float ViewportHeight;


// The current time, in seconds.
float CurrentTime;


// Parameters describing how the particles animate.
float Duration;
float DurationRandomness;
float3 Gravity;
float3 Colors[3];
float EndVelocity;
float StretchVelocity;
float StretchFactor;
bool AlignWithVelocity;
int FadePower;
float4 Mask;


// These float2 parameters describe the min and max of a range.
// The actual value is chosen differently for each particle,
// interpolating between x and y by some random amount.
float2 RotateSpeed;
float2 StartSize;
float2 MidSize;
float2 EndSize;


// Particle texture and sampler.
texture Texture;

sampler Sampler = sampler_state
{
  Texture = (Texture);
  
  MinFilter = Linear;
  MagFilter = Linear;
  MipFilter = Point;
  
  AddressU = Clamp;
  AddressV = Clamp;
};


// Vertex shader input structure describes the start position and
// velocity of the particle, and the time at which it was created,
// along with some random values that affect its size and rotation.
struct VertexShaderInput
{
  float3 Position : POSITION0;
  float3 Velocity : NORMAL0;
  float4 Random : COLOR0;
  float Time : TEXCOORD0;
};


// Vertex shader output structure specifies the position, size, and
// color of the particle, plus a 2x2 rotation matrix (packed into
// a float4 value because we don't have enough color interpolators
// to send this directly as a float2x2).
struct VertexShaderOutput
{
  float4 Position : POSITION0;
  float Size : PSIZE0;
  float4 Color : COLOR0;
  float4 Rotation : COLOR1;
};


// Vertex shader helper for computing the position of a particle.
float4 ComputeParticlePosition(float3 position, float3 velocity,
                               float age, float normalizedAge)
{
  float startVelocity = length(velocity);

  // Work out how fast the particle should be moving at the end of its life,
  // by applying a constant scaling factor to its starting velocity.
  float endVelocity = startVelocity * EndVelocity;
  
  // Our particles have constant acceleration, so given a starting velocity
  // S and ending velocity E, at time T their velocity should be S + (E-S)*T.
  // The particle position is the sum of this velocity over the range 0 to T.
  // To compute the position directly, we must integrate the velocity
  // equation. Integrating S + (E-S)*T for T produces S*T + (E-S)*T*T/2.

  float velocityIntegral = startVelocity * normalizedAge +
                           (endVelocity - startVelocity) * normalizedAge *
                                                           normalizedAge / 2;
   
  position += normalize(velocity) * velocityIntegral * Duration;
  
  // Apply the gravitational force.
  position += Gravity * age * normalizedAge;
  
  // Apply the camera view and projection transforms.
  return mul(mul(float4(position, 1), View), Projection);
}


// Vertex shader helper for computing the size of a particle.
float ComputeParticleSize(float4 projectedPosition, float3 velocity, 
                          float randomValue, float normalizedAge, out float stretch)
{
  float size;
  
  if ( MidSize.x == 0 && MidSize.y == 0 )
  {
    float startSize = lerp(StartSize.x, StartSize.y, randomValue);
    float endSize = lerp(EndSize.x, EndSize.y, randomValue);
    
    size = lerp(startSize, endSize, normalizedAge);
  }
  else
  {
    if ( normalizedAge < .5 )
    {
      float startSize = lerp(StartSize.x, StartSize.y, randomValue);
      float midSize = lerp(MidSize.x, MidSize.y, randomValue);
      size = lerp(startSize, midSize, normalizedAge * 2);
    }
    else
    {
      float midSize = lerp(MidSize.x, MidSize.y, randomValue);
      float endSize = lerp(EndSize.x, EndSize.y, randomValue);
      size = lerp(midSize, endSize, ( normalizedAge - .5 ) * 2);
    }
  }
  
  // Velocity stretching
  stretch = 1;
  if ( StretchVelocity != 0 && StretchFactor != 0 )
  {
    float3 startVelocity = length(velocity);
    float3 endVelocity = startVelocity * EndVelocity;
    velocity = lerp(length(velocity), length(velocity) * EndVelocity, normalizedAge);
    
    float3 projectedVelocity = mul(mul(velocity, View), Projection);
    float speed = length(projectedVelocity);
    if ( speed > StretchVelocity )
      stretch = 1 + StretchFactor * ( ( speed / StretchVelocity ) - 1 );
  }
  
  // Project the size into screen coordinates.
  return stretch * size * Projection._m11 / projectedPosition.w * ViewportHeight / 2;
}


// Vertex shader helper for computing the color of a particle.
float ComputeParticleAlpha(float normalizedAge)
{
  float a;
  
  if ( FadePower == 0 )
    a = normalizedAge * (1-normalizedAge) * (1-normalizedAge) * 6.7;
  else
    a = 1 - pow(normalizedAge, FadePower);
 
  return a;
}


// Vertex shader helper for computing the rotation of a particle.
float4 ComputeParticleRotation(float randomValue, float age)
{
  // Apply a random factor to make each particle rotate at a different speed.
  float rotateSpeed = lerp(RotateSpeed.x, RotateSpeed.y, randomValue);
  
  float rotation = rotateSpeed * age;

  // Compute a 2x2 rotation matrix.
  float c = cos(rotation);
  float s = sin(rotation);
  
  float4 rotationMatrix = float4(c, -s, s, c);
  
  // Normally we would output this matrix using a texture coordinate interpolator,
  // but texture coordinates are generated directly by the hardware when drawing
  // point sprites. So we have to use a color interpolator instead. Only trouble
  // is, color interpolators are clamped to the range 0 to 1. Our rotation values
  // range from -1 to 1, so we have to scale them to avoid unwanted clamping.
  
  rotationMatrix *= 0.5;
  rotationMatrix += 0.5;
  
  return rotationMatrix;
}


float4 ComputeVelocityAlignedRotation(float3 velocity)
{
  float2 screenVelocity = mul(mul(velocity, View), Projection);
  screenVelocity = normalize(screenVelocity);
  
  float c = screenVelocity.y;
  float s = screenVelocity.x;
  
  float4 rotationMatrix = float4(c, -s, s, c);
  
  rotationMatrix /= 2;
  rotationMatrix += .5;
  
  return rotationMatrix;
}


// Custom vertex shader animates particles entirely on the GPU.
VertexShaderOutput VertexShader(VertexShaderInput input)
{
  VertexShaderOutput output;
  
  // Compute the age of the particle.
  float age = CurrentTime - input.Time;
  
  // Apply a random factor to make different particles age at different rates.
  age *= 1 + input.Random.x * DurationRandomness;
  
  float normalizedAge = saturate(age / Duration);

  output.Position = ComputeParticlePosition(input.Position, input.Velocity,
                                            age, normalizedAge);
  
  float stretch;
  output.Size = ComputeParticleSize(output.Position, input.Velocity, input.Random.y, normalizedAge, stretch);
  output.Color.r = 1 / stretch;
  output.Color.g = 1 / ( 3 * input.Random.z / 255 );
  output.Color.b = 0; // not in use
  output.Color.a = ComputeParticleAlpha(normalizedAge);
  
  if ( AlignWithVelocity )
    output.Rotation = ComputeVelocityAlignedRotation(input.Velocity);
  else
    output.Rotation = ComputeParticleRotation(input.Random.w, age);
  
  return output;
}


// Pixel shader input structure for particles that do not rotate.
struct NonRotatingPixelShaderInput
{
  float4 Color : COLOR0;
    
#ifdef XBOX
  float2 TextureCoordinate : SPRITETEXCOORD;
#else
  float2 TextureCoordinate : TEXCOORD0;
#endif
};


// Pixel shader for drawing particles that do not rotate.
float4 NonRotatingPixelShader(NonRotatingPixelShaderInput input) : COLOR0
{
  return tex2D(Sampler, input.TextureCoordinate) * input.Color;
}


// Pixel shader input structure for particles that can rotate.
struct RotatingPixelShaderInput
{
  float4 Color : COLOR0;
  float4 Rotation : COLOR1;
  
#ifdef XBOX
  float2 TextureCoordinate : SPRITETEXCOORD;
#else
  float2 TextureCoordinate : TEXCOORD0;
#endif
};

struct PixelShaderOutput
{
  float4 Color : COLOR0;
  float4 Mask  : COLOR1;
};


// Pixel shader for drawing particles that can rotate. It is not actually
// possible to rotate a point sprite, so instead we rotate our texture
// coordinates. Leaving the sprite the regular way up but rotating the
// texture has the exact same effect as if we were able to rotate the
// point sprite itself.
PixelShaderOutput RotatingPixelShader( RotatingPixelShaderInput input )
{
  PixelShaderOutput output;

  float2 textureCoordinate = input.TextureCoordinate;

  // We want to rotate around the middle of the particle, not the origin,
  // so we offset the texture coordinate accordingly.
  textureCoordinate -= 0.5;
  
  // Apply the rotation matrix, after rescaling it back from the packed
  // color interpolator format into a full -1 to 1 range.
  float4 rotation = input.Rotation * 2 - 1;
  
  textureCoordinate = mul(textureCoordinate, float2x2(rotation));
  
  // Point sprites are squares. So are textures. When we rotate one square
  // inside another square, the corners of the texture will go past the
  // edge of the point sprite and get clipped. To avoid this, we scale
  // our texture coordinates to make sure the entire square can be rotated
  // inside the point sprite without any clipping.
  textureCoordinate.x /= input.Color.r;
  textureCoordinate *= sqrt(2);
  
  // Undo the offset used to control the rotation origin.
  textureCoordinate += 0.5;

  int index = (int)(input.Color.g) % 3;
  float3 color = Colors[clamp( index, 0, 2 )];
  output.Color = tex2D(Sampler, textureCoordinate) * float4( color.rgb, input.Color.a );
  if ( output.Color.a != 0 )
    output.Mask = Mask;
  else
    output.Mask = float4( 0, 0, 0, 0 );
  return output;
}


// Effect technique for drawing particles that do not rotate. Works with shader 1.1.
technique NonRotatingParticles
{
  pass P0
  {
    VertexShader = compile vs_2_0 VertexShader();
    PixelShader = compile ps_2_0 NonRotatingPixelShader();
  }
}


// Effect technique for drawing particles that can rotate. Requires shader 2.0.
technique RotatingParticles
{
  pass P0
  {
    VertexShader = compile vs_2_0 VertexShader();
    PixelShader = compile ps_2_0 RotatingPixelShader();
  }
}
