/// LazyExpression.cs
/// 
/// ActivistInvestor / Tony Tanzillo
///
/// Distributed under terms of the MIT License

using Autodesk.AutoCAD.Runtime.Diagnostics;

namespace System.Linq.Expressions.Extensions
{
   /// <summary>
   /// A class that encapsulates an Expression<Func<TArg, TResult>>
   /// and performs lazy compilation.
   /// 
   /// </summary>

   public struct LazyExpression<TArg, TResult>
   {
      Expression<Func<TArg, TResult>> expression;
      Func<TArg, TResult> function;

      public LazyExpression(Expression<Func<TArg, TResult>> expression = null)
      {
         Assert.IsNotNull(expression, nameof(expression));
         this.expression = expression;
         this.function = null;
      }

      public static LazyExpression<TArg, TResult> Create(Expression<Func<TArg, TResult>> expression)
      { 
         Assert.IsNotNull(expression, nameof(expression));
         return new LazyExpression<TArg, TResult>(expression);
      }

      public Func<TArg, TResult> Function
      {
         get
         {
            return function ?? (function = expression.Compile());
         }
      }

      public Expression<Func<TArg, TResult>> Expression 
      { 
         get => expression;
         private set
         {
            Assert.IsNotNull(value, nameof(value));
            if(!object.ReferenceEquals(expression, value))
            {
               expression = value;
               function = null;
            }
         }
      }

      public static implicit operator Expression<Func<TArg, TResult>>(LazyExpression<TArg, TResult> expr)
      {
         Assert.IsNotNull(expr, nameof(expr));
         return expr.expression;
      }

      public static implicit operator Func<TArg, TResult>(LazyExpression<TArg, TResult> expr)
      {
         Assert.IsNotNull(expr, nameof(expr));
         return expr.Function;
      }

      public static implicit operator LazyExpression<TArg, TResult>(Expression<Func<TArg, TResult>> expression)
      {
         Assert.IsNotNull(expression, nameof(expression));
         return new LazyExpression<TArg, TResult>(expression);
      }

      public override string ToString()
      {
         return expression.ToString();
      }

   }


}

