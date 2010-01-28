using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using InstancedModelSample;

namespace AvatarHamsterPanic.Objects
{
  class Boundary : GameObject
  {
    const float polyWidth = 100f;
    const float halfPolyWidth = polyWidth / 2f;

    PhysPolygon polyLeft, polyRight;
    float deathLine;
    int nTransforms;

    InstancedModel cageModel;
    Matrix[] transforms;

    public static float Size { get; private set; }

    static Boundary()
    {
      Size = 2.3f;
    }

    public float Left { get; private set; }
    public float Right { get; private set; }

    public Boundary( GameplayScreen screen, float left, float right )
      : base( screen )
    {
      Left  = left;
      Right = right;

      // left polygon
      polyLeft = new PhysPolygon( polyWidth, 100f, new Vector2( left - halfPolyWidth, 0f ), 1f );
      polyLeft.Elasticity = 1f;
      polyLeft.Friction = 1.5f;
      polyLeft.Flags = PhysBodyFlags.Anchored;

      // right polygon
      polyRight = new PhysPolygon( polyWidth, 100f, new Vector2( right + halfPolyWidth, 0f ), 1f );
      polyRight.Elasticity = 1f;
      polyRight.Friction = 1.5f;
      polyRight.Flags = PhysBodyFlags.Anchored;

      // model
      cageModel = Screen.Content.Load<InstancedModel>( "Models/cage" );

      Camera camera = screen.Camera;

      float dist = camera.Position.Z + Size / 2f;
      float tanFovyOverTwo = (float)Math.Tan( camera.Fov / 2f );
      deathLine = dist * tanFovyOverTwo + Size / 2f;

      int rows = (int)Math.Ceiling( 2f * deathLine / Size );
      nTransforms = rows * 2;
      transforms = new Matrix[nTransforms];
    }

    public override void Update( GameTime gameTime )
    {
      // update physics bodies
      Vector2 leftPos = new Vector2( polyLeft.Position.X, Screen.Camera.Position.Y );
      polyLeft.Position = leftPos;

      Vector2 rightPos = new Vector2( polyRight.Position.X, Screen.Camera.Position.Y );
      polyRight.Position = rightPos;

      // update model transforms
      Camera camera = Screen.Camera;
      // get y height of top transform
      float yTop = deathLine - Size + camera.Position.Y - camera.Position.Y % Size;

      Matrix rotateL = new Matrix( 0, 0,-1, 0,
                                   0, 1, 0, 0,
                                   1, 0, 0, 0,
                                   0, 0, 0, 1 );
      Matrix rotateR = new Matrix( 0, 0, 1, 0,
                                   0, 1, 0, 0,
                                  -1, 0, 0, 0,
                                   0, 0, 0, 1 );
      Matrix scale = Matrix.CreateScale( Size );

      float y = yTop;
      for ( int i = 0; i < nTransforms; i += 2 )
      {
        transforms[i] = scale * rotateL * Matrix.CreateTranslation( new Vector3( Left, y, 0 ) );
        transforms[i + 1] = scale * rotateR * Matrix.CreateTranslation( new Vector3( Right, y, 0 ) );
        y -= Size;
      }
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      SetRenderState( device.RenderState );

      cageModel.DrawInstances( transforms, nTransforms, Screen.View, Screen.Projection, Screen.Camera.Position );
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.AlphaBlendEnable = false;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }
  }
}