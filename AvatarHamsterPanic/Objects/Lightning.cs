using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Utilities;
using Menu;

namespace AvatarHamsterPanic.Objects
{
  class Lightning : GameObject
  {
    List<Vector3> verts;
    List<Vector3> velocities;
    int subdivisions;
    Random random;
    Vector3 start, end;
    Player player1, player2;
    //int counter;

    VertexDeclaration vertexDeclaration;
    VertexPositionColor[] drawVerts;

    static Effect lineEffect;
    static EffectParameter lineTransformParam;

    public Lightning( int subdivisions, Vector3 p0, Vector3 p1 )
      : base( GameplayScreen.Instance )
    {
      if ( subdivisions < 0 )
        throw new InvalidOperationException( "subdivisions cannot be less than zero" );

      DrawOrder = 9;

      this.subdivisions = subdivisions;

      if ( lineEffect == null )
      {
        lineEffect = GameCore.Instance.Content.Load<Effect>( "Effects/lineEffect" );
        lineEffect.CurrentTechnique = lineEffect.Techniques[0];
        lineTransformParam = lineEffect.Parameters["WorldViewProjection"];
      }

      int nVerts = ( 1 << subdivisions ) + 1;
      drawVerts = new VertexPositionColor[nVerts];
      for ( int i = 0; i < nVerts; ++i )
        drawVerts[i].Color = Color.Turquoise;

      start = p0;
      end = p1;

      velocities = new List<Vector3>( nVerts );
      verts = new List<Vector3>( nVerts );
      verts.Add( p0 );
      verts.Add( p1 );

      random = new Random();

      vertexDeclaration = new VertexDeclaration( Screen.ScreenManager.GraphicsDevice,
                                                 VertexPositionColor.VertexElements );

      Generate();
    }

    public void LinkToPlayers( Player player1, Player player2 )
    {
      this.player1 = player1;
      this.player2 = player2;
    }

    private void Generate()
    {
      verts.Clear();

      verts.Add( start );
      verts.Add( end );

      for ( int i = 0; i < subdivisions; ++i )
        Subdivide();

      //// velocities
      //float offsetMin = .5f;
      //float offsetMax = .5f;

      //for ( int i = 1; i < verts.Count - 1; ++i )
      //{
      //  float distance = Vector3.Distance( verts[i], verts[i + 1] );
      //  velocities.Add( Vector3.Normalize( random.NextVector3() ) * random.NextFloat( offsetMin, offsetMax ) * distance );
      //}
    }

    private void Subdivide()
    {
      float offsetMin = .05f;
      float offsetMax = .15f;

      for ( int i = verts.Count - 2; i >= 0; --i )
      {
        float distance = Vector3.Distance( verts[i], verts[i + 1] );

        /*/
        Vector3 offset = Vector3.Normalize( random.NextVector3() );
        /*/
        Vector3 offset = Vector3.Normalize( Vector3.Cross( random.NextVector3(), verts[i] - verts[i + 1] ) );
        /**/

        Vector3 newVert = Vector3.Lerp( verts[i], verts[i + 1], .5f );
        newVert += distance * offset * random.NextFloat( offsetMin, offsetMax );
        verts.Insert( i + 1, newVert );

        if ( i != 0 )
          velocities.Insert( 0, -distance * offset * offsetMax );
        else
          velocities.Insert( 0, Vector3.Zero );
      }
    }

    public override void Update( GameTime gameTime )
    {
      // update positions based on players
      if ( player1 != null && player2 != null )
      {
        start = new Vector3( player1.BoundingCircle.Position, 0 );
        end = new Vector3( player2.BoundingCircle.Position, 0 );
      }

      Generate();

      //if ( counter++ % 4 == 0 )
      //  Generate();

      //for ( int i = 0; i < velocities.Count; ++i )
      //  verts[i] += velocities[i] * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw()
    {
      GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;

      device.VertexDeclaration = vertexDeclaration;

      int i = 0;
      foreach ( Vector3 vert in verts )
        drawVerts[i++].Position = vert;

      lineTransformParam.SetValue( Screen.View * Screen.Projection );
      lineEffect.Begin();
      lineEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.LineStrip, drawVerts, 0, drawVerts.Length - 1 );
      lineEffect.CurrentTechnique.Passes[0].End();
      lineEffect.End();
    }
  }
}