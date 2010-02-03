using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CustomModelSample;
using Physics;
using Utilities;

namespace AvatarHamsterPanic.Objects
{
  class LaserBeam : GameObject
  {
    float startVelocity;
    float endVelocity;
    float duration;
    float age;

    CustomModel model;
    PhysPolygon body;

    static readonly Pool<LaserBeam> _pool = new Pool<LaserBeam>( 6, b => b.age < b.duration );

    private LaserBeam( GameplayScreen screen )
      : base( screen )
    {
      model = screen.Content.Load<CustomModel>( "Models/cylinder" );
    }

    public override void Update( GameTime gameTime )
    {

    }

    public override void Draw()
    {

    }
  }
}