using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using GameStateManagement;

namespace GameObjects
{
  class TestObject : GameObject
  {
    public PhysPolygon Bound { get; private set; }

    public TestObject( GameplayScreen screen, Vector2 position )
      : base( screen )
    {
      Vector2[] hexVerts = new Vector2[6];
      hexVerts[0] = new Vector2( 1f, .5f );
      hexVerts[1] = new Vector2( 0f, 1f );
      hexVerts[2] = new Vector2( -1f, .5f );
      hexVerts[3] = new Vector2( -1f, -.5f );
      hexVerts[4] = new Vector2( 0f, -1f );
      hexVerts[5] = new Vector2( 1f, -.5f );

      Bound = new PhysPolygon( hexVerts, position, 1f );
      Bound.Flags = PhysBodyFlags.Anchored;
    }

    public override void Update( GameTime gameTime )
    {
      
    }

    public override void Draw()
    {
      
    }
  }
}