/// Logical.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Classes that aid in the efficient filtering of 
/// DBObjects in Linq queries and other sceanrios.


namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   public enum Logical
   {
      /// <summary>
      /// Both input expressions must return true
      /// </summary>

      And, 

      /// <summary>
      /// Either input expression must return true
      /// </summary>

      Or, 

      /// <summary>
      /// Like And, except that the order in which 
      /// two expressions are evaluated is reversed.
      /// </summary>

      ReverseAnd,

      /// <summary>
      /// Like Or, except that the order in which 
      /// two expressions are evaluated is reversed.
      /// </summary>

      ReverseOr,

      /// <summary>
      /// Currently not supported
      /// </summary>

      Xor, 
      ReverseXor,

      /// <summary>
      /// The logical complement of a single expression
      /// </summary>

      Not
   }


}



