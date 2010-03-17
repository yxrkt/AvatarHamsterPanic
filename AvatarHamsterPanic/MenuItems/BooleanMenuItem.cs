using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathLibrary;

namespace Menu
{
  class BooleanMenuItem : MenuItem
  {
    static readonly string yes = "Yes";
    static readonly string no = "No";

    SpriteBatch spriteBatch;
    SpriteFont font;

    SpringInterpolater scaleSpring;

    bool value;
    public bool Value
    {
      get { return value; }
      set
      {
        scaleSpring.SetSource( 1.25f );
        this.value = value;

        if ( ValueChanged != null )
          ValueChanged();
      }
    }

    public delegate void ValueChangedEvent();
    public event ValueChangedEvent ValueChanged;

    public Color Color { get; set; }

    public BooleanMenuItem( MenuScreen screen, Vector2 position, bool value )
      : base( screen, position )
    {
      this.value = value;

      spriteBatch = screen.ScreenManager.SpriteBatch;
      font = Screen.ScreenManager.Font;
      Origin = new Vector2( 0, font.LineSpacing / 2 );
      scaleSpring = new SpringInterpolater( 1, 700, .4f * SpringInterpolater.GetCriticalDamping( 700 ) );
      scaleSpring.SetSource( 1 );
      scaleSpring.SetDest( 1 );
      scaleSpring.Active = true;
      Color = Color.White;
    }

    public override void Update( GameTime gameTime )
    {
      base.Update( gameTime );

      scaleSpring.Update( (float)gameTime.ElapsedGameTime.TotalSeconds );
    }

    public override void Draw( GameTime gameTime )
    {
      Color color = Color;
      color.A = Screen.TransitionAlpha;

      spriteBatch.DrawString( font, Value ? yes : no, curPos, color, 0f, Origin, 
                              Scale * scaleSpring.GetSource()[0], SpriteEffects.None, 0 );
    }
  }
}