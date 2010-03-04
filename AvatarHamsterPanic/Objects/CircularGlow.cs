using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Menu;
using Microsoft.Xna.Framework.Content;
using Graphics;

namespace AvatarHamsterPanic.Objects
{
  class CircularGlow : GameObject
  {
    static Texture2D glowTexture;
    static Effect effect;
    static EffectParameter effectTransformParam;
    static VertexPositionColorTexture[] verts;
    static VertexDeclaration vertexDeclaration;
    static GraphicsDevice device;

    public Vector3 Position;
    public float Size;
    public Color Color;
    public Player Player;


    public Camera Camera { get; set; }

    public CircularGlow()
      : this( Vector3.Zero, Color.White, 1f )
    {
    }

    public CircularGlow( Vector3 position, Color color, float size )
      : base( GameplayScreen.Instance )
    {
      Position = position;
      Color = color;
      Size = size;

      if ( GameplayScreen.Instance != null )
        Camera = GameplayScreen.Instance.Camera;

      DrawOrder = 10;

      if ( glowTexture == null )
      {
        ContentManager content = GameplayScreen.Instance.Content;

        device = GameCore.Instance.GraphicsDevice;

        glowTexture = content.Load<Texture2D>( "Textures/playerGlow" );
        effect = content.Load<Effect>( "Effects/addColorEffect" ).Clone( device );
        effect.CurrentTechnique = effect.Techniques["World"];
        effect.Parameters["Texture"].SetValue( glowTexture );
        effectTransformParam = effect.Parameters["Transform"];


        verts = new VertexPositionColorTexture[]
        {
          new VertexPositionColorTexture( new Vector3(-.5f, .5f, 0f ), Color.White, new Vector2( 0, 0 ) ),
          new VertexPositionColorTexture( new Vector3( .5f, .5f, 0f ), Color.White, new Vector2( 1, 0 ) ),
          new VertexPositionColorTexture( new Vector3( .5f,-.5f, 0f ), Color.White, new Vector2( 1, 1 ) ),
          new VertexPositionColorTexture( new Vector3(-.5f,-.5f, 0f ), Color.White, new Vector2( 0, 1 ) ),
        };

        vertexDeclaration = new VertexDeclaration( device, VertexPositionColorTexture.VertexElements );
      }
    }

    public override void Update( GameTime gameTime )
    {
      if ( Player != null )
      {
        Size = Player.Size * Player.Scale;
        Position = new Vector3( Player.BoundingCircle.Position, 0 );
      }
    }

    public override void Draw()
    {
      Matrix scale = Matrix.CreateScale( Size * 1.286f );
      Matrix billboard = Matrix.CreateBillboard( Position, Camera.Position, Camera.Up, -Vector3.UnitZ );

      for ( int i = 0; i < 4; ++i )
        verts[i].Color = Color;

      device.VertexDeclaration = vertexDeclaration;
      device.RenderState.AlphaBlendEnable = true;
      device.RenderState.AlphaTestEnable = false;
      device.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
      device.RenderState.DepthBufferWriteEnable = false;
      device.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
      device.RenderState.CullMode = CullMode.None;

      GameplayScreen screen = GameplayScreen.Instance;
      effectTransformParam.SetValue( scale * billboard * screen.View * screen.Projection );

      effect.Begin();
      effect.CurrentTechnique.Passes[0].Begin();
      device.DrawUserPrimitives( PrimitiveType.TriangleFan, verts, 0, 2 );
      effect.CurrentTechnique.Passes[0].End();
      effect.End();

      device.RenderState.DepthBufferWriteEnable = true;
    }
  }
}