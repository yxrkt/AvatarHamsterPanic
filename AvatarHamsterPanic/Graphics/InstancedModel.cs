#region File Description
//-----------------------------------------------------------------------------
// InstancedModel.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
#endregion

namespace InstancedModelSample
{
  /// <summary>
  /// Enum describes the various possible techniques
  /// that can be chosen to implement instancing.
  /// </summary>
  public enum InstancingTechnique
  {
    ColorDefault,
    Color,
    DiffuseColor,
    NormalDiffuseColor,
  }


  /// <summary>
  /// Custom model class can efficiently draw many copies of itself,
  /// using various different GPU instancing techniques.
  /// </summary>
  public class InstancedModel
  {
    #region Fields


    // Internally our custom model is made up from a list of model parts.
    // Most of the interesting code lives in the InstancedModelPart class.
    List<InstancedModelPart> modelParts = new List<InstancedModelPart>();
    ReadOnlyCollection<InstancedModelPart> readOnlyParts;
    internal ReadOnlyCollection<InstancedModelPart> ModelParts { get { return readOnlyParts; } }

    TransformArray instanceTransforms = new TransformArray( 100 );

    // Keep track of what graphics device we are using.
    GraphicsDevice graphicsDevice;


    #endregion

    #region Initialization


    /// <summary>
    /// Constructor reads instanced model data from our custom XNB format.
    /// </summary>
    internal InstancedModel( ContentReader input )
    {
      // Look up our graphics device.
      graphicsDevice = GetGraphicsDevice( input );

      readOnlyParts = new ReadOnlyCollection<InstancedModelPart>( modelParts );

      // Load the model data.
      int partCount = input.ReadInt32();

      for ( int i = 0; i < partCount; i++ )
      {
        modelParts.Add( new InstancedModelPart( input, graphicsDevice ) );
      }

      SetInstancingTechnique( InstancingTechnique.DiffuseColor );
    }


    /// <summary>
    /// Helper uses the IGraphicsDeviceService interface to find the GraphicsDevice.
    /// </summary>
    static GraphicsDevice GetGraphicsDevice( ContentReader input )
    {
      IServiceProvider serviceProvider = input.ContentManager.ServiceProvider;

      IGraphicsDeviceService deviceService =
          (IGraphicsDeviceService)serviceProvider.GetService(
                                      typeof( IGraphicsDeviceService ) );

      return deviceService.GraphicsDevice;
    }


    #endregion

    #region Technique Selection


    /// <summary>
    /// Gets the current instancing technique.
    /// </summary>
    public InstancingTechnique InstancingTechnique
    {
      get { return instancingTechnique; }
    }

    InstancingTechnique instancingTechnique;


    /// <summary>
    /// Changes which instancing technique we are using.
    /// </summary>
    public void SetInstancingTechnique( InstancingTechnique technique )
    {
      instancingTechnique = technique;

      foreach ( InstancedModelPart modelPart in modelParts )
      {
        modelPart.Initialize( technique );
      }
    }


    #endregion

    /// <summary>
    /// Adds an instance transform of the model to draw.
    /// </summary>
    public void AddInstance( Matrix transform )
    {
      instanceTransforms.Add( transform );
    }

    /// <summary>
    /// Draws all the added instances of the model and clears all transforms.
    /// </summary>
    public void DrawInstances( Matrix view, Matrix projection, Vector3 eye )
    {
      DrawInstances( instanceTransforms.Transforms, instanceTransforms.Count, view, projection, eye );
      instanceTransforms.Clear();
    }

    /// <summary>
    /// Draws the backfaces and then the front faces of each instance of the model.
    /// </summary>
    public void DrawTranslucentInstances( Matrix view, Matrix projection, Vector3 eye )
    {
      RenderState renderState = graphicsDevice.RenderState;
      renderState.AlphaBlendEnable = true;
      renderState.AlphaSourceBlend = Blend.SourceAlpha;
      renderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
      renderState.CullMode = CullMode.CullClockwiseFace;

      DrawInstances( instanceTransforms.Transforms, instanceTransforms.Count, view, projection, eye );

      renderState.CullMode = CullMode.CullCounterClockwiseFace;

      DrawInstances( instanceTransforms.Transforms, instanceTransforms.Count, view, projection, eye );

      instanceTransforms.Clear();
    }

    /// <summary>
    /// Draws a batch of instanced models.
    /// </summary>
    private void DrawInstances( Matrix[] instanceTransforms, int nTransforms,
                                Matrix view, Matrix projection, Vector3 eye )
    {
      if ( nTransforms == 0 )
        return;

      foreach ( InstancedModelPart modelPart in modelParts )
      {
        modelPart.Draw( instancingTechnique, instanceTransforms, nTransforms, 
                        view, projection, eye );
      }
    }

    private class TransformArray
    {
      #region Field and Properties

      int count = 0;
      public int Count { get { return count; } }

      int length;
      public int Length { get { return length; } }

      Matrix[] transforms;
      public Matrix[] Transforms { get { return transforms; } }

      #endregion

      public TransformArray( int initialSize )
      {
        length = initialSize;
        transforms = new Matrix[length];
      }

      public void Add( Matrix transform )
      {
        if ( count == length )
        {
          length <<= 1;
          Array.Resize( ref transforms, length );
        }

        transforms[count++] = transform;
      }

      public void Clear()
      {
        count = 0;
      }
    }
  }


}
