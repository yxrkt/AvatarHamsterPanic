using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Objects
{
  class MenuText : MenuItem
  {
    SpriteFont font;
    string text;

    public Color Color { get; set; }

    public MenuText( MenuScreen screen, Vector2 position, string text, SpriteFont font )
      : base( screen, position )
    {
      this.font = font;
      this.text = text;

      Color = Color.LightGray;
    }

    public override void Draw( GameTime gameTime )
    {
      SpriteBatch spriteBatch = Screen.ScreenManager.SpriteBatch;
      spriteBatch.DrawString( font, text, curPos, Color, 0f, Origin, Scale, SpriteEffects.None, Z );
    }
  }
}