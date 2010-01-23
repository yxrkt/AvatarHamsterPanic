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

    event UpdateMethod UpdateSelf;

    public PhysBody Body { get; private set; }
    public Model Model { get; private set; }
    public float Size { get; private set; }

    public static float DeathLine { get; private set; }

    public static Powerup CreatePowerup( GameplayScreen screen, Vector2 pos, PowerupType type )
    {
      switch ( type )
      {
        case PowerupType.ScoreCoin:
          float size = .6f;
          PhysCircle body = new PhysCircle( size / 2f, pos, 1f );
          body.Flags = PhysBodyFlags.Anchored | PhysBodyFlags.Ghost;
          Model model = screen.Content.Load<Model>( "Models/collectible" );
          Powerup coin = new Powerup( screen, body, model, size );
          coin.UpdateSelf += coin.UpdateScoreCoin;
          body.Collided += coin.HandleCoinCollision;
          return coin;
        default:
          return null;
      }
    }

    private Powerup( GameplayScreen screen, PhysBody body, Model model, float size )
      : base( screen )
    {
      owner = -1;

      Body = body;
      Model = model;
      Size = size;

      body.Parent = this;

      if ( DeathLine == 0f )
      {
        Camera camera = screen.Camera;
        float  dist   = camera.Position.Z + size / 2f;
        float  height = dist * (float)Math.Tan( camera.Fov / 2f );
        DeathLine = height + size / 2f;
      }
    }

    public override void Update( GameTime gameTime )
    {
      if ( Body.Position.Y > Screen.Camera.Position.Y + DeathLine )
      {
        Body.Release();
        Screen.ObjectTable.MoveToTrash( this );
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
      foreach ( ModelMesh mesh in Model.Meshes )
      {
        foreach ( BasicEffect effect in mesh.Effects )
        {
          effect.EnableDefaultLighting();
          effect.View = Screen.View;
          effect.Projection = Screen.Projection;
          effect.World = world;
        }
        mesh.Draw();
      }
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
      }

      return true;
    }
  }
}