#region File Description
//-----------------------------------------------------------------------------
// CustomModelContent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#endregion

namespace CustomModelPipeline
{
  /// <summary>
  /// Content Pipeline class provides a design time equivalent of the runtime
  /// CustomModel class. This stores the output from the CustomModelProcessor,
  /// right before it gets written into the XNB binary. This class is similar
  /// in shape to the runtime CustomModel, but stores the data as simple managed
  /// objects rather than GPU data types. This avoids us having to instantiate
  /// any actual GPU objects during the Content Pipeline build process, which
  /// is essential when building graphics for Xbox. The build always runs on
  /// Windows, and it would be problematic if we tried to instantiate Xbox
  /// types on the Windows GPU during this process!
  /// </summary>
  public class CustomModelContent
  {
    // Internally our custom model is made up from a list of model parts.
    List<ModelPart> modelParts = new List<ModelPart>();


    // Each model part represents a piece of geometry that uses one single
    // effect. Multiple parts are needed to represent models that use more
    // than one effect.
    class ModelPart
    {
      public int TriangleCount;
      public int VertexCount;
      public int VertexStride;

      // These properties are not the same type as their equivalents in the
      // runtime CustomModel! Here, we are using design time managed classes,
      // while the runtime CustomModel uses actual GPU types. The Content
      // Pipeline knows about the relationship between the design time and
      // runtime types (thanks to the ContentTypeWriter.GetRuntimeType method),
      // so it can automatically translate one to the other. At design time
      // we can pass things  like VertexElement[], VertexBufferContent,
      // IndexCollection and MaterialContent when we call WriteObject, but
      // when we read this data back at runtime, we tell the ReadObject
      // method to load into the corresponding VertexDeclaration,
      // VertexBuffer, IndexBuffer, and Effect classes.
      public VertexElement[] VertexElements;
      public VertexBufferContent VertexBufferContent;
      public IndexCollection IndexCollection;
      public MaterialContent MaterialContent;
    }


    /// <summary>
    /// Helper function used by the CustomModelProcessor
    /// to add new ModelPart information.
    /// </summary>
    public void AddModelPart( int triangleCount, int vertexCount, int vertexStride,
                              VertexElement[] vertexElements,
                              VertexBufferContent vertexBufferContent,
                              IndexCollection indexCollection,
                              MaterialContent materialContent )
    {
      ModelPart modelPart = new ModelPart();

      modelPart.TriangleCount = triangleCount;
      modelPart.VertexCount = vertexCount;
      modelPart.VertexStride = vertexStride;
      modelPart.VertexElements = vertexElements;
      modelPart.VertexBufferContent = vertexBufferContent;
      modelPart.IndexCollection = indexCollection;
      modelPart.MaterialContent = materialContent;

      modelParts.Add( modelPart );
    }


    /// <summary>
    /// Saves custom model data into an XNB file.
    /// </summary>
    public void Write( ContentWriter output )
    {
      output.Write( modelParts.Count );

      foreach ( ModelPart modelPart in modelParts )
      {
        // Simple data types like integers can be written directly.
        output.Write( modelPart.TriangleCount );
        output.Write( modelPart.VertexCount );
        output.Write( modelPart.VertexStride );

        // These design time graphics types will be automatically translated
        // into actual GPU data types when they are loaded back in at runtime.
        output.WriteObject( modelPart.VertexElements );
        output.WriteObject( modelPart.VertexBufferContent );
        output.WriteObject( modelPart.IndexCollection );

        // A single material instance may be shared by more than one ModelPart,
        // in which case we only want to write a single copy of the material
        // data into the XNB file. The WriteSharedResource method takes care
        // of this merging for us.
        output.WriteSharedResource( modelPart.MaterialContent );
      }
    }
  }
}
