using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

namespace GameObjects
{
  class TubeMaze : GameObject
  {
    TubePiece[,] tubes;
    int rows, cols;
    Vector3 topLeft;
    int highestRow;
    Matrix scaleMatrix;
    bool cupRow;

    Effect effect;
    Model[] tubeModels;

    static Random rand;
    static Matrix[] rotations;
    
    public float TubeSize { get; private set; }

    static TubeMaze()
    {
      rand = new Random(1);

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

    public TubeMaze( GameplayScreen screen, int rows, int cols, float tubeSize, Vector3 start )
      : base( screen )
    {
      ContentManager content = screen.Content;
      effect = content.Load<Effect>( "Effects/basic" ).Clone( screen.ScreenManager.GraphicsDevice );

      tubeModels = new Model[4];
      tubeModels[(int)TubePattern.Elbow] = content.Load<Model>( "Models/tubeElbow" );
      tubeModels[(int)TubePattern.Cup]   = content.Load<Model>( "Models/tubeCup"   );
      tubeModels[(int)TubePattern.Tee]   = content.Load<Model>( "Models/tubeTee"   );
      tubeModels[(int)TubePattern.Cross] = content.Load<Model>( "Models/tubeCross" );

      this.rows = rows;
      this.cols = cols;
      tubes = new TubePiece[rows, cols];
      cupRow = ( rows % 2 == 1 );

      TubeSize = tubeSize;
      scaleMatrix = Matrix.CreateScale( tubeSize );

      topLeft = new Vector3( start.X - tubeSize * (float)cols / 2f + TubeSize / 2f, start.Y, start.Z );
      highestRow = 0;

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
            else if ( tubes[r, c - 1].RightOpen() )
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
      // delete rows
      // spawn rows

      while ( topLeft.Y > Screen.Camera.Position.Y + Screen.CameraInfo.DeathLine )
      {
        int lowestRow = highestRow;
        int secondLowestRow = ( highestRow + rows - 1 ) % rows;
        highestRow = ( highestRow + 1 ) % rows;
        topLeft.Y -= TubeSize;

        // re-generate row to fit with its new predecessor
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

      device.VertexDeclaration = new VertexDeclaration( device, VertexPositionNormalTexture.VertexElements );
      SetRenderState( device.RenderState );

      Vector3 position = topLeft;

      // draw each tube
      for ( int r = 0; r < rows; ++r )
      {
        position.X = topLeft.X;
        int row = ( highestRow + r ) % rows;

        for ( int c = 0; c < cols; ++c )
        {
          TubePiece tube = tubes[row, c];

          if ( tube.Alive )
          {
            foreach ( ModelMesh mesh in tubeModels[(int)tube.Pattern].Meshes )
            {
              foreach ( BasicEffect effect in mesh.Effects )
              {
                effect.EnableDefaultLighting();
                effect.World = scaleMatrix * rotations[tube.Rotation] * Matrix.CreateTranslation( position );
                effect.View = Screen.View;
                effect.Projection = Screen.Projection;
              }
              mesh.Draw();
            }
          }
          position.X += TubeSize;
        }
        position.Y -= TubeSize;
      }
    }

    private void SetRenderState( RenderState renderState )
    {
      renderState.CullMode = CullMode.CullCounterClockwiseFace;//CullMode.None;

      renderState.AlphaBlendEnable = false;//true;
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
            rotation = 2;
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
}