using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;

namespace AvatarHamsterPanic.Objects
{
  public class ObjectTable<BaseType>
  {
    private delegate bool RemoveDelegate<T>( T obj ) where T : BaseType;

    delegate object InvokeDelegate( object obj, object[] parameters );

    private Dictionary<Type, object> table = new Dictionary<Type, object>();
    private Dictionary<Type, object> readOnlyTable = new Dictionary<Type, object>();
    private Dictionary<Type, InvokeDelegate> cleaner = new Dictionary<Type, InvokeDelegate>();
    private List<BaseType> trash = new List<BaseType>( 100 );
    private object[] objInArray = new object[1];

    public ReadOnlyCollection<BaseType> AllObjects
    {
      get { return ( (ReadOnlyCollection<BaseType>)readOnlyTable[typeof( BaseType )] ); }
    }

    public List<BaseType> AllObjectsList { get { return (List<BaseType>)table[typeof( BaseType )]; } }

    public ObjectTable()
    {
      List<BaseType> commonList = new List<BaseType>( 100 );
      table.Add( typeof( BaseType ), commonList );
      readOnlyTable.Add( typeof( BaseType ), new ReadOnlyCollection<BaseType>( commonList ) );
    }

    public void Add<T>( T obj ) where T : BaseType
    {
      // Add to specific list
      if ( !table.ContainsKey( obj.GetType() ) )
        InitList<T>();

      ( (List<T>)table[obj.GetType()] ).Add( obj );

      // Add to base list
      ( (List<BaseType>)table[typeof( BaseType )] ).Add( obj );
    }

    public void Remove<T>( T obj ) where T : BaseType
    {
      ( (List<T>)table[obj.GetType()] ).Remove( obj );
      ( (List<BaseType>)table[typeof( BaseType )] ).Remove( obj );
    }

    public ReadOnlyCollection<T> GetObjects<T>() where T : BaseType
    {
      if ( !readOnlyTable.ContainsKey( typeof( T ) ) )
        InitList<T>();

      return (ReadOnlyCollection<T>)readOnlyTable[typeof( T )];
    }

    public void Clear()
    {
      table.Clear();
      readOnlyTable.Clear();
      trash.Clear();

      List<BaseType> commonList = new List<BaseType>( 100 );
      table.Add( typeof( BaseType ), commonList );
      readOnlyTable.Add( typeof( BaseType ), new ReadOnlyCollection<BaseType>( commonList ) );
    }

    public bool InitList<T>() where T : BaseType
    {
      if ( table.ContainsKey( typeof( T ) ) )
        return false;
      List<T> list = new List<T>( 50 );
      table.Add( typeof( T ), list );
      readOnlyTable.Add( typeof( T ), new ReadOnlyCollection<T>( list ) );
      cleaner.Add( typeof( T ), new RemoveDelegate<T>( list.Remove ).Method.Invoke );
      return true;
    }

    public void MoveToTrash<T>( T obj ) where T : BaseType
    {
      trash.Add( obj );
    }

    public void RemoveFromTrash<T>( T obj ) where T : BaseType
    {
      trash.Remove( obj );
    }

    public void EmptyTrash()
    {
      foreach ( BaseType obj in trash )
      {
        objInArray[0] = obj;
        if ( cleaner.ContainsKey( obj.GetType() ) )
        {
          cleaner[obj.GetType()]( table[obj.GetType()], objInArray );
          ( (List<BaseType>)table[typeof( BaseType )] ).Remove( obj );
        }
      }
      trash.Clear();
    }
  }
}