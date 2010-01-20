using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GameStateManagement;

namespace AvatarHamsterPanic.Objects
{
  enum PowerupType
  {
    ScoreCoin,
  }

  class Powerup : GameObject
  {
    private Powerup( GameplayScreen screen )
      : base( screen )
    {
    }
  }
}