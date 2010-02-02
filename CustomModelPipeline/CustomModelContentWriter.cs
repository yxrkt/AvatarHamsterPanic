#region File Description
//-----------------------------------------------------------------------------
// CustomModelContentWriter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#endregion

namespace CustomModelPipeline
{
    /// <summary>
    /// Content pipeline support class for saving out CustomModelContent objects.
    /// </summary>
    [ContentTypeWriter]
    public class CustomModelContentWriter : ContentTypeWriter<CustomModelContent>
    {
        /// <summary>
        /// Saves custom model data into an XNB file.
        /// </summary>
        protected override void Write(ContentWriter output, CustomModelContent value)
        {
            value.Write(output);
        }


        /// <summary>
        /// Tells the content pipeline what CLR type the custom
        /// model data will be loaded into at runtime.
        /// </summary>
        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "CustomModelSample.CustomModel, " +
                   "Avatar Hamster Panic, Version=1.0.0.0, Culture=neutral";
        }


        /// <summary>
        /// Tells the content pipeline what worker type
        /// will be used to load the custom model data.
        /// </summary>
        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "CustomModelSample.CustomModelReader, " +
                   "Avatar Hamster Panic, Version=1.0.0.0, Culture=neutral";
        }
    }
}
