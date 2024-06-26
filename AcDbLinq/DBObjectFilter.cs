/// DBObjectFilter.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.

using System;
using System.Linq.Expressions;
using System.Linq.Expressions.Predicates;
using System.Windows.Input;
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
   /// <typeparam name="TKeySource"></typeparam>
   /// <typeparam name="TValueSource"></typeparam>

   public class DBObjectFilter<TKeySource, TValueSource>
      : DBObjectDataMap<TKeySource, TValueSource, bool> 

      where TKeySource : DBObject
      where TValueSource : DBObject

   {
      Expression<Func<TValueSource, bool>> valueSelectorExpr;
      ExpressionProperty expressionProperty;

      public DBObjectFilter(Func<TKeySource, ObjectId> keySelector,
                              Expression<Func<TValueSource, bool>> valueSelector)
         : base(keySelector, valueSelector.Compile())
      {
         this.valueSelectorExpr = valueSelector;
      }

      public Func<TKeySource, bool> Predicate => Accessor;

      public ExpressionProperty SourcePredicate 
      { 
         get 
         {
            if(expressionProperty == null) 
               expressionProperty = new ExpressionProperty(this);
            return expressionProperty;
         } 
      }

      /// <summary>
      /// Combines this filter with another DBObjectFilter<TKeySource,...>
      /// in a logical 'and' or 'or' operation. Can also combine the filter
      /// with an arbitrary Expression<Func<TKeySource, bool>>.
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

      public DBObjectFilter<TKeySource, TValueSource> And(
         Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = GetValueExpression.And(operand);
         return this;
      }

      public DBObjectFilter<TKeySource, TValueSource> Or(
         Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = GetValueExpression.Or(operand);
         return this;
      }

      /// <summary>
      /// Like And() and Or(), but reverses the order of evaluation.
      /// 
      /// With And() and Or(), the instance expression is evaluated
      /// first, and it evaluates to true, the operand expression is
      /// then evaluated.
      /// 
      /// With RevAnd() and RevOr(), the operand expression is
      /// evaluated first and if it evaluates to true, the instance
      /// expression is then evaluated.
      /// </summary>
      /// <param name="reverse">A value indicating if the operand
      /// expression is evaluated before the instance expression.</param>
      /// <param name="operand">The operand expression</param>
      
      public DBObjectFilter<TKeySource, TValueSource> RevAnd(
         Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = operand.And(GetValueExpression);
         return this;
      }

      public DBObjectFilter<TKeySource, TValueSource> RevOr(
         Expression<Func<TKeySource, bool>> operand)
      {
         Assert.IsNotNull(operand, nameof(operand));
         GetValueExpression = operand.Or(GetValueExpression);
         return this;
      }

      public bool IsMatch(TKeySource candidate)
      {
         return this[candidate];
      }

      public static implicit operator 
      Func<TKeySource, bool>(DBObjectFilter<TKeySource, TValueSource> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.Accessor;
      }

      public static implicit operator 
      Expression<Func<TKeySource, bool>>(DBObjectFilter<TKeySource, TValueSource> filter)
      {
         Assert.IsNotNull(filter, nameof(filter));
         return filter.GetValueExpression;
      }

      public class ExpressionProperty
      {
         DBObjectFilter<TKeySource, TValueSource> owner;

         public ExpressionProperty(DBObjectFilter<TKeySource, TValueSource> owner)
         {
            this.owner = owner;
         }

         public void And(Expression<Func<TValueSource, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.valueSelectorExpr = owner.valueSelectorExpr.And(operand);
            owner.valueSelector = CompileOnInvoke;
         }

         public void Or(Expression<Func<TValueSource, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.valueSelectorExpr = owner.valueSelectorExpr.Or(operand);
            owner.valueSelector = CompileOnInvoke;
         }

         public void RevAnd(Expression<Func<TValueSource, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.valueSelectorExpr = operand.And(owner.valueSelectorExpr);
            owner.valueSelector = CompileOnInvoke;
         }

         public void RevOr(Expression<Func<TValueSource, bool>> operand)
         {
            Assert.IsNotNull(operand, nameof(operand));
            owner.valueSelectorExpr = operand.Or(owner.valueSelectorExpr);
            owner.valueSelector = CompileOnInvoke;
         }

         bool CompileOnInvoke(TValueSource arg)
         {
            owner.valueSelector = owner.valueSelectorExpr.Compile();
            owner.Invalidate();
            return owner.valueSelector(arg);
         }

         public static implicit operator Expression<Func<TValueSource, bool>>(ExpressionProperty operand)
         {
            return operand?.owner?.valueSelectorExpr;
         }
      }

   }


   }



