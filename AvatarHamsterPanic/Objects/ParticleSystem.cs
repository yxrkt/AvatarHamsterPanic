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
  /// <summary>
  /// Base particle class
  /// </summary>
  class Particle
  {
    public Vector3 Position;
    public Vector3 Velocity;
    public float Time, Life;
    public float Size;
    public float Stretch;
    public Color Color;
    public bool Dead;
    public int FadePower;
  }

  /// <summary>
  /// Particle factory interface
  /// </summary>
  interface IParticleFactory
  {
    Particle CreateParticle( Random rand, Vector3 origin );
  }

  /// <summary>
  /// Base particle class
  /// </summary>
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

  /// <summary>
  /// Hacky mesh explosion particle system
  /// </summary>
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

  class ParticleEmitter : ParticleSystem
  {
    #region Fields

    IParticleFactory factory;
    public IParticleFactory Factory { get { return factory; } }

    bool spraying;
    bool dieAfterSpray;
    float sprayTime;
    float sprayDuration;
    float sprayRate;
    float sprayLastParticle;
    int nParticles;

    float spitRemainder;

    List<Particle> particles;

    Effect effect;
    EffectParameter effectParameterWorld;
    EffectParameter effectParameterView;
    EffectParameter effectParameterProjection;
    EffectParameter effectParameterColor;

    // quad info for drawing
    static VertexPositionTexture[] vertices;
    static int[] indices = { 0, 2, 1, 1, 2, 3 };

    #endregion Fields

    #region Initialization

    /// <summary>
    /// Initialize quad vertices
    /// </summary>
    static ParticleEmitter()
    {
      vertices = new VertexPositionTexture[4];
      vertices[0].Position = new Vector3( -.5f, .5f, 0f );
      vertices[1].Position = new Vector3( .5f, .5f, 0f );
      vertices[2].Position = new Vector3( -.5f, -.5f, 0f );
      vertices[3].Position = new Vector3( .5f, -.5f, 0f );

      vertices[0].TextureCoordinate = new Vector2( 0f, 0f );
      vertices[1].TextureCoordinate = new Vector2( 1f, 0f );
      vertices[2].TextureCoordinate = new Vector2( 0f, 1f );
      vertices[3].TextureCoordinate = new Vector2( 1f, 1f );
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public ParticleEmitter( GameplayScreen screen, Vector3 position, IParticleFactory factory, Texture2D texture )
      : base( screen, position )
    {
      this.factory = factory;

      spraying = false;
      dieAfterSpray = false;
      sprayTime = 0f;
      nParticles = 0;
      spitRemainder = 0f;

      particles = new List<Particle>( 64 );

      GraphicsDevice device = screen.ScreenManager.GraphicsDevice;
      effect = Screen.Content.Load<Effect>( "Effects/particleEffect" ).Clone( device );
      effect = effect.Clone( device );
      effect.Parameters["Diffuse"].SetValue( texture );

      effectParameterWorld = effect.Parameters["World"];
      effectParameterView = effect.Parameters["View"];
      effectParameterProjection = effect.Parameters["Projection"];
      effectParameterColor = effect.Parameters["Color"];
    }

    #endregion

    #region Update and Draw

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

      if ( spraying )
        UpdateSpray( elapsed );

      // update particles
      foreach ( Particle particle in particles )
      {
        if ( particle.Time >= particle.Life )
        {
          particle.Dead = true;
          nParticles--;
        }
        else
        {
          particle.Position += particle.Velocity * elapsed;
          particle.Color.A = (byte)( 255.0 * ( 1.0 - Math.Pow( particle.Time / particle.Life, particle.FadePower ) ) );
          particle.Time += elapsed;
        }
      }
    }

    private void UpdateSpray( float elapsed )
    {
      sprayTime += elapsed;

      // spawn particles if system hasn't expired
      if ( sprayTime < sprayDuration )
      {
        float invSprayRate = 1f / sprayRate;
        float sprayLimit = sprayTime - invSprayRate;
        while ( sprayLastParticle < sprayLimit )
        {
          AddParticle( factory.CreateParticle( rand, Position ) );
          sprayLastParticle += invSprayRate;
          nParticles++;
        }
      }
      // wait for all particles to finish before destructing
      else if ( nParticles == 0 )
      {
        spraying = false;
        if ( dieAfterSpray )
          Screen.ObjectTable.MoveToTrash( this );
      }
    }

    private void AddParticle( Particle particle )
    {
      // overwrite dead particle or add new particle if none are dead
      int deadParticleIndex = particles.FindIndex( p => p.Dead );
      if ( deadParticleIndex != -1 )
        particles[deadParticleIndex] = particle;
      else
        particles.Add( particle );
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      device.VertexDeclaration = new VertexDeclaration( device, VertexPositionTexture.VertexElements );
      SetRenderState( device.RenderState );

      Vector3 eye = Screen.Camera.Position;
      Vector3 up = Screen.Camera.Up;

      Matrix view = Screen.View;
      Matrix projection = Screen.Projection;

      effectParameterView.SetValue( view );
      effectParameterProjection.SetValue( projection );

      foreach ( Particle particle in particles )
      {
        if ( particle.Dead ) continue;

        effect.CurrentTechnique = effect.Techniques[0];
        effect.Begin();

        //Matrix world = Matrix.CreateBillboard( particle.Position, eye, up, null );
        float length = particle.Velocity.Length();
        Vector3 axis = particle.Velocity / length;
        Matrix world = Matrix.CreateConstrainedBillboard( particle.Position, eye, axis, null, null );
        world = Matrix.CreateScale( particle.Size, ( 1f + particle.Stretch * length ) * particle.Size, particle.Size ) * world;
        effectParameterWorld.SetValue( world );
        effectParameterColor.SetValue( particle.Color.ToVector4() );

        foreach ( EffectPass pass in effect.CurrentTechnique.Passes )
        {
          pass.Begin();
          device.DrawUserIndexedPrimitives( PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2 );
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
      renderState.DepthBufferEnable = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sprays particles at a given rate and duration. Can optionally
    /// cause the emitter to self-destruct when done.
    /// </summary>
    /// <param name="duration">The amount of time to spray particles.</param>
    /// <param name="rate">The amount of particles to spray per second.</param>
    /// <param name="dieWhenDone">True if the emitter should self-destruct when done spraying.</param>
    public void Spray( float duration, float rate, bool dieWhenDone )
    {
      spraying = true;
      sprayDuration = duration;
      sprayRate = rate;
      sprayLastParticle = 0f;
      sprayTime = 0f;
    }

    /// <summary>
    /// Instantly creates a desired amount of particles.
    /// </summary>
    /// <param name="nParticles">The number of particles to create.</param>
    public void Spit( float nParticles )
    {
      nParticles += spitRemainder;
      int floor = (int)nParticles;
      spitRemainder = nParticles - (float)floor; 
      for ( int i = 0; i < floor; ++i )
        AddParticle( factory.CreateParticle( rand, Position ) );
    }

    #endregion
  }

  class ParticleConeFactory : IParticleFactory
  {
    Vector3 direction;
    float coneRadius;
    float speedMin, speedMax;
    float lifeMin, lifeMax;
    float sizeMin, sizeMax;
    float stretch;
    Color color;
    int fadePower;

    public Vector3 Direction { get { return direction; } set { direction = value; } }

    public ParticleConeFactory( Vector3 direction, float angle, float speedMin, float speedMax, float lifeMin, float lifeMax,
                                float sizeMin, float sizeMax, float stretch, Color color, int fadePower )
    {
      coneRadius = (float)Math.Tan( angle / 2f );

      this.direction = direction;
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

    public Particle CreateParticle( Random rand, Vector3 origin )
    {
      Particle particle = new Particle();
      particle.Position = origin;

      // get random velocity
      float speed = ( speedMax - speedMin ) * (float)rand.NextDouble() + speedMin;
      Vector3 randAxis = new Vector3( (float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f );
      Vector3 cross = Vector3.Cross( direction, randAxis );

      if ( cross == Vector3.Zero )
      {
        // how unlucky can one be...
        particle.Velocity = direction * speed;
      }
      else
      {
        cross.Normalize();
        cross *= ( (float)rand.NextDouble() * coneRadius );
        particle.Velocity = speed * ( direction + cross );
      }

      // ...and everything else
      particle.Time = 0f;
      particle.Life = ( lifeMax - lifeMin ) * (float)rand.NextDouble() + lifeMin;
      particle.Size = ( sizeMax - sizeMin ) * (float)rand.NextDouble() + sizeMin;
      particle.Stretch = stretch;
      particle.Color = color;
      particle.Dead = false;
      particle.FadePower = fadePower;

      return particle;
    }
  }
}