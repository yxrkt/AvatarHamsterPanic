using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Menu
{
  class SliderMenuItem : MenuItem
  {
    float value;
    public float Value
    {
      get { return value; }
      set
      {
        float lastValue = this.value;
        float newValue = MathHelper.Clamp( value, 0, 1 );
        if ( newValue != lastValue )
        {
          sliderPosition.X = newValue * barTexture.Width * ss * Scale;
          this.value = newValue;
          if ( ValueChanged != null )
            ValueChanged();
        }
      }
    }

    float ss; // screen scale
    Vector2 sliderPosition;
    Vector2 sliderOrigin;
    SpriteBatch spriteBatch;
    Texture2D barTexture;
    Texture2D sliderTexture;

    public delegate void ValueChangeEvent();
    public event ValueChangeEvent ValueChanged;

    public SliderMenuItem( MenuScreen screen, Vector2 position, float value )
      : base( screen, position )
    {
      spriteBatch = screen.ScreenManager.SpriteBatch;

      ss = screen.ScreenManager.Game.GraphicsDevice.Viewport.Height / 1080f;

      ContentManager content = screen.ScreenManager.Game.Content;

      barTexture = content.Load<Texture2D>( "Textures/sliderBar" );
      sliderTexture = content.Load<Texture2D>( "Textures/sliderSlider" );

      Origin = new Vector2( 0, barTexture.Height / 2 );
      sliderOrigin = new Vector2( sliderTexture.Width, sliderTexture.Height ) / 2;

      Value = value;
    }

    public override void Update( GameTime gameTime )
    {
      base.Update( gameTime );
    }

    public override void Draw( GameTime gameTime )
    {
      Color color = new Color( Color.White, Screen.TransitionAlpha );

      //draw bar texture
      spriteBatch.Draw( barTexture, curPos, null, color, 0f, 
                        Origin, ss * Scale, SpriteEffects.None, 0f );

      //draw slider texture
      spriteBatch.Draw( sliderTexture, curPos + sliderPosition, null, color, 
                        0f, sliderOrigin, ss * Scale, SpriteEffects.None, 0f );
    }
  }
}