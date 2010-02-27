#region File Description
//-----------------------------------------------------------------------------
// CustomModel.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic.Objects;
using System.Diagnostics;
using Graphics;
#endregion

namespace CustomModelSample
{
  /// <summary>
  /// Custom class that can be used as a replacement for the built-in Model type.
  /// This provides functionality roughly similar to Model, but simplified as far
  /// as possible while still being able to correctly render data from arbitrary
  /// X or FBX files. This can be used as a starting point for building up your
  /// own more sophisticated Model replacements.
  /// </summary>
  public class CustomModel
  {
    // Internally our custom model is made up from a list of model parts.
    List<ModelPart> modelParts = new List<ModelPart>();
    public List<ModelPart> ModelParts { get { return modelParts; } }


    // Each model part represents a piece of geometry that uses one
    // single effect. Multiple parts are needed for models that use
    // more than one effect.
    public class ModelPart
    {
      public int TriangleCount;
      public int VertexCount;
      public int VertexStride;

      public VertexDeclaration VertexDeclaration;
      public VertexBuffer VertexBuffer;
      public IndexBuffer IndexBuffer;

      public Effect Effect;
      public EffectParameter EffectParamWorld;
      public EffectParameter EffectParamView;
      public EffectParameter EffectParamProjection;
      public EffectParameter EffectParamEye;
      public EffectParameter EffectParamColor;
    }


    /// <summary>
    /// The constructor reads model data from our custom XNB format.
    /// This is called by the CustomModelReader class, which is invoked
    /// whenever you ask the ContentManager to read a CustomModel object.
    /// </summary>
    internal CustomModel( ContentReader input )
    {
      int partCount = input.ReadInt32();

      for ( int i = 0; i < partCount; i++ )
      {
        ModelPart modelPart = new ModelPart();

        // Simple data types like integers can be read directly.
        modelPart.TriangleCount = input.ReadInt32();
        modelPart.VertexCount = input.ReadInt32();
        modelPart.VertexStride = input.ReadInt32();

        // These XNA Framework types can be read using the ReadObject method,
        // which calls into the appropriate ContentTypeReader for each type.
        // The framework provides built-in ContentTypeReader implementations
        // for important types such as vertex declarations, vertex buffers,
        // index buffers, effects, and textures.
        modelPart.VertexDeclaration = input.ReadObject<VertexDeclaration>();
        modelPart.VertexBuffer = input.ReadObject<VertexBuffer>();
        modelPart.IndexBuffer = input.ReadObject<IndexBuffer>();

        // Shared resources have to be read in a special way. Because the same
        // object can be referenced from many different parts of the file, the
        // actual object data is stored at the end of the XNB binary. When we
        // call ReadSharedResource we are just reading an ID that will later be
        // used to locate the actual data, so ReadSharedResource is unable to
        // directly return the shared instance. Instead, it takes in a delegate
        // parameter, and will call us back as soon as the shared value becomes
        // available. We use C# anonymous delegate syntax to store the value
        // into its final location.
        input.ReadSharedResource<Effect>( delegate( Effect effect )
        {
          //Effect effect = value.Clone( value.GraphicsDevice );
          if ( effect.Parameters["DiffuseMap"].GetValueTexture2D() != null )
          {
            if ( effect.Parameters["NormalMap"].GetValueTexture2D() != null )
            {
              effect.CurrentTechnique = effect.Techniques["NormalDiffuseColor"];
              modelPart.VertexDeclaration = new VertexDeclaration( effect.GraphicsDevice,
                                                                   VertexPositionNormalTextureTangentBinormal.VertexElements );
            }
            else
            {
              effect.CurrentTechnique = effect.Techniques["DiffuseColor"];
              modelPart.VertexDeclaration = new VertexDeclaration( effect.GraphicsDevice,
                                                                   VertexPositionNormalTexture.VertexElements );
            }
          }
          else
          {
            effect.CurrentTechnique = effect.Techniques["ColorDefault"];
            modelPart.VertexDeclaration = new VertexDeclaration( effect.GraphicsDevice,
                                                                 VertexPositionNormalColor.VertexElements );
          }
          modelPart.Effect = effect;
          modelPart.EffectParamWorld = modelPart.Effect.Parameters["World"];
          modelPart.EffectParamView = modelPart.Effect.Parameters["View"];
          modelPart.EffectParamProjection = modelPart.Effect.Parameters["Projection"];
          modelPart.EffectParamEye = modelPart.Effect.Parameters["Eye"];
          modelPart.EffectParamColor = modelPart.Effect.Parameters["Color"];
        } );

        modelParts.Add( modelPart );
      }
    }


    /// <summary>
    /// Draws the model using the specified camera matrices.
    /// </summary>
    public void Draw( Vector3 eye, Matrix world, Matrix view, Matrix projection )
    {
      foreach ( ModelPart part in modelParts )
      {
        part.EffectParamWorld.SetValue( world );
        part.EffectParamView.SetValue( view );
        part.EffectParamProjection.SetValue( projection );
        part.EffectParamEye.SetValue( eye );

        GraphicsDevice device = part.Effect.GraphicsDevice;

        device.VertexDeclaration = part.VertexDeclaration;

        device.Vertices[0].SetSource( part.VertexBuffer, 0, part.VertexStride );

        device.Indices = part.IndexBuffer;

        part.Effect.Begin();

        EffectPassCollection passes = part.Effect.CurrentTechnique.Passes;
        int nPasses = passes.Count;
        for ( int i = 0; i < nPasses; ++i )
        {
          EffectPass pass = passes[i];
          pass.Begin();
          device.DrawIndexedPrimitives( PrimitiveType.TriangleList, 0, 0,
                                        part.VertexCount, 0, part.TriangleCount );
          pass.End();
        }

        part.Effect.End();
      }
    }
  }
}
