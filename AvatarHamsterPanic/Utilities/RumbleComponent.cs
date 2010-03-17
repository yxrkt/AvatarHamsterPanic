using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections;
using Microsoft.Xna.Framework.GamerServices;

namespace AvatarHamsterPanic.Utilities
{
  public class RumbleComponent : GameComponent
  {
    const int maxPlayers = 4;
    RumbleState[] states;

    public RumbleComponent( Game game )
      : base( game )
    {
      states = new RumbleState[maxPlayers];
    }

    public override void Initialize()
    {
      base.Initialize();
    }

    public override void Update( GameTime gameTime )
    {
      float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

      for ( int i = 0; i < maxPlayers; ++i )
      {
        if ( states[i].LeftMotorTime > 0 )
        {
          states[i].LeftMotorTime -= elapsed;
          if ( states[i].LeftMotorMag <= 0 )
          {
            states[i].LeftMotorMag = states[i].LeftMotorRest;
          }
        }
        else
        {
          states[i].LeftMotorMag = states[i].LeftMotorRest;
        }

        if ( states[i].RightMotorTime > 0 )
        {
          states[i].RightMotorTime -= elapsed;
          if ( states[i].RightMotorTime <= 0 )
          {
            states[i].RightMotorMag = states[i].RightMotorRest;
          }
        }
        else
        {
          states[i].RightMotorMag = states[i].RightMotorRest;
        }

        GamePad.SetVibration( (PlayerIndex)i, states[i].LeftMotorMag, states[i].RightMotorMag );
      }
    }

    public void RumbleLow( PlayerIndex player, float strength, float duration )
    {
      if ( !Enabled ) return;

      int index = (int)player;
      states[index].LeftMotorMag = Math.Max( strength, states[index].LeftMotorRest );
      states[index].LeftMotorTime = duration;
      GamePad.SetVibration( player, states[index].LeftMotorMag, states[index].RightMotorMag );
    }

    public void RumbleHigh( PlayerIndex player, float strength, float duration )
    {
      if ( !Enabled ) return;

      int index = (int)player;
      states[index].RightMotorMag = Math.Max( strength, states[index].RightMotorRest );
      states[index].RightMotorTime = duration;
      GamePad.SetVibration( player, states[index].LeftMotorMag, states[index].RightMotorMag );
    }

    public void TurnOnLow( PlayerIndex player, float strength )
    {
      if ( !Enabled ) return;

      states[(int)player].LeftMotorRest = strength;
    }

    public void TurnOffLow( PlayerIndex player )
    {
      if ( !Enabled ) return;

      states[(int)player].LeftMotorRest = 0;
    }

    public void TurnOnHigh( PlayerIndex player, float strength )
    {
      if ( !Enabled ) return;

      states[(int)player].RightMotorRest = strength;
    }

    public void TurnOffHigh( PlayerIndex player )
    {
      if ( !Enabled ) return;

      states[(int)player].RightMotorRest = 0;
    }
  }

  struct RumbleState
  {
    public float LeftMotorMag;
    public float LeftMotorTime;
    public float LeftMotorRest;
    public float RightMotorMag;
    public float RightMotorTime;
    public float RightMotorRest;
  }
}