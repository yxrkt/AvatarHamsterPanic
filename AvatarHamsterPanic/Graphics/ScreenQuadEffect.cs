using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics
{
  enum ScreenQuadEffectTechnique
  {
    Color,
    Texture,
  }

  class ScreenQuadEffect : Effect
  {
    static public ScreenQuadEffect CreateScreenQuadEffect( GraphicsDevice device, ContentManager content )
    {
      return new ScreenQuadEffect( device, content.Load<Effect>( "Effects/screenAlignedEffect" ) );
    }

    public ScreenQuadEffectTechnique Technique
    {
      get
      {
        if ( CurrentTechnique == colorTechnique )
          return ScreenQuadEffectTechnique.Color;
        else
          return ScreenQuadEffectTechnique.Texture;
      }
      set
      {
        if ( value == ScreenQuadEffectTechnique.Color )
          CurrentTechnique = colorTechnique;
        else
          CurrentTechnique = textureTechnique;
      }
    }

    public Color Color
    {
      get { return new Color( colorParameter.GetValueVector4() ); }
      set { colorParameter.SetValue( value.ToVector4() ); }
    }

    public Texture2D Texture
    {
      get { return textureParameter.GetValueTexture2D(); }
      set { textureParameter.SetValue( value ); }
    }

    public VertexDeclaration ColorVertexDeclaration { get { return colorVertexDeclaration; } }
    public VertexDeclaration TextureVertexDeclaration { get { return textureVertexDeclaration; } }

    EffectTechnique colorTechnique;
    EffectTechnique textureTechnique;
    EffectParameter colorParameter;
    EffectParameter textureParameter;

    VertexDeclaration colorVertexDeclaration;
    VertexDeclaration textureVertexDeclaration;

    private ScreenQuadEffect( GraphicsDevice device, Effect effect )
      : base( device, effect )
    {
      Parameters["ScreenWidth"].SetValue( device.Viewport.Width );
      Parameters["ScreenHeight"].SetValue( device.Viewport.Height );

      colorTechnique = Techniques["Color"];
      textureTechnique = Techniques["Texture"];
      CurrentTechnique = colorTechnique;

      colorParameter = Parameters["Tint"];
      textureParameter = Parameters["Texture"];

      Color = Color.White;

      colorVertexDeclaration = new VertexDeclaration( device, VertexPositionColor.VertexElements );
      textureVertexDeclaration = new VertexDeclaration( device, VertexPositionTexture.VertexElements );
    }
  }
}