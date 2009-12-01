using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameObjects
{
  public abstract class GameObject
  {
    public GameplayScreen Screen { get; private set; }

    public GameObject( GameplayScreen screen )
    {
      Screen = screen;
    }

    public abstract void Update( GameTime gameTime );
    public abstract void Draw();
  }
}