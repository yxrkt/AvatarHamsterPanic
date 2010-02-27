using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathLibrary;

namespace Menu
{
  class StaticImageMenuItem : MenuItem
  {
    Texture2D texture;
    SpriteBatch spriteBatch;
    SpringInterpolater scaleSpring;

    public Color Tint { get; set; }
    public bool Fade { get; set; }
    public Texture2D Texture
    {
      get { return texture; }
      set { texture = value; }
    }
    public new float Scale
    {
      get { return scaleSpring.GetDest()[0]; }
      set { scaleSpring.SetDest( value ); }
    }

    public StaticImageMenuItem( MenuScreen screen, Vector2 position, Texture2D texture )
      : base( screen, position )
    {
      this.texture = texture;
      TransitionOffPosition = position;
      TransitionOnPosition = position;
      spriteBatch = Screen.ScreenManager.SpriteBatch;
      Dimensions = new Vector2( texture.Width, texture.Height );
      Tint = Color.White;
      Fade = true;
      scaleSpring = new SpringInterpolater( 1, 700, .35f * SpringInterpolater.GetCriticalDamping( 700 ) );
      scaleSpring.SetSource( 1 );
      scaleSpring.SetDest( 1 );
      scaleSpring.Active = true;
    }

    public void Flick( float amount )
    {
      scaleSpring.SetSource( scaleSpring.GetDest()[0] * ( 1 + amount ) );
    }

    public void SetImmediateScale( float scale )
    {
      scaleSpring.SetSource( scale );
      scaleSpring.SetDest( scale );
    }

    public override void Update( GameTime gameTime )
    {
      scaleSpring.Update( (float)gameTime.ElapsedGameTime.TotalSeconds );
      if ( scaleSpring.GetSource()[0] < 0 )
        scaleSpring.SetSource( 0 );

      base.Update( gameTime );
    }

    public override void Draw( GameTime gameTime )
    {
      Color color = Tint;
      if ( Fade )
        color.A = Screen.TransitionAlpha;

      spriteBatch.Draw( texture, curPos, null, color, 0f, Dimensions / 2, 
                        scaleSpring.GetSource()[0], SpriteEffects.None, 0 );
    }
  }
}