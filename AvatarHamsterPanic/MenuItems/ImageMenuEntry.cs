using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MathLibrary;

namespace Menu
{
  class ImageMenuEntry : MenuItem
  {
    #region Fields

    Texture2D texture;
    SpringInterpolater scaleSpring;
    SpriteBatch spriteBatch;
    float idleScale = 1;
    float focusScale = 1.5f;

    #endregion

    #region Properties

    public Color Tint { get; set; }
    public bool Fade { get; set; }
    public Texture2D Texture
    {
      get { return texture; }
      set { texture = value; }
    }
    public float IdleScale { get { return idleScale; } set { idleScale = value; } }
    public float FocusScale { get { return focusScale; } set { focusScale = value; } }
    public SpringInterpolater ScaleSpring { get { return scaleSpring; } }

    #endregion

    #region Initialization

    public ImageMenuEntry( MenuScreen screen, Vector2 position, Texture2D texture, Vector2? dimensions )
      : base( screen, position )
    {
      this.texture = texture;

      if ( dimensions != null )
        Dimensions = (Vector2)dimensions;
      else
        Dimensions = new Vector2( texture.Width, texture.Height );

      scaleSpring = new SpringInterpolater( 1, 700f, .35f * SpringInterpolater.GetCriticalDamping( 700f ) );
      scaleSpring.SetSource( idleScale );
      scaleSpring.SetDest( idleScale );
      scaleSpring.Active = true;

      spriteBatch = Screen.ScreenManager.SpriteBatch;

      Tint = Color.White;

      Fade = true;
    }

    #endregion

    #region Update and Draw


    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      if ( elapsed > 1f / 30f )
        elapsed = 1f / 30f;
      scaleSpring.Update( elapsed );

      base.Update( gameTime );
    }

    public override void Draw( GameTime gameTime )
    {
      // Scale up the selected entry.
      if ( Focused )
        scaleSpring.SetDest( focusScale );
      else
        scaleSpring.SetDest( idleScale );

      // Pulsate the size of the selected menu entry.
      float scale = scaleSpring.GetSource()[0];

      Color color = Tint;
      if ( Fade )
        color.A = Screen.TransitionAlpha;

      // Draw image
      spriteBatch.Draw( texture, curPos, null, color, 0f, Dimensions / 2, scale, SpriteEffects.None, 0 );
    }

    public override void UpdateTransition( float transitionPosition, ScreenState state )
    {
      base.UpdateTransition( transitionPosition, state );
    }

    #endregion
  }
}