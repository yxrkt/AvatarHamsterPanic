#region Using Statements

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

#endregion

namespace Debug
{
  public class DebugManager : DrawableGameComponent
  {
    #region Properties

    public ContentManager Content { get; private set; }

    public SpriteBatch SpriteBatch { get; private set; }

    public Texture2D WhiteTexture { get; private set; }

    public SpriteFont DebugFont { get; private set; }

    public SpriteFont PresentFont { get; private set; }

    #endregion

    #region Initialization

    public DebugManager( Game game )
      : base( game )
    {
      Game.Services.AddService( typeof( DebugManager ), this );

      Content = new ContentManager( game.Services );
      Content.RootDirectory = "Content/Debug";

      this.Enabled = false;
      this.Visible = false;
    }

    protected override void LoadContent()
    {
      SpriteBatch = new SpriteBatch( GraphicsDevice );

      WhiteTexture = Content.Load<Texture2D>( "WhiteTexture" );
      DebugFont = Content.Load<SpriteFont>( "DebugFont" );
      PresentFont = Content.Load<SpriteFont>( "PresentFont" );

      base.LoadContent();
    }

    #endregion
  }
}