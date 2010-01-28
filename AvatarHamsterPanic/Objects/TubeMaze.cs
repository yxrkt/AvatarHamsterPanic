using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using InstancedModelSample;

namespace AvatarHamsterPanic.Objects
{
  class TubeMaze : GameObject
  {
    TubePiece[,] tubes;
    Matrix[][] worldBuffer;
    int rows, cols;
    Vector3 topLeft;
    int highestRow;
    Matrix scaleMatrix;
    bool cupRow;

    InstancedModel[] tubeModels;
    int[] nTubes;
    Vector4[] colors;

    static Random rand;
    static Matrix[] rotations;

    public float DeathLine { get; private set; }
    public float TubeSize { get; private set; }

    static TubeMaze()
    {
      rand = new Random();

      rotations = new Matrix[4];

      rotations[0] = Matrix.Identity;

      rotations[1] = new Matrix(  0, -1,  0,  0,
                                  1,  0,  0,  0,
                                  0,  0,  1,  0,
                                  0,  0,  0,  1 );

      rotations[2] = new Matrix( -1,  0,  0,  0,
                                  0, -1,  0,  0,
                                  0,  0,  1,  0,
                                  0,  0,  0,  1 );

      rotations[3] = new Matrix(  0,  1,  0,  0,
                                 -1,  0,  0,  0,
                                  0,  0,  1,  0,
                                  0,  0,  0,  1 );
    }

    public TubeMaze( GameplayScreen screen, float z, float tubeSize )
      : base( screen )
    {
      colors = new Vector4[]
      {
        new Vector4( 1f, .7f, .7f, .3f ),
        new Vector4( .7f, 1f, .7f, .3f ),
        new Vector4( .7f, .7f, 1f, .3f ),
        new Vector4( 1f, 1f, .7f, .3f )
      };

      ContentManager content = screen.Content;

      tubeModels = new InstancedModel[4];
      tubeModels[(int)TubePattern.Elbow] = content.Load<InstancedModel>( "Models/tubeElbow" );
      tubeModels[(int)TubePattern.Cup]   = content.Load<InstancedModel>( "Models/tubeCup" );
      tubeModels[(int)TubePattern.Tee]   = content.Load<InstancedModel>( "Models/tubeTee" );
      tubeModels[(int)TubePattern.Cross] = content.Load<InstancedModel>( "Models/tubeCross" );
      foreach ( InstancedModel model in tubeModels )
        model.SetInstancingTechnique( InstancingTechnique.Color );

      Camera camera = screen.Camera;

      TubeSize = tubeSize;
      float dist = camera.Position.Z - ( z - TubeSize / 2f );
      float tanFovyOverTwo = (float)Math.Tan( camera.Fov / 2f );
      DeathLine = dist * tanFovyOverTwo + TubeSize / 2f;

      rows = (int)Math.Ceiling( 2f * DeathLine / TubeSize );

      float aspect = screen.ScreenManager.GraphicsDevice.Viewport.AspectRatio;
      float fovxOverTwo = (float)Math.Atan( aspect * tanFovyOverTwo );
      float worldWidth = 2f * ( dist * (float)Math.Tan( fovxOverTwo ) + TubeSize / 2f );

      cols = (int)Math.Ceiling( worldWidth / TubeSize );

      tubes = new TubePiece[rows, cols];
      int maxPipes = rows * cols;
      worldBuffer = new Matrix[4][];
      for ( int i = 0; i < 4; ++i )
        worldBuffer[i] = new Matrix[maxPipes];
      nTubes = new int[4];
      cupRow = ( rows % 2 == 1 );

      scaleMatrix = Matrix.CreateScale( tubeSize );

      topLeft = new Vector3( camera.Position.X - tubeSize * (float)cols / 2f + TubeSize / 2f, 
                             camera.Position.Y + DeathLine - TubeSize, z );
      highestRow = 0;

      InitializeGrid();
    }

    private void InitializeGrid()
    {
      // fill tube grid
      tubes[0, 0] = TubePiece.GetRandomPiece( rand );
      for ( int c = 1; c < cols; ++c )
      {
        bool rightOpen = tubes[0, c - 1].RightOpen();
        if ( c % 2 == 0 )
          tubes[0, c] = TubePiece.GetRandomPieceLeft( rand, rightOpen );
        else if ( rightOpen )
          tubes[0, c] = new TubePiece( TubePattern.Cup, 1 );
      }
      for ( int r = 1; r < rows; ++r )
      {
        bool bottomOpen = tubes[r - 1, 0].BottomOpen();
        if ( r % 2 == 0 )
          tubes[r, 0] = TubePiece.GetRandomPieceTop( rand, bottomOpen );
        else if ( bottomOpen )
          tubes[r, 0] = new TubePiece( TubePattern.Cup, 0 );
      }
      for ( int r = 1; r < rows; ++r )
      {
        if ( r % 2 == 0 )
        {
          for ( int c = 1; c < cols; ++c )
          {
            bool rightOpen = tubes[r, c - 1].RightOpen();
            bool bottomOpen = tubes[r - 1, c].BottomOpen();
            if ( c % 2 == 0 )
              tubes[r, c] = TubePiece.GetRandomPieceLeftTop( rand, rightOpen, bottomOpen );
            else if ( rightOpen )
              tubes[r, c] = new TubePiece( TubePattern.Cup, 1 );
          }
        }
        else
        {
          for ( int c = 1; c < cols; ++c )
          {
            if ( ( c % 2 == 0 ) && tubes[r - 1, c].BottomOpen() )
              tubes[r, c] = new TubePiece( TubePattern.Cup, 0 );
          }
        }
      }
    }

    public override void Update( GameTime gameTime )
    {
      int nColors = colors.Length;
      float timePerColor = 4f;
      float time = (float)gameTime.TotalGameTime.TotalSeconds;
      float fakeTime = time / timePerColor;

      int a = (int)( fakeTime ) % nColors;
      int b = ( a + 1 ) % nColors;
      float t = .5f + -(float)Math.Cos( (float)Math.PI * fakeTime ) / 2f;
      if ( (int)fakeTime % 2 == 1 )
        t = 1f - t;

      // set color for tubes
      Vector4 color = colors[a] + t * ( colors[b] - colors[a] );
      int nModels = tubeModels.Length;
      for ( int i = 0; i < nModels; ++i )
      {
        int nParts = tubeModels[i].ModelParts.Count;
        for ( int j = 0; j < nParts; ++j )
          tubeModels[i].ModelParts[j].EffectParameterColor.SetValue( color );
      }

      while ( topLeft.Y > Screen.Camera.Position.Y + DeathLine )
      {
        int lowestRow = highestRow;
        int secondLowestRow = ( highestRow + rows - 1 ) % rows;
        highestRow = ( highestRow + 1 ) % rows;
        topLeft.Y -= TubeSize;

        // recreate row to fit with its new predecessor
        if ( !cupRow )
        {
          tubes[lowestRow, 0] = TubePiece.GetRandomPieceTop( rand, tubes[secondLowestRow, 0].BottomOpen() );
          for ( int c = 1; c < cols; ++c )
          {
            bool rightOpen = tubes[lowestRow, c - 1].RightOpen();
            bool bottomOpen = tubes[secondLowestRow, c].BottomOpen();
            if ( c % 2 == 0 )
              tubes[lowestRow, c] = TubePiece.GetRandomPieceLeftTop( rand, rightOpen, bottomOpen );
            else if ( tubes[lowestRow, c - 1].RightOpen() )
              tubes[lowestRow, c] = new TubePiece( TubePattern.Cup, 1 );
            else
              tubes[lowestRow, c].Alive = false;
          }
        }
        else
        {
          for ( int c = 0; c < cols; ++c )
          {
            if ( c % 2 == 0 )
            {
              if ( tubes[secondLowestRow, c].BottomOpen() )
                tubes[lowestRow, c] = new TubePiece( TubePattern.Cup, 0 );
              else
                tubes[lowestRow, c].Alive = false;
            }
            else
            {
              tubes[lowestRow, c].Alive = false;
            }
          }
        }

        cupRow = !cupRow;
      }
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;

      SetRenderState( device.RenderState );
      device.RenderState.CullMode = CullMode.CullClockwiseFace;

      GetWorldTransforms();

      for ( int i = 0; i < 4; ++i )
      {
        tubeModels[i].DrawInstances( worldBuffer[i], nTubes[i], Screen.View,
                                     Screen.Projection, Screen.Camera.Position );
      }

      device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      for ( int i = 0; i < 4; ++i )
      {
        tubeModels[i].DrawInstances( worldBuffer[i], nTubes[i], Screen.View,
                                     Screen.Projection, Screen.Camera.Position );
      }
    }

    private void GetWorldTransforms()
    {
      nTubes[0] = nTubes[1] = nTubes[2] = nTubes[3] = 0;
      Vector3 position = topLeft;

      for ( int r = 0; r < rows; ++r )
      {
        position.X = topLeft.X;
        int row = ( highestRow + r ) % rows;
        for ( int c = 0; c < cols; ++c )
        {
          TubePiece tube = tubes[row, c];
          if ( tube.Alive )
          {
            int typeIndex = (int)tube.Pattern;
            Matrix translation = Matrix.CreateTranslation( position );
            worldBuffer[typeIndex][nTubes[typeIndex]++] = scaleMatrix * rotations[tube.Rotation] * translation;
          }
          position.X += TubeSize;
        }
        position.Y -= TubeSize;
      }
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.None;

      renderState.AlphaBlendEnable = true;
      //renderState.SourceBlend = Blend.SourceAlpha;
      //renderState.DestinationBlend = Blend.InverseSourceAlpha;

      renderState.DepthBufferEnable = true;
      renderState.DepthBufferWriteEnable = true;
    }
  }

  struct TubePiece
  {
    public TubePattern Pattern;
    public int Rotation;
    public bool Alive;

    // Generate a random piece with no requirements
    public static TubePiece GetRandomPiece( Random rand )
    {
      return new TubePiece( (TubePattern)rand.Next( 4 ), rand.Next( 4 ) );
    }

    // Generate a piece with a left requirement
    public static TubePiece GetRandomPieceLeft( Random rand, bool left )
    {
      TubePattern pattern = left ? (TubePattern)rand.Next( 4 ) : (TubePattern)rand.Next( 3 );
      int rotation = 0;

      switch ( pattern )
      {
        case TubePattern.Elbow:
          rotation = left ? rand.Next( 2, 4 ) : rand.Next( 0, 2 );
          break;
        case TubePattern.Cup:
          rotation = left ? 1 : 2;
          break;
        case TubePattern.Tee:
          rotation = left ? rand.Next( 1, 3 ) : 0;
          break;
        case TubePattern.Cross:
          break;
      }

      return new TubePiece( pattern, rotation );
    }

    // Generate a piece with a top requirement
    public static TubePiece GetRandomPieceTop( Random rand, bool top )
    {
      TubePiece piece = GetRandomPieceLeft( rand, top );
      piece.Rotation = piece.Rotation < 3 ? piece.Rotation + 1 : 0;
      return piece;
    }

    // Generate a pice with a left and top requirements
    public static TubePiece GetRandomPieceLeftTop( Random rand, bool left, bool top )
    {
      if ( !left && !top )
        return new TubePiece( TubePattern.Elbow, 1 );
      if ( !left && top )
        return new TubePiece( (TubePattern)rand.Next( 3 ), 0 );
      if ( left && !top )
      {
        TubePattern pattern = (TubePattern)rand.Next( 3 );
        int rotation = 2;

        switch ( pattern )
        {
          case TubePattern.Elbow:
            rotation = rand.Next( 2, 3 );
            break;
          case TubePattern.Cup:
            rotation = 1;
            break;
          case TubePattern.Tee:
            rotation = 1;
            break;
        }

        return new TubePiece( pattern, rotation );
      }
      else // if left and top
      {
        TubePattern pattern = (TubePattern)( rand.Next( 2, 5 ) % 4 );

        if ( pattern == TubePattern.Elbow )
          return new TubePiece( TubePattern.Elbow, 3 );
        if ( pattern == TubePattern.Tee )
          return new TubePiece( TubePattern.Tee, rand.Next( 2, 4 ) );
        return new TubePiece( TubePattern.Cross, 0 );
      }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public TubePiece( TubePattern pattern, int rotation )
    {
      Pattern = pattern;
      Rotation = rotation;
      Alive = true;
    }

    /// <summary>
    /// Determines whether or not the piece wants to connect through its bottom.
    /// </summary>
    public bool BottomOpen()
    {
      if ( !Alive )
        return false;

      if ( Pattern == TubePattern.Elbow )
        return ( Rotation == 1 || Rotation == 2 );
      if ( Pattern == TubePattern.Cup )
        return ( Rotation == 0 || Rotation == 2 );
      if ( Pattern == TubePattern.Tee )
        return ( Rotation != 3 );
      return true;
    }

    /// <summary>
    /// Determines whether or not the piece wants to connect through its right.
    /// </summary>
    public bool RightOpen()
    {
      if ( !Alive )
        return false;

      if ( Pattern == TubePattern.Elbow )
        return ( Rotation == 0 || Rotation == 1 );
      if ( Pattern == TubePattern.Cup )
        return ( Rotation == 1 || Rotation == 3 );
      if ( Pattern == TubePattern.Tee )
        return ( Rotation != 2 );
      return true;
    }
  }

  enum TubePattern
  {
    Elbow,
    Cup,
    Tee,
    Cross,
  }

  static class TubePatternHelper
  {
    public static TubePattern Increment( this TubePattern pattern )
    {
      pattern = (TubePattern)( (int)pattern + 1 );
      return pattern;
    }

    public static bool LessOrEqual( this TubePattern pattern, TubePattern value )
    {
      return ( (int)pattern < (int)value );
    }
  }
}