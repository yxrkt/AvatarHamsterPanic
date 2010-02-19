using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathLibrary;

namespace Menu
{
  // this class defines an entry for the wheel menu. it's pretty fucked up, because
  // first of all, we're using an image instead of text. Secondly, these aren't drawn
  // on nice, pretty screen aligned quads. In fact these are drawn on fucked up things
  // that bend to align themselves around the outer edge of the wheel.
  //
  // In the constructor, we need to generate the vertex positions and texture coordinates
  // for the entry, given the wheel's radius and an additional offset (we assume the 
  // position is 0, and when we draw we take the wheel's position and rotaiton into account).
  //
  // The entry will also have a single spring indicating how squashed it is (0 being 
  // hiding in the rim of the wheel, and 1 being fully extended). The best way to handle 
  // this would be to have a Vector2 array that acts as a buffer for 
  // GraphicsDevice.DrawUserPrimitives(). This buffer would get filled by a convenient 
  // function, ComputeCurrentVertices().
  //
  // Lastly, the entry needs to scale up somehow when it's the active entry. The best way
  // to do this would probably be to precompute the scaled-up vertices along with the 
  // regular ones in the constructor. A single spring could then be used to lerp between
  // the two.
  //
  // We will also need a float for the current angle, as well as a bool indicating 
  // whether or not the entry is active.

  class WheelMenuEntry
  {
    WheelMenu wheel; // the parent wheel

    VertexPositionNormalTexture[] idleVerts;    // vertices used when entry is idle
    VertexPositionNormalTexture[] activeVerts;  // vertices used when entry is the active entry
    VertexPositionNormalTexture[] vertexBuffer; // vertices used for drawing
    VertexDeclaration vertexDeclaration;  // the vertex declaration for drawing

    int segments;

    SpringInterpolater extendedSpring;  // 0 means collapsed, 1 means fully extended
    SpringInterpolater growSpring;      // 0 means idle position, 1 means active position

    Texture2D texture;

    public event EventHandler<PlayerIndexEventArgs> Selected;
    public void OnSelect( PlayerIndex playerIndex )
    {
      if ( Selected != null )
        Selected( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    public float Angle { get; set; }
    public bool Active
    {
      get { return growSpring.GetDest()[0] != 0; }
      set { growSpring.SetDest( value ? 1 : 0 ); }
    }
    public bool Collapsed
    {
      get { return extendedSpring.GetDest()[0] == 0; }
      set { extendedSpring.SetDest( value ? 0 : 1 ); }
    }

    public WheelMenuEntry( WheelMenu wheel, Texture2D texture )
    {
      this.wheel = wheel;
      this.texture = texture;

      segments = 10;

      extendedSpring = new SpringInterpolater( 1, 800, SpringInterpolater.GetCriticalDamping( 800 ) );
      extendedSpring.SetSource( 0 );
      extendedSpring.SetDest( 0 );
      extendedSpring.Active = true;

      growSpring = new SpringInterpolater( 1, 700, .25f * SpringInterpolater.GetCriticalDamping( 700 ) );
      growSpring.SetSource( 0 );
      growSpring.SetDest( 0 );
      growSpring.Active = true;

      float height = WheelMenu.EntryIdleSize;
      float width = height * (float)texture.Width / (float)texture.Height;
      GenerateVerts( width, height, segments, out idleVerts );
      GenerateVerts( width * WheelMenu.EntryActiveScale, height * WheelMenu.EntryActiveScale, segments, out activeVerts );

      vertexBuffer = new VertexPositionNormalTexture[( segments + 1 ) * 2];
      vertexDeclaration = new VertexDeclaration( wheel.Screen.ScreenManager.GraphicsDevice,
                                                 VertexPositionNormalTexture.VertexElements );
    }

    private void GenerateVerts( float width, float height, int segments, out VertexPositionNormalTexture[] verts )
    {
      float angle = ( width / 2 ) / wheel.Radius;
      Vector2 direction = new Vector2( -(float)Math.Sin( angle ), (float)Math.Cos( angle ) );
      Matrix rotate = Matrix.CreateRotationZ( -2 * angle / segments );
      verts = new VertexPositionNormalTexture[( segments + 1 ) * 2];
      int v = 0;
      for ( int i = 0; i <= segments; ++i )
      {
        float xTexCoord = (float)i / (float)segments;
        verts[v].Position = new Vector3( direction * ( wheel.Radius + WheelMenu.EntryOffset ), .25f );
        verts[v++].TextureCoordinate = new Vector2( xTexCoord, 1 );
        verts[v].Position = new Vector3( direction * ( wheel.Radius + WheelMenu.EntryOffset + height ), .25f );
        verts[v++].TextureCoordinate = new Vector2( xTexCoord, 0 );
        Vector2.Transform( ref direction, ref rotate, out direction );
      }
    }

    private void SetVertexBuffer()
    {
      float grow = growSpring.GetSource()[0];
      float extend = extendedSpring.GetSource()[0];

      int nVerts = vertexBuffer.Length;
      for ( int i = 0; i < nVerts; ++i )
      {
        vertexBuffer[i].Position = Vector3.Lerp( idleVerts[i].Position, activeVerts[i].Position, grow );
        vertexBuffer[i].TextureCoordinate = Vector2.Lerp( idleVerts[i].TextureCoordinate, 
                                                          activeVerts[i].TextureCoordinate, grow );
      }

      int v = 0;
      for ( int i = 0; i <= segments; ++i )
      {
        Vector3 rimPosition = Vector3.Normalize( vertexBuffer[v].Position ) * wheel.Radius;
        vertexBuffer[v].Position = Vector3.Lerp( rimPosition, vertexBuffer[v].Position, extend );
        v++;
        vertexBuffer[v].Position = Vector3.Lerp( rimPosition, vertexBuffer[v].Position, extend );
        v++;
      }
    }

    public void Update( float elapsed )
    {
      if ( elapsed > 1f / 60f )
        elapsed = 1f / 60f;
      growSpring.Update( elapsed );
      extendedSpring.Update( elapsed );
    }

    public void Draw()
    {
      GraphicsDevice device = wheel.Screen.ScreenManager.GraphicsDevice;
      device.VertexDeclaration = vertexDeclaration;
      device.RenderState.CullMode = CullMode.None;

      SetVertexBuffer();

      Effect effect = wheel.EntryEffect;

      wheel.EntryDiffuseEffectParameter.SetValue( texture );

      effect.Begin();
      EffectPassCollection passes = effect.CurrentTechnique.Passes;
      for ( int i = 0; i < passes.Count; ++i )
      {
        EffectPass pass = passes[i];

        pass.Begin();
        device.DrawUserPrimitives( PrimitiveType.TriangleStrip, vertexBuffer, 0, vertexBuffer.Length - 2 );
        pass.End();
      }
      effect.End();
    }
  }
}