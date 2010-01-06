using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace GameObjects
{
  public class ObjectTable<BaseType>
  {
    private delegate bool RemoveDelegate<T>( T obj );

    private Dictionary<Type, object> table = new Dictionary<Type, object>();
    private Dictionary<Type, Delegate> cleaner = new Dictionary<Type, Delegate>();
    private List<BaseType> trash = new List<BaseType>();
    private object[] objInArray = new object[1];

    public ReadOnlyCollection<BaseType> AllObjects
    {
      get { return ( (List<BaseType>)table[typeof( BaseType )] ).AsReadOnly(); }
    }

    public ObjectTable()
    {
      table.Add( typeof( BaseType ), new List<BaseType>() );
    }

    public void Add<T>( T obj )
    {
      // Add to specific list
      if ( !table.ContainsKey( obj.GetType() ) )
        InitList<T>();

      ( (List<T>)table[obj.GetType()] ).Add( obj );

      // Add to generic list
      ( (List<BaseType>)table[typeof( BaseType )] ).Add( (BaseType)(object)obj );
    }

    public void Remove<T>( T obj )
    {
      ( (List<T>)table[obj.GetType()] ).Remove( obj );
      ( (List<object>)table[typeof( object )] ).Remove( obj );
    }

    public ReadOnlyCollection<T> GetObjects<T>()
    {
      if ( table.ContainsKey( typeof( T ) ) )
      {
        object temp = table[typeof( T )];
        return ((List<T>)temp).AsReadOnly();
      }

      return null;
    }

    public void Clear()
    {
      table.Clear();
      trash.Clear();
      table.Add( typeof( BaseType ), new List<BaseType>() );
    }

    public bool InitList<T>()
    {
      if ( table.ContainsKey( typeof( T ) ) )
        return false;
      List<T> list = new List<T>();
      table.Add( typeof( T ), list );
      cleaner.Add( typeof( T ), new RemoveDelegate<T>( list.Remove ) );
      return true;
    }

    public void MoveToTrash<T>( T obj )
    {
      trash.Add( (BaseType)(object)obj );
    }

    public void RemoveFromTrash<T>( T obj )
    {
      trash.Remove( (BaseType)(object)obj );
    }

    public void EmptyTrash()
    {
      foreach ( object obj in trash )
      {
        objInArray[0] = obj;
        cleaner[obj.GetType()].Method.Invoke( table[obj.GetType()], objInArray );
        ( (List<BaseType>)table[typeof( BaseType )] ).Remove( (BaseType)(object)obj );
      }
      trash.Clear();
    }
  }
}