using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Utilities;
using Microsoft.Xna.Framework.Content;

namespace Menu
{
  class FAQScreen : MenuScreen
  {
    readonly string faqText;

    public FAQScreen( ScreenManager screenManager )
    {
      ScreenManager = screenManager;

      IsPopup = true;
      TransitionOnTime = TimeSpan.FromSeconds( .25 );
      TransitionOffTime = TimeSpan.FromSeconds( .25 );

      faqText =
        "Q: Why don't I see any scores but mine?\n" +
        "A: It is possible no one is playing the game at the same time as you.\n" +
        "     Avatar Hamster Panic cannot use Leaderboards and must share your scores\n" +
        "     with people playing the game at the same time as you. The best scores of\n" +
        "     others are shared and saved to your hard drive automatically for you.\n\n" +

        "Q: I'm playing at the same time with my friend but I don't see his/her scores.\n" +
        "A: Be patient. Because connections are done automatically, it may take some time\n" +
        "     before you connect to your friends. It's also possible you are both having NAT\n" +
        "     trouble. Please visit http://support.microsoft.com/kb/908880 for help in\n" +
        "     resolving NAT issues.\n\n" +

        "Q: My friend's score didn't save when they signed in on a local profile.\n" +
        "A: Only Xbox LIVE members can save and share scores.";

      float ss = GameCore.Instance.GraphicsDevice.Viewport.Height / 1080f;

      ContentManager content = GameCore.Instance.Content;
      SpriteFont font = content.Load<SpriteFont>( "Fonts/FAQFont" );

      Viewport viewport = GameCore.Instance.GraphicsDevice.Viewport;
      Vector2 center = new Vector2( viewport.Width / 2, viewport.Height / 2 - 30 * ss );
      TextMenuItem item = new TextMenuItem( this, center, faqText, font );
      item.TransitionOffPosition = center;
      item.TransitionOnPosition = center;
      item.Centered = true;
      item.Scale = ss;
      MenuItems.Add( item );

      Rectangle safeRect = ScreenRects.SafeRegion;
      Vector2 backPos = new Vector2( safeRect.Left, safeRect.Bottom );
      backPos.X = ( center.X - ss * item.Dimensions.X / 2 );
      Texture2D backText = content.Load<Texture2D>( "Textures/bBackText" );
      StaticImageMenuItem bBack = new StaticImageMenuItem( this, backPos, backText );
      bBack.Origin.X = 0;
      bBack.SetImmediateScale( .5f * ss );
      MenuItems.Add( bBack );
    }

    public override void Draw( GameTime gameTime )
    {
      ScreenManager.FadeBackBufferToBlack( TransitionAlpha );
      base.Draw( gameTime );
    }
  }
}