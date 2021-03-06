using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Menu
{
  class TextMenuItem : MenuItem
  {
    SpriteFont font;
    string text;
    float deathAlpha;

    public Color Color { get; set; }
    public bool Centered { get; set; }
    public bool Fade { get; set; }
    public string Text { get { return text; } set { text = value; } }
    public float MaxWidth { get; set; }
    public float DeathBegin { get; set; }
    public new Vector2 Dimensions
    {
      get
      {
        if ( text == null )
          return Vector2.Zero;
        return font.MeasureString( text );
      }
    }

    public TextMenuItem( MenuScreen screen, Vector2 position, string text, SpriteFont font )
      : base( screen, position )
    {
      this.font = font;
      this.text = text;

      Color = Color.LightGray;
      Fade = true;
      MaxWidth = float.MaxValue;
      DeathBegin = 0;
    }

    public override void Update( GameTime gameTime )
    {
      if ( DeathBegin != 0 )
      {
        float t = 1f - ( (float)gameTime.TotalGameTime.TotalSeconds - DeathBegin );
        if ( t < 0 )
          deathAlpha = 0;
        else
          deathAlpha = t;
      }
    }

    public override void Draw( GameTime gameTime )
    {
      if ( text != null )
      {
        SpriteBatch spriteBatch = Screen.ScreenManager.SpriteBatch;
        Vector2 origin = Vector2.Zero;
        Vector2 size = font.MeasureString( text );
        if ( Centered )
          origin = size / 2;
        Color color = Color;
        if ( Fade )
          color.A = Screen.TransitionAlpha;
        if ( DeathBegin != 0 )
          color.A = (byte)( color.A * deathAlpha );
        Vector2 scale = new Vector2( Scale, Scale );
        if ( size.X * Scale > MaxWidth )
          scale.X = MaxWidth / size.X;
        spriteBatch.DrawString( font, text, curPos, color, 0f, origin, scale, SpriteEffects.None, Z );
      }
    }
  }
}