using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Menu
{
  class CreditsMenuScreen : MenuScreen
  {
    float ss; // screen scale

    public CreditsMenuScreen( ScreenManager screenManager )
    {
      ScreenManager = screenManager;

      ContentManager content = screenManager.Game.Content;

      ss = screenManager.Game.GraphicsDevice.Viewport.Height / 1080f;

      float scale = ss;

      TextMenuItem item;
      Vector2 position;
      string names;

      SpriteFont font = content.Load<SpriteFont>( "Fonts/menufont" );
      Color headingColor = new Color( 255, 255, 100 );
      Color nameColor = new Color( 225, 225, 225 );

      // CREDITS title
      position = new Vector2( 80, 80 ) * ss;
      Texture2D image = content.Load<Texture2D>( "Textures/creditsTitleText" );
      StaticImageMenuItem title = new StaticImageMenuItem( this, position, image );
      title.Origin = Vector2.Zero;
      title.SetImmediateScale( scale );
      title.TransitionOnPosition = position - new Vector2( 0, 100 ) * ss;
      title.TransitionOffPosition = position - new Vector2( 0, 100 ) * ss;
      MenuItems.Add( title );

      // artwork
      position = new Vector2( 100, 220 ) * ss;
      item = new TextMenuItem( this, position, "Artwork", font );
      item.Scale = scale;
      item.Color = headingColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      position = new Vector2( 100, 260 ) * ss;
      names = "Sabrina Sullivan\nBryce Garrison\nKristine Serio";
      item = new TextMenuItem( this, position, names, font );
      item.Scale = scale;
      item.Color = nameColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      // programming
      position = new Vector2( 100, 440 ) * ss;
      item = new TextMenuItem( this, position, "Programming", font );
      item.Scale = scale;
      item.Color = headingColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      position = new Vector2( 100, 480 ) * ss;
      names = "Alex Serio";
      item = new TextMenuItem( this, position, names, font );
      item.Scale = scale;
      item.Color = nameColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      // special thanks
      position = new Vector2( 100, 600 ) * ss;
      item = new TextMenuItem( this, position, "Special Thanks", font );
      item.Scale = scale;
      item.Color = headingColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      position = new Vector2( 100, 640 ) * ss;
      names = "Jace Sangco\nPaul Flores\nRoy Flores";
      item = new TextMenuItem( this, position, names, font );
      item.Scale = scale;
      item.Color = nameColor;
      item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( item );

      //// sound effects
      //position = new Vector2( 100, 900 ) * ss;
      //string text = "Most sound effects from soundsnap.com.\nHigh Score component by Jon Watte.";
      //item = new TextMenuItem( this, position, text, font );
      //item.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      //item.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      //item.Scale = .85f * scale;
      //MenuItems.Add( item );
    }

    public override void LoadContent()
    {
      // content is loaded in ctor
    }
  }
}