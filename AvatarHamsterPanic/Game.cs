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
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// Sample showing how to manage different game states, with transitions
  /// between menu screens, a loading screen, the game itself, and a pause
  /// menu. This main game class is extremely simple: all the interesting
  /// stuff happens in the ScreenManager component.
  /// </summary>
  public class GameCore : Microsoft.Xna.Framework.Game
  {
    #region Fields

    GraphicsDeviceManager graphics;
    ScreenManager screenManager;

    #endregion

    #region Initialization


    /// <summary>
    /// The main game constructor.
    /// </summary>
    public GameCore()
    {
      Content.RootDirectory = "Content";

      graphics = new GraphicsDeviceManager( this );

      graphics.PreferredBackBufferWidth = 1920;//853;
      graphics.PreferredBackBufferHeight = 1080;//480;

      IsFixedTimeStep = false;
      graphics.SynchronizeWithVerticalRetrace = true;

      // Create the screen manager component.
      screenManager = new ScreenManager( this );

      Components.Add( screenManager );

      // Activate the first screens.
      screenManager.AddScreen( new BackgroundScreen(), null );
      screenManager.AddScreen( new MainMenuScreen(), null );

      // Avatars require GamerServices
      Components.Add( new GamerServicesComponent( this ) );
    }


    #endregion

    #region Draw


    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    protected override void Draw( GameTime gameTime )
    {
      graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
      graphics.GraphicsDevice.Clear( Color.Black );

      // The real drawing happens inside the screen manager component.
      base.Draw( gameTime );
    }


    #endregion
  }


  #region Entry Point

  /// <summary>
  /// The main entry point for the application.
  /// </summary>
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
