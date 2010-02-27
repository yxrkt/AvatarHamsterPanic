using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace AvatarHamsterPanic.Utilities
{
  public static class ScreenRects
  {
    static Effect screenEffect;
    static GraphicsDevice device;
    static VertexDeclaration vertexDeclaration;
    static Rectangle fourByThree;
    static VertexPositionColor[] fourByThreeVerts;

    public static Rectangle FourByThree { get { return fourByThree; } }

    public static void Initialize( Game game )
    {
      device = game.GraphicsDevice;

      ContentManager content = game.Content;
      screenEffect = content.Load<Effect>( "Effects/screenAlignedEffect" ).Clone( device );
      screenEffect.CurrentTechnique = screenEffect.Techniques[0];
      screenEffect.Parameters["ScreenWidth"].SetValue( device.Viewport.Width );
      screenEffect.Parameters["ScreenHeight"].SetValue( device.Viewport.Height );

      InitializeFourByThree();
    }

    private static void InitializeFourByThree()
    {
      // Initialize the 4x3 rectangle
      int width = device.Viewport.Width;
      int height = device.Viewport.Height;
      int x = 0;
      int y = 0;

      if ( device.Viewport.AspectRatio > 4f / 3f )
      {
        height = device.Viewport.Height;
        width = (int)( (float)height * ( 4f / 3f ) + .5f );
        y = 0;
        x = ( device.Viewport.Width - width ) / 2;
      }
      else if ( device.Viewport.AspectRatio < 4f / 3f )
      {
        width = device.Viewport.Width;
        height = (int)( (float)width / device.Viewport.AspectRatio + .5f );
        x = 0;
        y = ( device.Viewport.Height - height ) / 2;
      }

      fourByThree = new Rectangle( x, y, width, height );

      // Initialize vertices and vertex declaration
      fourByThreeVerts = new VertexPositionColor[]
      {
        new VertexPositionColor( new Vector3( fourByThree.X, fourByThree.Y, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( fourByThree.X, fourByThree.Y + fourByThree.Height - 1, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( fourByThree.X + fourByThree.Width, fourByThree.Y + fourByThree.Height - 1, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( fourByThree.X + fourByThree.Width, fourByThree.Y, 0 ), Color.Red ),
        new VertexPositionColor( new Vector3( fourByThree.X, fourByThree.Y, 0 ), Color.Red ),
      };

      vertexDeclaration = new VertexDeclaration( device, VertexPositionColor.VertexElements );
    }

    public static void DrawFourByThreeRect()
    {
      if ( device == null )
        throw new InvalidOperationException( "Initialize must be called before using ScreenDebug" );

      device.VertexDeclaration = vertexDeclaration;

      screenEffect.Begin();
      screenEffect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.LineStrip, fourByThreeVerts, 0, 4 );
      screenEffect.CurrentTechnique.Passes[0].End();
      screenEffect.End();
    }
  }
}