/// DBObjectExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DBObjectExtensions.cs
///     
/// A collection of old helper APIs that provide 
/// support for accessing/querying the contents 
/// of AutoCAD Databases using LINQ.
/// 
/// A few changes have been made along the way, since 
/// this library was first written (which happened over
/// the period of several years). 
/// 
/// Some of those revisions require C# 7.0.

using System;
using System.Linq.Expressions;
using System.Linq.Expressions.Predicates;
using Autodesk.AutoCAD.Runtime.Diagnostics;
using Expr = System.Linq.Expressions.Expression;


namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{


   /// <summary>
   /// DBObjectFilter
   /// 
   /// A specialization of DBObjectDataMap that performs query filtering 
   /// of TKeySource objects using criteria derived from TValueSource 
   /// objects that are directly or indirectly referenced by TKeySource 
   /// objects.
   /// 
   /// A simple example: A DBObjectFilter that excludes entities on
   /// locked layers:
   /// 
   /// <code>
   ///   
   ///    var unlocked = 
   ///      new DBObjectFilter<Entity, LayerTableRecord>(
   ///         entity => entity.LayerId,
   ///         layer => !layer.IsLocked
   ///      );
   /// 
   /// </code>
   /// In the above type, Entity is the type of object being
   /// queried, and LayerTableRecord is the referenced object
   /// whose data is used to determine if an entity satisfies
   /// the the query critiera (in this example, the entity
   /// satisfies the query criteria if it is not on a locked
   /// layer).7
   /// 
   /// The DBObjectFilter doesn't know anything about what type
   /// of objects are being queried, or what the criteria is.
   /// It only knows how to get the criteria from a referenced
   /// object. To determine what referenced object it must get 
   /// the data from, it requiresd a caller-supplied delegate
   /// that takes the entity as an argument, and returns the 
   /// referenced object's ObjectId. That is the first delegate 
   /// passed to the constructor in the above example:
   ///   
   ///    entity => entity.LayerId
   /// 
   /// The DBObjectFilter also doesn't know what data it must
   /// get from the referenced object, so it requires a second
   /// delegate which does that, which is the second delegate 
   /// passed to the constructor above:
   /// 
   ///    layer => !layer.IsLocked
   ///    
   /// These two delegates are the only variables within the 
   /// operation performed by DBObjectFilter. Everything else
   /// is boilerplate.
   /// 
   /// The first delgate takes an entity, and returns a 'key'
   /// that identifies the referenced object whose data is to
   /// be used to determine if the entity satisfies the query
   /// criteria. The key in this case, is the ObjectId of the
   /// LayerTableRecord representing the layer which the entity
   /// resides on. That key is first used to lookup the layer's
   /// query criteria in a cache, and if found, it is used to 
   /// determine if the entity satisfies the query. If the data 
   /// is not f7ound in the cache, the LayerTableRecord is opened, 
   /// and it is passed to the second delegate, which returns 
   /// the data that determines if the entity satisfies the query
   /// criteria. That returned data is then cached, keyed to the
   /// LayerTableRecord's ObjectId, and reused in all subsequent
   /// reequests for the query criteria for the same layer.
   /// 
   /// Hence, regardless of how many entities reference a given
   /// layer, the layer must be accessed only once in the life
   /// of a DBObjectQuery instance.
   /// 
   /// DBObjectQuery is just a specialization of DBObjectDataMap,
   /// where the value that is cached for each referenced object
   /// is a bool representing if each referenced object's data
   /// satisfies the query criteria (true), or not (false).
   /// 
   /// With the above defined instance, a sequence of entities 
   /// can be constrained to only those on unlocked layers with 
   /// nothing more than this:
   /// 
   /// <code>
   /// 
   ///    var unlocked = 
   ///      new DBObjectFilter<Entity, LayerTableRecord>(
   ///         entity => entity.LayerId,
   ///         layer => !layer.IsLocked
   ///      );
   /// 
   ///    IEnumerable<Entity> entities = ....
   ///     
   ///    foreach(entity in entities.Where(unlocked))
   ///    {
   ///       // entity is not on a locked layer
   ///    }
   ///    
   /// </code>
   /// <typeparam name="TKeySource"></typeparam>
   /// <typeparam name="TValueSource"></typeparam>

   public class DBObjectFilter<TKeySource, TValueSource>
         : DBObjectDataMap<TKeySource, TValueSource, bool> // , IFilter<TKeySource>

      where TKeySource : DBObject
      where TValueSource : DBObject

   {
      Expression<Func<TValueSource, bool>> expression;

      public DBObjectFilter(
               Func<TKeySource, ObjectId> keySelector,
               Expression<Func<TValueSource, bool>> valueSelector)
         : base(keySelector, valueSelector.Compile())
      {
         this.expression = valueSelector;
      }

      public Func<TKeySource, bool> Predicate => GetValue;

      /// <summary>
      /// And() and Or() combine predicates (the second delegate
      /// argument passed to DBObjectFilter's constructor), rather
      /// than the predicate used for filtering the TKeySource.
      /// 
      /// To combine the filter predicate with another predicate,
      /// use AndFilter() and OrFilter().
      /// </summary>
      /// <param name="operand"></param>

      public void And(Expression<Func<TValueSource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         base.valueSelector = expression.And(operand).Compile();
      }

      /// <summary>
      /// Combines this filter with another DBObjectFilter<TKeySource,...>
      /// in a logical 'and' or 'or' operation. Can also combine the filter
      /// with an arbitrary Expression<Func<TKeySource, bool>>.
      /// </summary>
      /// <param name="operand"></param>
      /// <returns></returns>

      public PredicateExpression<TKeySource> And(Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         Expression<Func<TKeySource, bool>> thisExpr = this;
         return thisExpr.And(operand);
      }

      public PredicateExpression<TKeySource> Or(Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         Expression<Func<TKeySource, bool>> thisExpr = this;
         return thisExpr.Or(operand);
      }

      /// <summary>
      /// Sets the valueSelector predicate to return the result 
      /// of the logical OR of its result and the result of the 
      /// given predicate. 
      /// 
      /// This method can be used to add additional conditions 
      /// to the valueSelector predicate that was supplied to 
      /// the constructor.
      /// </summary>
      /// <param name="operand"></param>

      public void Or(Expression<Func<TValueSource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         base.valueSelector = expression.Or(operand).Compile();
      }

      /// <summary>
      /// Implements IFilter<T>.IsMatch
      /// </summary>

      public bool IsMatch(TKeySource candidate)
      {
         return GetValue(candidate);
      }

      public static implicit operator Func<TKeySource, bool>(DBObjectFilter<TKeySource, TValueSource> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.Accessor;
      }

      public static implicit operator Func<TValueSource, bool>(DBObjectFilter<TKeySource, TValueSource> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.valueSelector;
      }

      public static implicit operator Expression<Func<TKeySource, bool>>(DBObjectFilter<TKeySource, TValueSource> filter)
      {
         return arg => filter.GetValue(arg);
      }
   }

}



