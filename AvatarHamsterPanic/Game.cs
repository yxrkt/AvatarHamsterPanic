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
#endregion

namespace AvatarHamsterPanic.Objects
{
  /// <summary>
  /// Sample showing how to manage different game states, with transitions
  /// between menu screens, a loading screen, the game itself, and a pause
  /// menu. This main game class is extremely simple: all the interesting
  /// stuff happens in the ScreenManager component.
  /// </summary>
  public class GameCore : Game
  {
    #region Fields

    GraphicsDeviceManager graphics;
    ScreenManager screenManager;

    public Dictionary<uint, int> PlayerWins { get; private set; }
    public Color[] PlayerColors { get; private set; }

    #endregion

    #region Initialization


    /// <summary>
    /// The main game constructor.
    /// </summary>
    public GameCore()
    {
      Content.RootDirectory = "Content";

      graphics = new GraphicsDeviceManager( this );

      PlayerWins = new Dictionary<uint, int>( 4 );
      PlayerColors = new Color[4]
      {
        new Color( 0xAA, 0xEA, 0xFF ),//AAEAFF
        new Color( 0xF7, 0xA4, 0xA4 ),//F7A4A4
        new Color( 0xFF, 0xEB, 0x9B ),//FFEB9B
        new Color( 0xC2, 0xF2, 0xA2 ),//C2F2A2
      };

      /*/
      graphics.PreferredBackBufferWidth = 1280;
      graphics.PreferredBackBufferHeight = 720;
      /*/
      graphics.PreferredBackBufferWidth = 1920;
      graphics.PreferredBackBufferHeight = 1080;
      /**/

      graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

      IsFixedTimeStep = false;
      graphics.SynchronizeWithVerticalRetrace = false;

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
