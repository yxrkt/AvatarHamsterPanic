using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Graphics
{
  public class Camera
  {
    private Camera() { }

    public Camera( float fov, float aspect, float near, float far, Vector3 pos, Vector3 target )
    {
      Fov      = fov;
      Aspect   = aspect;
      Near     = near;
      Far      = far;
      Position = pos;
      Target   = target;
      Up       = Vector3.UnitY;
    }

    public float Fov { get; set; }
    public float Aspect { get; set; }
    public float Near { get; set; }
    public float Far { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; }

    public void Translate( Vector3 trans )
    {
      Position += trans;
      Target += trans;
    }

    public Matrix GetViewMatrix()
    {
      return Matrix.CreateLookAt( Position, Target, Up );
    }

    public Matrix GetProjectionMatrix()
    {
      return Matrix.CreatePerspectiveFieldOfView( Fov, Aspect, Near, Far );
    }
  }
}