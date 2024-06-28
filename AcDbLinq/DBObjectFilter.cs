/// DBObjectFilter.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq.Expressions.Predicates;
using System.Windows.Input;
using Autodesk.AutoCAD.Runtime.Diagnostics;


namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// DBObjectFilter<TFiltered, TCriteria>
   /// 
   /// A specialization of DBObjectDataMap that performs query filtering 
   /// of TFiltered objects using criteria derived from TCriteria 
   /// objects that are directly or indirectly referenced by TFiltered 
   /// objects.
   /// 
   /// Generic arguments:
   /// 
   ///    TFiltered: The type of the object being filtered/queried
   ///    
   ///    TCriteria: The type of an object that is referenced by
   ///    all TFiltered instances, and is used to determine if a
   ///    each referencing TFiltered instance satisfies the filter 
   ///    criteria.
   ///    
   ///    Both TFitlered and TCriteria must be DBObjects.
   ///    
   ///    The ObjectId of a TCriteria instance must be obtainable
   ///    from a referencing TFiltered instance.
   /// 
   /// A simple example: A DBObjectFilter that filters Entities,
   /// excluding those residing on locked layers:
   /// 
   /// <code>
   ///   
   ///    var unlockedLayerFilter = 
   ///       new DBObjectFilter<Entity, LayerTableRecord>(
   ///          entity => entity.LayerId,
   ///          layer => !layer.IsLocked
   ///       );
   /// 
   /// </code>
   /// 
   /// In the above type, Entity is the type of object being
   /// queried (TFiltered), and the LayerTableRecord is the 
   /// referenced object whose data is used to determine if 
   /// an entity satisfies the the query critiera (TCriteria).
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
   /// delegate which does that, which is the second argument
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
   /// of a DBObjectFilter instance.
   /// 
   /// DBObjectFilter is just a specialization of DBObjectDataMap,
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
   /// <typeparam name="TFiltered">The type that is to be filtered/queried</typeparam>
   /// <typeparam name="TCriteria">The type that provides the filter criteria</typeparam>

   public class DBObjectFilter<TFiltered, TCriteria>
      : DBObjectDataMap<TFiltered, TCriteria, bool> 

      where TFiltered : DBObject
      where TCriteria : DBObject

   {
      Expression<Func<TCriteria, bool>> criteriaPredicate;
      CriteriaProperty sourcePredicateProperty;

      public DBObjectFilter(
         Func<TFiltered, ObjectId> keySelector,
         Expression<Func<TCriteria, bool>> valueSelector)
         
         : base(keySelector, valueSelector.Compile())
      {
         this.criteriaPredicate = valueSelector;
      }

      /// <summary>
      /// The predicate that's applied to each TCriteria
      /// </summary>
      
      Expression<Func<TCriteria, bool>> CriteriaPredicate
      {
         get
         {
            return criteriaPredicate;
         }
         set
         {
            Assert.IsNotNull(value, nameof(value));
            if(value != criteriaPredicate)
            {
               criteriaPredicate = value;
               valueSelector = CompileAndInvoke;
            }
         }
      }

      bool CompileAndInvoke(TCriteria arg)
      {
         valueSelector = criteriaPredicate.Compile();
         Invalidate();
         return valueSelector(arg);
      }

      public Func<TFiltered, bool> Predicate => Accessor;

      public CriteriaProperty Criteria
      {
         get
         {
            if(sourcePredicateProperty == null)
               sourcePredicateProperty = new CriteriaProperty(this);
            return sourcePredicateProperty;
         }
      }

      /// <summary>
      /// Combines this filter with another DBObjectFilter<TFiltered,...>
      /// in a logical 'and' or 'or' operation. Can also combine the filter
      /// with an arbitrary Expression<Func<TFiltered, bool>>.
      /// 
      /// This method modifies the instance to perform a
      /// logical and/or operation on the conditions defined
      /// by the instance and the conditions defined by the
      /// argument. 
      /// 
      /// </summary>
      /// <param name="operand">The expression that is to be
      /// logically unioned with the current instance.</param>
      /// <returns>The current instance</returns>

      public DBObjectFilter<TFiltered, TCriteria> And(
         Expression<Func<TFiltered, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = GetValueExpression.And(operand);
         return this;
      }

      public DBObjectFilter<TFiltered, TCriteria> Or(
         Expression<Func<TFiltered, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = GetValueExpression.Or(operand);
         return this;
      }

      /// <summary>
      /// Slated to superseed the And() and Or() methods for
      /// DBObjectFilters, but not for arbitrary predicates.
      /// </summary>
      /// <typeparam name="TNewCriteria"></typeparam>
      /// <param name="logicalOperation"></param>
      /// <param name="keySelector"></param>
      /// <param name="predicate"></param>
      /// <exception cref="ArgumentException"></exception>

      public void Add<TNewCriteria>(
         Func<TFiltered, ObjectId> keySelector,
         Expression<Func<TNewCriteria, bool>> predicate) where TNewCriteria : DBObject
      {
         Add<TNewCriteria>(Logical.And, keySelector, predicate);
      }

      HashSet<Type> criteriaTypes = new HashSet<Type>(new Type[] { typeof(TCriteria) });

      /// <summary>
      /// Adds a child DBObjectFilter whose predicate 
      /// is logically-combined with the predicate of
      /// the instance the method is called on.
      /// 
      /// This method cannot be called multiple times
      /// with the same generic argument type. Doing so
      /// would be redunduant (e.g., the predicate can
      /// be logically-combined with the predicate of
      /// the existing child filter targeting the same
      /// type).
      /// </summary>
      /// <typeparam name="TNewCriteria"></typeparam>
      /// <param name="logicalOperation"></param>
      /// <param name="keySelector"></param>
      /// <param name="predicate"></param>
      /// <exception cref="ArgumentException"></exception>
      /// <exception cref="NotSupportedException"></exception>

      public void Add<TNewCriteria>(Logical logicalOperation,
         Func<TFiltered, ObjectId> keySelector, 
         Expression<Func<TNewCriteria, bool>> predicate) where TNewCriteria: DBObject
      {
         Assert.IsNotNull(keySelector, nameof(keySelector));
         Assert.IsNotNull(predicate, nameof(predicate));

         if(!criteriaTypes.Add(typeof(TNewCriteria)))
            throw new ArgumentException(
               $"Instance already contains a child filter targeting {typeof(TNewCriteria).Name}");

         var newFilter = new DBObjectFilter<TFiltered, TNewCriteria>(
            keySelector, predicate);

         switch(logicalOperation)
         {
            case Logical.And: 
               And(newFilter); 
               break;
            case Logical.Or: 
               Or(newFilter); 
               break;
            case Logical.AndFirst:
               ReverseAnd(newFilter);
               break;
            case Logical.OrFirst: 
               ReverseOr(newFilter); 
               break;
            default:
               throw new NotSupportedException(nameof(logicalOperation));
         }
      }

      /// <summary>
      /// These methods are like And() and Or(), but reverse the 
      /// order of evaluation.
      /// 
      /// With And() and Or(), the instance expression is evaluated
      /// first and if it evaluates to true, the expression argument
      /// is then evaluated.
      /// 
      /// With RevAnd() and RevOr(), the expression argument is
      /// evaluated first and if it evaluates to true, the instance
      /// expression is then evaluated.
      /// </summary>
      /// <param name="operand">The operand expression</param>

      public DBObjectFilter<TFiltered, TCriteria> ReverseAnd(
         Expression<Func<TFiltered, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = operand.And(GetValueExpression);
         return this;
      }

      public DBObjectFilter<TFiltered, TCriteria> ReverseOr(
         Expression<Func<TFiltered, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = operand.Or(GetValueExpression);
         return this;
      }

      public bool IsMatch(TFiltered candidate)
      {
         return this[candidate];
      }

      public static implicit operator 
      Func<TFiltered, bool>(DBObjectFilter<TFiltered, TCriteria> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.Accessor;
      }

      public static implicit operator 
      Expression<Func<TFiltered, bool>>(DBObjectFilter<TFiltered, TCriteria> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.GetValueExpression;
      }

      public class CriteriaProperty
      {
         DBObjectFilter<TFiltered, TCriteria> owner;

         public CriteriaProperty(DBObjectFilter<TFiltered, TCriteria> owner)
         {
            this.owner = owner;
         }

         public void And(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaPredicate = owner.CriteriaPredicate.And(operand);
         }

         public void Or(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaPredicate = owner.CriteriaPredicate.Or(operand);
         }

         public void RevAnd(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaPredicate = operand.And(owner.CriteriaPredicate);
         }

         public void RevOr(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaPredicate = operand.Or(owner.CriteriaPredicate);
         }

         public static implicit operator Expression<Func<TCriteria, bool>>(CriteriaProperty operand)
         {
            return operand?.owner?.CriteriaPredicate;
         }

      }// CriteriaProperty

   }

   public enum Logical
   {
      And, 
      Or, 
      AndFirst, 
      OrFirst, 
      Not
   }


}



