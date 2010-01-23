using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AvatarHamsterPanic.Objects
{
  public struct VertexPositionNormalTextureTangentBinormal
  {
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinate;
    public Vector3 Tangent;
    public Vector3 Binormal;

    public static readonly VertexElement[] VertexElements =
    {
      new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
      new VertexElement(0, sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
      new VertexElement(0, sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
      new VertexElement(0, sizeof(float) * 8, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
      new VertexElement(0, sizeof(float) * 11, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0),
    };

    public VertexPositionNormalTextureTangentBinormal( Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector3 tangent, Vector3 binormal )
    {
      Position = position;
      Normal = normal;
      TextureCoordinate = textureCoordinate;
      Tangent = tangent;
      Binormal = binormal;
    }

    public static int SizeInBytes { get { return sizeof( float ) * 14; } }
  }
}