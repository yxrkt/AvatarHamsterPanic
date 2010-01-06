using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using Microsoft.Xna.Framework.Content;
using CustomAvatarAnimationFramework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;

namespace GameStateManagement
{
  public struct SlotState
  {
    public Avatar Avatar;
    public PlayerIndex Player;
    public bool Ready;
    public Vector3 Position;
    public List<int> CreatedBots;
  }

  class SignInMenuScreen : MenuScreen
  {
    ContentManager content;
    SlotState[] slots = new SlotState[4];
    float scale = 1f;
    const PlayerIndex NoPlayer = (PlayerIndex)( -2 );
    const PlayerIndex BotPlayer = (PlayerIndex)( -1 );

    public SignInMenuScreen()
    {
    }

    public override void LoadContent()
    {
      content = new ContentManager( ScreenManager.Game.Services, "Content" );

      // Add menu title
      SpriteFont font = ScreenManager.Font;
      MenuText title = new MenuText( this, new Vector2( 100f, 82f ), "Please sign in!", font );
      title.Scale = 1.25f;
      title.TransitionOnPosition = title.Position + new Vector2( 0f, -100f );
      title.TransitionOffPosition = title.TransitionOnPosition;
      MenuItems.Add( title );

      // Initialize slots
      float spacing = .25f;
      float xStep = scale + spacing;
      Vector3 worldPos = new Vector3( -1.5f * scale - 1.5f * spacing, -1f, 0f );

      for ( int i = 0; i < 4; ++i )
      {
        slots[i].Avatar = null;
        slots[i].CreatedBots = new List<int>();
        slots[i].Player = NoPlayer;
        slots[i].Ready = false;
        slots[i].Position = worldPos;

        worldPos.X += xStep;
      }
    }

    public override void UnloadContent()
    {
      content.Unload();
    }

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Avatar != null )
          slots[i].Avatar.Update( gameTime.ElapsedGameTime, true );
      }
    }

    public override void HandleInput( InputState input )
    {
      //base.HandleInput( input );

      for ( int i = 0; i < 4; ++i )
      {
        GamePadState lastPadState = input.LastGamePadStates[i];
        GamePadState pad = input.CurrentGamePadStates[i];
        if ( pad.IsConnected )
        {
          if ( pad.IsButtonDown( Buttons.A ) && lastPadState.IsButtonUp( Buttons.A ) )
            OnButtonAHit( (PlayerIndex)i, ref slots[i] );
          else if ( pad.IsButtonDown( Buttons.B ) && lastPadState.IsButtonUp( Buttons.B ) )
            OnButtonBHit( (PlayerIndex)i, ref slots[i] );
          else if ( pad.IsButtonDown( Buttons.X ) && lastPadState.IsButtonUp( Buttons.X ) )
            OnButtonXHit( (PlayerIndex)i, ref slots[i] );
        }
      }
    }

    public void OnButtonAHit( PlayerIndex playerIndex, ref SlotState slot )
    {
      if ( slot.Player < PlayerIndex.One )
      {
        bool found = false;
        foreach ( SignedInGamer gamer in Gamer.SignedInGamers )
        {
          if ( gamer.PlayerIndex == playerIndex )
          {
            found = true;
            break;
          }
        }

        if ( !found )
        {
          Guide.ShowSignIn( 4, false );
        }
        else
        {
          slot.Player = playerIndex;
          slot.Avatar = new Avatar( Gamer.SignedInGamers[playerIndex].Avatar, AvatarAnimationPreset.Stand0,
                                    1f, Vector3.UnitZ, slot.Position );
        }
      }
      else if ( slot.Ready == false )
      {
        // change 'ready' graphic
        slot.Ready = true;
      }
      else if ( slots.Count( s => s.Player >= PlayerIndex.One && !s.Ready ) == 0 )
      {
        LoadingScreen.Load( ScreenManager, true, playerIndex, new GameplayScreen( slots ) );
      }
    }

    public void OnButtonBHit( PlayerIndex playerIndex, ref SlotState slot )
    {
      if ( slot.Ready )
      {
        slot.Ready = false;
      }
      else if ( slots.Count( s => s.Player != NoPlayer ) <= 1 )
      {
        OnCancel( playerIndex );
      }
      else if ( slot.CreatedBots.Count > 0 )
      {
        int botIndex = slot.CreatedBots.Last();
        slot.CreatedBots.Remove( botIndex );
        slots[botIndex].Player = NoPlayer;
        slots[botIndex].Avatar = null;
      }
      else
      {
        slot.Player = NoPlayer;
        slot.Avatar = null;
      }
    }

    public void OnButtonXHit( PlayerIndex playerIndex, ref SlotState slot )
    {
      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Player == NoPlayer )
        {
          slots[i].Player = BotPlayer;
          slots[i].Avatar = new Avatar( AvatarDescription.CreateRandom(), AvatarAnimationPreset.Stand0, 
                                        1f, Vector3.UnitZ, slots[i].Position );
          return;
        }
      }
    }

    public override void Draw( GameTime gameTime )
    {
      base.Draw( gameTime );

      // need to set vertex declaration every frame for shaders
      GraphicsDevice graphics = ScreenManager.GraphicsDevice;
      graphics.VertexDeclaration = new VertexDeclaration( graphics, VertexPositionNormalTexture.VertexElements );

      float aspect = ScreenManager.GraphicsDevice.DisplayMode.AspectRatio;
      Matrix view, proj;
      view = Matrix.CreateLookAt( new Vector3( 0f, 0f, 10f ), Vector3.Zero, Vector3.Up );
      Matrix.CreatePerspectiveFieldOfView( MathHelper.ToRadians( 30f ), aspect, 1f, 100f, out proj );

      graphics.RenderState.DepthBufferEnable = true;

      Model model = content.Load<Model>( "block" );

      for ( int i = 0; i < 4; ++i )
      {
        // platforms
        foreach ( ModelMesh mesh in model.Meshes )
        {
          foreach ( BasicEffect effect in mesh.Effects )
          {
            effect.EnableDefaultLighting();
            effect.View = view;
            effect.Projection = proj;

            Vector3 position = slots[i].Position;
            position.Y -= scale / 8f;
            effect.World = Matrix.CreateScale( scale ) * Matrix.CreateTranslation( position );
          }
          mesh.Draw();
        }

        // avatars
        if ( slots[i].Avatar != null )
        {
          Avatar avatar = slots[i].Avatar;
          Matrix matRot = Matrix.CreateWorld( Vector3.Zero, avatar.Direction, Vector3.Up );
          Matrix matTrans = Matrix.CreateTranslation( slots[i].Position );
          avatar.Renderer.World = Matrix.CreateScale( scale ) * matRot * matTrans;
          avatar.Renderer.View = view;
          avatar.Renderer.Projection = proj;
          avatar.Renderer.Draw( avatar.BoneTransforms, avatar.Expression );
        }
      }
    }
  }
}