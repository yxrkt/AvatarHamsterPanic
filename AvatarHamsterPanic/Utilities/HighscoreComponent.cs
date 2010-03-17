
/*
This code is released Open Source under the MIT license. Copyright 2008 Jon Watte, 
All Rights Reserved. You may use it free of charge for any purposes, provided that 
Jon Watte's copyright is reproduced in your use, and that you indemnify and hold 
harmless Jon Watte from any claim arising out of any use (or lack of use or lack of 
ability of use) you make of it. This software is provided as-is, without any 
warranty or guarantee, including any implicit guarantee of merchantability or fitness 
for any particular purpose. Use at your own risk!

For more information and updates, stop by my XNA programming area at
http://www.enchantedage.com/highscores
*/


using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using System.Linq;
using System.Diagnostics;

// This is how the highscore component works (I think) ///////////////////////////////////
//
// 1  Load all scores for each user from local data and remote save files
// 2  Chop off duplicate user scores and scores more than 50
// 3  Put all scores into a giant list
// 4  Sort the giant list
// 5  Trim the giant list entries to 50 only
// 6  Remove duplicates
// 7  Save the giant list to the remote save files list
// 8  Save each user score list to the local save file
// 9  Send the giant list to connected players. Also personal scores if they are friended
// 10 Recieve the giant list from any connected players and save each user scores to the userscores list
// 11 Repeat

namespace Menu
{
  public enum LeaderBoardType
  {
    Local,
    Friend,
    Global,
  }

  public class HighscoreComponent : GameComponent
  {
    NetworkSessionProperties identifier_;
    bool _hasReadFile;
    bool _hasReadNetwork;
    //bool _versionMismatchSeen;
    bool _saveSoon;
    bool _userWantsToLoad = true;

    public bool UserWantsToLoad
    {
      get { return _userWantsToLoad; }
      set { _userWantsToLoad = value; }
    }
    bool isJoin_;
    bool isCreate_;
    bool timeToDisconnect_;
    double sessionDuration_;
    float backoffTime_ = 1;
    PacketReader reader_ = new PacketReader();
    PacketWriter writer_ = new PacketWriter();
    const byte Version = 4;
    public const int MaxAggregateCount = 100; //  top 50 network wide
    IAsyncResult async_;
    NetworkSession _session;
    Dictionary<string, Boolean> previousHosts_ = new Dictionary<string, Boolean>();
    Dictionary<string, List<Highscore>> userScores_ = new Dictionary<string, List<Highscore>>();
    Dictionary<NetworkGamer, List<Highscore>> toSend_ = new Dictionary<NetworkGamer, List<Highscore>>();
    List<Highscore> aggregateHighscores_ = new List<Highscore>();
    string titleName_;
    public static HighscoreComponent Global;
    const string remoteSaveFile_ = "remoteSaveFile1.dat";
    const string localSaveFile_ = "localSaveFile1.dat";
    TimeSpan prevHostClearTimer_;
    bool connectedToFriend_ = false;

    Dictionary<string, string> friends_ = new Dictionary<string, string>();
    Dictionary<string, string> liveGamers_ = new Dictionary<string, string>();

#if XBOX360
    public static string HostType = "Xbox360";
#else
    public static string HostType = "Win32";
#endif
    /// <summary>
    /// Create a new HighscoreComponent, which will keep track of your highscores.
    /// It will also use Xbox Live! networking to exchange scores with other users, 
    /// while the component is enabled. If you don't want this to happen in the 
    /// background while your game is running, disable the component at that time.
    /// </summary>
    /// <param name="g">The game.</param>
    /// <param name="identifier">How to identify the highscore sessions (can be null 
    /// if you don't want to use Live! yourself at other times).</param>
    /// <param name="titleName">The name of your game.</param>
    public HighscoreComponent( Game g, NetworkSessionProperties identifier, string titleName )
      : base( g )
    {
      identifier_ = identifier;
      titleName_ = titleName;
      Global = this;

    }

    /// <summary>
    /// Update the scores for the given player to reflect a new score result. 
    /// These scores may be pushed through the network in the future, and they 
    /// will be saved to disk/memory unit within the next few seconds.
    /// </summary>
    /// <param name="player">The player to record a new score for.</param>
    /// <param name="score">The score. This does not need to be a high score or top score.</param>
    /// <param name="message">A message to aggregate with the score -- for example, the max level 
    /// reached. Max 32 characters.</param>
    public bool SetNewScore( PlayerIndex player, long score, string message )
    {
      if ( !SignedInGamer.SignedInGamers[player].IsLiveEnabled() || score == 0 )
        return false;

      bool isNewRecordScore = false;
      string name = NameFromPlayer( player );
      List<Highscore> lhs;
      if ( !userScores_.TryGetValue( name, out lhs ) )
      {
        lhs = new List<Highscore>();
        userScores_.Add( name, lhs );
      }

      //set whether or not this is a new highscore. Assume the first score is the highest score.
      if ( lhs.Count == 0 || lhs[0].Score < score )
        isNewRecordScore = true;

      //Don't save duplicate scores.
      if ( lhs.Select( i => i.Score ).Contains( score ) )
        return false;

      Highscore hs = new Highscore();
      hs.Gamer = name;
      hs.Message = message;
      hs.Score = score;
      hs.When = DateTime.Now;
      hs.IsLocal = true;
      lhs.Add( hs );
      _saveSoon = true;

      //Sort the list
      lhs.Sort();

      return isNewRecordScore;
    }

    /// <summary>
    /// Manually forces a name into the highscore list. You cannot enter scores at 0 points
    /// or duplicate scores
    /// </summary>
    /// <param name="player">The player to record a new score for.</param>
    /// <param name="score">The score. This does not need to be a high score or top score.</param>
    /// <param name="message">A message to aggregate with the score -- for example, the max level 
    /// reached. Max 32 characters.</param>
    public bool SetNewScore( string gamerTag, long score, string message )
    {
      if ( score == 0 )
        return false;

      bool isNewRecordScore = false;
      string name = gamerTag;
      List<Highscore> lhs;
      if ( !userScores_.TryGetValue( name, out lhs ) )
      {
        lhs = new List<Highscore>();
        userScores_.Add( name, lhs );
      }

      //set whether or not this is a new highscore. Assume the first score is the highest score.
      if ( lhs.Count == 0 || lhs[0].Score < score )
        isNewRecordScore = true;

      //Don't save duplicate scores.
      if ( lhs.Select( i => i.Score ).Contains( score ) )
        return false;

      Highscore hs = new Highscore();
      hs.Gamer = name;
      hs.Message = message;
      hs.Score = score;
      hs.When = DateTime.Now;
      hs.IsLocal = true;
      lhs.Add( hs );
      _saveSoon = true;

      //Sort the scores
      lhs.Sort();

      return isNewRecordScore;
    }

    public static string NameFromPlayer( PlayerIndex player )
    {
      string name = "* Not Signed In *";
      foreach ( SignedInGamer sig in Gamer.SignedInGamers )
      {
        if ( sig.PlayerIndex == player )
        {
          name = sig.Gamertag;
          break;
        }
      }
      return name;
    }

    public bool HasReadFile { get { return _hasReadFile; } }
    public bool HasReadNetwork { get { return _hasReadNetwork; } }

    /// <summary>
    /// Fill in a given array with highscores. Return the actual number of highscores 
    /// that were returned (which may be smaller). Returns highscores for everyone I've 
    /// ever seen.
    /// </summary>
    /// <param name="space">Where to put the highscores.</param>
    /// <returns>The number of highscores returned in the array.</returns>
    public int GetHighscores( Highscore[] space, LeaderBoardType type, PlayerIndex? pi )
    {
      switch ( type )
      {
        case LeaderBoardType.Local:
          {
            if ( pi.HasValue )
            {
              string name = NameFromPlayer( pi.Value );
              List<Highscore> lhs;
              if ( !userScores_.TryGetValue( name, out lhs ) )
                lhs = new List<Highscore>();
              int n = Math.Min( space.Length, lhs.Count );
              for ( int i = 0; i != space.Length; ++i )
              {
                space[i] = ( i < n ) ? lhs[i] : null;
              }
              return n;
            }
            else
            {
              System.Diagnostics.Debug.Assert( false );
              return 0;
            }
          }
        case LeaderBoardType.Friend:
          {
            if ( pi.HasValue )
            {
              var friendScoreList = new List<Highscore>();
              foreach ( var userScore in userScores_ )
              {
                //Add all friend scores
                foreach ( var friend in friends_ )
                {
                  if ( userScore.Key == friend.Key )
                  {
                    friendScoreList.AddRange( userScore.Value );
                  }
                }

                //Add all local gamer scores
                foreach ( var gamer in liveGamers_ )
                {
                  if ( userScore.Key == gamer.Key )
                    friendScoreList.AddRange( userScore.Value );
                }
              }

              friendScoreList.Sort();
              int n = Math.Min( space.Length, friendScoreList.Count );
              for ( int i = 0; i < space.Length; i++ )
              {
                space[i] = ( i < n ) ? friendScoreList[i] : null;
              }
              return n;
            }
            else
            {
              System.Diagnostics.Debug.Assert( false );
              return 0;
            }
          }
        case LeaderBoardType.Global:
          {
            int n = Math.Min( space.Length, aggregateHighscores_.Count );
            for ( int i = 0; i != space.Length; ++i )
            {
              space[i] = ( i < n ) ? aggregateHighscores_[i] : null;
            }
            return n;
          }
        default:
          System.Diagnostics.Debug.Assert( false );
          return 0;
      }
    }

    /// <summary>
    /// Fill in a given array with highscores. Return the actual number of highscores 
    /// that were returned (which may be smaller). Returns highscores only for the 
    /// given player (or "Anonymous" if not signed in).
    /// </summary>
    /// <param name="space">Where to put the highscores.</param>
    /// <param name="player">The player to return highscores for.</param>
    /// <returns>The number of highscores returned in the array.</returns>
    public int GetHighscores( Highscore[] space, PlayerIndex player )
    {
      string name = NameFromPlayer( player );
      List<Highscore> lhs;
      if ( !userScores_.TryGetValue( name, out lhs ) )
        lhs = new List<Highscore>();
      int n = Math.Min( space.Length, lhs.Count );
      for ( int i = 0; i != space.Length; ++i )
      {
        space[i] = ( i < n ) ? lhs[i] : null;
      }
      return n;
    }

    public delegate bool FilterFunc<T>( T t );

    public static GameComponent Find<T>( IEnumerable<T> collection, FilterFunc<T> func ) where T : class
    {
      foreach ( T t in collection )
        if ( func( t ) )
          return t as GameComponent;
      return null;
    }

    public override void Initialize()
    {
      base.Initialize();

      GamerServicesComponent gsc = Find<IGameComponent>( Game.Components, FilterGamerServicesComponent )
        as GamerServicesComponent;
      if ( gsc == null )
        throw new InvalidOperationException( "You must add the GamerServicesComponent to your component collection." );
    }

    private static bool FilterGamerServicesComponent( IGameComponent gc )
    {
      return gc is GamerServicesComponent;
    }

    protected override void Dispose( bool disposing )
    {
      StopImmediately();
      base.Dispose( disposing );
    }

    protected override void OnEnabledChanged( object sender, EventArgs args )
    {
      base.OnEnabledChanged( sender, args );
      if ( this.Enabled == false )
        StopImmediately();
    }

    void StopImmediately()
    {
      if ( this.async_ != null )
      {
        try
        {
          if ( isCreate_ )
          {
            NetworkSession.EndCreate( async_ );
          }
          else if ( isJoin_ )
          {
            NetworkSession.EndJoin( async_ );
          }
          else
          {
            NetworkSession.EndFind( async_ );
          }
        }
        catch ( System.Exception x )
        {
          System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
        }
        async_ = null;
        isJoin_ = false;
        isCreate_ = false;
      }
      if ( this._session != null )
      {
        try
        {
          //  abruptly terminate the session if you want to disable this component
          _session.Dispose();
        }
        catch ( System.Exception x )
        {
          System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
        }
        _session = null;
      }
    }

    public void ClearHighscores()
    {
      _clearOne = true;
      aggregateHighscores_ = new List<Highscore>();
      userScores_ = new Dictionary<string, List<Highscore>>();
      _saveSoon = true;
    }

    public void PurgeHighscoresOlderThan( DateTime time )
    {
      bool removed = false;
      foreach ( KeyValuePair<string, List<Highscore>> kvp in userScores_ )
      {
        for ( int i = 0, n = kvp.Value.Count; i != n; ++i )
        {
          Highscore hs = kvp.Value[i];
          if ( hs.When < time )
          {
            kvp.Value.RemoveAt( i );
            --i;
            --n;
            removed = true;
          }
        }
      }
      if ( removed )
        _saveSoon = true;
    }

    IAsyncResult _deviceAsync;
    StorageDevice _storage;

    public StorageDevice Storage
    {
      get { return _storage; }
    }
    bool _clearOne;

    public override void Update( GameTime gameTime )
    {
      UpdateFriendsList();
      UpdatePrevHostList( gameTime );

      if ( _session != null )
      {
        sessionDuration_ += gameTime.ElapsedRealTime.TotalSeconds;
        if ( sessionDuration_ > 30 )
        {
          timeToDisconnect_ = true;
        }
      }
      if ( _userWantsToLoad || _saveSoon )
      {
        if ( _storage != null && !_storage.IsConnected )
        {
          _storage = null;
        }
        if ( _storage == null )
        {
          LoadStorageDevice();
        }
        else
        {
          try
          {
            byte[] data = new byte[1000];
            GamePadState gps = GamePad.GetState( PlayerIndex.One );
            using ( StorageContainer sc = _storage.OpenContainer( titleName_ ) )
            {
              if ( _userWantsToLoad )
              {
                AddRemoteScores( sc );

                AddLocalScores( sc );
              }
              //  now, sort out all scores
              AggregateScores();

              //  finally, write out the information we have
              SaveRemoteScores( sc );
              SaveLocalScores( sc );

              _userWantsToLoad = false;
              _saveSoon = false;
              _hasReadFile = true;
              _clearOne = false;
            }
          }
          catch ( System.Exception x )
          {
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
            _storage = null;
            _userWantsToLoad = false;
            _saveSoon = false;
          }
        }
      }
      base.Update( gameTime );
      if ( backoffTime_ > 0 )
      {
        backoffTime_ -= (float)gameTime.ElapsedRealTime.TotalSeconds;
        return;
      }
      try
      {
        if ( this.Enabled == true && this._session == null && async_ == null )
        {
          backoffTime_ = 15;
          foreach ( SignedInGamer sig in Gamer.SignedInGamers )
          {
            if ( sig.Privileges.AllowOnlineSessions )
            {
              //  if I already got data from 5 people, then host myself instead
              if ( previousHosts_.Count > 4 )
              {
                //  OK, it's time for me to host
                System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning PlayerMatch creation.", HostType ) );
                async_ = NetworkSession.BeginCreate( NetworkSessionType.PlayerMatch, 1, 31, 0,
                    identifier_, null, null );
                isCreate_ = true;
              }
              else
              {
                System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning PlayerMatch find.", HostType ) );
                async_ = NetworkSession.BeginFind( NetworkSessionType.PlayerMatch, 1, identifier_, null, null );
              }
              backoffTime_ = 0;
              break;
            }
          }
          if ( async_ == null )
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: No signed in gamer is allowed online sessions.", HostType ) );
        }
        if ( async_ != null && async_.IsCompleted )
        {
          if ( isJoin_ )
          {
            isJoin_ = false;
            _session = NetworkSession.EndJoin( async_ );
            async_ = null;
            previousHosts_.Add( _session.Host.Gamertag, true );
            List<Highscore> lhs = new List<Highscore>();
            lhs.AddRange( aggregateHighscores_ );

            //If we are connected to a friend we should send all of our scores, not just the top scores
            if ( connectedToFriend_ )
            {
              foreach ( var liveGamer in liveGamers_ )
              {
                //Add all the scores of the signend in gamers on the local xbox to the scores
                string gamerTag = liveGamer.Key;
                if ( userScores_.ContainsKey( gamerTag ) )
                {
                  lhs.AddRange( userScores_[gamerTag] );
                }
              }
            }

            toSend_.Add( _session.Host, lhs );
            _session.SessionEnded += new EventHandler<NetworkSessionEndedEventArgs>( session__SessionEnded );
          }
          else if ( isCreate_ )
          {
            isCreate_ = false;
            _session = NetworkSession.EndCreate( async_ );
            async_ = null;
            _session.GamerJoined += new EventHandler<GamerJoinedEventArgs>( session__GamerJoined );
            _session.GamerLeft += new EventHandler<GamerLeftEventArgs>( session__GamerLeft );
            _session.SessionEnded += new EventHandler<NetworkSessionEndedEventArgs>( session__SessionEnded );
          }
          else
          {
            backoffTime_ = 15;
            AvailableNetworkSessionCollection ansc = NetworkSession.EndFind( async_ );
            async_ = null;

            //Attepmt to connect to friends before anyone else
            connectedToFriend_ = false;
            foreach ( var ans in ansc )
            {
              if ( friends_.ContainsKey( ans.HostGamertag ) && !previousHosts_.ContainsKey( ans.HostGamertag ) )
              {
                isJoin_ = true;
                System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning PlayerMatch join.", HostType ) );
                async_ = NetworkSession.BeginJoin( ans, null, null );
                backoffTime_ = 0;
                connectedToFriend_ = true;
              }
            }

            //Connect to other people if we failed to connect to friends.
            if ( !connectedToFriend_ )
            {
              foreach ( AvailableNetworkSession ans in ansc )
              {
                if ( !previousHosts_.ContainsKey( ans.HostGamertag ) )
                {
                  //  connect to this guy
                  isJoin_ = true;
                  System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning PlayerMatch join.", HostType ) );
                  async_ = NetworkSession.BeginJoin( ans, null, null );
                  backoffTime_ = 0;
                }
              }
            }

            if ( !isJoin_ && aggregateHighscores_.Count > 0 )
            {
              //  OK, it's time for me to host
              System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning PlayerMatch creation.", HostType ) );
              async_ = NetworkSession.BeginCreate( NetworkSessionType.PlayerMatch, 1, 31, 0,
                  identifier_, null, null );
              isCreate_ = true;
              backoffTime_ = 0;
            }
          }
        }
      }
      catch ( System.Exception x )
      {
        async_ = null;
        _session = null;
        System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
        backoffTime_ = 15;
      }
      if ( _session != null )
      {
        try
        {
          _session.Update();
          if ( _session == null ) //  did it go away by an event from within update?
            return;
          foreach ( LocalNetworkGamer lng in _session.LocalGamers )
          {
            NetworkGamer sender = null;
            bool gotdata = false;
            while ( lng.IsDataAvailable )
            {
              gotdata = true;
              lng.ReceiveData( reader_, out sender );
              byte v = reader_.ReadByte();
              if ( v != Version )
              {
                //  v == 0 just means "disconnect now"
                if ( v != 0 )
                {
                  System.Diagnostics.Trace.WriteLine( String.Format( "{3}: Found protocol version {0} from {2}, I have {1}.",
                      v, Version, _session.Host.Gamertag, HostType ) );
                  //_versionMismatchSeen = true;
                }
                System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Disconnect packet from {1}", HostType, sender.Gamertag ) );
                timeToDisconnect_ = !_session.IsHost;
              }
              else
              {
                Highscore hs = new Highscore();
                if ( hs.Read( reader_ ) )
                {
                  _hasReadNetwork = true;
                  if ( !userScores_.ContainsKey( hs.Gamer ) )
                    userScores_.Add( hs.Gamer, new List<Highscore>() );
                  if ( userScores_[hs.Gamer].Select( i => i.Score ).Contains( hs.Score ) == false )
                    userScores_[hs.Gamer].Add( hs );
                  System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Received highscore {1}",
                      HostType, hs.Encode() ) );
                }
              }
            }
            if ( gotdata && _session != null )
            {
              System.Diagnostics.Trace.WriteLine( String.Format( "{1}: Got data from {0}", sender.Gamertag, HostType ) );
            }
          }
          System.Diagnostics.Debug.Assert( _session != null );
          if ( toSend_.Count > 0 )
          {
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Sending data...", HostType ) );
            NetworkGamer toRemove = null;
            foreach ( KeyValuePair<NetworkGamer, List<Highscore>> kvp in toSend_ )
            {
              //  create the disconnect packet
              if ( kvp.Value.Count == 0 )
              {
                System.Diagnostics.Trace.WriteLine( String.Format( "{1}: Writing bye-bye packet to {0}", kvp.Key.Gamertag, HostType ) );
                writer_.Write( (byte)0 );
                _session.LocalGamers[0].SendData( writer_, SendDataOptions.ReliableInOrder, kvp.Key );
                toRemove = kvp.Key;
              }
              else
              {
                System.Diagnostics.Trace.WriteLine( String.Format( "{2}: Writing packet to {0}, {1} to go.", kvp.Key.Gamertag, kvp.Value.Count, HostType ) );
                writer_.Write( (byte)Version );
                if ( kvp.Value[kvp.Value.Count - 1].Write( writer_ ) )
                  _session.LocalGamers[0].SendData( writer_, SendDataOptions.Reliable, kvp.Key );
                kvp.Value.RemoveAt( kvp.Value.Count - 1 );
              }
            }
            if ( toRemove != null )
            {
              System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Removing {1} from list of send destinations.",
                  HostType, toRemove.Gamertag ) );
              toSend_.Remove( toRemove );
            }
          }
          else if ( timeToDisconnect_ )
          {
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Time has come to say good-bye!", HostType ) );
            session__SessionEnded( null, null );
          }
        }
        catch ( System.Exception x )
        {
          System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
          _session = null;
          //  effectively disable any more updates
          backoffTime_ = 1e30f;
        }
      }

    }

    private void UpdateFriendsList()
    {
      //Scan to see if anyone else signed in
      int xboxLiveGamers = 0;
      foreach ( var gamer in SignedInGamer.SignedInGamers )
      {
        if ( gamer.IsLiveEnabled() )
          ++xboxLiveGamers;
      }

      //Re-initialize the list if someone new signed on.
      if ( xboxLiveGamers != liveGamers_.Count )
      {
        try
        {
          liveGamers_.Clear();
          friends_.Clear();
          foreach ( var gamer in SignedInGamer.SignedInGamers )
          {
            if ( gamer.IsLiveEnabled() )
            {
              liveGamers_[gamer.Gamertag] = gamer.Gamertag;

              var friends = gamer.GetFriends();
              foreach ( var friend in friends )
              {
                friends_[friend.Gamertag] = gamer.Gamertag;
              }
            }

          }
        }
        catch
        {
        }
      }
    }

    private void UpdatePrevHostList( GameTime gametime )
    {
      prevHostClearTimer_ -= gametime.ElapsedGameTime;
      if ( prevHostClearTimer_ <= TimeSpan.Zero )
      {
        previousHosts_.Clear();
        prevHostClearTimer_ = TimeSpan.FromMinutes( 2 );
      }
    }

    private void SaveLocalScores( StorageContainer sc )
    {
      string path = System.IO.Path.Combine( sc.Path, localSaveFile_ );
      using ( System.IO.StreamWriter sw = new System.IO.StreamWriter( path ) )
      {
        foreach ( KeyValuePair<string, List<Highscore>> kvp in userScores_ )
        {
          foreach ( Highscore hs in kvp.Value )
          {
            if ( hs.IsLocal )
            {
              string str = hs.Encode();
              if ( str != null && str != "" )
                sw.WriteLine( str );
            }
          }
        }
        sw.WriteLine(); //  empty line terminates file
      }
    }

    private void SaveRemoteScores( StorageContainer sc )
    {
      string path = System.IO.Path.Combine( sc.Path, remoteSaveFile_ );
      using ( System.IO.StreamWriter sw = new System.IO.StreamWriter( path ) )
      {
        foreach ( Highscore hs in aggregateHighscores_ )
        {
          string str = hs.Encode();
          if ( str != null && str != "" )
            sw.WriteLine( str );
        }
        sw.WriteLine(); //  empty line ends the file
      }
    }

    private void AddLocalScores( StorageContainer sc )
    {
      string path = System.IO.Path.Combine( sc.Path, localSaveFile_ );
      if ( System.IO.File.Exists( path ) && !_clearOne )
      {
        using ( System.IO.StreamReader sr = new System.IO.StreamReader( path ) )
        {
          while ( !sr.EndOfStream )
          {
            string str = sr.ReadLine();
            if ( str == "" )
              break;
            Highscore hs = new Highscore();
            if ( hs.Decode( str ) )
            {
              hs.IsLocal = true;
              if ( !userScores_.ContainsKey( hs.Gamer ) )
                userScores_.Add( hs.Gamer, new List<Highscore>() );
              userScores_[hs.Gamer].Add( hs );
            }
          }
        }
      }
    }

    private void AddRemoteScores( StorageContainer sc )
    {
      string path = System.IO.Path.Combine( sc.Path, remoteSaveFile_ );
      if ( System.IO.File.Exists( path ) && !_clearOne )
      {
        using ( System.IO.StreamReader sr = new System.IO.StreamReader( path ) )
        {
          while ( !sr.EndOfStream )
          {
            string str = sr.ReadLine();
            if ( str == "" )
              break;
            Highscore hs = new Highscore();
            if ( hs.Decode( str ) )
            {
              if ( !userScores_.ContainsKey( hs.Gamer ) )
                userScores_.Add( hs.Gamer, new List<Highscore>() );
              userScores_[hs.Gamer].Add( hs );
            }
          }
        }
      }
    }

    private void LoadStorageDevice()
    {
      if ( _deviceAsync == null )
      {
        if ( !Guide.IsVisible )
        {
          try
          {
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Beginning device selection.", HostType ) );
            _deviceAsync = Guide.BeginShowStorageDeviceSelector( null, null );
          }
          catch ( GuideAlreadyVisibleException gavx )
          {
            System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, gavx.Message ) );
          }
        }
      }
      else if ( _deviceAsync.IsCompleted )
      {
        try
        {
          _storage = Guide.EndShowStorageDeviceSelector( _deviceAsync );
        }
        catch ( System.Exception x )
        {
          System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HostType, x.Message ) );
        }
        _deviceAsync = null;
        if ( _storage == null )
        {
          _userWantsToLoad = false;
        }
      }
    }

    void session__SessionEnded( object sender, NetworkSessionEndedEventArgs e )
    {
      System.Diagnostics.Trace.WriteLine( String.Format( "{0}: SessionEnded", HostType ) );
      if ( _session != null )
      {
        sessionDuration_ = 0;
        timeToDisconnect_ = false;
        _session.Dispose();
        _session = null;
        AggregateScores();
      }
      toSend_ = new Dictionary<NetworkGamer, List<Highscore>>();
      //  create a new session in 15 seconds
      backoffTime_ = 15;
    }

    void session__GamerLeft( object sender, GamerLeftEventArgs e )
    {
      System.Diagnostics.Trace.WriteLine( String.Format( "{0}: GamerLeft: {1}", HostType, e.Gamer.Gamertag ) );
      if ( toSend_.ContainsKey( e.Gamer ) )
        toSend_.Remove( e.Gamer );
      _saveSoon = true;
    }

    void session__GamerJoined( object sender, GamerJoinedEventArgs e )
    {
      if ( !e.Gamer.IsLocal )
      {
        foreach ( LocalNetworkGamer lng in _session.LocalGamers )
        {
          lng.EnableSendVoice( e.Gamer, false );
        }
      }
      //  don't send to myself
      System.Diagnostics.Trace.WriteLine( String.Format( "{0}: GamerJoined: {1}", HostType, e.Gamer.Gamertag ) );
      if ( e.Gamer == _session.LocalGamers[0] )
        return;
      List<Highscore> lhs = new List<Highscore>();
      lhs.InsertRange( 0, aggregateHighscores_ );

      //If this is a friend connection, we want to send all of our friend scores too
      if ( friends_.ContainsKey( e.Gamer.Gamertag ) )
      {
        foreach ( var liveGamer in liveGamers_ )
        {
          //Add all the scores of the signend in gamers on the local xbox to the scores
          string gamerTag = liveGamer.Key;
          if ( userScores_.ContainsKey( gamerTag ) )
          {
            lhs.AddRange( userScores_[gamerTag] );
          }
        }
      }

      toSend_.Add( e.Gamer, lhs );
    }

    void AggregateScores()
    {
      System.Diagnostics.Trace.WriteLine( String.Format( "{0}: Calculating Aggregate Scores", HostType ) );
      aggregateHighscores_.Clear();

      long lowestScore = long.MaxValue;
      foreach ( KeyValuePair<string, List<Highscore>> kvp in userScores_ )
      {
        if ( kvp.Value.Count > 0 )
        {

          kvp.Value.Sort();
          //  remove duplicates
          Highscore prev = kvp.Value[0];
          for ( int i = 1, n = kvp.Value.Count; i != n; ++i )
          {
            Highscore hs = kvp.Value[i];
            if ( hs.Gamer == prev.Gamer && hs.When == prev.When && hs.Score == prev.Score )
            {
              if ( !hs.IsLocal )
                kvp.Value.RemoveAt( i );
              else
                kvp.Value.RemoveAt( i - 1 );
              --i;
              --n;
            }
            else
            {
              prev = hs;
            }
          }
          //  make sure each entry is not too big
          if ( kvp.Value.Count > MaxAggregateCount )
          {
            kvp.Value.RemoveRange( MaxAggregateCount, kvp.Value.Count - MaxAggregateCount );
          }

          //Alawys add scores if we are below MaxAggregateCount but if we go over, we only want to add the score if it's greater than the lowest score.
          if ( aggregateHighscores_.Count < MaxAggregateCount || kvp.Value[0].Score > lowestScore )
            aggregateHighscores_.Add( kvp.Value[0] );
          lowestScore = (long)Math.Min( lowestScore, kvp.Value[0].Score );
        }
      }
      //  sort and prune the "remote" high score list
      if ( aggregateHighscores_.Count > 0 )
      {
        aggregateHighscores_.Sort();
        if ( aggregateHighscores_.Count > MaxAggregateCount )
          aggregateHighscores_.RemoveRange( MaxAggregateCount, aggregateHighscores_.Count - MaxAggregateCount );
        Highscore prev = aggregateHighscores_[0];
        for ( int i = 1, n = aggregateHighscores_.Count; i != n; ++i )
        {
          Highscore cur = aggregateHighscores_[i];
          if ( prev.Score == cur.Score && prev.Gamer == cur.Gamer && prev.When == cur.When )
          {
            //  remove a duplicate
            if ( cur.IsLocal )
            {
              //  if I got one from a remote guy, and another from locally, then 
              //  keep the local score
              aggregateHighscores_.RemoveAt( i - 1 );
            }
            else
            {
              aggregateHighscores_.RemoveAt( i );
            }
            --i;
            --n;
          }
          else
          {
            prev = cur;
          }
        }
      }
      _hasReadNetwork = previousHosts_.Count > 0;
      if ( HighscoresChanged != null && aggregateHighscores_.Count > 0 )
        HighscoresChanged( this, aggregateHighscores_[0] );
    }

    //  You can be told when the set of known highscores changes.    
    public event HighscoresChanged HighscoresChanged;
  }

  public delegate void HighscoresChanged( HighscoreComponent sender, Highscore highestScore );

  public class Highscore : IComparable<Highscore>
  {
    public DateTime When;
    public string Gamer;
    public long Score;
    public string Message;
    public bool IsLocal;

    public string Encode()
    {
      return String.Format( "{0};{1};{2};{3}", When.Ticks, Gamer, Score, Message );
    }

    public bool Decode( string str )
    {
      try
      {
        if ( str == null ) return false;
        string[] data = str.Split( ';' );
        if ( data.Length != 4 ) return false;
        When = new DateTime( Int64.Parse( data[0] ) );
        Gamer = data[1];
        Score = Int64.Parse( data[2] );
        Message = data[3];
        return true;
      }
      catch ( System.Exception x )
      {
        System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HighscoreComponent.HostType, x.Message ) );
        return false;
      }
    }

    public bool Write( PacketWriter wr )
    {
      try
      {
        if ( Gamer.Length > 32 )
          Gamer = Gamer.Substring( 0, 32 );
        if ( Message.Length > 32 )
          Message = Message.Substring( 0, 32 );
        wr.Write( (long)When.Ticks );
        wr.Write( Gamer );
        wr.Write( Score );
        wr.Write( Message );
      }
      catch ( System.Exception x )
      {
        System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HighscoreComponent.HostType, x.Message ) );
        return false;
      }
      return true;
    }

    public bool Read( PacketReader rd )
    {
      try
      {
        When = new DateTime( rd.ReadInt64() );
        Gamer = rd.ReadString();
        Score = rd.ReadInt64();
        Message = rd.ReadString();
      }
      catch ( System.Exception x )
      {
        System.Diagnostics.Trace.WriteLine( String.Format( "{0}: {1}", HighscoreComponent.HostType, x.Message ) );
        return false;
      }
      return true;
    }

    #region IComparable<Highscore> Members

    public int CompareTo( Highscore other )
    {
      if ( other == null ) return -1;
      if ( other.Score > Score ) return 1;
      if ( other.Score < Score ) return -1;
      if ( other.When > When ) return 1;
      if ( other.When < When ) return -1;
      return other.Gamer.CompareTo( Gamer );
    }

    #endregion
  }


  public static class IsLiveAccountExt
  {
    //Live accounts should have any one of the following privleges
    static public bool IsLiveEnabled( this SignedInGamer sig )
    {
      if ( sig == null )
        return false;

      return
          sig.IsSignedInToLive || sig.Privileges.AllowCommunication != GamerPrivilegeSetting.Blocked || sig.Privileges.AllowOnlineSessions ||
          sig.Privileges.AllowProfileViewing != GamerPrivilegeSetting.Blocked || sig.Privileges.AllowPurchaseContent ||
          sig.Privileges.AllowTradeContent || sig.Privileges.AllowUserCreatedContent != GamerPrivilegeSetting.Blocked;
    }
  }
}
