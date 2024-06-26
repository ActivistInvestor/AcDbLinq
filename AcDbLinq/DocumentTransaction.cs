﻿/// DocumentTransaction.cs  
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
using Autodesk.AutoCAD.Runtime.Extensions;
using AcRx = Autodesk.AutoCAD.Runtime;
using AcAp = Autodesk.AutoCAD.ApplicationServices;

namespace Autodesk.AutoCAD.ApplicationServices.Extensions
{
   /// <summary>
   /// This class serves as a foundation for a fundamental
   /// shift to a 'Transaction-centric' programming model,
   /// verses the Document- or Database-centric model that
   /// most AutoCAD managed extensions commonly use.
   /// 
   /// A specialization of DatabaseServices.Transaction
   /// 
   /// In addition to all of the functionality of a 
   /// Transaction, this class provides the following:
   /// 
   /// 1. Implicit document locking:
   /// 
   ///   When constructed from the application context,
   ///   this class implicitly locks the document. The
   ///   scope of the document lock is the scope of the
   ///   instance, up to the point when it is disposed.
   ///   
   ///   An optional argument to the constructor allows
   ///   implicit document locking to be suppressed if 
   ///   needed.
   ///   
   /// 2. Graphics refresh 
   /// 
   ///   The same support for graphics refresh that's
   ///   performed by the Transactions returned by the
   ///   Document's TransactionManager (FlushGraphics()
   ///   and EnableGraphicsFlush()).
   ///   
   /// 3. Encapsulation of the associated Document and
   ///    Database.
   ///    
   ///   This class is derived from DatabaseTransaction, 
   ///   which exposes the same set of APIs available as 
   ///   extension methods of the Database class. However
   ///   in the case of the DocumentTransaction, they are
   ///   instance methods rather than extension methods. 
   ///   See the DatabaseExtensions class for details and 
   ///   documentation on those methods.
   ///   
   /// The included DBObjectFilterExample.cs example shows
   /// how the Transaction-centric programming model enabled 
   /// by this class and it base class can be used to simplify 
   /// and/or automate common operations performed by AutoCAD 
   /// extensions.
   /// 
   /// </summary>
   
   public class DocumentTransaction : DatabaseTransaction
   {
      Document doc = null;
      DocumentLock docLock = null;

      public DocumentTransaction(bool lockDocument = true) 
         : this(ActiveDocument, lockDocument)
      {
      }

      public DocumentTransaction(Document doc, bool lockDocument = true) 
         : base(doc?.Database, doc?.TransactionManager)
      {
         Assert.IsNotNullOrDisposed(doc, nameof(doc));
         this.doc = doc;
         if(lockDocument && Documents.IsApplicationContext)
            docLock = doc.LockDocument();
         doc.TransactionManager.EnableGraphicsFlush(true);
         doc.TransactionManager.StartTransaction().ReplaceWith(this);
      }

      public override void Commit()
      {
         base.Commit();
         doc.TransactionManager.FlushGraphics();
      }

      protected override void Dispose(bool disposing)
      {
         if(disposing && docLock != null)
         {
            docLock.Dispose();
            docLock = null;
         }
         base.Dispose(disposing);
      }

      public Document Document => doc;
      public Editor Editor => doc.Editor;

      protected static Document ActiveDocument
      {
         get
         {
            Document doc = Documents.MdiActiveDocument;
            AcRx.ErrorStatus.NoDocument.ThrowIf(doc == null);
            return doc;
         }
      }
   }
}




