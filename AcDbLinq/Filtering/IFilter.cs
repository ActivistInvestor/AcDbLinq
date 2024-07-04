

using System;

/// IFilter.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   public interface IFilter<TKeySource> where TKeySource : DBObject
   {
      bool IsMatch(TKeySource source);
      Func<TKeySource, bool> MatchPredicate { get;}
   }
}