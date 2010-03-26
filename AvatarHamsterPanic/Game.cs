#region File Description
//-----------------------------------------------------------------------------
// Game.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Storage;
using Menu;
using System.Collections.Generic;
using Debug;
using Audio;
using CustomModelSample;
using CustomAvatarAnimationFramework;
using AvatarHamsterPanic.Utilities;
#endregion

namespace AvatarHamsterPanic
{
  public class GameCore : Game
  {
    #region Fields and Properties

    GraphicsDeviceManager graphics;
    ScreenManager screenManager;

    public Dictionary<uint, int> PlayerWins { get; private set; }
    public Color[] PlayerColors { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public bool DisplayGamertags { get; set; }
    public bool ShareHighScores
    {
      get { return HighscoreComponent.Global.Enabled; }
      set { HighscoreComponent.Global.Enabled = value; }
    }
    public float SoundEffectsVolume { get; set; }

    public delegate void ChangeVolumeEvent( float prev, float cur );
    public event ChangeVolumeEvent MusicVolumeChanged;
    float musicVolume = 1f;
    public float MusicVolume
    {
      get { return musicVolume; }
      set
      {
        if ( MusicVolumeChanged != null )
          MusicVolumeChanged( musicVolume, value );
        musicVolume = value;
      }
    }

    public RumbleComponent Rumble { get; private set; }

    public DebugManager DebugManager { get; private set; }
    public DebugCommandUI DebugCommand { get; private set; }
    public FpsCounter FpsCounter { get; private set; }
    public TimeRuler TimeRuler { get; private set; }

    public static GameCore Instance { get; private set; }

    public int Counter = 0;

    #endregion

    #region Initialization

    public GameCore()
    {
      Content.RootDirectory = "Content";

      graphics = new GraphicsDeviceManager( this );

      PlayerWins = new Dictionary<uint, int>( 4 );
      PlayerColors = new Color[4]
      {
        new Color( 10,  100, 220 ),
        new Color( 200,  31,   7 ),
        new Color( 240, 180,   0 ),
        new Color( 80,  200,  10 ),
      };

      graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

      IsFixedTimeStep = false;
      graphics.SynchronizeWithVerticalRetrace = true;

      AudioManager = new AudioManager( this );
      Components.Add( AudioManager );

      // Create the screen manager component.
      screenManager = new ScreenManager( this );

      Components.Add( screenManager );

      HighscoreComponent highscoreComponent = new HighscoreComponent( this, null, "Avatar Hamster Panic" );
      HighscoreComponent.Global = highscoreComponent;
      Components.Add( highscoreComponent );

      // Activate the first screens.
      //screenManager.AddScreen( new BackgroundScreen(), null );
      //screenManager.AddScreen( new MainMenuScreen(), null );

      DisplayGamertags = true;
      ShareHighScores = true;
      SoundEffectsVolume = 1f;
      MusicVolume = 1f;

      // Avatars require GamerServices
      Components.Add( new GamerServicesComponent( this ) );

      Rumble = new RumbleComponent( this );
      Components.Add( Rumble );

      Instance = this;

      // Debugging components
      DebugManager = new DebugManager( this );
      DebugManager.DrawOrder = 200;
      Components.Add( DebugManager );
      DebugCommand = new DebugCommandUI( this );
      DebugCommand.DrawOrder = 200;
      Components.Add( DebugCommand );
      FpsCounter = new FpsCounter( this );
      FpsCounter.DrawOrder = 200;
      Components.Add( FpsCounter );
      TimeRuler = new TimeRuler( this );
      TimeRuler.DrawOrder = 200;
      Components.Add( TimeRuler );
    }

    protected override void LoadContent()
    {
      base.LoadContent();
      TimeRuler.Visible = true;
      TimeRuler.ShowLog = true;
      //FpsCounter.Visible = true;

      /**/
      graphics.PreferredBackBufferWidth  = graphics.GraphicsDevice.DisplayMode.Width;
      graphics.PreferredBackBufferHeight = graphics.GraphicsDevice.DisplayMode.Height;
      graphics.ApplyChanges();
      /*/
      graphics.PreferredBackBufferWidth = 1280;
      graphics.PreferredBackBufferHeight = 1024;
      graphics.ApplyChanges();
      /**/

      ScreenRects.Initialize( this );

      screenManager.AddScreen( new BackgroundScreen(), null );
      screenManager.AddScreen( new MainMenuScreen(), null );

      Content.Load<Effect>( "Effects/lineEffect" );
      Content.Load<CustomModel>( "Models/hamsterBall" );
      Content.Load<Texture2D>( "Textures/controls" );
      Content.Load<Texture2D>( "Textures/loadingText" );
      Content.Load<Texture2D>( "Textures/pressStartText" );

      TimeRuler.StartFrame();
      TimeRuler.BeginMark( 2, totalMark, Color.White );
    }

    public void ShowBuy( object o, PlayerIndexEventArgs args )
    {
      SignedInGamer gamer = null;
      foreach ( SignedInGamer temp in SignedInGamer.SignedInGamers )
      {
        if ( temp.IsSignedInToLive )
        {
          if ( gamer == null || ( temp.PlayerIndex == args.PlayerIndex ) )
            gamer = temp;
        }
      }
      if ( gamer != null && gamer.PlayerIndex == args.PlayerIndex )
        Guide.ShowMarketplace( gamer.PlayerIndex );
      else
        screenManager.AddScreen( new MessageBoxScreen( "You must be signed in to Xbox Live" ), null );
    }


    #endregion

    #region Update and Draw

    string updateMark = "Update";
    string drawMark = "Draw";
    string totalMark = "Total";

    protected override void Update( GameTime gameTime )
    {
      TimeRuler.EndMark( 2, totalMark );

      TimeRuler.StartFrame();
      TimeRuler.BeginMark( 2, totalMark, Color.White );
      TimeRuler.BeginMark( 0, updateMark, Color.Yellow );
      base.Update( gameTime );
      TimeRuler.EndMark( 0, updateMark );
    }


    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    protected override void Draw( GameTime gameTime )
    {
      TimeRuler.BeginMark( 1, drawMark, Color.SkyBlue );
      graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
      graphics.GraphicsDevice.Clear( Color.Black );

      // The real drawing happens inside the screen manager component.
      base.Draw( gameTime );
      TimeRuler.EndMark( 1, drawMark );
    }


    #endregion
  }


  #region Entry Point

  static class Program
  {
    static void Main()
    {
      using ( GameCore game = new GameCore() )
      {
        game.Run();
      }
    }
  }

  #endregion
}
