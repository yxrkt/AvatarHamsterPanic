using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Screens
{
  class ControlsMenuScreen : MenuScreen
  {
    public ControlsMenuScreen()
    {
      TransitionOnTime = TimeSpan.FromSeconds( 0e );
    }

    public override void LoadContent()
    {
      IsPopup = true;
    }

    public override void Draw( GameTime gameTime )
    {
      SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

      spriteBatch.Begin();
      spriteBatch.End();
    }
  }
}