#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using AvatarHamsterPanic.Utilities;
using AvatarHamsterPanic;
#endregion

namespace Menu
{
  /// <summary>
  /// The options screen is brought up over the top of the main menu
  /// screen, and gives the user a chance to configure the game
  /// in various hopefully useful ways.
  /// </summary>
  class OptionsMenuScreen : MenuScreen
  {
    #region Fields

    float ss; // screen scale

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public OptionsMenuScreen( ScreenManager screenManager )
    {
      ScreenManager = screenManager;

      // Create our menu entries.
      Rectangle screenRect = ScreenRects.FourByThree;
      Vector2 entryPosition = new Vector2( screenRect.Center.X, screenRect.Center.Y );
      Vector2 entryStep = new Vector2( 0, 50 );

      ss = ScreenManager.Game.GraphicsDevice.Viewport.Height / 1080f;

      ContentManager content = ScreenManager.Game.Content;

      Vector2 position;

      // OPTIONS title
      Texture2D titleTexture = content.Load<Texture2D>( "Textures/optionsTitleText" );
      position = new Vector2( 80, 80 ) * ss;
      StaticImageMenuItem title = new StaticImageMenuItem( this, position, titleTexture );
      title.Scale = ss;
      title.Origin = Vector2.Zero;
      title.TransitionOnPosition = position - new Vector2( 0, 200 ) * ss;
      title.TransitionOffPosition = position - new Vector2( 0, 200 ) * ss;
      MenuItems.Add( title );


      MenuEntry entry;

      position = new Vector2( 100, 260 ) * ss;
      float entrySpacing = 50 * ss;
      float leftEdge = position.X + 520 * ss;

      // Sound Effects Volume: [------]
      entry = new MenuEntry( this, position, "Sound Effects Volume: " );
      entry.Focused = true;
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( entry );

      float soundVolume = GameCore.Instance.SoundEffectsVolume;
      Vector2 sliderPos = new Vector2( leftEdge, position.Y );
      SliderMenuItem soundSlider = new SliderMenuItem( this, sliderPos, soundVolume );
      soundSlider.TransitionOnPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      soundSlider.TransitionOffPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      soundSlider.ValueChanged += ( () =>
      {
        GameCore.Instance.SoundEffectsVolume = soundSlider.Value;
        GameCore.Instance.AudioManager.Play2DCue( "laserShot", 1f );
        //GameCore.Instance.AudioManager.Play2DCue( "plasticHit", 1f );
      } );
      MenuItems.Add( soundSlider );

      entry.Decremented += ( ( o, args ) => soundSlider.Value -= .1f );
      entry.Incremented += ( ( o, args ) => soundSlider.Value += .1f );

      position.Y += entrySpacing;

      // Music Volume: [------]
      entry = new MenuEntry( this, position, "Music Volume: " );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( entry );

      float musicVolume = GameCore.Instance.MusicVolume;
      sliderPos = new Vector2( leftEdge, position.Y );
      SliderMenuItem musicSlider = new SliderMenuItem( this, sliderPos, musicVolume );
      musicSlider.TransitionOnPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      musicSlider.TransitionOffPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      musicSlider.ValueChanged += ( () =>
      {
        GameCore.Instance.MusicVolume = musicSlider.Value;
        GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );
      } );
      MenuItems.Add( musicSlider );

      entry.Decremented += ( ( o, args ) => musicSlider.Value -= .1f );
      entry.Incremented += ( ( o, args ) => musicSlider.Value += .1f );

      position.Y += entrySpacing;

      // Display Gamertags: YES/NO
      entry = new MenuEntry( this, position, "Display Gamertags: " );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 );
      entry.TransitionOffPosition = position - new Vector2( 200, 0 );
      MenuItems.Add( entry );

      bool displayTags = GameCore.Instance.DisplayGamertags;
      Vector2 tagBoolPos = new Vector2( leftEdge, position.Y );
      BooleanMenuItem tagBool = new BooleanMenuItem( this, tagBoolPos, displayTags );
      tagBool.TransitionOnPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      tagBool.TransitionOffPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      tagBool.ValueChanged += ( () =>
      {
        GameCore.Instance.DisplayGamertags = tagBool.Value;
        GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );
      } );
      MenuItems.Add( tagBool );

      EventHandler<PlayerIndexEventArgs> toggleTags = ( ( o, args ) =>
      {
        tagBool.Value = !tagBool.Value;
      } );

      entry.Selected += toggleTags;
      entry.Incremented += toggleTags;
      entry.Decremented += toggleTags;

      position.Y += entrySpacing;

      // Enable Controller Rumble: YES/NO
      entry = new MenuEntry( this, position, "Enable Controller Rumble: " );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 );
      entry.TransitionOffPosition = position - new Vector2( 200, 0 );
      MenuItems.Add( entry );

      bool enableRumble = GameCore.Instance.Rumble.Enabled;
      Vector2 rumbleBoolPos = new Vector2( leftEdge, position.Y );
      BooleanMenuItem rumbleBool = new BooleanMenuItem( this, rumbleBoolPos, enableRumble );
      rumbleBool.TransitionOnPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      rumbleBool.TransitionOffPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      rumbleBool.ValueChanged += ( () =>
      {
        GameCore.Instance.Rumble.Enabled = rumbleBool.Value;
        GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );
      } );
      MenuItems.Add( rumbleBool );

      EventHandler<PlayerIndexEventArgs> toggleRumble = ( ( o, args ) =>
      {
        rumbleBool.Value = !rumbleBool.Value;
      } );

      entry.Selected += toggleRumble;
      entry.Incremented += toggleRumble;
      entry.Decremented += toggleRumble;

      position.Y += entrySpacing;

      // Share Highscores
      entry = new MenuEntry( this, position, "Share High Scores" );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( entry );

      bool shareScores = GameCore.Instance.ShareHighScores;
      Vector2 shareBoolPos = new Vector2( leftEdge, position.Y );
      BooleanMenuItem shareBool = new BooleanMenuItem( this, shareBoolPos, shareScores );
      shareBool.TransitionOnPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      shareBool.TransitionOffPosition = position - new Vector2( 200 - leftEdge, 0 ) * ss;
      shareBool.ValueChanged += ( () =>
      {
        GameCore.Instance.ShareHighScores = shareBool.Value;
        GameCore.Instance.AudioManager.Play2DCue( "selectItem", 1f );
      } );
      MenuItems.Add( shareBool );

      EventHandler<PlayerIndexEventArgs> toggleShare = ( ( o, args ) =>
      {
        shareBool.Value = !shareBool.Value;
      } );

      entry.Selected += toggleShare;
      entry.Incremented += toggleShare;
      entry.Decremented += toggleShare;

      position.Y += entrySpacing;

      // Clear Highscores
      entry = new MenuEntry( this, position, "Clear High Scores" );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( entry );

      entry.Selected += ( ( o, args ) =>
      {
        HighscoreComponent.Global.ClearHighscores();
      } );

      position.Y += entrySpacing;

      // Restore Defaults
      entry = new MenuEntry( this, position, "Restore Defaults" );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      MenuItems.Add( entry );
      position.Y += entrySpacing;

      entry.Selected += ( ( o, args ) =>
      {
        soundSlider.Value = 1f;
        musicSlider.Value = 1f;
        tagBool.Value = true;
        rumbleBool.Value = true;
        shareBool.Value = true;
      } );

      // Back
      entry = new MenuEntry( this, position, "Back" );
      entry.TransitionOnPosition = position - new Vector2( 200, 0 ) * ss;
      entry.TransitionOffPosition = position - new Vector2( 200, 0 ) * ss;
      entry.Selected += OnCancel;
      MenuItems.Add( entry );
    }

    public override void LoadContent()
    {
      // content loaded in ctor
    }


    #endregion

    #region Handle Input

    #endregion
  }
}
