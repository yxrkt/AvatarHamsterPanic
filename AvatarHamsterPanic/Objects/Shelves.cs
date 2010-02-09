using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InstancedModelSample;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Physics;
using MathLibrary;
using Menu;

namespace AvatarHamsterPanic.Objects
{
  public class Shelves : GameObject
  {
    const int nCagesPerBox = 3;
    const int nMaxPlayers = 4;
    const float offsetFromWall = .5f;
    const float size = 2.3f;


    static readonly Matrix rotateL = new Matrix( 0, 0,-1, 0,
                                                 0, 1, 0, 0,
                                                 1, 0, 0, 0,
                                                 0, 0, 0, 1 );

    static readonly Matrix rotateR = new Matrix( 0, 0, 1, 0,
                                                 0, 1, 0, 0,
                                                -1, 0, 0, 0,
                                                 0, 0, 0, 1 );

    static readonly Matrix bottomStart;

    static readonly Matrix scale = Matrix.CreateScale( size );

    readonly InstancedModel cageModel;
    readonly float deathLine;

    readonly Boundary boundary;
    readonly float topLine;

    CagePiece[] cagePieces;
    SpringInterpolater angleSpring;
    float spacing;

    static Shelves()
    {
      bottomStart = rotateL;
      bottomStart *= new Matrix( 0, 1, 0, 0,
                                -1, 0, 0, 0,
                                 0, 0, 1, 0,
                                 0, 0, 0, 1 );
      bottomStart *= Matrix.CreateTranslation( size / 2f, 0f, 0f );
    }

    public Shelves( GameplayScreen screen )
      : base( screen )
    {
      int nCages = nMaxPlayers * nCagesPerBox;
      cagePieces = new CagePiece[nCages];

      for ( int i = 0; i < nCages; ++i )
        cagePieces[i] = new CagePiece();

      angleSpring = new SpringInterpolater( 1, 50, .5f * SpringInterpolater.GetCriticalDamping( 50 ) );

      cageModel = screen.Content.Load<InstancedModel>( "Models/cage" );

      // determine transforms for each piece
      boundary = screen.ObjectTable.GetObjects<Boundary>()[0];
      if ( boundary == null )
        throw new InvalidOperationException( "boundary must be initialized before shelf" );
      float totalLength = boundary.Right - boundary.Left - size - 2f * offsetFromWall;
      spacing = totalLength / 3f;

      Camera camera = Screen.Camera;
      
      float tanFovOver2 = (float)Math.Tan( camera.Fov / 2f );
      float depth = camera.Position.Z + size / 2f;
      float height = depth * tanFovOver2;
      topLine = height - size / 2f;

      depth = camera.Position.Z - size / 2f;
      height = depth * tanFovOver2;
      deathLine = Screen.Camera.Position.Y - height;

      // cage bottoms stored in first four indices
      for ( int i = 0; i < nMaxPlayers; ++i )
      {
        Vector3 pos = new Vector3( boundary.Left + offsetFromWall + i * spacing, topLine - size / 2f, 0f );
        cagePieces[i].Translation = Matrix.CreateTranslation( pos );
        cagePieces[i].Rotation = bottomStart;
        cagePieces[i].Transform = scale * bottomStart * cagePieces[i].Translation;
        cagePieces[i].Body = new PhysPolygon( size, .014f * size, new Vector2( pos.X + size / 2f, pos.Y ), 1f );
        cagePieces[i].Body.SetPivotPoint( new Vector2( -size / 2, 0f ) );
        cagePieces[i].Body.Flags = BodyFlags.Anchored;
        Screen.PhysicsSpace.AddBody( cagePieces[i].Body );
      }

      // all other cage pieces won't change
      for ( int i = nMaxPlayers; i < nCages; ++i )
      {
        int box = ( i - nMaxPlayers ) / 2;
        int side = i % 2;

        float x = boundary.Left + offsetFromWall + box * spacing + side * size;
        Vector3 pos = new Vector3( x, topLine, 0f );
        Matrix translation = Matrix.CreateTranslation( pos );
        Matrix rotation = side == 0 ? rotateL : rotateR;
        cagePieces[i].Translation = translation;
        cagePieces[i].Rotation = rotation;
        cagePieces[i].Transform = scale * rotation * translation;
        cagePieces[i].Body = new PhysPolygon( .014f, size, new Vector2( pos.X, pos.Y ), 1f );
        cagePieces[i].Body.Flags = BodyFlags.Anchored;
        Screen.PhysicsSpace.AddBody( cagePieces[i].Body );
      }
    }

    public Vector2 GetPlayerPos( int playerNumber )
    {
      float x = boundary.Left + offsetFromWall + size / 2f + playerNumber * spacing;
      return new Vector2( x, topLine );
    }

    public override void Update( GameTime gameTime )
    {
      // kill self if past death line
      if ( Screen.Camera.Position.Y < deathLine )
      {
        foreach ( CagePiece piece in cagePieces )
          Screen.PhysicsSpace.RemoveBody( piece.Body );
        Screen.ObjectTable.MoveToTrash( this );
        return;
      }

      // swing open when countdown is over
      if ( Screen.CountdownTime > Screen.CountdownEnd && !angleSpring.Active )
      {
        angleSpring.SetDest( -MathHelper.PiOver2 );
        angleSpring.Active = true;
      }
      if ( angleSpring.Active )
      {
        float angle = angleSpring.GetSource()[0];
        Matrix rotation = Matrix.CreateRotationZ( angle );
        for ( int i = 0; i < nMaxPlayers; ++i )
        {
          CagePiece piece = cagePieces[i];
          piece.Transform = scale * piece.Rotation * rotation * piece.Translation;
          piece.Body.Angle = angle;
        }
      }

      angleSpring.Update( (float)gameTime.ElapsedGameTime.TotalSeconds );
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
      RenderState renderState = device.RenderState;

      foreach ( CagePiece piece in cagePieces )
        cageModel.AddInstance( piece.Transform );

      // draw is called in the boundary
    }
  }

  class CagePiece
  {
    public Matrix Translation;
    public Matrix Rotation;
    public Matrix Transform;
    public PhysPolygon Body;
  }
}