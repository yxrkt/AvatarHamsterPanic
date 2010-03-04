using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Audio
{
  class DummyAudioEmitter : IAudioEmitter
  {
    private static readonly DummyAudioEmitter _instance;

    public Vector3 Position { get; set; }
    public Vector3 Forward { get; set; }
    public Vector3 Up { get; set; }
    public Vector3 Velocity { get; set; }

    public static DummyAudioEmitter ClearInstance
    {
      get
      {
        _instance.Position = Vector3.Zero;
        _instance.Forward = Vector3.UnitZ;
        _instance.Up = Vector3.Up;
        _instance.Velocity = Vector3.Zero;
        return _instance;
      }
    }

    public static DummyAudioEmitter InstanceAtPos( Vector3 position )
    {
      _instance.Position = position;
      _instance.Forward = Vector3.UnitZ;
      _instance.Up = Vector3.Up;
      _instance.Velocity = Vector3.Zero;
      return _instance;
    }

    static DummyAudioEmitter()
    {
      _instance = new DummyAudioEmitter();
    }

    private DummyAudioEmitter()
    {
    }
  }
}