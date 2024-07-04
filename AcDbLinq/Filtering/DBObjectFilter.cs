/// DBObjectFilter.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Linq.Expressions.Extensions;
using System.Linq.Expressions.Predicates;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Autodesk.AutoCAD.Runtime;
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
   ///    var unlockedFilter = 
   ///      new DBObjectFilter<Entity, LayerTableRecord>(
   ///         entity => entity.LayerId,
   ///         layer => !layer.IsLocked
   ///      );
   /// 
   ///    IEnumerable<Entity> entities = ....
   ///     
   ///    foreach(entity in entities.Where(unlockedFilter))
   ///    {
   ///       // entity is not on a locked layer
   ///    }
   ///    
   /// Without using the cached relational lookup performed by
   /// DBObjectFilter, one would need to access the referenced
   /// layer of each entity, accessing each layer numerous times, 
   /// to determine if each entity resides on a locked layer.
   ///    
   /// </code>
   /// <typeparam name="TFiltered">The type that is to be filtered/queried</typeparam>
   /// <typeparam name="TCriteria">The type that provides the filter/query criteria</typeparam>

   public class DBObjectFilter<TFiltered, TCriteria> 
      : DBObjectDataMap<TFiltered, TCriteria, bool>, IFilter<TFiltered>

      where TFiltered : DBObject
      where TCriteria : DBObject

   {
      Expression<Func<TCriteria, bool>> criteriaExpression;
      PredicateProperty predicateProperty;
      CriteriaProperty criteriaProperty;

      DBObjectFilterList filters;

      // The Type key is a TCriteria
      //Dictionary<(Type, Expression<Func<TFiltered, ObjectId>>), DBObjectDataMap> filters
      //   = new Dictionary<(Type, Expression<Func<TFiltered, ObjectId>>), DBObjectDataMap>();

      public DBObjectFilter(
         Expression<Func<TFiltered, ObjectId>> keySelector,
         Expression<Func<TCriteria, bool>> valueSelector)
         
         : base(keySelector, valueSelector.Compile())
      {
         this.criteriaExpression = valueSelector;
         filters = new DBObjectFilterList(this);
      }

      /// <summary>
      /// The predicate that's applied to each TCriteria
      /// </summary>
      
      Expression<Func<TCriteria, bool>> CriteriaExpression
      {
         get
         {
            return criteriaExpression;
         }
         set
         {
            Assert.IsNotNull(value, nameof(value));
            if(value != criteriaExpression)
            {
               CheckInitialized();
               criteriaExpression = value;
               valueSelector = criteriaExpression.Compile();
            }
         }
      }

      public bool IsMatch(TFiltered candidate)
      {
         return this[candidate];
      }

      public Func<TFiltered, bool> MatchPredicate => Accessor;

      public PredicateProperty Predicate => 
         predicateProperty ?? (predicateProperty = new PredicateProperty(this));

      public CriteriaProperty Criteria => 
         criteriaProperty ?? (criteriaProperty = new CriteriaProperty(this));

      /// <summary>
      /// Combines this filter with another DBObjectFilter in 
      /// a logical 'and' or 'or' operation. 
      /// 
      /// Can also be used to logically-combine the filter with 
      /// an arbitrary Expression<Func<TFiltered, bool>>.
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

      // Can't overload because calls become ambiguous
      // between this and Add(Expression<Func<TCriteria, bool>>)
      //public void Add(Expression<Func<TFiltered, bool>> operand)
      //{
      //   And(operand);
      //}

      public DBObjectFilter<TFiltered, TCriteria> Or(
         Expression<Func<TFiltered, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = GetValueExpression.Or(operand);
         return this;
      }

      /// <summary>
      /// Adds a condition to the test applied to TCriteria
      /// instances. The Logical argument specifies which
      /// logical operation the predicate is to be combined 
      /// using. The default Logical operation is Logcal.And
      /// 
      /// The Criteria predicate from other filters can be
      /// added along with arbitrary or 'ad-hoc' predicates.
      /// To add the criteria predicate from another filter,
      /// it must have the same TCriteria generic argument.
      /// The filter itself can be passed to this method to
      /// logically-combine its criteria predicate with the 
      /// criteria predicate of the instance.
      /// </summary>
      /// <param name="predicate">A predicate expression that
      /// is applied to TCriteria instances.</param>

      /// Revised: These have been made non-public, and must 
      /// be accessed through the Criteria property
      
      void Add(Expression<Func<TCriteria, bool>> predicate)
      {
         Add(Logical.And, predicate);
      }

      void Add(Logical operation, Expression<Func<TCriteria, bool>> predicate) 
      {
         Assert.IsNotNull(predicate, nameof(predicate));
         switch(operation)
         {
            case Logical.And:
               CriteriaExpression = CriteriaExpression.And(predicate);
               break;
            case Logical.Or:
               CriteriaExpression = CriteriaExpression.Or(predicate);
               break;
            case Logical.ReverseAnd:
               CriteriaExpression = predicate.And(CriteriaExpression);
               break;
            case Logical.ReverseOr:
               CriteriaExpression = predicate.Or(CriteriaExpression);
               break;
            default:
               throw new NotSupportedException(operation.ToString());
         }
      }


      /// <summary>
      /// Adds a child DBObjectFilter whose predicate 
      /// is logically-combined with the predicate of
      /// the instance this method is called on.
      /// 
      /// If the instance already contains a child filter
      /// targeting the given generic argument type and
      /// has a keySelector that is identical to the one
      /// passed as the argument, that existing filter's 
      /// predicate is combined with the given predicate, 
      /// and a new child filter is not created or added 
      /// to the instance.
      /// 
      /// If no child filter exists that targets the given
      /// generic argument type and has a keySelector that
      /// is equivalent to the first argument, a new child 
      /// filter is added to the instance and is logically 
      /// combined with the instance.
      /// 
      /// Note: this method can return both new and existing
      /// instances.
      /// </summary>
      /// <typeparam name="TNewCriteria">The type of the
      /// predicate's argument</typeparam>
      /// <param name="operation">The logical operation to
      /// combine the filters or predicates with</param>
      /// <param name="keySelector">A delegate that produces
      /// the ObjectId key for a given TFiltered instance</param>
      /// <param name="predicate">A predicate that is applied
      /// to the TCriteria instance referenced by the ObjectId
      /// return by the keySelector, that determines if the 
      /// TFiltered instance satisfies the filter criteria</param>
      /// <exception cref="ArgumentException"></exception>
      /// <exception cref="NotSupportedException"></exception>

      public DBObjectFilter<TFiltered, TNewCriteria> Add<TNewCriteria>(Logical operation,
         Expression<Func<TFiltered, ObjectId>> keySelector, 
         Expression<Func<TNewCriteria, bool>> predicate) where TNewCriteria: DBObject
      {
         Assert.IsNotNull(keySelector, nameof(keySelector));
         Assert.IsNotNull(predicate, nameof(predicate));

         DataMap item = filters[typeof(TNewCriteria), keySelector];
         if(item != null)
         {
            var result = (DBObjectFilter<TFiltered, TNewCriteria>)item;
            result.Criteria.Add(operation, predicate);
            return result;
         }

         // No existing filter is compatible,
         // so a new filter instance is needed:
         
         var filter = new DBObjectFilter<TFiltered, TNewCriteria>(keySelector, predicate);
         filter.Parent = this;
         filters.Add(filter); 
         Predicate.Add(operation, filter);
         return filter;
      }

      /// <summary>
      /// An overload of the above that uses Logical.And as the default operation
      /// </summary>

      public DBObjectFilter<TFiltered, TNewCriteria> Add<TNewCriteria>(
         Expression<Func<TFiltered, ObjectId>> keySelector,
         Expression<Func<TNewCriteria, bool>> predicate) where TNewCriteria : DBObject
      {
         return Add<TNewCriteria>(Logical.And, keySelector, predicate);
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

      public static implicit operator
      Expression<Func<TCriteria, bool>>(DBObjectFilter<TFiltered, TCriteria> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.CriteriaExpression;
      }

      public class PredicateProperty
      {
         DBObjectFilter<TFiltered, TCriteria> owner;

         public PredicateProperty(DBObjectFilter<TFiltered, TCriteria> owner)
         {
            this.owner = owner;
         }

         public DBObjectFilter<TFiltered, TCriteria> And(Expression<Func<TFiltered, bool>> predicate)
         {
            Add(Logical.And, predicate);
            return owner;
         }

         public DBObjectFilter<TFiltered, TCriteria> Or(Expression<Func<TFiltered, bool>> predicate)
         {
            Add(Logical.Or, predicate);
            return owner;
         }

         public void Add(Expression<Func<TFiltered, bool>> predicate)
         {
            Add(Logical.And, predicate);
         }

         public void Add(Logical operation, Expression<Func<TFiltered, bool>> predicate)
         {
            Assert.IsNotNull(predicate, nameof(predicate));
            switch(operation)
            {
               case Logical.And:
                  owner.GetValueExpression = owner.GetValueExpression.And(predicate);
                  break;
               case Logical.Or:
                  owner.GetValueExpression = owner.GetValueExpression.Or(predicate);
                  break;
               case Logical.ReverseAnd:
                  owner.GetValueExpression = predicate.And(owner.GetValueExpression);
                  break;
               case Logical.ReverseOr:
                  owner.GetValueExpression = predicate.Or(owner.GetValueExpression);
                  break;
               default:
                  throw new NotSupportedException(operation.ToString());
            }
         }

         public static implicit operator Func<TFiltered, bool>(PredicateProperty prop)
         {
            Assert.IsNotNull(prop, nameof(prop));
            return prop.owner.Accessor;
         }
      }

      public class CriteriaProperty
      {
         DBObjectFilter<TFiltered, TCriteria> owner;

         public CriteriaProperty(DBObjectFilter<TFiltered, TCriteria> owner)
         {
            this.owner = owner;
         }

         public void Add(Expression<Func<TCriteria, bool>> predicate)
         {
            owner.Add(Logical.And, predicate);
         }

         public void Add(Logical operation, Expression<Func<TCriteria, bool>> predicate)
         {
            Assert.IsNotNull(predicate);
            owner.Add(operation, predicate);
         }

         public void And(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaExpression = owner.CriteriaExpression.And(operand);
         }

         public void Or(Expression<Func<TCriteria, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.CriteriaExpression = owner.CriteriaExpression.Or(operand);
         }

         public static implicit operator Expression<Func<TCriteria, bool>>(CriteriaProperty operand)
         {
            return operand?.owner?.CriteriaExpression;
         }

      } // CriteriaProperty

      /// <summary>
      /// A diagnostic function that emits a dump of the current 
      /// instance expressions and generic argument types:
      /// </summary>
      /// <param name="label"></param>
      /// <returns></returns>
      
      public override string Dump(string label = null, string pad = "")
      {
         StringBuilder sb = new StringBuilder(base.Dump(label, pad));
         sb.AppendLine(Format($"{pad}Criteria key:       ", KeySelectorExpression));
         sb.AppendLine(Format($"{pad}Criteria predicate: ", criteriaExpression));
         sb.AppendLine(Format($"{pad}Match predicate     ", GetValueExpression));
         if(filters.HasChildren) 
         {
            sb.AppendLine($"{pad}Child filters: ");
            int i = 0;
            foreach(var child in filters.Children)
               sb.Append(child.Dump($"child[{i++}]:", pad + "   "));
         }
         return sb.ToString();
      }

      static string Format(string label, Expression expr)
      {
         int n = label.Length;
         string s = new string(' ', n);
         return label + expr.ToShortString(s);
      }

   } // DBObjectFilter

} // namespace



