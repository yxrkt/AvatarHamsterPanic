using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Menu
{
  class ScoreboardPopupMenuScreen : MenuScreen
  {
    const float entryIdleScale  = .75f;
    const float entryFocusScale = 1.35f;

    Slot[] slots;
    bool fadeBackBuffer;

    public Slot[] Slots { get { return slots; } }

    public bool GoingBackToScoreboard { get { return ScreenState == ScreenState.TransitionOff && !fadeBackBuffer; } }

    public ScoreboardPopupMenuScreen( ScreenManager screenManager, Slot[] slots )
    {
      ScreenManager = screenManager;

      this.slots = slots;
      IsPopup = true;

      ContentManager content = screenManager.Game.Content;
      GraphicsDevice device = screenManager.GraphicsDevice;

      ImageMenuEntry entry;

      float x = device.Viewport.Width / 2;
      float yStart = .8f * device.Viewport.Height;
      float ySpace = .06f * device.Viewport.Height;
      float y = yStart;

      float screenScale = (float)device.Viewport.Height / 1080f;

      // Play Again
      entry = new ImageMenuEntry( this, new Vector2( x, y ), content.Load<Texture2D>( "Textures/playAgainText" ), null );
      entry.Selected += PlayAgain;
      entry.TransitionOnPosition = entry.TransitionOffPosition = entry.Position + new Vector2( 0, ySpace * 5 );
      entry.IdleScale = entryIdleScale * screenScale;
      entry.FocusScale = entryFocusScale * screenScale;
      entry.Focused = true;
      MenuItems.Add( entry );

      // High Scores
      y += ySpace;
      entry = new ImageMenuEntry( this, new Vector2( x, y ), content.Load<Texture2D>( "Textures/highScoresText" ), null );
      entry.Selected += ViewHighScores;
      entry.TransitionOnPosition = entry.TransitionOffPosition = entry.Position + new Vector2( 0, ySpace * 5 );
      entry.IdleScale = entryIdleScale * screenScale;
      entry.FocusScale = entryFocusScale * screenScale;
      MenuItems.Add( entry );

      // Exit
      y += ySpace;
      entry = new ImageMenuEntry( this, new Vector2( x, y ), content.Load<Texture2D>( "Textures/exitText" ), null );
      entry.Selected += Exit;
      entry.TransitionOnPosition = entry.TransitionOffPosition = entry.Position + new Vector2( 0, ySpace * 5 );
      entry.IdleScale = entryIdleScale * screenScale;
      entry.FocusScale = entryFocusScale * screenScale;
      MenuItems.Add( entry );
    }

    public void SetPlayerID( int slot, uint id )
    {
      slots[slot].ID = id;
    }

    void PlayAgain( object sender, PlayerIndexEventArgs e )
    {
      LoadingScreen.Load( ScreenManager, true, null, new GameplayScreen( slots ) );
    }

    void ViewHighScores( object sender, PlayerIndexEventArgs e )
    {
      ScreenManager.AddScreen( new LeaderboardMenuScreen(), null );
      fadeBackBuffer = true;
    }

    void Exit( object sender, PlayerIndexEventArgs e )
    {
      LoadingScreen.Load( ScreenManager, false, null, new BackgroundScreen(),
                                                      new MainMenuScreen() );
      fadeBackBuffer = true;
    }

    protected override void OnCancel( PlayerIndex playerIndex )
    {
      fadeBackBuffer = false;
      base.OnCancel( playerIndex );
    }

    public override void LoadContent()
    {
      // content loaded in ctor
      fadeBackBuffer = false;

      base.LoadContent();
    }

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );
    }

    public override void Draw( GameTime gameTime )
    {
      base.Draw( gameTime );

      if ( TransitionPosition != 0 && fadeBackBuffer )
        ScreenManager.FadeBackBufferToBlack( 255 - TransitionAlpha );
    }
  }
}