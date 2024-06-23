/// DBObjectExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DBObjectExtensions.cs
///     
/// A collection of old helper APIs that provide 
/// support for accessing/querying the contents 
/// of AutoCAD Databases using LINQ.
/// 
/// Only minor changes have been made since this
/// library was first written (which happened over
/// the period of several years). 
/// 
/// Some of those revisions require C# 7.0.


namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// An OpenCloseTransaction that always commits,
   /// that is intended exclusively for read-only use.
   /// </summary>

   class ReadOnlyTransaction : OpenCloseTransaction
   {
      protected override void Dispose(bool A_0)
      {
         if(!this.IsDisposed)
            this.Abort();
         base.Dispose(A_0);
      }

      public T GetObject<T>(ObjectId id) where T : DBObject
      {
         return (T)base.GetObject(id, OpenMode.ForRead, false, false);
      }

      public override void Abort()
      {
         base.Commit();
      }
   }
}



