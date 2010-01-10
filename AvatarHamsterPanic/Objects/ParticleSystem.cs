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
    Vector3 contactPoint;
    MeshParticle[] particles;
    
    const float minSpeed = 2f;
    const float maxSpeed = 9f;

    public MeshClusterExplosion( GameplayScreen screen, Vector3 position, Vector3 contactPoint, ModelMeshCollection meshes )
      : base( screen, position )
    {
      this.contactPoint = contactPoint;

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

      // TODO: Render backfaces
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
}