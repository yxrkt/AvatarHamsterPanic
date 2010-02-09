using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Utilities
{
  static class XFileUtils
  {
    /// <summary>
    /// Gets the concatenated matrix for a bone in a hierarchy.
    /// </summary>
    /// <param name="bone">The bone in the hierarchy to start from.</param>
    /// <returns>The final matrix.</returns>
    static public Matrix GetTransform( ModelBone bone )
    {
      if ( bone.Parent != null )
        return ( bone.Transform * GetTransform( bone.Parent ) );
      return bone.Transform;
    }

    /// <summary>
    /// Gets the transform to move a mesh to its exported origin.
    /// </summary>
    /// <param name="bone">The mesh's 'ParentBone'.</param>
    /// <param name="startBone">The bone to call GetTransform() on after world transform has been applied.</param>
    /// <returns>The matrix to move the mesh to its origin.</returns>
    static public Matrix GetOriginTransform( ModelBone bone, out ModelBone startBone )
    {
      startBone = bone.Parent;
      if ( bone.Parent != null && bone.Parent.Name == "" )
        return ( bone.Transform * GetOriginTransform( bone.Parent, out startBone ) );
      return bone.Transform;
    }
  }
}