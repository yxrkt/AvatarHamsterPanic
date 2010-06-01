using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Menu
{
  class ControlsMenuScreen : MenuScreen
  {
    public ControlsMenuScreen( ScreenManager screenManager )
    {
      ScreenManager = screenManager;

      ContentManager content = GameCore.Instance.Content;
      Texture2D background = content.Load<Texture2D>( "Textures/controls" );
      StaticImageMenuItem item = new StaticImageMenuItem( this, Vector2.Zero, background );

      Viewport viewport = GameCore.Instance.GraphicsDevice.Viewport;
      if ( viewport.AspectRatio < 16f / 9f )
      {
        item.SetImmediateScale( (float)viewport.Height / (float)background.Height );
        item.Origin.Y = 0;
        item.Origin.X = ( background.Width - background.Height * viewport.AspectRatio ) / 2;
      }
      else
      {
        item.SetImmediateScale( (float)viewport.Width / (float)background.Width );
        item.Origin.X = 0;
        item.Origin.Y = ( background.Height - background.Width / viewport.AspectRatio ) / 2;
      }

      MenuItems.Add( item );
    }

    public override void LoadContent()
    {
      // content loaded in ctor to prevent spiking
    }
  }
}