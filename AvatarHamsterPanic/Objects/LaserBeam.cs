using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CustomModelSample;
using Physics;
using Utilities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AvatarHamsterPanic.Objects
{
  class LaserBeam : GameObject
  {
    Vector2 startPosition;
    Vector2 parentVelocity;
    float startVelocity;
    float endVelocity;
    float accel;
    float duration;
    float age;
    Player parent;

    List<Player> pwntPlayers = new List<Player>( 3 );

    static Model model;

    Matrix worldTransform;
    PhysPolygon body;
    bool valid;

    static readonly Pool<LaserBeam> pool = new Pool<LaserBeam>( 6, b => b.valid )
    {
      Initialize = ( i => i.valid = true )
    };
    static readonly float beamThickness = .1f;
    static readonly float ageOffset = .0625f;

    public static void Initialize()
    {
      model = GameplayScreen.Instance.Content.Load<Model>( "Models/cylinder" );
    }

    public static void Unload()
    {
      ReadOnlyCollection<LaserBeam> lasers = GameplayScreen.Instance.ObjectTable.GetObjects<LaserBeam>();
      for ( int i = 0; i < lasers.Count; ++i )
        lasers[i].valid = false;
      pool.CleanUp();
    }

    public static LaserBeam CreateBeam( Vector2 position, Vector2 parentVelocity, Player parent, bool left )
    {
      LaserBeam beam = pool.New();

      float startVelocity = left ? -18f : 18f;
      beam.Initialize( position, parentVelocity, parent, startVelocity, 0, 1f );

      return beam;
    }

    private void Initialize( Vector2 position, Vector2 parentVelocity, Player parent, 
                             float startVelocity, float endVelocity, float duration )
    {
      Screen = GameplayScreen.Instance;
      body.Released = false;
      PhysBody.AllBodies.Add( body );
      for ( int i = 0; i < 4; ++i )
      {
        body.Vertices[i].X = position.X;
        body.Vertices[i].Y = ( i < 2 ) ? position.Y + beamThickness / 2 : position.Y - beamThickness / 2;
      }

      this.startPosition  = position;
      this.parentVelocity = parentVelocity;
      this.parent         = parent;
      this.startVelocity  = startVelocity;
      this.endVelocity    = endVelocity;
      this.accel          = ( startVelocity - endVelocity ) / ( duration * 2 );
      this.duration       = duration;

      pwntPlayers.Clear();

      age = 0;
    }

    private LaserBeam()
      : this( GameplayScreen.Instance )
    {
    }

    private LaserBeam( GameplayScreen screen )
      : base( screen )
    {
      body = new PhysPolygon( 0f, beamThickness, Vector2.Zero, 1f );
      body.Collided += HandlePlayerCollision;
      body.Flags = PhysBodyFlags.Anchored | PhysBodyFlags.Ghost;
      body.Release();
    }

    public override void Update( GameTime gameTime )
    {
      if ( age - ageOffset > duration )
      {
        Die();
        return;
      }

      Vector2 parentVelocityOffset = parentVelocity * age;

      // update first side
      float age0 = Math.Min( age, duration );
      float x0 = startPosition.X + startVelocity * age0 - age0 * age0 * accel + parentVelocityOffset.X;
      body.Vertices[1].X = x0;
      body.Vertices[2].X = x0;

      // update second side
      float age1 = MathHelper.Clamp( age - ageOffset, 0, duration );
      float x1 = startPosition.X + startVelocity * age1 - age1 * age1 * accel + parentVelocityOffset.X;
      body.Vertices[3].X = x1;
      body.Vertices[0].X = x1;

      // update model transform
      worldTransform  = Matrix.CreateTranslation( .5f, 0, 0 );
      worldTransform *= Matrix.CreateScale( x0 - x1, beamThickness, beamThickness );
      worldTransform *= Matrix.CreateTranslation( x1, startPosition.Y + parentVelocityOffset.Y, 0f );

      age += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw()
    {
      foreach ( ModelMesh mesh in model.Meshes )
      {
        for ( int i = 0; i < mesh.Effects.Count; ++i )
        {
          BasicEffect effect = (BasicEffect)mesh.Effects[i];
          effect.EnableDefaultLighting();
          effect.World = worldTransform;
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
        }
        mesh.Draw();
      }
    }

    private void Die()
    {
      Screen.ObjectTable.MoveToTrash( this );
      body.Release();
      valid = false;
    }

    private bool HandlePlayerCollision( CollisResult result )
    {
      Player player = result.BodyB.Parent as Player;
      if ( player != null && player != parent && !player.Seizuring && !pwntPlayers.Contains( player ) )
      {
        player.TakeLaserUpAss( result );
        pwntPlayers.Add( player );
      }

      return true;
    }
  }
}