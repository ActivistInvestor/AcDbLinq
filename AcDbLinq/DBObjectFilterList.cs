/// DBObjectFilterList.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.

using Autodesk.AutoCAD.Runtime.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Linq.Expressions.Extensions;


namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   class DBObjectFilterList : KeyedCollection<(Type, Expression), DBObjectDataMap>
   {
      DBObjectDataMap owner;

      public DBObjectFilterList(DBObjectDataMap owner) : base(new ItemComparer())
      {
         Assert.IsNotNull(owner, nameof(owner));
         this.Add(owner);
         this.owner = owner; 
      }

      protected override (Type, Expression) GetKeyForItem(DBObjectDataMap item)
      {
         return (item.TValueSourceType, item.KeySelectorExpression);
      }

      public DBObjectDataMap this[Type type, Expression expression]
      {
         get
         {
            if(base.Dictionary.TryGetValue((type, expression), out DBObjectDataMap map))
            {
               return map;
            }
            return null;
         }
      }

      class ItemComparer : IEqualityComparer<(Type, Expression)>
      {
         static ExpressionEqualityComparer comparer = ExpressionEqualityComparer.Instance;

         public bool Equals((Type, Expression) x, (Type, Expression) y)
         {
            return x.Item1 == y.Item1 && comparer.Equals(x.Item2, y.Item2);
         }

         public int GetHashCode((Type, Expression) obj)
         {
            return HashCode.Combine(obj.Item1.GetHashCode(),
               comparer.GetHashCode(obj.Item2));
         }
      }
   }


}



