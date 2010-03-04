#region Using Statements

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace Debug
{
  public class DebugCommandUI : DrawableGameComponent, IDebugCommandHost
  {
    #region Constant Declarations

    const int MaxLineCount = 20;

    const int MaxCommandHistory = 32;

    const string Cursor = "\u2582";

    public string DefaultPrompt = "CMD>";

    #endregion

    #region Properties

    public string Prompt { get; set; }

    public bool Focused { get { return state != State.Closed; } }

    #endregion

    #region Fields

    enum State
    {
      Closed,
      Opening,
      Opened,
      Closing
    }

    class CommandInfo
    {
      public CommandInfo(
          string command, string description, DebugCommandExecute callback )
      {
        this.command = command;
        this.description = description;
        this.callback = callback;
      }

      public string command;

      public string description;

      public DebugCommandExecute callback;
    }

    private DebugManager debugManager;

    private State state = State.Closed;

    private float stateTransition;

    List<IDebugEchoListner> listenrs = new List<IDebugEchoListner>();

    Stack<IDebugCommandExecutioner> executioners = new Stack<IDebugCommandExecutioner>();

    private Dictionary<string, CommandInfo> commandTable =
                                            new Dictionary<string, CommandInfo>();

    private string commandLine = String.Empty;
    private int cursorIndex = 0;

    private Queue<string> lines = new Queue<string>();

    private List<string> commandHistory = new List<string>();

    private int commandHistoryIndex;

    #region Keyboard Input

    private KeyboardState prevKeyState;

    private Keys pressedKey;

    private float keyRepeatTimer;

    private float keyRepeatStartDuration = 0.3f;

    private float keyRepeatDuration = 0.03f;

    #endregion

    #endregion

    #region Initialization

    public DebugCommandUI( Game game )
      : base( game )
    {
      Prompt = DefaultPrompt;

      Game.Services.AddService( typeof( IDebugCommandHost ), this );

      RegisterCommand( "help", "Show Command helps",
      delegate( IDebugCommandHost host, string command, IList<string> args )
      {
        int maxLen = 0;
        foreach ( CommandInfo cmd in commandTable.Values )
          maxLen = Math.Max( maxLen, cmd.command.Length );

        string fmt = String.Format( "{{0,-{0}}}    {{1}}", maxLen );

        foreach ( CommandInfo cmd in commandTable.Values )
        {
          Echo( String.Format( fmt, cmd.command, cmd.description ) );
        }
      } );

      RegisterCommand( "cls", "Clear Screen",
      delegate( IDebugCommandHost host, string command, IList<string> args )
      {
        lines.Clear();
      } );

      RegisterCommand( "echo", "Display Messages",
      delegate( IDebugCommandHost host, string command, IList<string> args )
      {
        Echo( command.Substring( 5 ) );
      } );
    }

    public override void Initialize()
    {
      debugManager =
          Game.Services.GetService( typeof( DebugManager ) ) as DebugManager;

      if ( debugManager == null )
        throw new InvalidOperationException( "DebugManagerが見つかりません。" );

      base.Initialize();
    }

    #endregion

    #region IDebugCommandHost

    public void RegisterCommand(
        string command, string description, DebugCommandExecute callback )
    {
      string lowerCommand = command.ToLower();
      if ( commandTable.ContainsKey( lowerCommand ) )
      {
        throw new InvalidOperationException(
            String.Format( "{0} has already been registered.", command ) );
      }

      commandTable.Add(
          lowerCommand, new CommandInfo( command, description, callback ) );
    }

    public void UnregisterCommand( string command )
    {
      string lowerCommand = command.ToLower();
      if ( !commandTable.ContainsKey( lowerCommand ) )
      {
        throw new InvalidOperationException(
            String.Format( "{0} has not been registered.", command ) );
      }

      commandTable.Remove( command );
    }

    public void ExecuteCommand( string command )
    {
      if ( executioners.Count != 0 )
      {
        executioners.Peek().ExecuteCommand( command );
        return;
      }

      char[] spaceChars = new char[] { ' ' };

      Echo( Prompt + command );

      command = command.TrimStart( spaceChars );

      List<string> args = new List<string>( command.Split( spaceChars ) );
      string cmdText = args[0];
      args.RemoveAt( 0 );

      CommandInfo cmd;
      if ( commandTable.TryGetValue( cmdText.ToLower(), out cmd ) )
      {
        try
        {
          cmd.callback( this, command, args );
        }
        catch ( Exception e )
        {
          EchoError( "Unhandled Exception occured" );

          string[] lines = e.Message.Split( new char[] { '\n' } );
          foreach ( string line in lines )
            EchoError( line );
        }
      }
      else
      {
        Echo( "Unknown Command" );
      }

      commandHistory.Add( command );
      while ( commandHistory.Count > MaxCommandHistory )
        commandHistory.RemoveAt( 0 );

      commandHistoryIndex = commandHistory.Count;
    }

    public void RegisterEchoListner( IDebugEchoListner listner )
    {
      listenrs.Add( listner );
    }

    public void UnregisterEchoListner( IDebugEchoListner listner )
    {
      listenrs.Remove( listner );
    }

    public void Echo( DebugCommandMessage messageType, string text )
    {
      lines.Enqueue( text );
      while ( lines.Count >= MaxLineCount )
        lines.Dequeue();

      foreach ( IDebugEchoListner listner in listenrs )
        listner.Echo( messageType, text );
    }

    public void Echo( string text )
    {
      Echo( DebugCommandMessage.Standard, text );
    }

    public void EchoWarning( string text )
    {
      Echo( DebugCommandMessage.Warning, text );
    }

    public void EchoError( string text )
    {
      Echo( DebugCommandMessage.Error, text );
    }

    public void PushExecutioner( IDebugCommandExecutioner executioner )
    {
      executioners.Push( executioner );
    }

    public void PopExecutioner()
    {
      executioners.Pop();
    }

    #endregion

    #region Update and Draw

    public override void Update( GameTime gameTime )
    {
      KeyboardState keyState = Keyboard.GetState();

      float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
      const float OpenSpeed = 8.0f;
      const float CloseSpeed = 8.0f;

      switch ( state )
      {
        case State.Closed:
          if ( keyState.IsKeyDown( Keys.Tab ) )
            state = State.Opening;
          break;
        case State.Opening:
          stateTransition += dt * OpenSpeed;
          if ( stateTransition > 1.0f )
          {
            stateTransition = 1.0f;
            state = State.Opened;
          }
          break;
        case State.Opened:
          ProcessKeyInputs( dt );
          break;
        case State.Closing:
          stateTransition -= dt * CloseSpeed;
          if ( stateTransition < 0.0f )
          {
            stateTransition = 0.0f;
            state = State.Closed;
          }
          break;
      }

      prevKeyState = keyState;

      base.Update( gameTime );
    }

    public void ProcessKeyInputs( float dt )
    {
      KeyboardState keyState = Keyboard.GetState();
      Keys[] keys = keyState.GetPressedKeys();

      bool shift = keyState.IsKeyDown( Keys.LeftShift ) ||
                      keyState.IsKeyDown( Keys.RightShift );

      foreach ( Keys key in keys )
      {
        if ( !IsKeyPressed( key, dt ) ) continue;

        char ch;
        if ( KeyboardUtils.KeyToString( key, shift, out ch ) )
        {
          commandLine = commandLine.Insert( cursorIndex, new string( ch, 1 ) );
          cursorIndex++;
        }
        else
        {
          switch ( key )
          {
            case Keys.Back:
              if ( cursorIndex > 0 )
                commandLine = commandLine.Remove( --cursorIndex, 1 );
              break;
            case Keys.Delete:
              if ( cursorIndex < commandLine.Length )
                commandLine = commandLine.Remove( cursorIndex, 1 );
              break;
            case Keys.Left:
              if ( cursorIndex > 0 )
                cursorIndex--;
              break;
            case Keys.Right:
              if ( cursorIndex < commandLine.Length )
                cursorIndex++;
              break;
            case Keys.Enter:
              ExecuteCommand( commandLine );
              commandLine = string.Empty;
              cursorIndex = 0;
              break;
            case Keys.Up:
              if ( commandHistory.Count > 0 )
              {
                commandHistoryIndex =
                    Math.Max( 0, commandHistoryIndex - 1 );

                commandLine = commandHistory[commandHistoryIndex];
                cursorIndex = commandLine.Length;
              }
              break;
            case Keys.Down:
              if ( commandHistory.Count > 0 )
              {
                commandHistoryIndex = Math.Min( commandHistory.Count - 1,
                                                commandHistoryIndex + 1 );
                commandLine = commandHistory[commandHistoryIndex];
                cursorIndex = commandLine.Length;
              }
              break;
            case Keys.Tab:
              state = State.Closing;
              break;
          }
        }
      }

    }

    bool IsKeyPressed( Keys key, float dt )
    {
      if ( prevKeyState.IsKeyUp( key ) )
      {
        keyRepeatTimer = keyRepeatStartDuration;
        pressedKey = key;
        return true;
      }

      if ( key == pressedKey )
      {
        keyRepeatTimer -= dt;
        if ( keyRepeatTimer <= 0.0f )
        {
          keyRepeatTimer += keyRepeatDuration;
          return true;
        }
      }

      return false;
    }

    public override void Draw( GameTime gameTime )
    {
      if ( state == State.Closed )
        return;

      SpriteFont font = debugManager.DebugFont;
      SpriteBatch spriteBatch = debugManager.SpriteBatch;
      Texture2D whiteTexture = debugManager.WhiteTexture;

      float w = GraphicsDevice.Viewport.Width;
      float h = GraphicsDevice.Viewport.Height;
      float topMargin = h * 0.1f;
      float leftMargin = w * 0.1f;

      Rectangle rect = new Rectangle();
      rect.X = (int)leftMargin;
      rect.Y = (int)topMargin;
      rect.Width = (int)( w * 0.8f );
      rect.Height = (int)( MaxLineCount * font.LineSpacing );

      Matrix mtx = Matrix.CreateTranslation(
                  new Vector3( 0, -rect.Height * ( 1.0f - stateTransition ), 0 ) );

      spriteBatch.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                          SaveStateMode.SaveState, mtx );

      spriteBatch.Draw( whiteTexture, rect, new Color( 0, 0, 0, 200 ) );

      Vector2 pos = new Vector2( leftMargin, topMargin );
      foreach ( string line in lines )
      {
        spriteBatch.DrawString( font, line, pos, Color.White );
        pos.Y += font.LineSpacing;
      }

      string leftPart = Prompt + commandLine.Substring( 0, cursorIndex );
      Vector2 cursorPos = pos + font.MeasureString( leftPart );
      cursorPos.Y = pos.Y;

      spriteBatch.DrawString( font,
          String.Format( "{0}{1}", Prompt, commandLine ), pos, Color.White );
      spriteBatch.DrawString( font, Cursor, cursorPos, Color.White );

      spriteBatch.End();
    }

    #endregion
  }
}
