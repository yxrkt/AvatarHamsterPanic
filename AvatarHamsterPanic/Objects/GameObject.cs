using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Menu;

namespace AvatarHamsterPanic.Objects
{
  public abstract class GameObject
  {
    public GameplayScreen Screen { get; protected set; }
    public int DrawOrder { get; set; }

    public GameObject( GameplayScreen screen )
    {
      Screen = screen;
    }

    public abstract void Update( GameTime gameTime );
    public abstract void Draw();
  }
}