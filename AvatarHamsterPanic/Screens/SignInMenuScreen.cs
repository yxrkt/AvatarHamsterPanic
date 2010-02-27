using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AvatarHamsterPanic.Objects;
using Microsoft.Xna.Framework.Content;
using CustomAvatarAnimationFramework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using CustomModelSample;
using Graphics;
using AvatarHamsterPanic.Utilities;
using System.Diagnostics;

namespace Menu
{
  class SignInMenuScreen : MenuScreen
  {
    const PlayerIndex NoPlayer = (PlayerIndex)( -2 );
    const PlayerIndex BotPlayer = (PlayerIndex)( -1 );

    readonly Camera camera;
    readonly CustomModel boxModel;
    readonly Matrix boxScaleMatrix = Matrix.CreateScale( 2.1f );
    readonly Matrix avatarScaleMatrix = Matrix.CreateScale( 1.5f );
    readonly float boxSpacing = .45f;
    readonly float boxAlpha = .4f;
    readonly float textScale = .5f;
    readonly float textColumnStart = .14f;
    readonly float nameHeight = .3f;
    readonly float joinHeight = .5f;
    readonly float cpuHeight = .55f;
    readonly float readyHeight = .8f;

    SignInSlot[] slots = new SignInSlot[4];
    bool autoSignIn = false;
    bool toggleAutoSignIn = false;

    public SignInMenuScreen( ScreenManager screenManager )
    {
      TransitionOnTime = TimeSpan.FromSeconds( 1 );
      TransitionOffTime = TimeSpan.FromSeconds( .25 );

      this.ScreenManager = screenManager;
      ContentManager content = screenManager.Game.Content;
      GraphicsDevice device = screenManager.GraphicsDevice;

      boxModel = content.Load<CustomModel>( "Models/signInBox" );
      foreach ( CustomModel.ModelPart part in boxModel.ModelParts )
      {
        part.Effect.CurrentTechnique = part.Effect.Techniques["Color"];
      }


      camera = new Camera( MathHelper.PiOver4, device.Viewport.AspectRatio, 1, 100,
                           new Vector3( 0, 0, 10 ), new Vector3( 0, 0, 0 ) );

      StaticImageMenuItem item;
      Vector2 itemPosition;

      SpriteFont nameFont = content.Load<SpriteFont>( "Fonts/signInNameFont" );
      Texture2D joinTexture = content.Load<Texture2D>( "Textures/aJoinText" );
      Texture2D cpuTexture = content.Load<Texture2D>( "Textures/xAddCPUText" );
      Texture2D aStartTexture = content.Load<Texture2D>( "Textures/aStartText" );
      Texture2D readyTexture = content.Load<Texture2D>( "Textures/readyText" );

      Rectangle rectangle = ScreenRects.FourByThree;

      float x = textColumnStart * (float)rectangle.Width + (float)rectangle.X;
      float xStep = (float)rectangle.Width * ( 1f - ( 2f * textColumnStart ) ) / 3f;

      float nameY = nameHeight * (float)rectangle.Height + (float)rectangle.Y;
      float joinY = joinHeight * (float)rectangle.Height + (float)rectangle.Y;
      float cpuY = cpuHeight * (float)rectangle.Height + (float)rectangle.Y;
      float readyY = readyHeight * (float)rectangle.Height + (float)rectangle.Y;

      for ( int i = 0; i < 4; ++i )
      {
        // <GAMERTAG>
        itemPosition = new Vector2( x, nameY );
        TextMenuItem textItem = new TextMenuItem( this, itemPosition, null, nameFont );
        textItem.Centered = true;
        textItem.MaxWidth = xStep;
        slots[i].NameItem = textItem;
        MenuItems.Add( textItem );

        // A Join
        itemPosition = new Vector2( x, joinY );
        item = new StaticImageMenuItem( this, itemPosition, joinTexture );
        item.SetImmediateScale( textScale );
        slots[i].JoinItem = item;
        MenuItems.Add( item );

        // X Add CPU
        itemPosition = new Vector2( x, cpuY );
        item = new StaticImageMenuItem( this, itemPosition, cpuTexture );
        item.SetImmediateScale( textScale );
        slots[i].CPUItem = item;
        MenuItems.Add( item );

        // A Start
        itemPosition = new Vector2( x, readyY );
        item = new StaticImageMenuItem( this, itemPosition, aStartTexture );
        item.SetImmediateScale( textScale );
        slots[i].StartItem = item;
        MenuItems.Add( item );

        // Ready!
        itemPosition = new Vector2( x, readyY );
        item = new StaticImageMenuItem( this, itemPosition, readyTexture );
        item.SetImmediateScale( textScale );
        slots[i].ReadyItem = item;
        MenuItems.Add( item );

        x += xStep;
      }
    }

    private void ClearSlots()
    {
      float xStep = boxScaleMatrix.M11 + boxSpacing;
      Vector3 worldPos = new Vector3( -1.5f * boxScaleMatrix.M11 - 1.5f * boxSpacing, -1.5f, 0f );

      for ( int i = 0; i < 4; ++i )
      {
        slots[i].Slot.Avatar = null;
        slots[i].Slot.Player = NoPlayer;
        slots[i].Slot.ID = 0;
        slots[i].CreatedBots = new List<int>( 3 );
        slots[i].Ready = false;
        slots[i].ActivePosition = worldPos;
        slots[i].TransitionOnPosition = worldPos + new Vector3( 0, 4 * ( i + 1 ), 0 );
        slots[i].TransitionOffPosition = worldPos + new Vector3( 0, -2 * ( 4 - i ), 0 );
        slots[i].JoinItem.SetImmediateScale( textScale );
        slots[i].CPUItem.SetImmediateScale( textScale );
        slots[i].StartItem.SetImmediateScale( 0 );
        slots[i].ReadyItem.SetImmediateScale( 0 );

        worldPos.X += xStep;
      }
    }

    public override void LoadContent()
    {
      // Content is loaded in the constructor to prevent spikes when 
      // going from main menu to this screen

      ClearSlots();
      SignedInGamer.SignedIn += PlayerSignedIn;
    }

    public override void UnloadContent()
    {
      SignedInGamer.SignedIn -= PlayerSignedIn;
    }

    void PlayerSignedIn( object sender, SignedInEventArgs e )
    {
      if ( autoSignIn )
      {
        //Debug.WriteLine( e.Gamer.Gamertag + " signed in" );
        PlayerIndex playerIndex = e.Gamer.PlayerIndex;
        AddPlayer( ref slots[(int)playerIndex], playerIndex );
        toggleAutoSignIn = true;
      }
    }

    public override void Update( GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen )
    {
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Slot.Avatar != null )
          slots[i].Slot.Avatar.Update( gameTime.ElapsedGameTime, true );

        // player name
        slots[i].NameItem.Text = slots[i].GetName();

        // update 'join' and 'add cpu'
        if ( slots[i].Slot.Player != NoPlayer )
        {
          slots[i].JoinItem.Scale = 0;
          slots[i].CPUItem.Scale = 0;
        }
        else
        {
          slots[i].JoinItem.Scale = textScale;
          slots[i].CPUItem.Scale = textScale;
        }

        // update 'start' and 'ready'
        if ( slots[i].Slot.Player.IsPlayer() )
        {
          if ( slots[i].Ready )
          {
            slots[i].StartItem.Scale = 0;
            slots[i].ReadyItem.Scale = textScale;
          }
          else
          {
            slots[i].StartItem.Scale = textScale;
            slots[i].ReadyItem.Scale = 0;
          }
        }
        else
        {
          slots[i].StartItem.Scale = 0;
          slots[i].ReadyItem.Scale = 0;
        }
      }

      if ( toggleAutoSignIn )
        autoSignIn = false;
    }

    public override void HandleInput( InputState input )
    {
      if ( ScreenState != ScreenState.Active ) return;

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

    public void OnButtonAHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      if ( slot.Slot.Player < PlayerIndex.One )
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
          autoSignIn = true;
        }
        else
        {
          AddPlayer( ref slot, playerIndex );
        }
      }
      else if ( slot.Ready == false )
      {
        slot.Ready = true;
      }

      if ( slots.Count( s => s.Slot.Player.IsPlayer() && !s.Ready ) == 0 )
      {
        Slot[] initSlots = new Slot[4];
        for ( int i = 0; i < 4; ++i )
          initSlots[i] = slots[i].Slot;
        LoadingScreen.Load( ScreenManager, true, playerIndex, new GameplayScreen( initSlots ) );
      }
    }

    public void OnButtonBHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      if ( slot.Ready )
      {
        slot.Ready = false;
      }
      else if ( slots.Count( s => s.Slot.Player != NoPlayer ) <= 1 )
      {
        if ( slot.Slot.Player.IsPlayer() )
          OnCancel( playerIndex );
      }
      else if ( slot.CreatedBots.Count > 0 )
      {
        int botIndex = slot.CreatedBots.Last();
        slot.CreatedBots.Remove( botIndex );
        slots[botIndex].Slot.Player = NoPlayer;
        slots[botIndex].Slot.Avatar = null;
      }
      else
      {
        slot.Slot.Player = NoPlayer;
        slot.Slot.Avatar = null;
      }
    }

    public void OnButtonXHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Slot.Player == NoPlayer )
        {
          slots[i].Slot.Player = BotPlayer;
          slots[i].Slot.Avatar = new Avatar( AvatarDescription.CreateRandom(), AvatarAnimationPreset.Stand0, 
                                             1f, Vector3.UnitZ, Vector3.Zero );
          slots[(int)playerIndex].CreatedBots.Add( i );
          return;
        }
      }
    }

    private void AddPlayer( ref SignInSlot slot, PlayerIndex playerIndex )
    {
      slot.Slot.Player = playerIndex;
      slot.Slot.Avatar = new Avatar( Gamer.SignedInGamers[playerIndex].Avatar, AvatarAnimationPreset.Stand0,
                                     1f, Vector3.UnitZ, Vector3.Zero );
      foreach ( SignInSlot signInSlot in slots )
        signInSlot.CreatedBots.Remove( (int)playerIndex );
    }

    public override void Draw( GameTime gameTime )
    {
      GraphicsDevice device = ScreenManager.GraphicsDevice;

      Matrix view = camera.GetViewMatrix();
      Matrix projection = camera.GetProjectionMatrix();

      float t = TransitionPosition * TransitionPosition;

      Matrix translation = Matrix.Identity;
      Vector3 position;

      int colorIndex = 0;
      foreach ( SignInSlot slot in slots )
      {
        // get translation matrix
        switch ( ScreenState )
        {
          case ScreenState.TransitionOn:
            position = Vector3.Lerp( slot.ActivePosition, slot.TransitionOnPosition, t );
            translation = Matrix.CreateTranslation( position );
            break;
          case ScreenState.Active:
            translation = Matrix.CreateTranslation( slot.ActivePosition );
            break;
          case ScreenState.TransitionOff:
            position = Vector3.Lerp( slot.ActivePosition, slot.TransitionOffPosition, t );
            translation = Matrix.CreateTranslation( position );
            break;
        }

        // draw box
        Matrix world = boxScaleMatrix * translation;
        GameCore game = ScreenManager.Game as GameCore;
        foreach ( CustomModel.ModelPart part in boxModel.ModelParts )
        {
          float alpha = ( 1 - TransitionPosition ) * boxAlpha;
          Vector4 color = new Vector4( game.PlayerColors[colorIndex++].ToVector3(), alpha );
          part.EffectParamColor.SetValue( color );
        }

        device.RenderState.AlphaBlendEnable = true;
        device.RenderState.DepthBufferEnable = true;
        device.RenderState.CullMode = CullMode.CullClockwiseFace;
        boxModel.Draw( camera.Position, world, view, projection );

        device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        boxModel.Draw( camera.Position, world, view, projection );

        // draw avatar
        if ( slot.Slot.Avatar != null )
        {
          Avatar avatar = slot.Slot.Avatar;
          Matrix matRot = Matrix.CreateWorld( Vector3.Zero, avatar.Direction, camera.Up );
          avatar.Renderer.World = avatarScaleMatrix * matRot * translation;
          avatar.Renderer.View = view;
          avatar.Renderer.Projection = projection;
          avatar.Renderer.Draw( avatar.BoneTransforms, avatar.Expression );
        }
      }

      base.Draw( gameTime );
    }
  }

  public struct Slot
  {
    public Avatar Avatar;
    public PlayerIndex Player;
    public uint ID;
  }

  struct SignInSlot
  {
    public Slot Slot;
    public bool Ready;
    public Vector3 TransitionOnPosition;
    public Vector3 TransitionOffPosition;
    public Vector3 ActivePosition;
    public List<int> CreatedBots;
    public TextMenuItem NameItem;
    public StaticImageMenuItem JoinItem;
    public StaticImageMenuItem CPUItem;
    public StaticImageMenuItem StartItem;
    public StaticImageMenuItem ReadyItem;

    const string CPUName = "CPU";

    public string GetName()
    {
      if ( Slot.Player == (PlayerIndex)( -2 ) )
        return null;
      if ( Slot.Player == (PlayerIndex)( -1 ) )
        return CPUName;
      return SignedInGamer.SignedInGamers[Slot.Player].Gamertag;
    }
  }
}