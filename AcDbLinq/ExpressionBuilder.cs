using Autodesk.AutoCAD.Runtime.Diagnostics;
using System.Collections.Generic;

namespace System.Linq.Expressions.Predicates
{

   /// <summary>
   /// A class that composes predicate expressions of 
   /// varying complexity.
   /// 
   /// This code is very loosely based on Joe Albahari's
   /// PredicateBuilder:
   /// 
   ///   https://www.albahari.com/nutshell/predicatebuilder.aspx
   ///   
   /// But, takes a somewhat different route by 
   /// making everything an extension method.
   /// 
   /// </summary>

   public static class ExpressionBuilder
   {
      public static Expression<Func<T, bool>> Join<T>(
         this Expression<Func<T, bool>> left,
         Expression<Func<T, bool>> right,
         Func<Expression, Expression, BinaryExpression> Operator)
      {
         Assert.IsNotNull(left, nameof(left));
         Assert.IsNotNull(right, nameof(right));
         Assert.IsNotNull(Operator, nameof(Operator));
         if(left.IsDefault())
            return right;
         if(right.IsDefault())
            return left;
         var name = left.Parameters.First().Name;
         var parameter = Expression.Parameter(typeof(T), name);
         return Expression.Lambda<Func<T, bool>>(
            Visitor.Replace(parameter, Operator, left, right), parameter);
      }

      public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
      {
         Assert.IsNotNull(expression, nameof(expression));
         var negated = Expression.Not(expression.Body);
         return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
      }

      public static Expression<Func<T, bool>> And<T>(
         this Expression<Func<T, bool>> left,
         Expression<Func<T, bool>> right)
      {
         Assert.IsNotNull(left, nameof(left));
         return left.Join(right, Expression.AndAlso);
      }

      public static Expression<Func<T, bool>> Or<T>(
         this Expression<Func<T, bool>> left,
         Expression<Func<T, bool>> right)
      {
         Assert.IsNotNull(left, nameof(left));
         return left.Join(right, Expression.OrElse);
      }

      public static Expression<Func<T, bool>> Any<T>(IEnumerable<Expression<Func<T, bool>>> args)
      {
         Assert.IsNotNull(args, nameof(args));
         return Any(args as Expression<Func<T, bool>>[] ?? args.ToArray());
      }

      public static Expression<Func<T, bool>> Any<T>(params Expression<Func<T, bool>>[] args)
      {
         if(args == null || args.Length == 0)
            throw new ArgumentNullException(nameof(args));
         if(args.Length == 1)
            return args.GetAt(0, nameof(args));
         return args.Aggregate((left, right) => left.Join(right, Expression.OrElse));
      }

      public static Expression<Func<T, bool>> All<T>(IEnumerable<Expression<Func<T, bool>>> args)
      {
         Assert.IsNotNull(args, nameof(args));
         return All(args as Expression<Func<T, bool>>[] ?? args.ToArray());
      }

      public static Expression<Func<T, bool>> All<T>(params Expression<Func<T, bool>>[] args)
      {
         if(args == null || args.Length == 0)
            throw new ArgumentNullException(nameof(args));
         if(args.Length == 1)
            return args.GetAt(0, nameof(args));
         return args.Aggregate((left, right) 
            => left.Join(right, Expression.AndAlso));
      }

      /// <expr>.AndAll(<expr1>, <expr2>, <expr3>[, ...])
      ///    => <expr> && <expr1> && <expr2> && <expr3> [&& ...]

      public static Expression<Func<T, bool>> AndAll<T>(
         this Expression<Func<T, bool>> target,
         params Expression<Func<T, bool>>[] args)
      {
         Assert.IsNotNull(target, nameof(target));
         Assert.IsNotNullOrEmpty(args, nameof(args));
         if(args.Length == 1)
            return And(target, args.GetAt(0, nameof(args)));
         return args.Cons(target).Aggregate((left, right) 
            => left.Join(right, Expression.AndAlso));
      }

      public static Expression<Func<T, bool>> AndAll<T>(
         this Expression<Func<T, bool>> target,
         IEnumerable<Expression<Func<T, bool>>> args)
      {
         Assert.IsNotNull(args, nameof(args));
         return AndAll(target, args.ToArray());
      }

      /// <expr>.OrAny(<expr1>, <expr2>, <expr3>, ....)
      ///    => <expr> || <expr1> || <expr2> || <expr3> || ....

      public static Expression<Func<T, bool>> OrAny<T>(
         this Expression<Func<T, bool>> target,
         params Expression<Func<T, bool>>[] args)
      {
         Assert.IsNotNull(target, nameof(target));
         Assert.IsNotNullOrEmpty(args, nameof(args));
         if(args.Length == 1)
            return Or(target, args.GetAt(0, nameof(args)));
         return args.Cons(target).Aggregate((l, r) => l.Join(r, Expression.OrElse));
      }

      public static Expression<Func<T, bool>> OrAny<T>(
         this Expression<Func<T, bool>> target,
         IEnumerable<Expression<Func<T, bool>>> args)
      {
         Assert.IsNotNull(target, nameof(target));
         return OrAny(target, args.ToArray());
      }

      public static bool IsDefault<T>(this Expression<Func<T, bool>> expression)
      {
         return DefaultExpression<T>.IsDefault(expression);
      }

      public static Expression<Func<T, bool>> Default<T>(bool value = false)
      {
         return DefaultExpression<T>.GetValue(value);
      }

      public static IEnumerable<T> Cons<T>(this IEnumerable<T> rest, T head)
      {
         yield return head;
         foreach(T item in rest)
            yield return item;
      }

      public static T GetAt<T>(this T[] array, int index, string name = "array")
      {
         Assert.IsNotNull(array, name);
         if(index > array.Length - 1)
            throw new ArgumentOutOfRangeException(name,
               $"{name} requires at least {index + 1} elements");
         if(array[index] == null)
            throw new ArgumentException($"{name}[{index}] is null");
         return array[index];
      }

      class Visitor : ExpressionVisitor
      {
         readonly ParameterExpression parameter;

         Visitor(ParameterExpression parameter)
         {
            this.parameter = parameter;
         }

         protected override Expression VisitParameter(ParameterExpression node)
             => base.VisitParameter(parameter);

         public static BinaryExpression Replace<T>(ParameterExpression parameter,
            Func<Expression, Expression, BinaryExpression> LogicalOperator,
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
         {
            return (BinaryExpression)new Visitor(parameter)
               .Visit(LogicalOperator(left.Body, right.Body));
         }
      }

      /// <summary>
      /// The concept of a 'default' expression allows them to act
      /// as invocation targets for extension methods that combine
      /// multiple expressions into compound expressions. 
      /// 
      /// When a default expression is logically combined with another 
      /// expression, the result is always the other expression.
      /// 
      /// There are two default expressions, one that returns true
      /// and one that returns false. Which one should be used is
      /// entirely dependent on the context, although it is usually
      /// the one that returns false.
      /// </summary>
      /// <typeparam name="T"></typeparam>

      static class DefaultExpression<T>
      {
         public static Expression<Func<T, bool>> GetValue(bool value) => value ? True : False;

         public static readonly Expression<Func<T, bool>> True = x => true;
         public static readonly Expression<Func<T, bool>> False = x => false;

         public static bool IsDefault(Expression<Func<T, bool>> expr)
         {
            Assert.IsNotNull(expr, nameof(expr));
            if(expr == True || expr == False)
               return true;
            string s = expr.ToString().Trim();
            return s.EndsWith(strTrue) || s.EndsWith(strFalse);
         }

         const string strTrue = "=> True";
         const string strFalse = "=> False";
      }
   }

   //class TestCases
   //{
   //   public void Main()
   //   {
   //      var expr = PredicateExpression<int>.Empty;

   //      expr |= x => x > 10;
   //      expr |= x => x < 5;

   //      Expected result equivalent

   //      Func<int, bool> f = x => x > 10 || x < 5;

   //   }
   //}


}

