using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Physics;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Objects
{
  enum PowerupType
  {
    ScoreCoin,
  }

  class Powerup : GameObject
  {
    // event delegates
    private delegate void UpdateMethod( GameTime gameTime );

    Matrix world;
    int owner;
    bool alive;

    static VertexDeclaration vertexDeclaration;
    static GameplayScreen screen;
    static Model scoreCoinModel;
    static Powerup[] pool;

    event UpdateMethod UpdateSelf;

    public PhysCircle Body { get; private set; }
    public Model Model { get; private set; }
    public float Size { get; private set; }

    public static float DeathLine { get; private set; }

    public static void Initialize( GameplayScreen screen )
    {
      Powerup.screen = screen;
      GraphicsDevice device = screen.ScreenManager.GraphicsDevice;
      vertexDeclaration = new VertexDeclaration( device, VertexPositionColor.VertexElements );

      scoreCoinModel = screen.Content.Load<Model>( "Models/collectible" );

      float maxPowerupSize = 2f;
      Camera camera = screen.Camera;
      float dist = camera.Position.Z + maxPowerupSize / 2f;
      float height = dist * (float)Math.Tan( camera.Fov / 2f );
      DeathLine = height + maxPowerupSize / 2f;

      const int poolSize = 20;
      pool = new Powerup[poolSize];
      for ( int i = 0; i < poolSize; ++i )
        pool[i] = new Powerup( screen );
    }

    public static Powerup CreatePowerup( Vector2 pos, PowerupType type )
    {
      foreach ( Powerup powerup in pool )
      {
        if ( !powerup.alive )
        {
          powerup.Initialize( pos, type );
          return powerup;
        }
      }

      return null;
    }

    private void Initialize( Vector2 pos, PowerupType type )
    {
      Body.Position = pos;
      Body.released = false;
      PhysBody.AllBodies.Add( Body );
      alive = true;
      owner = -1;

      switch ( type )
      {
        case PowerupType.ScoreCoin:
          Size = .6f;
          Body.Radius = Size / 2f;
          Model = scoreCoinModel;
          UpdateSelf += UpdateScoreCoin;
          Body.Collided += HandleCoinCollision;
          break;
      }
    }

    private Powerup( GameplayScreen screen )
      : base( screen )
    {
      Body = new PhysCircle( 1f, Vector2.Zero, 1f );
      Body.Flags = PhysBodyFlags.Anchored | PhysBodyFlags.Ghost;
      Body.Parent = this;
      Body.Release();
    }

    public override void Update( GameTime gameTime )
    {
      if ( Body.Position.Y > Screen.Camera.Position.Y + DeathLine )
      {
        Body.Release();
        Screen.ObjectTable.MoveToTrash( this );
        alive = false;
      }
      else if ( UpdateSelf != null )
        UpdateSelf( gameTime );
    }

    private void UpdateScoreCoin( GameTime gameTime )
    {
      float angle = .25f * MathHelper.TwoPi * (float)gameTime.TotalGameTime.TotalSeconds;
      world = Matrix.CreateScale( Size ) * Matrix.CreateRotationY( angle );
      world *= Matrix.CreateTranslation( new Vector3( Body.Position, 0f ) );
    }

    public override void Draw()
    {
      SetupRendering();

      foreach ( ModelMesh mesh in Model.Meshes )
      {
        ModelEffectCollection effects = mesh.Effects;
        int nEffects = effects.Count;
        for ( int i = 0; i < nEffects; ++i )
        {
          BasicEffect effect = (BasicEffect)effects[i];
          effect.EnableDefaultLighting();
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
          effect.World = world;
        }
        mesh.Draw();
      }
    }

    void SetupRendering()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      device.VertexDeclaration = vertexDeclaration;
      renderState.AlphaBlendEnable = false;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
    }

    public bool HandleCoinCollision( CollisResult result )
    {
      if ( owner == -1 && result.BodyB.Parent is Player )
      {
        Player player = (Player)result.BodyB.Parent;
        owner = player.PlayerNumber;
        player.HUD.AddPoints( 1 );

        result.BodyA.Release();
        Screen.ObjectTable.MoveToTrash( this );
        alive = false;
      }

      return true;
    }
  }
}