/// DBObjectFilterExample.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.

using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices.Extensions;
using Autodesk.AutoCAD.EditorInput;
using AcRx = Autodesk.AutoCAD.Runtime;
using AcAp = Autodesk.AutoCAD.ApplicationServices;

namespace Autodesk.AutoCAD.ApplicationServices.Extensions
{
   /// <summary>
   /// When constructed from the application context,
   /// this class implicitly locks the document. The
   /// scope of the document lock is the scope of the
   /// instance, up to the point when the instance is
   /// disposed.
   /// </summary>
   
   public class DocTransaction : DBTransaction
   {
      Document doc = null;
      DocumentLock docLock = null;
      TransactionManager manager = null; // AcAp.TransactionManager

      public DocTransaction() : this(ActiveDocument)
      {
      }

      public DocTransaction(Document doc) : base(doc?.Database, doc?.TransactionManager)
      {
         Assert.IsNotNullOrDisposed(doc, nameof(doc));
         this.doc = doc;
         manager = doc.TransactionManager;
         manager.EnableGraphicsFlush(true);
         if(Application.DocumentManager.IsApplicationContext)
            docLock = doc.LockDocument();
      }

      public override void Commit()
      {
         base.Commit();
         if(doc != null)
            manager.FlushGraphics();
         GC.KeepAlive(this);
      }

      protected override void Dispose(bool disposing)
      {
         docLock?.Dispose();
         base.Dispose(disposing);
      }

      public Document Document => doc;
      public Editor Editor => doc.Editor;

      protected static Document ActiveDocument
      {
         get
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            AcRx.ErrorStatus.NoDocument.ThrowIf(doc == null);
            return doc;
         }
      }
   }
}




