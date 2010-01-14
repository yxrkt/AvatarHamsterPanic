using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;
using System.Diagnostics;

namespace GameObjects
{
  abstract class ParticleSystem : GameObject
  {
    protected static Random rand = new Random();

    public Vector3 Position { get; set; }

    public ParticleSystem( GameplayScreen screen, Vector3 position )
      : base( screen )
    {
      Position = position;
    }

    public abstract override void Update( GameTime gameTime );
    public abstract override void Draw();
  }

  class MeshClusterExplosion : ParticleSystem
  {
    const float fadeTime = 2f;
    float totalTime = 0f;
    MeshParticle[] particles;
    
    const float minSpeed = 2f;
    const float maxSpeed = 9f;

    public MeshClusterExplosion( GameplayScreen screen, Vector3 position, ModelMeshCollection meshes )
      : base( screen, position )
    {
      int nMeshes = meshes.Count;
      particles = new MeshParticle[nMeshes];
      for ( int i = 0; i < nMeshes; ++i )
      {
        ModelBone startBone;
        Matrix originTransform = XFileUtils.GetOriginTransform( meshes[i].ParentBone, out startBone );
        float speed = (float)rand.NextDouble() * ( maxSpeed - minSpeed ) + minSpeed;
        Vector3 velocity = speed * -originTransform.Translation;
        particles[i] = new MeshParticle( meshes[i], velocity );
      }
    }

    public override void Update( GameTime gameTime )
    {
      float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

      // TODO: alpha out and kill
      float alpha = 1f - totalTime / fadeTime;
      if ( alpha <= 0f )
      {
        Screen.ObjectTable.MoveToTrash( this );
      }
      else
      {
        int nParticles = particles.Length;
        for ( int i = 0; i < nParticles; ++i )
        {
          particles[i].position += ( particles[i].velocity * t );
          particles[i].angle += ( particles[i].angularVelocity * t );
          foreach ( BasicEffect effect in particles[i].mesh.Effects )
            particles[i].alpha = MeshParticle.maxAlpha * alpha;
        }
      }

      totalTime += t;
    }

    public override void Draw()
    {
      Matrix modelTransform = Matrix.CreateScale( FloorBlock.Size ) * Matrix.CreateTranslation( Position );

      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      CullMode lastCullMode = graphics.RenderState.CullMode;
      graphics.RenderState.CullMode = CullMode.None;

      foreach ( MeshParticle particle in particles )
      {
        foreach ( BasicEffect effect in particle.mesh.Effects )
        {
          effect.DiffuseColor = Color.White.ToVector3();
          effect.EnableDefaultLighting();
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
          effect.Alpha = particle.alpha;
          effect.DiffuseColor = new Color( 0xB1, 0xBF, 0xD0, 0xFF ).ToVector3();

          ModelBone startBone;
          Matrix originTransform = XFileUtils.GetOriginTransform( particle.mesh.ParentBone, out startBone );
          Matrix rotate = Matrix.CreateFromAxisAngle( particle.axis, particle.angle );
          Matrix translate = Matrix.CreateTranslation( particle.position );
          Matrix offsetTransform = XFileUtils.GetTransform( startBone );
          effect.World = originTransform * rotate * translate * offsetTransform * modelTransform;
        }
        particle.mesh.Draw();
      }

      graphics.RenderState.CullMode = lastCullMode;
    }

    struct MeshParticle
    {
      private static Random rand = new Random( (int)Stopwatch.GetTimestamp() );

      public ModelMesh mesh;
      public Vector3 position;
      public Vector3 velocity;
      public Vector3 axis;
      public float angle;
      public float angularVelocity;
      public float alpha;
      public static float maxAlpha = .5f;

      const float minAngularSpeed = 0.2f;
      const float maxAngularSpeed = 1.5f;

      public MeshParticle( ModelMesh mesh, Vector3 velocity )
      {
        this.mesh = mesh;
        this.position = Vector3.Zero;
        this.velocity = velocity;

        alpha = ( (BasicEffect)mesh.Effects[0] ).Alpha;

        axis = new Vector3( (float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble() );
        if ( axis == Vector3.Zero )
          axis = Vector3.UnitY;
        else
          axis.Normalize();
        if ( rand.Next( 1 ) != 0 )
          axis *= -1f;

        angle = 0f;
        float speed = (float)rand.NextDouble() * ( maxAngularSpeed - minAngularSpeed ) + minAngularSpeed;
        angularVelocity = (float)( MathHelper.TwoPi * speed );
      }
    }
  }

  class ParticleCone : ParticleSystem
  {
    Vector3 direction;
    float angle;
    float coneRadius;
    float life, time;
    float rate;
    Texture2D texture;
    int nParticlesSpawned;
    int nParticlesAlive;
    List<Particle> particles;

    Effect effect;
    EffectParameter effectParameterWorld;
    EffectParameter effectParameterView;
    EffectParameter effectParameterProjection;
    EffectParameter effectParameterColor;

    static VertexPositionTexture[] vertices;
    static int[] indices = { 0, 2, 1, 1, 2, 3 };

    ParticleConeParams creationParams;

    static ParticleCone()
    {
      vertices = new VertexPositionTexture[4];
      vertices[0].Position = new Vector3( -.5f,  .5f,  0f );
      vertices[1].Position = new Vector3(  .5f,  .5f,  0f );
      vertices[2].Position = new Vector3( -.5f, -.5f,  0f );
      vertices[3].Position = new Vector3(  .5f, -.5f,  0f );

      vertices[0].TextureCoordinate = new Vector2( 0f, 0f );
      vertices[1].TextureCoordinate = new Vector2( 1f, 0f );
      vertices[2].TextureCoordinate = new Vector2( 0f, 1f );
      vertices[3].TextureCoordinate = new Vector2( 1f, 1f );
    }

    public ParticleCone( GameplayScreen screen, Vector3 position, Texture2D texture, Vector3 direction, 
                         float angle, float life, float rate, ParticleConeParams creationParams )
      : base( screen, position )
    {
      this.texture = texture;
      this.life = life;
      this.rate = rate;
      this.angle = angle;
      this.direction = direction;
      this.creationParams = creationParams;

      coneRadius = (float)Math.Tan( angle / 2f );

      time = 0f;
      nParticlesSpawned = 0;
      nParticlesAlive = 0;
      particles = new List<Particle>();

      effect = Screen.Content.Load<Effect>( "particleEffect" );
      effect = effect.Clone( Screen.ScreenManager.GraphicsDevice );
      effect.Parameters["Diffuse"].SetValue( texture );

      effectParameterWorld = effect.Parameters["World"];
      effectParameterView = effect.Parameters["View"];
      effectParameterProjection = effect.Parameters["Projection"];
      effectParameterColor = effect.Parameters["Color"];
    }

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      time += elapsed;

      // spawn particles if system hasn't expired
      if ( time < life )
      {
        int nParticlesTotal = (int)( rate * time );
        while ( nParticlesSpawned < nParticlesTotal )
        {
          SpawnParticle();
          nParticlesSpawned++;
          nParticlesAlive++;
        }
      }
      // wait for all particles to finish before destructing
      else if ( nParticlesAlive == 0 )
      {
        Screen.ObjectTable.MoveToTrash( this );
      }

      // update particles
      foreach ( Particle particle in particles )
      {
        if ( particle.time >= particle.life )
        {
          particle.dead = true;
          nParticlesAlive--;
        }
        else
        {
          particle.Position += particle.velocity * elapsed;
          particle.color.A = (byte)( 255.0 * ( 1.0 - Math.Pow( particle.time / particle.life, creationParams.fadePower ) ) );
          particle.time += elapsed;
        }
      }
    }

    public override void Draw()
    {
      GraphicsDevice graphics = Screen.ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = new VertexDeclaration( graphics, VertexPositionTexture.VertexElements );
      SetRenderState( graphics.RenderState );

      Vector3 eye = Screen.Camera.Position;
      Vector3 up = Screen.Camera.Up;

      Matrix view = Screen.View;
      Matrix projection = Screen.Projection;

      effectParameterView.SetValue( view );
      effectParameterProjection.SetValue( projection );

      foreach ( Particle particle in particles )
      {
        if ( particle.dead ) continue;

        effect.CurrentTechnique = effect.Techniques[0];
        effect.Begin();

        //Matrix world = Matrix.CreateBillboard( particle.Position, eye, up, null );
        float length = particle.velocity.Length();
        Vector3 axis = particle.velocity / length;
        Matrix world = Matrix.CreateConstrainedBillboard( particle.Position, eye, axis, null, null );
        world = Matrix.CreateScale( particle.size, ( 1f + creationParams.stretch * length ) * particle.size, particle.size ) * world;
        effectParameterWorld.SetValue( world );
        effectParameterColor.SetValue( particle.color.ToVector4() );

        foreach ( EffectPass pass in effect.CurrentTechnique.Passes )
        {
          pass.Begin();
          graphics.DrawUserIndexedPrimitives( PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2 );
          pass.End();
        }

        effect.End();
      }
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.AlphaBlendEnable = true;
      renderState.SourceBlend = Blend.SourceAlpha;
      renderState.DestinationBlend = Blend.InverseSourceAlpha;

      renderState.AlphaTestEnable = true;
      renderState.AlphaFunction = CompareFunction.Greater;
      renderState.ReferenceAlpha = 0;

      renderState.DepthBufferWriteEnable = false;
    }

    private void SpawnParticle()
    {
      Particle particle = new Particle();
      particle.Position = Position;

      // get random velocity
      float speed = ( creationParams.speedMax - creationParams.speedMin ) * (float)rand.NextDouble() + creationParams.speedMin;
      Vector3 randAxis = new Vector3( (float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f );
      Vector3 cross = Vector3.Cross( direction, randAxis );

      if ( cross == Vector3.Zero )
      {
        // how unlucky can one be...
        particle.velocity = direction * speed;
      }
      else
      {
        cross.Normalize();
        cross *= ( (float)rand.NextDouble() * coneRadius );
        particle.velocity = speed * ( direction + cross );
      }

      // ...and everything else
      particle.time = 0f;
      particle.life = ( creationParams.lifeMax - creationParams.lifeMin ) * (float)rand.NextDouble() + creationParams.lifeMin;
      particle.size = ( creationParams.sizeMax - creationParams.sizeMin ) * (float)rand.NextDouble() + creationParams.sizeMin;
      particle.color = creationParams.color;
      particle.dead = false;


      // overwrite dead particle or add new particle if none are dead
      int deadParticleIndex = particles.FindIndex( p => p.dead );
      if ( deadParticleIndex != -1 )
        particles[deadParticleIndex] = particle;
      else
        particles.Add( particle );
    }

    class Particle
    {
      public Vector3 Position;
      public Vector3 velocity;
      public float time, life;
      public float size;
      public Color color;
      public bool dead;
    }
  }

  struct ParticleConeParams
  {
    public float speedMin, speedMax;
    public float lifeMin, lifeMax;
    public float sizeMin, sizeMax;
    public float stretch;
    public Color color;
    public int fadePower;

    public ParticleConeParams( float speedMin, float speedMax, float lifeMin, float lifeMax,
                               float sizeMin, float sizeMax, float stretch, Color color, int fadePower )
    {
      this.speedMin = speedMin;
      this.speedMax = speedMax;
      this.lifeMin = lifeMin;
      this.lifeMax = lifeMax;
      this.sizeMin = sizeMin;
      this.sizeMax = sizeMax;
      this.stretch = stretch;
      this.color = color;
      this.fadePower = fadePower;
    }
  }
}