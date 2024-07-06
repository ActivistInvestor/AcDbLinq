/// FilteredEnumerable.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Autodesk.AutoCAD.Runtime.Diagnostics;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   public interface IFilteredEnumerable<T, TCriteria> : IEnumerable<T>
      where T: DBObject
      where TCriteria : DBObject
   {
   }

   /// <summary>
   /// A specialization of DBObjectFilter that encapsulates the
   /// source sequence that is to be filtered, and which can be 
   /// enumerated to obtain the result.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <typeparam name="TCriteria"></typeparam>

   public class FilteredEnumerable<T, TCriteria>
         : DBObjectFilter<T, TCriteria>, IFilteredEnumerable<T, TCriteria>
      where T : DBObject
      where TCriteria : DBObject
   {
      IEnumerable<T> source = new T[0];

      public FilteredEnumerable(IEnumerable<T> source,
            Expression<Func<T, ObjectId>> criteriaKeySelector,
            Expression<Func<TCriteria, bool>> predicate) 
         : base(criteriaKeySelector, predicate)
      {
         this.source = source ?? new T[0];
      }

      public IEnumerable<T> DataSource
      {
         get { return source; }
         set { source = value ?? new T[0]; }
      }

      public IEnumerator<T> GetEnumerator()
      {
         return source.Where(this).GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return this.GetEnumerator();
      }
   }

   public static class FilteredEnumerableExtensions
   {
      /// <summary>
      /// An overload of GetObjects() targeting BlockTableRecord
      /// that implicitly filters the result sequence in accordance
      /// with the specified filter criteria.
      /// 
      /// Overloads are provided for BlockTableRecord,
      /// ObjectIdCollection, and IEnumerable<ObjectId>
      /// 
      /// (Complete documentation to come)
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <typeparam name="TCriteria"></typeparam>
      /// <param name="source"></param>
      /// <param name="trans"></param>
      /// <param name="keySelector"></param>
      /// <param name="predicate"></param>
      /// <param name="mode"></param>
      /// <param name="exact"></param>
      /// <returns></returns>
      
      public static IFilteredEnumerable<T, TCriteria> GetObjects<T, TCriteria>(
         this BlockTableRecord source,
         Transaction trans,
         Expression<Func<T, ObjectId>> keySelector,
         Expression<Func<TCriteria, bool>> predicate,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false)

         where T : Entity
         where TCriteria : DBObject
      {
         Assert.IsNotNullOrDisposed(source, nameof(source));
         return new FilteredEnumerable<T, TCriteria>(
            source.GetObjectsOfType<T>(trans, exact, mode, false, false),
            keySelector, 
            predicate);
      }

      public static IFilteredEnumerable<T, TCriteria> GetObjects<T, TCriteria>(
         this ObjectIdCollection source,
         Transaction trans,
         Expression<Func<T, ObjectId>> keySelector,
         Expression<Func<TCriteria, bool>> predicate,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false)

         where T : DBObject
         where TCriteria : DBObject
      {
         Assert.IsNotNullOrDisposed(source, nameof(source));
         return new FilteredEnumerable<T, TCriteria>(
            source.GetObjectsOfType<T>(trans, exact, mode, false, false),
            keySelector,
            predicate);
      }

      public static IFilteredEnumerable<T, TCriteria> GetObjects<T, TCriteria>(
         this IEnumerable<ObjectId> source,
         Transaction trans,
         Expression<Func<T, ObjectId>> keySelector,
         Expression<Func<TCriteria, bool>> predicate,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false)

         where T : DBObject
         where TCriteria : DBObject
      {
         Assert.IsNotNull(source, nameof(source));
         return new FilteredEnumerable<T, TCriteria>(
            source.GetObjectsOfType<T>(trans, exact, mode, false, false),
            keySelector,
            predicate);
      }

      /// <summary>
      /// An overload of Enumerable.Where() that uses a
      /// DBObjectFilter internally to perform relational 
      /// filtering of the input sequence.
      /// 
      /// Example: Reduce a sequence of entities to
      /// the subset that are on unlocked layers:
      /// <code>
      /// 
      ///   IEnumerable<Entity> source = // assign to a sequence of entity
      ///   
      ///   var filtered = source.Where<Entity, LayerTableRecord>(
      ///      entity => entity.LayerId,
      ///      layer => !layer.IsLocked);
      ///      
      /// </code>  
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <typeparam name="TCriteria"></typeparam>
      /// <param name="source"></param>
      /// <param name="keySelector"></param>
      /// <param name="predicate"></param>
      /// <returns></returns>

      public static IFilteredEnumerable<T, TCriteria> WhereBy<T, TCriteria>(
            this IEnumerable<T> source,
            Expression<Func<T, ObjectId>> criteriaKeySelector,
            Expression<Func<TCriteria, bool>> predicate)
         where T : DBObject
         where TCriteria : DBObject
      {
         Assert.IsNotNull(source, nameof(source));
         return new FilteredEnumerable<T, TCriteria>(source, criteriaKeySelector, predicate);
      }
   }


}



