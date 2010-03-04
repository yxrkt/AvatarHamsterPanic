using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Menu;
using MathLibrary;
using Microsoft.Xna.Framework.Content;

namespace AvatarHamsterPanic.Objects
{
  class ReadyGo : GameObject
  {
    readonly Texture2D readyTexture;
    readonly Texture2D goTexture;
    readonly SpringInterpolater spring =
      new SpringInterpolater( 1, 250, .5f * SpringInterpolater.GetCriticalDamping( 250 ) );
    readonly Vector2 position;
    readonly float screenScale;

    Texture2D currentTexture;
    float postCountdownTime = 0;

    public ReadyGo( GameplayScreen screen, Vector2 position )
      : base( screen )
    {
      this.position = position;

      ContentManager content = Screen.Content;

      readyTexture = content.Load<Texture2D>( "Textures/bigReadyText" );
      goTexture = content.Load<Texture2D>( "Textures/goText" );

      currentTexture = readyTexture;

      spring.SetSource( 0 );
      spring.SetDest( 1 );

      screenScale = Screen.ScreenManager.GraphicsDevice.Viewport.Height / 1080f;
    }

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

      if ( elapsed < 1f / 30f )
        elapsed = 1f / 30f;
      spring.Update( elapsed );

      if ( Screen.CountdownTime > 1f && Screen.CountdownTime < Screen.CountdownEnd )
      {
        spring.Active = true;
      }
      else if ( Screen.CountdownTime >= Screen.CountdownEnd )
      {
        // switch to "Go!"
        if ( currentTexture == readyTexture )
        {
          currentTexture = goTexture;
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
      Vector2 origin = new Vector2( currentTexture.Width, currentTexture.Height ) / 2;
      spriteBatch.Draw( currentTexture, position, null, Color.White, 0f, origin,
                        spring.GetSource()[0] * screenScale, SpriteEffects.None, 0 );
    }
  }
}