#region File Description
//-----------------------------------------------------------------------------
// CustomModelReader.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework.Content;
#endregion

namespace CustomModelSample
{
  /// <summary>
  /// Content pipeline support class for loading CustomModel objects.
  /// </summary>
  public class CustomModelReader : ContentTypeReader<CustomModel>
  {
    /// <summary>
    /// Reads custom model data from an XNB file.
    /// </summary>
    protected override CustomModel Read( ContentReader input,
                                         CustomModel existingInstance )
    {
      return new CustomModel( input );
    }
  }
}
