/// DBObjectDataMap.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 

using System;
using System.Linq.Expressions;
using System.Text;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// An abstract base type for DBObjectDataMap<> that
   /// encapsulates operations that are not dependent on
   /// generic arguments.
   /// </summary>
   
   public abstract class DBObjectDataMap
   {
      public abstract Type TKeySourceType { get; }
      public abstract Type TValueSourceType { get; }
      public abstract Type TValueType { get; }
      public abstract Expression KeySelectorExpression { get; }

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

      /// <summary>
      /// Diagnostics function that displays the
      /// type of the generic arguments in derived
      /// types.
      /// </summary>

      public virtual string Dump(string label = null, string indent = "")
      {
         StringBuilder sb = new StringBuilder();
         if(!string.IsNullOrWhiteSpace(label))
            sb.AppendLine($"{indent}{label}: ");
         sb.AppendLine($"{indent}KeySouce Type: {TKeySourceType.Name}");
         sb.AppendLine($"{indent}ValueSource Type: {TValueSourceType.Name}");
         sb.AppendLine($"{indent}Value Type {TValueType.Name}");
         return sb.ToString();
      }

      protected void NotifyCacheChanged(MapChangeType type, ObjectId id = default(ObjectId))
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
            NotifyCacheChanged(type, id);
      }

      event MapChangedEventHandler mapChanged = null;

      protected virtual void IsObservedChanged(bool value)
      {
         hasObservers = value;
      }

      bool hasObservers = false;

      /// <summary>
      /// Returns a value indicating if there are
      /// any handlers listening to the MapChanged
      /// event.
      /// </summary>

      protected bool IsObserved => hasObservers;

      public event MapChangedEventHandler MapChanged
      {
         add
         {
            bool flag = mapChanged == null;
            mapChanged += value;
            if(flag)
               IsObservedChanged(true);
         }
         remove
         {
            bool flag = mapChanged != null;
            mapChanged -= value;
            if(flag && mapChanged == null)
               IsObservedChanged(false);
         }
      }
   }

   public delegate void MapChangedEventHandler(object sender, MapChangedEventArgs e);

   public class MapChangedEventArgs : EventArgs
   {
      public MapChangedEventArgs(DBObjectDataMap map, MapChangeType type, ObjectId id = default(ObjectId))
      {
         this.Map = map;
         this.ChangeType = type;
         this.ObjectId = id;
      }

      public DBObjectDataMap Map { get; private set; }
      public ObjectId ObjectId { get; private set; }
      public MapChangeType ChangeType { get; private set; }

   }

   public enum MapChangeType
   {
      ItemAdded,
      ItemRemoved,
      ItemModified,
      Clear
   }


}



