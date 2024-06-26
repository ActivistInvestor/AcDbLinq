/// FilteredEnumerable.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.

using Autodesk.AutoCAD.Runtime.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Predicates;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// A specialization of DBObjectFilter that encapsulates the
   /// source sequence that is to be filtered, and which can be 
   /// enumerated to obtain the result.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <typeparam name="TValueSource"></typeparam>

   public class FilteredEnumerable<T, TValueSource>
         : DBObjectFilter<T, TValueSource>, IEnumerable<T> 
      where T : DBObject
      where TValueSource : DBObject
   {
      IEnumerable<T> source = new T[0];

      public FilteredEnumerable(IEnumerable<T> source,
            Func<T, ObjectId> keySelector,
            Expression<Func<TValueSource, bool>> predicate)
         : base(keySelector, predicate)
      {
         this.source = source ?? new T[0];
      }

      public IEnumerable<T> DataSource
      {
         get { return source; }
         set { source = value ?? new T[0]; }
      }

      PredicateExpression<T> predicateOverride = null;

      // public override Func<T, bool> Accessor => predicateOverride ?? base.Accessor;


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
      public static FilteredEnumerable<T, TSource> AsFiltered<T, TSource>(
            this IEnumerable<T> source, 
            Func<T, ObjectId> keySelector,
            Expression<Func<TSource, bool>> predicate)

         where T : DBObject
         where TSource : DBObject
      {
         Assert.IsNotNull(source, nameof(source));
         return new FilteredEnumerable<T, TSource>(source, keySelector, predicate);
      }

      /// <summary>
      /// OpenMode.ForWrite not supported
      /// </summary>

      public static FilteredEnumerable<T, TSource> AsFiltered<T, TSource>(
            this BlockTableRecord source,
            Transaction trans,
            Func<T, ObjectId> keySelector,
            Expression<Func<TSource, bool>> predicate,
            bool exact = false)

         where T : Entity
         where TSource : DBObject
      {
         Assert.IsNotNullOrDisposed(source, nameof(source));
         return new FilteredEnumerable<T, TSource>(
            source.GetObjects<T>(trans),
            keySelector, predicate);
      }


   }


}



