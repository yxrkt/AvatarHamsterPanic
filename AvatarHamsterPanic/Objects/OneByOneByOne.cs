using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace AvatarHamsterPanic.Objects
{
  class OneByOneByOne : GameObject
  {
    Model model;
    Texture2D texture;
    float width, height;
    Matrix[] transforms = new Matrix[4];

    public OneByOneByOne( GameplayScreen screen )
      : base( screen )
    {
      model = screen.Content.Load<Model>( "Models/1x1x1" );
      texture = screen.Content.Load<Texture2D>( "Textures/plus" );

      Camera camera = Screen.Camera;
      float depth = camera.Position.Z + .5f;
      height = depth * (float)Math.Tan( camera.Fov / 2f );
      width = height * camera.Aspect;
    }

    public override void Update( GameTime gameTime )
    {
      Vector3 camPos = Screen.Camera.Position;
      transforms[0] = Matrix.CreateTranslation( -width, camPos.Y, -.5f );
      transforms[1] = Matrix.CreateTranslation(  width, camPos.Y, -.5f );
      transforms[2] = Matrix.CreateTranslation( 0f, camPos.Y - height, -.5f );
      transforms[3] = Matrix.CreateTranslation( 0f, camPos.Y + height, -.5f );
    }

    public override void Draw()
    {
      Matrix view = Screen.View;
      Matrix proj = Screen.Projection;

      foreach ( Matrix world in transforms )
      {
        foreach ( ModelMesh mesh in model.Meshes )
        {
          foreach ( BasicEffect effect in mesh.Effects )
          {
            effect.Texture = texture;
            effect.TextureEnabled = true;
            effect.DiffuseColor = Color.White.ToVector3();
            effect.EnableDefaultLighting();
            effect.World = world;
            effect.View = view;
            effect.Projection = proj;
          }
          mesh.Draw();
        }
      }
    }
  }
}