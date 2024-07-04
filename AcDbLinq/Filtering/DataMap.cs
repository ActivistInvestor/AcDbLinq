/// DBObjectDataMapBase.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 

using System;
using System.Linq.Expressions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// An abstract base type for DBObjectDataMap<> that
   /// encapsulates operations that are not dependent on
   /// generic arguments.
   /// </summary>

   public abstract class DataMap
   {
      DataMap parent = null;

      public DataMap Parent
      {
         get => parent;
         protected internal set => parent = value;
      }

      public abstract Type TKeySourceType { get; }
      public abstract Type TKeyType { get; }
      public abstract Type TValueSourceType { get; }
      public abstract Type TValueType { get; }

      public abstract Expression KeySelectorExpression { get; }

      public static bool Trace { get; set; } = false;

      /// <summary>
      /// Invalidates the cache entry having 
      /// the given key.
      /// </summary>
      /// <param name="id"></param>
      
      public abstract void Invalidate(ObjectId id);

      /// <summary>
      /// Invalidates the entire cache
      /// </summary>

      public abstract void Invalidate();


      protected void NotifyMapChanged(MapChangeType type, ObjectId id = default(ObjectId))
      {
         mapChanged.Invoke(this, new MapChangedEventArgs(this, type, id));
      }

      /// <summary>
      /// Provides derived types with notification that
      /// the contents of the data cache has changed.
      /// </summary>

      protected virtual void OnMapChanged(MapChangeType type, ObjectId id = default(ObjectId))
      {
         if(hasObservers)
            NotifyMapChanged(type, id);
      }

      event MapChangedEventHandler mapChanged = null;

      protected virtual void HasObserversChanged(bool value)
      {
         hasObservers = value;
      }

      bool hasObservers = false;

      /// <summary>
      /// Returns a value indicating if there are
      /// any handlers listening to the MapChanged
      /// event.
      /// </summary>

      public bool HasObservers => hasObservers;

      public event MapChangedEventHandler MapChanged
      {
         add
         {
            bool flag = mapChanged == null;
            mapChanged += value;
            if(flag && mapChanged != null)
               HasObserversChanged(true);
         }
         remove
         {
            bool flag = mapChanged != null;
            mapChanged -= value;
            if(flag && mapChanged == null)
               HasObserversChanged(false);
         }
      }

      public virtual string Dump(string label = null, string indent = "")
      {
         return string.Empty;
      }


   }

   public delegate void MapChangedEventHandler(object sender, MapChangedEventArgs e);

   public class MapChangedEventArgs : EventArgs
   {
      public MapChangedEventArgs(DataMap map, MapChangeType type, ObjectId id = default(ObjectId))
      {
         this.Map = map;
         this.ChangeType = type;
         this.ObjectId = id;
      }

      public DataMap Map { get; private set; }
      public ObjectId ObjectId { get; private set; }
      public MapChangeType ChangeType { get; private set; }

   }

}



