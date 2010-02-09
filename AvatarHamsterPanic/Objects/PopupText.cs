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

    public PopupText( float size, Vector2 showPos, Vector2 hidePos, float timeout )
    {
      this.size = size;
      this.showPos = showPos;
      this.hidePos = hidePos;
      this.timeout = timeout;
      this.text = new StringBuilder( 4 );

      PositionSpring = new SpringInterpolater( 2, 600f, SpringInterpolater.GetCriticalDamping( 600f ) );
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
          PositionSpring.Active = true;
        }

        if ( ScaleSpring.GetSource()[0] < .05f )
        {
          Active = false;
          return number;
        }
      }

      time += elapsed;

      return 0;
    }

    public void Add( int n )
    {
      time = 0f;

      if ( !Active )
      {
        Active = true;
        ResetSprings();
        number = n;
      }
      else
      {
        number += n;
        if ( ScaleSpring.GetDest()[0] != size )
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
    }

    public void Draw( SpriteBatch spriteBatch, SpriteFont font, Color color )
    {
      text.Remove( 0, text.Length );
      if ( number >= 0 )
        text.Append( '+' );
      text.AppendInt( number );
      Vector2 origin = font.MeasureString( text ) / 2f;
      float[] source = PositionSpring.GetSource();
      Vector2 position = new Vector2( source[0], source[1] );
      spriteBatch.DrawString( font, text, position, color, 0f, origin, ScaleSpring.GetSource()[0], SpriteEffects.None, 0f );
    }

    private void ResetSprings()
    {
      PositionSpring.SetSource( showPos );
      PositionSpring.SetDest( hidePos );
      ScaleSpring.SetSource( 0f );
      ScaleSpring.SetDest( size );
      ScaleSpring.B = .25f * SpringInterpolater.GetCriticalDamping( ScaleSpring.K );
      ScaleSpring.Active = true;
      PositionSpring.Active = false;
    }
  }
}