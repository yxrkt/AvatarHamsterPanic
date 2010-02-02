using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Objects
{
  class ReadyGo : GameObject
  {
    readonly string readyString = "Ready";
    readonly string goString = "Go!";
    readonly SpringInterpolater spring =
      new SpringInterpolater( 1, 200, .5f * SpringInterpolater.GetCriticalDamping( 200 ) );
    readonly SpriteFont font;
    readonly Vector2 position;

    Vector2 origin;
    string currentString;
    float postCountdownTime = 0;

    public ReadyGo( GameplayScreen screen, Vector2 position )
      : base( screen )
    {
      this.position = position;

      currentString = readyString;

      font = Screen.Content.Load<SpriteFont>( "Fonts/readyGoFont" );
      origin = font.MeasureString( readyString ) / 2;

      spring.SetSource( 0 );
      spring.SetDest( 1 );
    }

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

      spring.Update( elapsed );

      if ( Screen.CountdownTime > 1f && Screen.CountdownTime < Screen.CountdownEnd )
      {
        spring.Active = true;
      }
      else if ( Screen.CountdownTime >= Screen.CountdownEnd )
      {
        // switch to "Go!"
        if ( currentString == readyString )
        {
          currentString = goString;
          origin = font.MeasureString( goString ) / 2;
          spring.SetSource( 1.5f );
        }

        // scale down "Go!" here
        if ( spring.GetDest()[0] == 1f && postCountdownTime > 1 )
        {
          spring.SetSource( 1.15f );
          spring.SetDest( 0 );
        }

        // kill self here
        if ( spring.GetSource()[0] < .05f )
          Screen.ObjectTable.MoveToTrash( this );

        postCountdownTime += elapsed;
      }
    }

    public override void Draw()
    {
      // drawing called manually in Draw2D
    }

    public void Draw2D()
    {
      SpriteBatch spriteBatch = Screen.ScreenManager.SpriteBatch;

      spriteBatch.DrawString( font, currentString, position, Color.Black, 0f,
                              origin, spring.GetSource()[0], SpriteEffects.None, 0 );
    }
  }
}