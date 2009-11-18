using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameObjects
{
  class Camera
  {
    float m_fov, m_aspect;
    float m_near, m_far;
    Vector3 m_pos, m_target;
    Vector3 m_up = Vector3.Up;

    public Camera( float fov, float aspect, float near, float far, Vector3 pos, Vector3 target )
    {
      m_fov    = fov;
      m_aspect = aspect;
      m_near   = near;
      m_far    = far;
      m_pos    = pos;
      m_target = target;
    }

    public float Fov { get { return m_fov; } set { m_fov = value; } }
    public float Aspect { get { return m_aspect; } set { m_aspect = value; } }
    public float Near { get { return m_near; } set { m_near = value; } }
    public float Far { get { return m_far; } set { m_far = value; } }
    public Vector3 Position { get { return m_pos; } set { m_pos = value; } }
    public Vector3 Target { get { return m_target; } set { m_target = value; } }
    public Vector3 Up { get { return m_up; } set { m_up = value; } }

    public void Translate( Vector3 trans )
    {
      m_pos += trans;
      m_target += trans;
    }
  }
}