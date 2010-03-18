using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Graphics;
using Menu;
using MathLibrary;

namespace AvatarHamsterPanic.Objects
{
  class PlayerTag
  {
    readonly Vector2 tagOffset = new Vector2( 0, -150 );
    readonly SpriteFont font;
    readonly Color color;

    Player player;
    GraphicsDevice device;
    Camera camera;
    SpriteBatch spriteBatch;

    float nearHeightOver2;
    float nearWidthOver2;
    Vector2 screenPos;
    Vector2 origin;
    SpringInterpolater spring;
    float totalTime;

    float ss;

    public Vector2 PlayerScreenPosition { get; private set; }
    public Vector2 TagScreenPosition { get { return screenPos; } }

    public PlayerTag( Player player, SpriteFont font )
    {
      this.player = player;
      this.font = font;

      ss = GameCore.Instance.GraphicsDevice.Viewport.Height / 1080f;

      color = GameCore.Instance.PlayerColors[player.PlayerNumber];

      device = player.Screen.ScreenManager.GraphicsDevice;
      camera = player.Screen.Camera;
      spriteBatch = player.Screen.ScreenManager.SpriteBatch;

      tagOffset *= device.Viewport.Height / 1080f;

      nearHeightOver2 = camera.Near * (float)Math.Tan( camera.Fov / 2 );
      nearWidthOver2  = camera.Aspect * nearHeightOver2;

      screenPos = Vector2.Zero;
      origin = font.MeasureString( player.HUD.Name ) / 2;

      spring = new SpringInterpolater( 1, 700, .3f * SpringInterpolater.GetCriticalDamping( 700 ) );
      spring.SetSource( 0 );
      spring.SetDest( 1 );
    }

    public void Update( GameTime gameTime )
    {
      GameplayScreen screen = player.Screen;

      if ( !spring.Active && totalTime > screen.CountdownEnd + .5f )
        spring.Active = true;

      Vector3 position = new Vector3( player.BoundingCircle.Position, 0 );
      Vector4 ndc = Vector4.Transform( position, screen.View * screen.Projection );
      ndc /= ndc.W;

      screenPos.X = ( .5f * ndc.X + .5f ) * device.Viewport.Width;
      screenPos.Y = ( -.5f * ndc.Y + .5f ) * device.Viewport.Height;
      PlayerScreenPosition = screenPos;
      screenPos += tagOffset;

      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
      spring.Update( elapsed );

      totalTime += elapsed;
    }

    public void Draw()
    {
      spriteBatch.DrawString( font, player.HUD.Name, screenPos, color, 0f, origin, 
                              spring.GetSource()[0] * ss, SpriteEffects.None, 0f );
    }
  }
}