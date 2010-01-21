using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GameStateManagement;
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

    event UpdateMethod UpdateSelf;

    public PhysBody Body { get; private set; }
    public Model Model { get; private set; }
    public float Size { get; private set; }

    public static Powerup CreatePowerup( GameplayScreen screen, Vector2 pos, PowerupType type )
    {
      switch ( type )
      {
        case PowerupType.ScoreCoin:
          float size = .3f;
          PhysCircle body = new PhysCircle( size / 2f, pos, 1f );
          body.Flags = PhysBodyFlags.Anchored | PhysBodyFlags.Ghost;
          Model model = screen.Content.Load<Model>( "Models/collectible" );
          UpdateMethod update = UpdateScoreCoin;
          return new Powerup( screen, update, body, model, .3f );
        default:
          return null;
      }
    }

    private Powerup( GameplayScreen screen, UpdateMethod update, PhysBody body, Model model, float size )
      : base( screen )
    {
      Body = body;
      Model = model;
      Size = size;
    }

    public override void Update( GameTime gameTime )
    {
      if ( UpdateSelf != null )
        UpdateSelf( gameTime );
    }

    private static void UpdateScoreCoin( GameTime gameTime )
    {

    }

    public override void Draw()
    {

    }
  }
}