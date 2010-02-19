using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Audio
{
  class SoundSource : IAudioEmitter
  {
    Vector3 IAudioEmitter.Position
    {
      get { throw new NotImplementedException(); }
    }

    Vector3 IAudioEmitter.Forward
    {
      get { throw new NotImplementedException(); }
    }

    Vector3 IAudioEmitter.Up
    {
      get { throw new NotImplementedException(); }
    }

    Vector3 IAudioEmitter.Velocity
    {
      get { throw new NotImplementedException(); }
    }
  }
}