using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Utilities;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic.Utilities;

namespace AvatarHamsterPanic.Objects
{
  public class ModelExplosion
  {
    private static readonly Pool<ModelExplosion> _pool =
      new Pool<ModelExplosion>( 10, m => m.Valid )
      {
        Initialize = m => 
        {
          m.time = 0f;
          m.Valid = true;
        },
        Deinitialize = m =>
        {
          m.particles.Clear();
        }
      };

    private static Random rand = new Random();

    List<MeshParticle> particles;
    float time = 0;
    float expiration = 2;
    Matrix worldTransform;

    public ParticleManager Manager { get; internal set; }
    public bool Valid { get; private set; }

    public static ModelExplosion CreateExplosion( Vector3 position, float scale, Model model, 
                                                  ModelExplosionSettings settings )
    {
      ModelExplosion explosion = _pool.New();

      explosion.worldTransform = Matrix.CreateScale( scale ) * Matrix.CreateTranslation( position );

      foreach ( ModelMesh mesh in model.Meshes )
      {
        ModelBone startBone;
        Matrix originTransform = XFileUtils.GetOriginTransform( mesh.ParentBone, out startBone );
        Vector3 velocity = rand.NextFloat( settings.MinSpeed, settings.MaxSpeed ) * -originTransform.Translation;
        float rotationsPerSec = rand.NextFloat( settings.MinRotationsPerSecond, settings.MaxRotationsPerSecond );
        explosion.particles.Add( MeshParticle.CreateParticle( mesh, velocity, rotationsPerSec, .5f ) );
      }

      return explosion;
    }

    private ModelExplosion()
    {
      particles = new List<MeshParticle>( 40 );
    }

    public void Update( GameTime gameTime )
    {
      if ( time > expiration )
      {
        Valid = false;
      }
      else
      {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach ( MeshParticle particle in particles )
        {
          particle.Position += ( particle.Velocity * t );
          particle.Angle += ( particle.AngularVelocity * t );
          particle.Alpha = particle.StartingAlpha * ( 1f - time / expiration );
        }

        time += t;
      }
    }

    public void Draw( GraphicsDevice device, Effect effect, EffectParameter worldParam, EffectParameter colorParam )
    {
      foreach ( MeshParticle particle in particles )
      {
        colorParam.SetValue( new Vector4( .69f, .75f, .82f, particle.Alpha ) );

        ModelBone startBone;
        Matrix originTransform = XFileUtils.GetOriginTransform( particle.Mesh.ParentBone, out startBone );
        Matrix rotate = Matrix.CreateFromAxisAngle( particle.Axis, particle.Angle );
        Matrix translate = Matrix.CreateTranslation( particle.Position );
        Matrix offsetTransform = XFileUtils.GetTransform( startBone );
        worldParam.SetValue( originTransform * rotate * translate * offsetTransform * worldTransform );

        effect.CommitChanges();

        foreach ( ModelMeshPart part in particle.Mesh.MeshParts )
        {
          device.Vertices[0].SetSource( particle.Mesh.VertexBuffer, part.StreamOffset, part.VertexStride );
          device.Indices = particle.Mesh.IndexBuffer;
          device.DrawIndexedPrimitives( PrimitiveType.TriangleList, part.BaseVertex, 0, part.NumVertices,
                                        part.StartIndex, part.PrimitiveCount );
        }
      }
    }
  }

  public class MeshParticle
  {
    private static Random rand = new Random();

    public ModelMesh Mesh;
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Axis;
    public float Angle;
    public float AngularVelocity;
    public float Alpha;
    public float StartingAlpha;

    private static readonly Pool<MeshParticle> _pool = new Pool<MeshParticle>( 300, p => p.Alpha > 0f );

    public static MeshParticle CreateParticle( ModelMesh mesh, Vector3 velocity, 
                                               float rotationsPerSec, float startingAlpha )
    {
      MeshParticle particle = _pool.New();

      particle.Mesh = mesh;
      particle.Position = Vector3.Zero;
      particle.Velocity = velocity;

      particle.StartingAlpha = startingAlpha;
      particle.Alpha = startingAlpha;

      particle.Axis = rand.NextVector3();
      if ( particle.Axis == Vector3.Zero )
        particle.Axis = Vector3.UnitY;
      else
        particle.Axis.Normalize();
      if ( rand.Next( 1 ) != 0 )
        particle.Axis *= -1f;

      particle.Angle = 0f;
      particle.AngularVelocity = MathHelper.TwoPi * rotationsPerSec;

      return particle;
    }

    private MeshParticle()
    {
    }
  }

  public class ModelExplosionSettings
  {
    public float MinSpeed { get; set; }
    public float MaxSpeed { get; set; }

    public float MinRotationsPerSecond { get; set; }
    public float MaxRotationsPerSecond { get; set; }

    public float StartingAlpha { get; set; }

    public ModelExplosionSettings()
    {
      MinSpeed = 2f;
      MaxSpeed = 9f;

      MinRotationsPerSecond = .2f;
      MaxRotationsPerSecond = 1.5f;

      StartingAlpha = .5f;
    }
  }
}