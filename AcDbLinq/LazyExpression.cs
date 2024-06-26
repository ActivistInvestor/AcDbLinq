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
   /// and enables just-in-time compilation.
   /// /// </summary>

   public struct LazyExpression<TArg, TResult>
   {
      Expression<Func<TArg, TResult>> expression;
      Func<TArg, TResult> function;

      public LazyExpression(Expression<Func<TArg, TResult>> expression = null)
      {
         Assert.IsNotNull(expression, nameof(expression));
         this.expression = expression;
         this.function = CompileAndInvoke;
      }

      /// <summary>
      /// Just-in-time delegate compilation:
      /// 
      /// The core function of this class is to compile an
      /// expression and generate a delegate that can be
      /// called to produce a result. For that, this class 
      /// uses a 'just-in-time' pattern where the expression
      /// is only compiled if/when the delegate is called.
      /// If the delegate is never called, the expression
      /// is never compiled.
      /// 
      /// Because instances of this class are implicitly-
      /// convertable to a delegate (Func<TArg, TResult>), one
      /// can pass an instance of this class wherever the
      /// delegate function is accepted.
      /// 
      /// The CompileAndInvoke() method is assigned to the 
      /// delegate field when the expression has changed and 
      /// needs to be compiled or recompiled, which includes
      /// the point at which the instance is constructed.
      /// 
      /// When this method is assigned to the delegate field
      /// and is subsequently invoked, it performs just-in-time 
      /// compilation of the expression, assigns the result to 
      /// the same field CompileAndInvoke() was assigned to,
      /// and then invokes the newly-generated delegate on the 
      /// argument and returns the result. The next time the
      /// delegate field is invoked, the delegate generated
      /// by CompileAndInvoke() ands assigned to the delegate
      /// field is invoked directly.
      /// 
      /// This makes the compilation of the expression lazy or
      /// 'just-in-time', meaning that if the delegate field
      /// of this instance is never invoked, compilation of the
      /// expression never happens, and if/when it does happen,
      /// it does so transparently. This also allows an instance
      /// to be used as component in the composition of a more-
      /// complex LazyExpression<TArg, TResult> without ever having 
      /// to compile any expressions.
      /// 
      /// If for any reason the expression field of the instance
      /// is assigned a value, the delegate field must then be
      /// assigned to this method.
      /// </summary>
      /// <param name="arg"></param>
      /// <returns></returns>

      TResult CompileAndInvoke(TArg arg)
      {
         Assert.IsNotNull(expression, nameof(expression));
         function = expression.Compile();
         return function(arg);
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
            return function;
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
               function = CompileAndInvoke;
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
         return expr.function;
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

