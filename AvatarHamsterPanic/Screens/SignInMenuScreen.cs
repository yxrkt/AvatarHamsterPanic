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
using AvatarHamsterPanic;

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
    readonly float screenScale;

    SignInSlot[] slots = new SignInSlot[4];
    bool autoSignIn = false;
    bool toggleAutoSignIn = false;
    bool eventSet = false;
    List<int> activeBots = new List<int>( 4 );
    TextMenuItem nagText;
    GameTime lastGameTime;

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
      Texture2D addCpuTexture = content.Load<Texture2D>( "Textures/xAddCPUText" );
      Texture2D removeCpuTexture = content.Load<Texture2D>( "Textures/yRemoveCPUText" );
      Texture2D aStartTexture = content.Load<Texture2D>( "Textures/aStartText" );
      Texture2D readyTexture = content.Load<Texture2D>( "Textures/readyText" );

      screenScale = device.Viewport.Height / 1080f;
      textScale *= screenScale;

      Rectangle rectangle = ScreenRects.FourByThree;

      float x = textColumnStart * (float)rectangle.Width + (float)rectangle.X;
      float xStep = (float)rectangle.Width * ( 1f - ( 2f * textColumnStart ) ) / 3f;

      float nameY = nameHeight * (float)rectangle.Height + (float)rectangle.Y;
      float joinY = joinHeight * (float)rectangle.Height + (float)rectangle.Y;
      float cpuY = cpuHeight * (float)rectangle.Height + (float)rectangle.Y;
      float readyY = readyHeight * (float)rectangle.Height + (float)rectangle.Y;

      // Full version required for multiplayer
      Vector2 nagPos = new Vector2( device.Viewport.Width / 2, device.Viewport.Height * .14f );
      nagText = new TextMenuItem( this, nagPos, "Full version required for multiplayer.", nameFont );
      nagText.TransitionOnPosition = nagPos;
      nagText.TransitionOffPosition = nagPos;
      nagText.Centered = true;
      nagText.Color = Color.DarkOrange;
      nagText.DeathBegin = .01f;
      MenuItems.Add( nagText );


      for ( int i = 0; i < 4; ++i )
      {
        // <GAMERTAG>
        itemPosition = new Vector2( x, nameY );
        TextMenuItem textItem = new TextMenuItem( this, itemPosition, null, nameFont );
        textItem.Centered = true;
        textItem.MaxWidth = xStep;
        textItem.Scale = screenScale;
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
        item = new StaticImageMenuItem( this, itemPosition, addCpuTexture );
        item.SetImmediateScale( textScale );
        slots[i].AddCPUItem = item;
        MenuItems.Add( item );

        // Y Add CPU
        itemPosition = new Vector2( x, cpuY );
        item = new StaticImageMenuItem( this, itemPosition, removeCpuTexture );
        item.SetImmediateScale( textScale );
        slots[i].RemoveCPUItem = item;
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
        slots[i].Ready = false;
        slots[i].ActivePosition = worldPos;
        slots[i].TransitionOnPosition = worldPos + new Vector3( 0, 4 * ( i + 1 ), 0 );
        slots[i].TransitionOffPosition = worldPos + new Vector3( 0, -2 * ( 4 - i ), 0 );
        slots[i].JoinItem.SetImmediateScale( textScale );
        slots[i].AddCPUItem.SetImmediateScale( textScale );
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
      if ( !eventSet )
      {
        SignedInGamer.SignedIn += PlayerSignedIn;
        eventSet = true;
      }

      for ( int i = 0; i < 4; ++i )
        AddBot();
    }

    public override void UnloadContent()
    {
      if ( eventSet )
      {
        SignedInGamer.SignedIn -= PlayerSignedIn;
        eventSet = false;
      }
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
      lastGameTime = gameTime;
      base.Update( gameTime, otherScreenHasFocus, coveredByOtherScreen );

      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Slot.Avatar != null )
          slots[i].Slot.Avatar.Update( gameTime.ElapsedGameTime, true );

        // player name
        slots[i].NameItem.Text = slots[i].GetName();

        // update 'join' and 'add cpu'
        if ( slots[i].Slot.Player == NoPlayer )
        {
          slots[i].JoinItem.Scale = textScale;
          slots[i].AddCPUItem.Scale = textScale;
          slots[i].RemoveCPUItem.Scale = 0;
        }
        else if ( slots[i].Slot.Player == BotPlayer )
        {
          slots[i].JoinItem.Scale = textScale;
          slots[i].AddCPUItem.Scale = 0;
          slots[i].RemoveCPUItem.Scale = textScale;
        }
        else
        {
          slots[i].JoinItem.Scale = 0;
          slots[i].AddCPUItem.Scale = 0;
          slots[i].RemoveCPUItem.Scale = 0;
        }

        // update 'start' and 'ready'
        if ( slots[i].Slot.Player.IsHuman() )
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
      {
        autoSignIn = false;
        toggleAutoSignIn = false;
      }
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
          else if ( pad.IsButtonDown( Buttons.Y ) && lastPadState.IsButtonUp( Buttons.Y ) )
            OnButtonYHit( (PlayerIndex)i, ref slots[i] );
        }
      }
    }

    public void OnButtonAHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      if ( slot.Slot.Player < PlayerIndex.One )
      {
        if ( Guide.IsTrialMode && slots.Count( s => s.Slot.Player.IsHuman() ) > 0 )
        {
          nagText.DeathBegin = (float)lastGameTime.TotalGameTime.TotalSeconds;

          MessageBoxScreen messageBox = new MessageBoxScreen( "Full version required for Multiplayer. " +
                                                              "Buy Avatar Hamster Panic now?" );
          messageBox.Accepted += GameCore.Instance.ShowBuy;
          ScreenManager.AddScreen( messageBox, null );
        }
        else
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
      }
      else if ( slot.Ready == false )
      {
        GameCore.Instance.AudioManager.Play2DCue( "readyUp", 1f );
        slot.Ready = true;
      }

      if ( slots.Count( s => s.Slot.Player.IsHuman() ) != 0 )
      {
        if ( slots.Count( s => s.Slot.Player.IsHuman() && !s.Ready ) == 0 )
        {
          Slot[] initSlots = new Slot[4];
          for ( int i = 0; i < 4; ++i )
            initSlots[i] = slots[i].Slot;
          GameCore.Instance.AudioManager.Play2DCue( "startGame", 1f );
          //ScreenManager.MenuTrack.Pause();
          LoadingScreen.Load( ScreenManager, true, playerIndex, new GameplayScreen( initSlots ) );
        }
      }
    }

    public void OnButtonBHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      //int numPlayers = slots.Count( s => s.Slot.Player != NoPlayer );
      int numHumans = slots.Count( s => s.Slot.Player.IsHuman() );

      if ( slot.Ready )
      {
        slot.Ready = false;
      }
      //else if ( numPlayers <= 1 )
      //{
      //  if ( numHumans == 0 || numPlayers == 0 || numPlayers == 1 && slot.Slot.Player.IsHuman() )
      //    OnCancel( playerIndex );
      //}
      else if ( numHumans == 1 )
      {
        while ( activeBots.Count > 0 )
          RemoveLastBot();
        OnCancel( playerIndex );
      }
      else
      {
        slot.Slot.Player = NoPlayer;
        slot.Slot.Avatar = null;
        GameCore.Instance.AudioManager.Play2DCue( "signOut", 1f );
      }
    }

    public void OnButtonXHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      AddBot();
    }

    public void OnButtonYHit( PlayerIndex playerIndex, ref SignInSlot slot )
    {
      RemoveLastBot();
    }

    private void AddPlayer( ref SignInSlot slot, PlayerIndex playerIndex )
    {
      slot.Slot.Player = playerIndex;
      slot.Slot.Avatar = new Avatar( Gamer.SignedInGamers[playerIndex].Avatar, AvatarAnimationPreset.Stand0,
                                     1f, Vector3.UnitZ, Vector3.Zero );
      activeBots.Remove( (int)playerIndex );
      GameCore.Instance.AudioManager.Play2DCue( "signIn", 1f );
    }

    private void AddBot()
    {
      for ( int i = 0; i < 4; ++i )
      {
        if ( slots[i].Slot.Player == NoPlayer )
        {
          slots[i].Slot.Player = BotPlayer;
          slots[i].Slot.Avatar = new Avatar( AvatarDescription.CreateRandom(), AvatarAnimationPreset.Stand0,
                                             1f, Vector3.UnitZ, Vector3.Zero );
          //slots[(int)playerIndex].CreatedBots.Add( i );
          activeBots.Add( i );
          GameCore.Instance.AudioManager.Play2DCue( "addCPU", 1f );
          return;
        }
      }
    }

    private void RemoveLastBot()
    {
      if ( activeBots.Count > 0 )
      {
        int botIndex = activeBots.Last();
        activeBots.Remove( botIndex );
        slots[botIndex].Slot.Player = NoPlayer;
        slots[botIndex].Slot.Avatar = null;
        GameCore.Instance.AudioManager.Play2DCue( "signOut", 1f );
      }
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
    public TextMenuItem NameItem;
    public StaticImageMenuItem JoinItem;
    public StaticImageMenuItem AddCPUItem;
    public StaticImageMenuItem RemoveCPUItem;
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