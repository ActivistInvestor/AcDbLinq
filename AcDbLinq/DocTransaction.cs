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
using AcRx = Autodesk.AutoCAD.Runtime;
using AcAp = Autodesk.AutoCAD.ApplicationServices;

namespace Autodesk.AutoCAD.ApplicationServices.Extensions
{
   public class DocTransaction : DBTransaction
   {
      Document doc = null;
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
      }

      public override void Commit()
      {
         base.Commit();
         if(doc != null)
            manager.FlushGraphics();
         GC.KeepAlive(this);
      }

      public Document Document => doc;

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




