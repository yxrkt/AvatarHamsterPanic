using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Menu
{
  abstract class MenuItem
  {
    #region Fields


    protected Vector2 curPos; // position that should be used 
                              // when drawing the item


    #endregion

    #region Properties


    public MenuScreen Screen { get; set; }
    public float Scale { get; set; }
    public float Z { get; set; }
    public bool Hidden { get; set; }
    public Vector2 Dimensions;
    public Vector2 Origin;
    public Vector2 Position;
    public Vector2 TransitionOnPosition;
    public Vector2 TransitionOffPosition;
    public bool Focused { get; set; }


    #endregion

    #region Events


    public event EventHandler<PlayerIndexEventArgs> Selected;

    protected internal virtual void OnSelect( PlayerIndex playerIndex )
    {
      if ( Selected != null )
        Selected( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    public event EventHandler<PlayerIndexEventArgs> Decremented;

    protected internal virtual void OnDecrement( PlayerIndex playerIndex )
    {
      if ( Decremented != null )
        Decremented( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    public event EventHandler<PlayerIndexEventArgs> Incremented;

    protected internal virtual void OnIncrement( PlayerIndex playerIndex )
    {
      if ( Incremented != null )
        Incremented( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    #endregion

    #region Initialization


    public MenuItem( MenuScreen screen, Vector2 position )
    {
      Screen = screen;
      Position = position;
      Z = 0f;
      Scale = 1f;
      Hidden = false;
      Focused = false;
    }


    #endregion

    #region Update and Draw

    public virtual void Update( GameTime gameTime )
    {
    }

    public virtual void UpdateTransition( float transitionPosition, ScreenState state )
    {
      Vector2 position = Position;
      transitionPosition *= transitionPosition;

      if ( state == ScreenState.TransitionOn )
        position += transitionPosition * ( TransitionOnPosition - position );
      else if ( state == ScreenState.TransitionOff )
        position += transitionPosition * ( TransitionOffPosition - position );

      curPos = position;
    }

    public virtual void Draw( GameTime gameTime ) { }

    #endregion
  }
}