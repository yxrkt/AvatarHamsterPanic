using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Utilities;
using MathLibrary;

namespace AvatarHamsterPanic.Objects
{
  class PopupText
  {
    int number;
    Vector2 showPos, hidePos;
    float size;
    float time, timeout;
    StringBuilder text;

    public bool Active { get; private set; }
    public SpringInterpolater PositionSpring { get; private set; }
    public SpringInterpolater ScaleSpring { get; private set; }
    public int Points { get { return number; } }

    public PopupText( float size, Vector2 showPos, Vector2 hidePos, float timeout )
    {
      this.size = size;
      this.showPos = showPos;
      this.hidePos = hidePos;
      this.timeout = timeout;
      this.text = new StringBuilder( 4 );

      PositionSpring = new SpringInterpolater( 2, 300f, SpringInterpolater.GetCriticalDamping( 300f ) );
      ScaleSpring = new SpringInterpolater( 1, 700f, .25f * SpringInterpolater.GetCriticalDamping( 700f ) );
      ResetSprings();
    }

    public int Update( float elapsed )
    {
      if ( !Active ) return 0;

      PositionSpring.Update( elapsed );
      ScaleSpring.Update( elapsed );

      // hide number
      if ( time > timeout )
      {
        if ( ScaleSpring.GetDest()[0] != 0f )
        {
          ScaleSpring.B = SpringInterpolater.GetCriticalDamping( ScaleSpring.K );
          ScaleSpring.SetDest( 0f );
          PositionSpring.SetDest( hidePos );
        }

        if ( ScaleSpring.GetSource()[0] < .05f )
        {
          Active = false;
          int temp = number;
          number = 0;
          return temp;
        }
      }

      time += elapsed;

      return 0;
    }

    public void Add( int n )
    {
      if ( !Active )
      {
        Active = true;
        ResetSprings();
        number = n;
      }
      else
      {
        number += n;
        if ( time > timeout )
        {
          ScaleSpring.SetDest( size );
          ScaleSpring.B = .25f * SpringInterpolater.GetCriticalDamping( ScaleSpring.K );
          PositionSpring.SetDest( showPos );
        }
        else
        {
          ScaleSpring.SetSource( 1.3f * ScaleSpring.GetSource()[0] );
        }
      }

      time = 0f;
    }

    public void Draw( SpriteBatch spriteBatch, SpriteFont font, Color color )
    {
      Draw( spriteBatch, font, color, Vector2.Zero );
    }

    public void Draw( SpriteBatch spriteBatch, SpriteFont font, Color color, Vector2 origin )
    {
      text.Remove( 0, text.Length );
      if ( number >= 0 )
        text.Append( '+' );
      text.AppendInt( number );
      Vector2 fontOrigin = font.MeasureString( text ) / 2f;
      float[] source = PositionSpring.GetSource();
      Vector2 position = new Vector2( source[0], source[1] );
      spriteBatch.DrawString( font, text, position + origin, color, 0f, fontOrigin, 
                              ScaleSpring.GetSource()[0], SpriteEffects.None, 0f );
    }

    private void ResetSprings()
    {
      //PositionSpring.SetSource( showPos );
      PositionSpring.SetDest( showPos );
      ScaleSpring.SetSource( 0f );
      ScaleSpring.SetDest( size );
      ScaleSpring.B = .25f * SpringInterpolater.GetCriticalDamping( ScaleSpring.K );
      ScaleSpring.Active = true;
      PositionSpring.Active = true;
    }
  }
}