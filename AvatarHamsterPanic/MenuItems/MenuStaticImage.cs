using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AvatarHamsterPanic.Objects
{
  class MenuStaticImage : MenuItem
  {
    public MenuStaticImage( MenuScreen screen, Vector2 position, string imageFile )
      : base( screen, position )
    {
    }
  }
}