#region File Description
//-----------------------------------------------------------------------------
// InstancedModelReader.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework.Content;
#endregion

namespace InstancedModelSample
{
    /// <summary>
    /// Content pipeline support class for loading InstancedModel objects.
    /// </summary>
    public class InstancedModelReader : ContentTypeReader<InstancedModel>
    {
        /// <summary>
        /// Reads instanced model data from an XNB file.
        /// </summary>
        protected override InstancedModel Read(ContentReader input,
                                               InstancedModel existingInstance)
        {
            return new InstancedModel(input);
        }
    }
}
