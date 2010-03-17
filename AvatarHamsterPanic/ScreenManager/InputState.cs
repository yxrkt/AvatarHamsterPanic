#region File Description
//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
#endregion

namespace Menu
{
  [Serializable]
  public class InputState
  {
    #region Fields

    public const int MaxInputs = 4;

    public readonly KeyboardState[] CurrentKeyboardStates;
    public readonly GamePadState[] CurrentGamePadStates;

    public readonly KeyboardState[] LastKeyboardStates;
    public readonly GamePadState[] LastGamePadStates;

    public readonly bool[] GamePadWasConnected;

    #endregion

    #region Initialization


    public InputState()
    {
      CurrentKeyboardStates = new KeyboardState[MaxInputs];
      CurrentGamePadStates = new GamePadState[MaxInputs];

      LastKeyboardStates = new KeyboardState[MaxInputs];
      LastGamePadStates = new GamePadState[MaxInputs];

      GamePadWasConnected = new bool[MaxInputs];
    }


    #endregion

    #region Public Methods


    public void Update()
    {
      for ( int i = 0; i < MaxInputs; i++ )
      {
        LastKeyboardStates[i] = CurrentKeyboardStates[i];
        LastGamePadStates[i] = CurrentGamePadStates[i];

        CurrentKeyboardStates[i] = Keyboard.GetState( (PlayerIndex)i );
        CurrentGamePadStates[i] = GamePad.GetState( (PlayerIndex)i );

        // Keep track of whether a gamepad has ever been
        // connected, so we can detect if it is unplugged.
        if ( CurrentGamePadStates[i].IsConnected )
        {
          GamePadWasConnected[i] = true;
        }
      }
    }

    public bool IsNewKeyPress( Keys key, PlayerIndex? controllingPlayer,
                                        out PlayerIndex playerIndex )
    {
      if ( controllingPlayer.HasValue )
      {
        // Read input from the specified player.
        playerIndex = controllingPlayer.Value;

        int i = (int)playerIndex;

        return ( CurrentKeyboardStates[i].IsKeyDown( key ) &&
                LastKeyboardStates[i].IsKeyUp( key ) );
      }
      else
      {
        // Accept input from any player.
        return ( IsNewKeyPress( key, PlayerIndex.One, out playerIndex ) ||
                 IsNewKeyPress( key, PlayerIndex.Two, out playerIndex ) ||
                 IsNewKeyPress( key, PlayerIndex.Three, out playerIndex ) ||
                 IsNewKeyPress( key, PlayerIndex.Four, out playerIndex ) );
      }
    }

    public bool IsNewButtonPress( Buttons button, PlayerIndex? controllingPlayer,
                                                  out PlayerIndex playerIndex )
    {
      if ( controllingPlayer.HasValue )
      {
        // Read input from the specified player.
        playerIndex = controllingPlayer.Value;

        int i = (int)playerIndex;

        return ( CurrentGamePadStates[i].IsButtonDown( button ) &&
                LastGamePadStates[i].IsButtonUp( button ) );
      }
      else
      {
        // Accept input from any player.
        return ( IsNewButtonPress( button, PlayerIndex.One, out playerIndex ) ||
                 IsNewButtonPress( button, PlayerIndex.Two, out playerIndex ) ||
                 IsNewButtonPress( button, PlayerIndex.Three, out playerIndex ) ||
                 IsNewButtonPress( button, PlayerIndex.Four, out playerIndex ) );
      }
    }

    public bool IsMenuSelect( PlayerIndex? controllingPlayer,
                             out PlayerIndex playerIndex )
    {
      return IsNewButtonPress( Buttons.A, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.Start, controllingPlayer, out playerIndex );
    }

    public bool IsMenuCancel( PlayerIndex? controllingPlayer,
                             out PlayerIndex playerIndex )
    {
      return IsNewButtonPress( Buttons.B, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.Back, controllingPlayer, out playerIndex );
    }

    public bool IsMenuUp( PlayerIndex? controllingPlayer )
    {
      PlayerIndex playerIndex;

      return IsNewButtonPress( Buttons.DPadUp, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.LeftThumbstickUp, controllingPlayer, out playerIndex );
    }

    public bool IsMenuDown( PlayerIndex? controllingPlayer )
    {
      PlayerIndex playerIndex;

      return IsNewButtonPress( Buttons.DPadDown, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.LeftThumbstickDown, controllingPlayer, out playerIndex );
    }

    public bool IsMenuLeft( PlayerIndex? controllingPlayer )
    {
      PlayerIndex playerIndex;
      return IsMenuLeft( controllingPlayer, out playerIndex );
    }

    public bool IsMenuLeft( PlayerIndex? controllingPlayer, out PlayerIndex playerIndex )
    {
      return IsNewButtonPress( Buttons.LeftShoulder, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.DPadLeft, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.LeftThumbstickLeft, controllingPlayer, out playerIndex );
    }

    public bool IsMenuRight( PlayerIndex? controllingPlayer )
    {
      PlayerIndex playerIndex;
      return IsMenuRight( controllingPlayer, out playerIndex );
    }

    public bool IsMenuRight( PlayerIndex? controllingPlayer, out PlayerIndex playerIndex )
    {
      return IsNewButtonPress( Buttons.RightShoulder, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.DPadRight, controllingPlayer, out playerIndex ) ||
             IsNewButtonPress( Buttons.LeftThumbstickRight, controllingPlayer, out playerIndex );
    }

    public bool IsPauseGame( PlayerIndex? controllingPlayer )
    {
      PlayerIndex playerIndex;

      return IsNewButtonPress( Buttons.Start, controllingPlayer, out playerIndex );
    }


    #endregion
  }
}
