using Autodesk.AutoCAD.Runtime.Diagnostics;

namespace System.Linq.Expressions.Predicates
{
   /// <summary>
   /// A class that encapsulates an Expression<Func<T, bool>>
   /// that supports performing various logical operations on 
   /// them using binary operator syntax.
   /// /// </summary>

   public struct PredicateExpression<T>
   {
      Expression<Func<T, bool>> expression;
      Func<T, bool> predicate;

      public static readonly PredicateExpression<T> True =
         new PredicateExpression<T>(a => true);
      public static readonly PredicateExpression<T> False =
         new PredicateExpression<T>(a => false);
      public static readonly PredicateExpression<T> Default = False;

      /// <summary>
      /// The "Empty" expression is treated specially by any
      /// operation that combines expressions with a logical
      /// operator. If the empty expression appears in that
      /// context, it is ignored and the result is the other 
      /// expression argument unmodified, such that given:
      /// 
      ///    PredicateExpression<int> expr = 
      ///      new PredicateExpression<int>(x => x > 10);
      ///      
      ///    PredicateExpression<int> empty = 
      ///      PredicateExpression<int>.Empty; 
      ///    
      /// These will always be true:
      /// 
      ///    empty.And(expr) == expr;
      ///    
      ///    expr == expr.And(empty, empty, empty);
      ///    
      /// The main purpose behind the use of the Empty 
      /// expression is to allow it to serve as an 
      /// invocation target for extension methods that
      /// target Expression<Func<T, bool>> and return
      /// results that can be implicitly converted to 
      /// PredicateExpression<T>. One can construct a
      /// compound PredicateExpression<T> by starting
      /// with the Empty expression, and then use And()
      /// and Or() to create compound expressions that
      /// do not include the initial Empty expression.
      /// 
      /// <code>
      /// 
      ///   var expr = PredicateExpression<int>.Empty;
      ///   
      ///   expr |= x => x > 10;
      ///   expr |= x < 5;
      ///   
      ///      ->  x => x > 10 || x < 5;
      /// 
      /// </code>
      /// 
      ///    
      /// </summary>

      public static readonly PredicateExpression<T> Empty = False;
         
      /// <summary>
      /// If no argument is provided, the expression is set 
      /// to an expression that unconditionaly returns false;
      /// </summary>
      /// <param name="expression"></param>

      public PredicateExpression(Expression<Func<T, bool>> expression = null)
      {
         this.expression = expression ?? Empty.expression;
         this.predicate = null;
         this.predicate = CompileAndInvoke;
      }

      /// <summary>
      /// Just-in-time predicate compilation:
      /// 
      /// The core function of this class is to compile an
      /// expression and generate a predicate that can be
      /// called to produce a result. For that, this class 
      /// uses a 'just-in-time' pattern where the expression
      /// is only compiled if/when the predicate is called.
      /// If the predicate is never called, the expression
      /// is never compiled.
      /// 
      /// This CompileAndInvoke() method is assigned to the 
      /// predicate field when the expression has changed and 
      /// needs to be compiled or recompiled, which includes
      /// the point at which the instance is constructed.
      /// 
      /// When this method is assigned to the predicate field
      /// and is subsequently invoked, it performs just-in-time 
      /// compilation of the expression, assigns the result to 
      /// the same field CompileAndInvoke() was assigned to,
      /// and then invokes the newly-generated predicate on the 
      /// argument and returns the result. The next time the
      /// predicate field is invoked, the predicate generated
      /// by CompileAndInvoke() is assigned to the predicate
      /// field, and is invoked directly.
      /// 
      /// This makes the compilation of the expression lazy or
      /// 'just-in-time', meaning that if the predicate field
      /// of this instance is never invoked, compilation of the
      /// expression never happens, and if/when it does happen,
      /// it does so transparently. This also allows an instance
      /// to be used as component in the composition of a more-
      /// complex PredicateExpression<T> without ever having to 
      /// compile its expression.
      /// 
      /// If for any reason the expression field of the instance
      /// is assigned a value, the predicate field must then be
      /// assigned to this method.
      /// </summary>
      /// <param name="arg"></param>
      /// <returns></returns>

      bool CompileAndInvoke(T arg)
      {
         Assert.IsNotNull(expression, nameof(expression));
         return (predicate = expression.Compile())(arg);
      }

      public static PredicateExpression<T> Create(Expression<Func<T, bool>> expression)
      { 
         Assert.IsNotNull(expression, nameof(expression));
         return new PredicateExpression<T>(expression);
      }

      public static PredicateExpression<T> GetDefault(bool value = false)
      {
         return value ? True : False;
      }

      public Func<T, bool> Predicate
      {
         get
         {
            return predicate;
         }
      }

      public Expression<Func<T, bool>> Expression 
      { 
         get => expression;
         private set
         {
            Assert.IsNotNull(value, nameof(value));
            expression = value;
            predicate = CompileAndInvoke;
         }
      }

      public bool IsDefault()
      {
         return expression.IsDefault();
      }

      /// params Expression<Func<T, bool>>[]
      public PredicateExpression<T> And(params Expression<Func<T, bool>>[] elements)
      {
         Assert.IsNotNullOrEmpty(elements, nameof(elements));
         if(elements.Length == 1)
            return ExpressionBuilder.And(this, elements.GetAt(0, nameof(elements)));
         return this.expression.AndAll(elements);
      }

      /// params PredicateExpression<T>[]
      public PredicateExpression<T> And(params PredicateExpression<T>[] elements)
      {
         Assert.IsNotNull(elements, nameof(elements));
         return And(elements.Select(e => e.expression).ToArray());
      }

      /// params Expression<Func<T, bool>>[]
      public PredicateExpression<T> Or(params Expression<Func<T, bool>>[] elements)
      {
         Assert.IsNotNullOrEmpty(elements, nameof(elements));
         if(elements.Length == 1)
            return ExpressionBuilder.Or(this, elements.GetAt(0, nameof(elements)));
         else
            return this.expression.OrAny(elements);
      }

      /// params PredicateExpression<T>[]
      public PredicateExpression<T> Or(params PredicateExpression<T>[] elements)
      {
         Assert.IsNotNull(elements, nameof(elements));
         return Or(elements.Select(e => e.expression).ToArray());
      }

      public static PredicateExpression<T> All(params Expression<Func<T, bool>>[] elements)
      {
         Assert.IsNotNull(elements, nameof(elements));
         if(elements.Length < 2)
            throw new ArgumentException("Requires at least 2 arguments.");
         return Create(ExpressionBuilder.All(elements));
      }

      public static PredicateExpression<T> Any(params Expression<Func<T, bool>>[] elements)
      {
         Assert.IsNotNull(elements, nameof(elements));
         if(elements.Length < 2)
            throw new ArgumentException("Requires at least 2 arguments.");
         return Create(ExpressionBuilder.Any(elements));
      }

      public static PredicateExpression<T> Not(Expression<Func<T, bool>> expression)
      {
         Assert.IsNotNull(Not(expression), nameof(expression));
         return expression.Not();
      }

      /// <summary>
      /// Operators
      ///   
      ///    x & y is equivalent to x.And(y)
      ///    x | y is equivalent to x.Or(y)
      ///    
      ///    x &= y1 & y2 & y3 
      ///    
      ///    is equivalent to either of these:
      ///    
      ///        x = x.And(y1, y2, y3);
      ///        
      ///        x = x.And(y1).And(y2).And(y3);
      ///        
      /// & and | operators can accept a combination of
      /// PredicateExpression<T> and Expression<Func<T, bool>>
      /// on either side, but one operand must be the former.
      /// 
      /// With C# 13, things are going to become a bit more
      /// interesting.
      ///     
      /// </summary>

      public static PredicateExpression<T> operator &(
         PredicateExpression<T> left,
         Expression<Func<T, bool>> right)
      {
         return left.And(right);
      }

      public static PredicateExpression<T> operator &(
         Expression<Func<T, bool>> left,
         PredicateExpression<T> right)

      {
         return left.And(right);
      }

      public static PredicateExpression<T> operator |(
         PredicateExpression<T> left,
         Expression<Func<T, bool>> right)
      {
         return left.Or(right);
      }

      public static PredicateExpression<T> operator |(
         Expression<Func<T, bool>> left,
         PredicateExpression<T> right)

      {
         return left.Or(right);
      }

      /// <summary>
      /// Conversion operators
      /// 
      /// Bi-directional conversion from/to
      /// PredicateExpression<T> and 
      /// Expression<Func<T, bool>>,
      /// 
      /// Unidirectional conversion to Func<T, bool>
      /// </summary>

      public static implicit operator Expression<Func<T, bool>>(PredicateExpression<T> expr)
      {
         Assert.IsNotNull(expr, nameof(expr));
         return expr.expression;
      }

      public static implicit operator Func<T, bool>(PredicateExpression<T> expr)
      {
         Assert.IsNotNull(expr, nameof(expr));
         return expr.predicate;
      }

      public static implicit operator PredicateExpression<T>(Expression<Func<T, bool>> expression)
      {
         return Create(expression ?? throw new ArgumentNullException(nameof(expression))); 
      }

      public override string ToString()
      {
         return expression.ToString();
      }

   }


}

