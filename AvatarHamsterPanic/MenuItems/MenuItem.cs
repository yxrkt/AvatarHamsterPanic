using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameStateManagement
{
  abstract class MenuItem
  {
    #region Fields


    protected float selectionFade; // Tracks a fading selection effect on the entry.
    protected Vector2 curPos;      // Current position.


    #endregion

    #region Properties


    public MenuScreen Screen { get; set; }
    public float Scale { get; set; }
    public float Z { get; set; }
    public bool Hidden { get; set; }
    public Vector2 Dimensions { get; protected set; }
    public Vector2 Origin { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 TransitionOnPosition { get; set; }
    public Vector2 TransitionOffPosition { get; set; }
    public bool Focused { get; set; }


    #endregion

    #region Events


    /// <summary>
    /// Event raised when the menu entry is selected.
    /// </summary>
    public event EventHandler<PlayerIndexEventArgs> Selected;

    /// <summary>
    /// Method for raising the Selected event.
    /// </summary>
    protected internal virtual void OnSelect( PlayerIndex playerIndex )
    {
      if ( Selected != null )
        Selected( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    /// <summary>
    /// Event raised when 'left' is pressed on menu item.
    /// </summary>
    public event EventHandler<PlayerIndexEventArgs> Decremented;

    /// <summary>
    /// Method for raising the Decremented event.
    /// </summary>
    protected internal virtual void OnDecrement( PlayerIndex playerIndex )
    {
      if ( Decremented != null )
        Decremented( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    /// <summary>
    /// Event raised when 'right' is pressed on menu item.
    /// </summary>
    public event EventHandler<PlayerIndexEventArgs> Incremented;

    /// <summary>
    /// Method for raising the Incremented event.
    /// </summary>
    protected internal virtual void OnIncrement( PlayerIndex playerIndex )
    {
      if ( Incremented != null )
        Incremented( this, new PlayerIndexEventArgs( playerIndex ) );
    }

    #endregion

    #region Initialization


    public MenuItem( MenuScreen screen, Vector2 position )
    {
      selectionFade = 0f;
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
      float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 4f;

      if ( Focused )
        selectionFade = Math.Min( selectionFade + fadeSpeed, 1f );
      else
        selectionFade = Math.Max( selectionFade - fadeSpeed, 0f );
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