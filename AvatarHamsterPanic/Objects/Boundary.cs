using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Physics;
using Microsoft.Xna.Framework;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Graphics;
using InstancedModelSample;
using System.Collections.ObjectModel;

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
    float rowStart;
    float minHoleDist;
    float lastTopY;
    float topLine;
    int highestRow = 0;
    int rows;

    InstancedModel cageModel;
    InstancedModel cageHoleModel;
    InstancedModel teeModel;
    InstancedModel cupModel;
    SidePiece[] sidePieces;
    GameTime oneFrame;

    Matrix rotateL, rotateR;
    Matrix scale;
    Matrix flip;

    List<BoundaryTubeObject> objects;
    public int NumObjectsInTubes { get; private set; }
    public Vector2 FirstObjPos
    {
      get
      {
        if ( objects[0].Object == null )
          return Vector2.Zero;
        return objects[0].Body.Position;
      }
    }

    static Random rand = new Random();

    public static float Size { get; private set; }

    static Boundary()
    {
      Size = 2.3f;
    }

    public float Left { get; private set; }
    public float Right { get; private set; }

    public Boundary( GameplayScreen screen, float left, float right, float rowStart, float rowSpacing )
      : base( screen )
    {
      Left  = left;
      Right = right;

      oneFrame = new GameTime( TimeSpan.FromSeconds( 0 ), TimeSpan.FromSeconds( 0 ),
                               TimeSpan.FromSeconds( 1f / 60f ), TimeSpan.FromSeconds( 1f / 60 ) );

      this.rowSpacing = rowSpacing;
      this.rowStart = rowStart;
      minHoleDist = ( FloorBlock.Height + Size ) / 2f;

      // this is for objects, such as powerups and players, so they can travel through the tubes
      objects = new List<BoundaryTubeObject>( 10 );
      for ( int i = 0; i < 10; ++i )
        objects.Add( new BoundaryTubeObject() );

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

      while ( topLine > Screen.Camera.Position.Y + deathLine )
      {
        float yPos = sidePieces[( highestRow + rows - 1 ) % rows].CagePosition.Y - Size;

        ConfigurePiece( sidePieces[highestRow], yPos, true );
        ConfigurePiece( sidePieces[highestRow + rows], yPos, false );

        highestRow = ( highestRow + 1 ) % rows;
        topLine -= Size;
      }

      // update objects
      UpdateObjects( (float)gameTime.ElapsedGameTime.TotalSeconds );
    }

    private void ConfigurePiece( SidePiece piece, float height, bool leftSide )
    {
      piece.CagePosition.Y = height;
      piece.CageTransform.M42 = height;
      piece.Hole = IsHoleHere( height );
      piece.TubePosition.Y = height;

      if ( first && piece.Hole )
        first = true;
      else
        piece.Hole = false;

      if ( piece.Hole )
      {
        piece.Tube = TubePattern.Tee;
        if ( leftSide )
          piece.TubeTransform = scale * Matrix.CreateTranslation( piece.TubePosition );
        else
          piece.TubeTransform = scale * flip * Matrix.CreateTranslation( piece.TubePosition );

        if ( rand.Next( 100 ) < 100 )
          ShootRandomPowerup( piece );
      }
      else
      {
        piece.Tube = TubePattern.Cup;
        piece.TubeTransform = scale * Matrix.CreateTranslation( piece.TubePosition );
      }
    }

    bool first = true;
    private bool IsHoleHere( float y )
    {
      bool holeHere = first && rand.Next( 100 ) < 100;
      float quotient = Math.Abs( ( y + rowStart ) / rowSpacing );
      float remainder = quotient - (float)Math.Floor( quotient );
      return ( holeHere && minHoleDist <= remainder * rowSpacing && minHoleDist <= ( 1 - remainder ) * rowSpacing );
    }

    private void ShootRandomPowerup( SidePiece piece )
    {
      BoundaryTubeObject tubeObject = objects.Find( o => o.Object == null );
      if ( tubeObject == null )
      {
        tubeObject = new BoundaryTubeObject();
        objects.Add( tubeObject );
      }
      else
      {
        tubeObject.Path.Clear();
        tubeObject.Time = 0f;
      }

      Vector2 startPos = new Vector2( piece.TubePosition.X, deathLine + Screen.Camera.Position.Y );
      Powerup powerup = Powerup.CreatePowerup( startPos, PowerupType.ScoreCoin );
      powerup.InTube = true;
      powerup.Update( oneFrame );
      Screen.ObjectTable.Add( powerup );
      tubeObject.Object = powerup;
      tubeObject.Body = powerup.Body;

      Vector2 tubePos = new Vector2( piece.TubePosition.X, piece.TubePosition.Y );
      Vector2 finalPos = new Vector2( Math.Sign( tubePos.X ) == -1 ? Left : Right, tubePos.Y );
      tubeObject.Path.Add( startPos );
      tubeObject.Path.Add( tubePos );
      tubeObject.Path.Add( finalPos );
    }

    private void UpdateObjects( float elapsed )
    {
      foreach ( BoundaryTubeObject tubeObject in objects )
      {
        if ( tubeObject.Object == null ) continue;

        Vector2 position;
        if ( tubeObject.Path.GetPosition( tubeObject.Time * tubeObject.Speed, out position ) )
        {
          tubeObject.Body.Position = position;
          tubeObject.Time += elapsed;
        }
        else
        {
          Powerup powerup = tubeObject.Object as Powerup;
          if ( powerup != null )
          {
            NumObjectsInTubes--;
            powerup.InTube = false;
            //powerup.Body.Flags ^= PhysBodyFlags.Anchored;
          }

          tubeObject.Object = null;
          tubeObject.Body = null;
        }
      }
    }

    private void GetModelTransforms()
    {
      foreach ( SidePiece piece in sidePieces )
      {
        if ( !piece.Hole )
          cageModel.AddInstance( piece.CageTransform );
        else
          cageHoleModel.AddInstance( piece.CageTransform );

        if ( piece.Tube == TubePattern.Cup )
          cupModel.AddInstance( piece.TubeTransform );
        else
          teeModel.AddInstance( piece.TubeTransform );
      }
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      GetModelTransforms();

      Matrix view = Screen.View;
      Matrix proj = Screen.Projection;
      Vector3 eye = Screen.Camera.Position;

      renderState.AlphaBlendEnable = false;
      renderState.CullMode = CullMode.CullCounterClockwiseFace;
      renderState.DepthBufferEnable = true;

      cageHoleModel.DrawInstances( view, proj, eye );

      cupModel.DrawTranslucentInstances( view, proj, eye );
      teeModel.DrawTranslucentInstances( view, proj, eye );
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

  class BoundaryTubeObject
  {
    public PhysBody Body;
    public GameObject Object;
    public Path Path;
    public float Speed;
    public float Time;

    public BoundaryTubeObject()
    {
      Body = null;
      Object = null;
      Path = new Path();
      Speed = 3f;
      Time = 0f;
    }
  }

  class Path
  {
    List<Vector2> points = new List<Vector2>( 4 );
    List<float> lengths = new List<float>( 3 );

    public Path() { }

    public void Add( Vector2 point )
    {
      points.Add( point );
      if ( points.Count > 1 )
        lengths.Add( ( point - points[points.Count - 2] ).Length() );
    }

    public bool GetPosition( float dist, out Vector2 position )
    {
      position = Vector2.Zero;

      int pointIndex = 0;
      foreach ( float length in lengths )
      {
        if ( dist > length )
        {
          dist -= length;
          pointIndex++;
        }
        else
        {
          float t = dist / length;
          position = points[pointIndex] + t * ( points[pointIndex + 1] - points[pointIndex] );
          return true;
        }
      }

      return false;
    }

    public void Clear()
    {
      points.Clear();
      lengths.Clear();
    }
  }
}