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
    float rowSpacing;
    float minHoleDist;
    float lastTopY;
    float topLine;
    int highestRow = 0;
    int rows;

    int nCages = 0, nCageHoles = 0, nTees = 0, nCups = 0;
    InstancedModel cageModel;
    InstancedModel cageHoleModel;
    InstancedModel teeModel;
    InstancedModel cupModel;
    Matrix[] cageTransforms;
    Matrix[] cageHoleTransforms;
    Matrix[] teeTransforms;
    Matrix[] cupTransforms;
    SidePiece[] sidePieces;

    Matrix rotateL, rotateR;
    Matrix scale;
    Matrix flip;

    static Random rand = new Random();

    public static float Size { get; private set; }

    static Boundary()
    {
      Size = 2.3f;
    }

    public float Left { get; private set; }
    public float Right { get; private set; }

    public Boundary( GameplayScreen screen, float left, float right, float rowSpacing )
      : base( screen )
    {
      Left  = left;
      Right = right;

      this.rowSpacing = rowSpacing;
      minHoleDist = ( FloorBlock.Height + Size ) / 2f;

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
      cageHoleModel = Screen.Content.Load<InstancedModel>( "Models/cageHole" );
      teeModel = Screen.Content.Load<InstancedModel>( "Models/tubeTee" );
      cupModel = Screen.Content.Load<InstancedModel>( "Models/tubeCup" );

      Camera camera = screen.Camera;

      float dist = camera.Position.Z + Size / 2f;
      float tanFovyOverTwo = (float)Math.Tan( camera.Fov / 2f );
      deathLine = dist * tanFovyOverTwo + Size / 2f;

      topLine = camera.Position.Y + deathLine - Size;
      lastTopY = camera.Position.Y + deathLine;

      rows = (int)Math.Ceiling( 2f * deathLine / Size );
      nTransforms = rows * 2;
      cageTransforms = new Matrix[nTransforms];
      cageHoleTransforms = new Matrix[nTransforms];
      cupTransforms = new Matrix[nTransforms];
      teeTransforms = new Matrix[nTransforms];

      rotateL = new Matrix( 0, 0,-1, 0,
                            0, 1, 0, 0,
                            1, 0, 0, 0,
                            0, 0, 0, 1 );
      rotateR = new Matrix( 0, 0, 1, 0,
                            0, 1, 0, 0,
                           -1, 0, 0, 0,
                            0, 0, 0, 1 );
      flip = new Matrix(-1, 0, 0, 0,
                         0,-1, 0, 0,
                         0, 0, 1, 0,
                         0, 0, 0, 1 );
      scale = Matrix.CreateScale( Size );

      sidePieces = new SidePiece[nTransforms];
      for ( int i = 0; i < nTransforms; ++i )
      {
        sidePieces[i] = new SidePiece();
        SidePiece piece = sidePieces[i];

        int row = i % rows;
        bool onLeftSide = i / rows == 0;
        piece.Hole = false;
        piece.CagePosition = new Vector3( onLeftSide ? Left : Right, topLine - row * Size, 0f );
        Matrix scaleRotate = scale * ( onLeftSide ? rotateL : rotateR );
        piece.CageTransform = scaleRotate * Matrix.CreateTranslation( piece.CagePosition );
        piece.TubePosition = piece.CagePosition + new Vector3( onLeftSide ? -Size / 2 : Size / 2, 0, 0 );
        piece.TubeTransform = scale * Matrix.CreateTranslation( piece.TubePosition );
        piece.Tube = TubePattern.Cup;
      }
    }

    public override void Update( GameTime gameTime )
    {
      // update physics bodies
      Vector2 leftPos = new Vector2( polyLeft.Position.X, Screen.Camera.Position.Y );
      polyLeft.Position = leftPos;

      Vector2 rightPos = new Vector2( polyRight.Position.X, Screen.Camera.Position.Y );
      polyRight.Position = rightPos;

      // update side pieces and transforms
      Camera camera = Screen.Camera;

      nCages = 0;
      nCageHoles = 0;
      nCups = 0;
      nTees = 0;

      while ( topLine > Screen.Camera.Position.Y + deathLine )
      {
        float yPos = sidePieces[( highestRow + rows - 1 ) % rows].CagePosition.Y - Size;

        ConfigurePiece( sidePieces[highestRow], yPos, true );
        ConfigurePiece( sidePieces[highestRow + rows], yPos, false );

        highestRow = ( highestRow + 1 ) % rows;
        topLine -= Size;
      }

      foreach ( SidePiece piece in sidePieces )
      {
        if ( !piece.Hole )
          cageTransforms[nCages++] = piece.CageTransform;
        else
          cageHoleTransforms[nCageHoles++] = piece.CageTransform;

        if ( piece.Tube == TubePattern.Cup )
          cupTransforms[nCups++] = piece.TubeTransform;
        else
          teeTransforms[nTees++] = piece.TubeTransform;
      }
    }

    private void ConfigurePiece( SidePiece piece, float height, bool leftSide )
    {
      piece.CagePosition.Y = height;
      piece.CageTransform.M42 = height;
      piece.Hole = IsHoleHere( height );
      piece.TubePosition.Y = height;

      if ( piece.Hole )
      {
        piece.Tube = TubePattern.Tee;
        if ( leftSide )
          piece.TubeTransform = scale * Matrix.CreateTranslation( piece.TubePosition );
        else
          piece.TubeTransform = scale * flip * Matrix.CreateTranslation( piece.TubePosition );
      }
      else
      {
        piece.Tube = TubePattern.Cup;
        piece.TubeTransform = scale * Matrix.CreateTranslation( piece.TubePosition );
      }
    }

    private bool IsHoleHere( float y )
    {
      bool holeHere = rand.Next( 100 ) < 50;
      float quotient = Math.Abs( y / rowSpacing );
      float remainder = quotient - (float)Math.Floor( quotient );
      return ( holeHere && minHoleDist <= remainder * rowSpacing && minHoleDist <= ( 1 - remainder ) * rowSpacing );
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      renderState.AlphaBlendEnable = false;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;

      Matrix view = Screen.View;
      Matrix proj = Screen.Projection;
      Vector3 eye = Screen.Camera.Position;

      cageModel.DrawInstances( cageTransforms, nCages, view, proj, eye );
      cageHoleModel.DrawInstances( cageHoleTransforms, nCageHoles, view, proj, eye );

      renderState.AlphaBlendEnable = true;
      renderState.CullMode = CullMode.CullClockwiseFace;
      cupModel.DrawInstances( cupTransforms, nCups, view, proj, eye );
      teeModel.DrawInstances( teeTransforms, nTees, view, proj, eye );

      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      cupModel.DrawInstances( cupTransforms, nCups, view, proj, eye );
      teeModel.DrawInstances( teeTransforms, nTees, view, proj, eye );
    }
  }

  class SidePiece
  {
    public bool Hole;
    public Vector3 CagePosition;
    public Matrix CageTransform;
    public Vector3 TubePosition;
    public Matrix TubeTransform;
    public TubePattern Tube;
  }
}