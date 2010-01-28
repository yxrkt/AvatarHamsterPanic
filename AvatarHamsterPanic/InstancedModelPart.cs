#region File Description
//-----------------------------------------------------------------------------
// InstancedModelPart.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
#endregion

namespace InstancedModelSample
{
  /// <summary>
  /// Internal worker class implements most of the functionality of the public
  /// InstancedModel type. An InstancedModel is basically just a collection of
  /// model parts, each one of which can have a different material. These model
  /// parts are responsible for all the heavy lifting of drawing themselves
  /// using the various different instancing techniques.
  /// </summary>
  internal class InstancedModelPart
  {
    #region Constants


    // This must match the constant at the top of Instanced.fx!
    const int MaxShaderMatrices = 59;

    const int SizeOfVector4 = sizeof( float ) * 4;
    const int SizeOfMatrix = sizeof( float ) * 16;


    #endregion

    #region Fields


    // Model data.
    int indexCount;
    int vertexCount;
    int vertexStride;

    VertexDeclaration vertexDeclaration;
    VertexBuffer vertexBuffer;
    IndexBuffer indexBuffer;

    Effect effect;
    public Effect Effect { get { return effect; } }

    public EffectParameter EffectParameterColor { get; private set; }
    public EffectParameter EffectParameterDiffuseMap { get; private set; }

    // Track whether effect.CurrentTechnique is dirty.
    bool techniqueChanged;


    // Track which graphics device we are using.
    GraphicsDevice graphicsDevice;


    // The maximum number of instances we can draw in a single batch using the
    // VFetch or ShaderInstancing techniques depends not only on the global
    // MaxShaderMatrices constant, but also on how many times we can replicate
    // the index data before overflowing range of the 16 bit index values.
    int maxInstances;


    // Array of temporary matrices for the VFetch and ShaderInstancing techniques.
    Matrix[] tempMatrices = new Matrix[MaxShaderMatrices];

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor reads instanced model data from our custom XNB format.
    /// </summary>
    internal InstancedModelPart( ContentReader input, GraphicsDevice graphicsDevice )
    {
      this.graphicsDevice = graphicsDevice;

      // Load the model data.
      indexCount = input.ReadInt32();
      vertexCount = input.ReadInt32();
      vertexStride = input.ReadInt32();

      vertexDeclaration = input.ReadObject<VertexDeclaration>();
      vertexBuffer = input.ReadObject<VertexBuffer>();
      indexBuffer = input.ReadObject<IndexBuffer>();

      input.ReadSharedResource<Effect>( delegate( Effect value )
      {
        effect = value;

        // convenience stuff
        EffectParameterColor = effect.Parameters["Color"];
        EffectParameterDiffuseMap = effect.Parameters["DiffuseMap"];
      } );

      // Work out how many shader instances we can fit into a single batch.
      int indexOverflowLimit = ushort.MaxValue / vertexCount;

      maxInstances = Math.Min( indexOverflowLimit, MaxShaderMatrices );

      // On Xbox, we must replicate several copies of our index buffer data for
      // the VFetch instancing technique. We could alternatively precompute this
      // in the content processor, but that would bloat the size of the XNB file.
      // It is more efficient to generate the repeated values at load time.
      //
      // We also require replicated index data for the Windows ShaderInstancing
      // technique, but this is computed lazily on Windows, so as to avoid
      // bloating the index buffer if it turns out that we only ever use the
      // HardwareInstancingTechnique (which does not require any repeated data).

      ReplicateIndexData();
    }


    /// <summary>
    /// Initializes a model part to use the specified instancing
    /// technique. This is called once at startup, and then again
    /// whenever the instancing technique is changed.
    /// </summary>
    internal void Initialize( InstancingTechnique instancingTechnique )
    {
      techniqueChanged = true;
    }


    /// <summary>
    /// In preparation for using the VFetch or ShaderInstancing techniques,
    /// replicates the index buffer data several times, offseting the values
    /// for each copy of the data.
    /// </summary>
    void ReplicateIndexData()
    {
      // Read the existing index data, then destroy the existing index buffer.
      ushort[] oldIndices = new ushort[indexCount];

      indexBuffer.GetData( oldIndices );
      indexBuffer.Dispose();

      // Allocate a temporary array to hold the replicated index data.
      ushort[] newIndices = new ushort[indexCount * maxInstances];

      int outputPosition = 0;

      // Replicate one copy of the original index buffer for each instance.
      for ( int instanceIndex = 0; instanceIndex < maxInstances; instanceIndex++ )
      {
        int instanceOffset = instanceIndex * vertexCount;

        for ( int i = 0; i < indexCount; i++ )
        {
          newIndices[outputPosition] = (ushort)( oldIndices[i] +
                                                instanceOffset );

          outputPosition++;
        }
      }

      // Create a new index buffer, and set the replicated data into it.
      indexBuffer = new IndexBuffer( graphicsDevice,
                                    sizeof( ushort ) * newIndices.Length,
                                    BufferUsage.None,
                                    IndexElementSize.SixteenBits );

      indexBuffer.SetData( newIndices );
    }


    #endregion

    #region Draw


    /// <summary>
    /// Draws a batch of instanced model geometry,
    /// using the specified technique and camera matrices.
    /// </summary>
    public void Draw( InstancingTechnique instancingTechnique, Matrix[] instanceTransforms,
                      int nTransforms, Matrix view, Matrix projection, Vector3 eye )
    {
      SetRenderStates( instancingTechnique, view, projection, eye );

      // Begin the effect, then loop over all the effect passes.
      effect.Begin();

      EffectPassCollection passes = effect.CurrentTechnique.Passes;
      int nPasses = passes.Count;
      for ( int i = 0; i < nPasses; ++i )
      {
        EffectPass pass = passes[i];
        pass.Begin();

        DrawShaderInstancing( instanceTransforms, nTransforms );

        pass.End();
      }

      effect.End();
    }


    /// <summary>
    /// Helper function sets up the graphics device and
    /// effect ready for drawing instanced geometry.
    /// </summary>
    void SetRenderStates( InstancingTechnique instancingTechnique,
                          Matrix view, Matrix projection, Vector3 eye )
    {
      // Set the graphics device to use our vertex data.
      graphicsDevice.VertexDeclaration = vertexDeclaration;
      graphicsDevice.Vertices[0].SetSource( vertexBuffer, 0, vertexStride );
      graphicsDevice.Indices = indexBuffer;

      // Make sure our effect is set to use the right technique.
      if ( techniqueChanged )
      {
        string techniqueName = instancingTechnique.ToString();
        effect.CurrentTechnique = effect.Techniques[techniqueName];
        techniqueChanged = false;
      }

      // Pass camera matrices through to the effect.
      effect.Parameters["View"].SetValue( view );
      effect.Parameters["Projection"].SetValue( projection );
      effect.Parameters["Eye"].SetValue( eye );

      // Set the vertex count (used by the VFetch instancing technique).
      effect.Parameters["VertexCount"].SetValue( vertexCount );
    }


    /// <summary>
    /// Draws instanced geometry using the VFetch or ShaderInstancing techniques.
    /// </summary>
    void DrawShaderInstancing( Matrix[] instanceTransforms, int nTransforms )
    {
      // We can only fit maxInstances into a single call. If asked to draw
      // more than that, we must split them up into several smaller batches.
      for ( int i = 0; i < nTransforms; i += maxInstances )
      {
        // How many instances can we fit into this batch?
        int instanceCount = nTransforms - i;

        if ( instanceCount > maxInstances )
          instanceCount = maxInstances;

        // Upload transform matrices as shader constants.
        Array.Copy( instanceTransforms, i, tempMatrices, 0, instanceCount );

        effect.Parameters["InstanceTransforms"].SetValue( tempMatrices );
        effect.CommitChanges();

        // Draw maxInstances copies of our geometry in a single batch.
        graphicsDevice.DrawIndexedPrimitives( PrimitiveType.TriangleList,
                                              0, 0, instanceCount * vertexCount,
                                              0, instanceCount * indexCount / 3 );
      }
    }

    #endregion
  }
}
