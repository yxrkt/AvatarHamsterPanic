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
using Menu;
using Graphics;
using Audio;

namespace AvatarHamsterPanic.Objects
{
  class LaserBeam : GameObject, IAudioEmitter
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

    static CustomModel model;

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
      model = GameplayScreen.Instance.Content.Load<CustomModel>( "Models/cylinder" );

      Vector4 color = new Color( Color.Green, 1f ).ToVector4();
      Vector4 maskValue = MaskHelper.Glow( 1f );
      foreach ( CustomModel.ModelPart part in model.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
        part.Effect.Parameters["Color"].SetValue( color );
        part.Effect.Parameters["Mask"].SetValue( maskValue );
        //part.Effect.Parameters["LightingEnabled"].SetValue( false );
      }
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
      Screen.PhysicsSpace.AddBody( body );
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

      Update( Screen.LastGameTime );
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
      body.Flags = BodyFlags.Anchored | BodyFlags.Ghost;
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

      body.ComputeBoundingCircle();

      age += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw()
    {
      model.Draw( Screen.Camera.Position, worldTransform, Screen.View, Screen.Projection );
    }

    private void Die()
    {
      Screen.ObjectTable.MoveToTrash( this );
      Screen.PhysicsSpace.RemoveBody( body );
      valid = false;
    }

    private bool HandlePlayerCollision( Collision result )
    {
      Player player = result.BodyB.Parent as Player;
      if ( player != null && player != parent && !player.Seizuring && !pwntPlayers.Contains( player ) )
      {
        player.TakeLaserUpAss( result );
        pwntPlayers.Add( player );

        IAudioEmitter emitter = DummyAudioEmitter.InstanceAtPos( new Vector3( result.Intersection, 0 ) );
        GameCore.Instance.AudioManager.Play3DCue( "laserHit", emitter, 1 );
      }

      return true;
    }

    #region IAudioEmitter Members

    public Vector3 Position
    {
      get { return new Vector3( body.Position, 0 ); }
    }

    public Vector3 Forward
    {
      get { return Vector3.Forward; }
    }

    public Vector3 Up
    {
      get { return Screen.Camera.Up; }
    }

    public Vector3 Velocity
    {
      get { return Vector3.Zero; }
    }

    #endregion
  }
}