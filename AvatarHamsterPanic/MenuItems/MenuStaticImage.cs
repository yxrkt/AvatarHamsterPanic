using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Menu
{
  class MenuStaticImage : MenuItem
  {
    Texture2D texture;
    SpriteBatch spriteBatch;
    float scale;

    public new float Scale { get { return scale; } set { scale = value; Dimensions *= scale; } }
    public Color Tint { get; set; }
    public bool Fade { get; set; }

    public MenuStaticImage( MenuScreen screen, Vector2 position, Texture2D texture )
      : base( screen, position )
    {
      this.texture = texture;
      spriteBatch = Screen.ScreenManager.SpriteBatch;
      scale = 1;
      Dimensions = new Vector2( texture.Width, texture.Height );
      Tint = Color.White;
      Fade = true;
    }

    public override void Draw( GameTime gameTime )
    {
      Color color = Tint;
      if ( Fade )
        color.A = Screen.TransitionAlpha;

      spriteBatch.Draw( texture, curPos, null, color, 0f, Dimensions / 2, scale, SpriteEffects.None, 0 );
    }
  }
}