/// DatabaseTransaction.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.


using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime.Diagnostics;
using Autodesk.AutoCAD.Runtime.Extensions;
using Autodesk.AutoCAD.ApplicationServices;
using System.Runtime.CompilerServices;
using AcRx = Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime;

/// Alternate pattern that allows the use of a custom 
/// Transaction to serve as the invocation target for 
/// Database extension methods provided by this library.
/// 
/// Note that complete documentation of the APIs below
/// is an ongoing work-in-progress.

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// <summary>
   /// A specialization of Autodesk.AutoCAD.DatabaseServices.Transaction
   /// 
   /// This class encapsulates both a Database and a Transaction. 
   /// 
   /// Instance members mirroringthe methods of the DatabaseExtensions 
   /// class are included in this class, and can be used to perform a 
   /// variety of operations. The methods that mirror the methods of the
   /// DatabaseExtensions class are instance methods that can be invoked
   /// on an instance of this type or a derived type. That allows these
   /// methods to be called without the need to pass a Transaction or a
   /// Database as arguments.
   /// 
   /// If the Database argument's AutoDelete property is true (meaning it 
   /// is not a Database that is open in the Editor), the constructor will 
   /// set that Database to the current working database, and will restore 
   /// the previous working database when the instance is disposed. 
   /// 
   /// An optional argument to the constructor can be specified to
   /// suppress changing the current working database.
   /// </summary>

   public class DatabaseTransaction : Transaction
   {
      Database db;
      TransactionManager manager = null;   
      Database prevWorkingDb = null;
      protected static readonly DocumentCollection Documents = Application.DocumentManager;

      /// <summary>
      /// Creates and starts a DatabaseTransaction. 
      /// </summary>
      /// <param name="database">The Database in which to start the transaction</param>
      /// <param name="asWorkingDatabase">A value indicating if the Database
      /// should be made the current working database for the life of the 
      /// transaction. This argument is not applicable to databases that are
      /// open in the AutoCAD editor, or databases representing external
      /// references.</param>

      public DatabaseTransaction(Database database, bool asWorkingDatabase = true)
         : base(new IntPtr(-1), false)
      {
         Assert.IsNotNullOrDisposed(database, nameof(database));
         this.db = database;
         this.manager = database.TransactionManager;
         if(asWorkingDatabase && database.AutoDelete && WorkingDatabase != database)
         {
            prevWorkingDb = WorkingDatabase;
            WorkingDatabase = database;
         }
         manager.StartTransaction().ReplaceWith(this);
      }

      /// <summary>
      /// This is only intended to be called from the
      /// constructor of DocumentTransaction
      /// </summary>

      protected DatabaseTransaction(Database db, TransactionManager mgr)
         : base(new IntPtr(-1), false)
      {
         Assert.IsNotNullOrDisposed(db, nameof(db));
         Assert.IsNotNullOrDisposed(mgr, nameof(mgr));
         this.db = db;
         this.manager = mgr;
      }

      public Database Database => db;

      /// <summary>
      /// Can be set to true, to prevent the transaction
      /// from aborting, which has high overhead. Use only
      /// when the database, document, and editor state has
      /// not been altered in any way (including sysvars,
      /// view changes, etc.).
      /// </summary>

      public bool IsReadOnly { get; set; }

      public override void Abort()
      {
         if(IsReadOnly)
            Commit();
         else
            base.Abort();
      }

      protected override void Dispose(bool disposing)
      {
         if(disposing && prevWorkingDb != null && prevWorkingDb != WorkingDatabase)
         {
            WorkingDatabase = prevWorkingDb;
            prevWorkingDb = null;
         }
         base.Dispose(disposing);
      }

      public override TransactionManager TransactionManager => manager;

      static Database WorkingDatabase
      {
         get => HostApplicationServices.WorkingDatabase;
         set => HostApplicationServices.WorkingDatabase = value;
      }

      /// <summary>
      /// Database-oriented Operations
      /// </summary>

      public ObjectIdCollection Append(DBObjectCollection entities,
         ObjectId ownerId = default(ObjectId)) 
      {
         return Append<Entity>(entities.OfType<Entity>(), ownerId);
      }

      public ObjectIdCollection Append<T>(IEnumerable<T> entities, 
         ObjectId ownerId = default(ObjectId)) where T:Entity
      {
         Assert.IsNotNull(entities, nameof(entities));
         ObjectId target = ownerId.IsNull ? db.CurrentSpaceId : ownerId;
         AcRx.ErrorStatus.WrongObjectType.Requires<BlockTableRecord>(target);
         var owner = GetObject<BlockTableRecord>(target, OpenMode.ForWrite);
         ObjectIdCollection result = new ObjectIdCollection();
         foreach(T entity in entities)
         {
            result.Add(owner.AppendEntity(entity));
            AddNewlyCreatedDBObject(entity, true);
         }
         return result;
      }

      /// <summary>
      /// Overload of Append() that takes the owner
      /// BlockTableRecord as an argument. The owner
      /// must be open for write or an exception will
      /// be thrown.

      public ObjectIdCollection Append<T>(IEnumerable<T> entities,
         BlockTableRecord owner) where T : Entity
      {
         Assert.IsNotNull(entities, nameof(entities));
         Assert.IsNotNullOrDisposed(owner, nameof(owner));
         AcRx.ErrorStatus.NotOpenForWrite.ThrowIf(!owner.IsWriteEnabled);
         ObjectIdCollection result = new ObjectIdCollection();
         foreach(T entity in entities)
         {
            result.Add(owner.AppendEntity(entity));
            AddNewlyCreatedDBObject(entity, true);
         }
         return result;
      }

      /// <summary>
      /// A strongly-typed verion of GetObject() that
      /// merely allows the caller to avoid an explicit
      /// cast to the desired type.
      /// </summary>

      public T GetObject<T>(ObjectId id, OpenMode mode = OpenMode.ForRead, bool openErased = false, bool openOnLockedLayer = false) where T:DBObject
      {
         return (T) base.GetObject(id, mode, openErased, openOnLockedLayer);
      }

      /// <summary>
      /// An indexer that can be used to open objects for read,
      /// typed as DBObject:
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>

      public DBObject this[ObjectId key]
      {
         get
         {
            return base.GetObject(key, OpenMode.ForRead, false, false);
         }
      }

      public static implicit operator Database(DatabaseTransaction operand)
      {
         return operand?.db ?? throw new ArgumentNullException(nameof(operand));
      }

      /// What follows are replications of most methods of the
      /// DatabaseExtensions class, expressed as instance methods
      /// of this type, that pass the encapsulated Database and 
      /// the instance as the Database and Transaction arguments
      /// respectively. Making these methods instance methods of
      /// this class allows them to be called without having to
      /// be passed a Transaction or a Database argument, serving
      /// to greatly simplify their use.
      /// 
      /// The usage pattern for this class is to create an instance
      /// of it in lieu of a standard Transaction, and then invoke
      /// the included methods to perform operations on a database.
      /// 
      /// See the docs for methods of the DatabaseExtensions class
      /// for more information on these APIs. The main difference
      /// between these methods and the equivalent methods of the
      /// DatabaseExtensions class, is that all methods of this type
      /// replace the Database as the invocation target with the 
      /// instance of this type, and omit all Transaction arguments.

      public IEnumerable<T> GetModelSpaceObjects<T>(
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         return db.GetModelSpaceObjects<T>(this, mode, exact, openLocked);
      }

      /// <summary>
      /// Opens model space objects of the specified generic
      /// argument type for read, filtered by the specified filter.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="filter">An object that implements IFilter<T>,
      /// (such as DBObjectFilter), that is used to constrain the
      /// elements produced by this method.</param>
      /// <returns>A sequence of entities that 
      /// satisfy the filter criteria</returns>

      public IEnumerable<T> GetModelSpaceObjects<T>(IFilter<T> filter) where T:Entity
      {
         Assert.IsNotNull(filter, nameof(filter));
         return db.GetModelSpaceObjects<T>(this, OpenMode.ForRead, false, false)
            .Where(filter.MatchPredicate);
      }

      public IEnumerable<Entity> GetModelSpaceEntities(
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false)
      {
         return db.GetModelSpaceObjects<Entity>(this, mode, exact, openLocked);
      }

      public IEnumerable<T> GetCurrentSpaceObjects<T>(
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         return db.GetCurrentSpaceObjects<T>(this, mode, exact, openLocked);
      }

      public IEnumerable<T> GetCurrentSpaceObjects<T>(IFilter<T> filter) where T : Entity
      {
         Assert.IsNotNull(filter, nameof(filter));
         return db.GetCurrentSpaceObjects<T>(this, OpenMode.ForRead, false, false)
            .Where(filter.MatchPredicate);
      }


      public IEnumerable<Entity> GetCurrentSpaceEntities(
         OpenMode mode = OpenMode.ForRead,
         bool openLocked = false)
      {
         return db.GetCurrentSpaceObjects<Entity>(this, mode, false, openLocked);
      }

      public IEnumerable<T> GetPaperSpaceObjects<T>(
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         return db.GetPaperSpaceObjects<T>(this, mode, exact, openLocked);
      }

      public Layout GetLayout(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetLayout(name, this, mode, throwIfNotFound);
      }

      public BlockTableRecord GetBlock(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<BlockTableRecord>(name, this, mode, throwIfNotFound);
      }

      public LayerTableRecord GetLayer(string name, 
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<LayerTableRecord>(name, this, mode, throwIfNotFound);
      }

      public LinetypeTableRecord GetLinetype(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<LinetypeTableRecord>(name, this, mode, throwIfNotFound);
      }

      public ViewportTableRecord GetViewportTableRecord(Func<ViewportTableRecord, bool> predicate,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<ViewportTableRecord>(predicate, this, mode);
      }

      public ViewportTableRecord GetViewportTableRecord(int vpnum,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<ViewportTableRecord>(vptr => vptr.Number == vpnum, this, mode);
      }

      public ViewTableRecord GetView(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<ViewTableRecord>(name, this, mode, throwIfNotFound);
      }

      public DimStyleTableRecord GetDimStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<DimStyleTableRecord>(name, this, mode, throwIfNotFound);
      }

      public RegAppTableRecord GetRegApp(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<RegAppTableRecord>(name, this, mode, throwIfNotFound);
      }

      public TextStyleTableRecord GetTextStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<TextStyleTableRecord>(name, this, mode, throwIfNotFound);
      }

      public UcsTableRecord GetUcs(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetRecord<UcsTableRecord>(name, this, mode, throwIfNotFound);
      }

      public T GetNamedObject<T>(string key,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = true) where T : DBObject
      {
         return db.GetNamedObject<T>(key, this, mode, throwIfNotFound);
      }

      public IEnumerable<T> GetNamedObjects<T>(OpenMode mode = OpenMode.ForRead) where T : DBObject
      {
         return db.GetNamedObjects<T>(this, mode);
      }

      public T GetRecord<T>(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false) where T : SymbolTableRecord
      {
         return db.GetRecord<T>(name, this, mode, throwIfNotFound);
      }

      public ObjectId GetRecordId(SymbolTable table, string key)
      {
         if(table.Has(key))
         {
            try
            {
               return table[key];
            }
            catch(AcRx.Exception)
            {
            }
         }
         return ObjectId.Null;
      }

      public T GetRecord<T>(Func<T, bool> predicate,
         OpenMode mode = OpenMode.ForRead) where T : SymbolTableRecord
      {
         return db.GetRecord<T>(predicate, this, mode);
      }

      public ObjectId GetLayoutId(string layoutName, bool throwIfNotFound = false)
      {
         return db.GetLayoutId(layoutName, throwIfNotFound);
      }

      public ObjectId GetDictionaryEntryId<T>(string key, bool throwIfNotFound = false)
         where T : DBObject
      {
         return db.GetDictionaryEntryId<T>(key, throwIfNotFound);
      }

      public ObjectId GetDictionaryEntryId<T>(Func<T, bool> predicate)
         where T : DBObject
      {
         return db.GetDictionaryEntryId<T>(predicate);
      }

      public IEnumerable<ObjectId> GetDictionaryEntryIds<T>(Func<T, bool> predicate) 
         where T : DBObject
      {
         return db.GetDictionaryEntryIds<T>(predicate);
      }

      public T GetDictionaryObject<T>(string key,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = true) where T : DBObject
      {
         return db.GetDictionaryObject<T>(key, this, mode, throwIfNotFound);
      }

      public T GetDictionaryObject<T>(Func<T, bool> predicate,
         OpenMode mode = OpenMode.ForRead) where T : DBObject
      {
         return db.GetDictionaryObject<T>(this, predicate, mode);
      }

      public IEnumerable<T> GetDictionaryObjects<T>(OpenMode mode = OpenMode.ForRead) 
         where T : DBObject
      {
         return db.GetDictionaryObjects<T>(this, mode);
      }

      public Group GetGroup(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetGroup(name, this, mode, throwIfNotFound);
      }

      public DataLink GetDataLink(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetDataLink(name, this, mode, throwIfNotFound);
      }

      public DetailViewStyle GetDetailViewStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetDetailViewStyle(name, this, mode, throwIfNotFound);
      }

      public SectionViewStyle GetSectionViewStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetSectionViewStyle(name, this, mode, throwIfNotFound);
      }

      public MLeaderStyle GetMLeaderStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetMLeaderStyle(name, this, mode, throwIfNotFound);
      }

      public TableStyle GetTableStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetTableStyle(name, this, mode, throwIfNotFound);
      }

      public PlotSettings GetPlotSettings(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetPlotSettings(name, this, mode, throwIfNotFound);
      }

      public DBVisualStyle GetVisualStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetVisualStyle(name, this, mode, throwIfNotFound);
      }

      public Material GetMaterial(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetMaterial(name, this, mode, throwIfNotFound);
      }

      public MlineStyle GetMlineStyle(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetMlineStyle(name, this, mode, throwIfNotFound);
      }

      public Layout GetLayoutByKey(string name,
         OpenMode mode = OpenMode.ForRead,
         bool throwIfNotFound = false)
      {
         return db.GetLayoutByKey(name, this, mode, throwIfNotFound);
      }

      public IEnumerable<BlockTableRecord> GetLayoutBlocks(
         OpenMode mode = OpenMode.ForRead,
         bool includingModelSpace = false)
      {
         return this.db.GetLayoutBlocks(this, mode, includingModelSpace);
      }

      public IEnumerable<BlockReference> GetBlockReferences(string pattern,
               OpenMode mode = OpenMode.ForRead,
               Func<BlockTableRecord, bool> predicate = null)
      {
         return this.db.GetBlockReferences(pattern, this, mode, predicate);
      }

      /// SymbolUtilityServices methods transposed to instance properties:

      public ObjectId ModelSpaceBlockId => 
         SymbolUtilityServices.GetBlockModelSpaceId(db);
      public ObjectId PaperSpaceBlockId => 
         SymbolUtilityServices.GetBlockPaperSpaceId(db);
      public ObjectId LinetypeByBlockId => 
         SymbolUtilityServices.GetLinetypeByBlockId(db);
      public ObjectId LinetypeByLayerId => 
         SymbolUtilityServices.GetLinetypeByLayerId(db);
      public ObjectId LinetypeContinuousId => 
         SymbolUtilityServices.GetLinetypeContinuousId(db);
      public ObjectId RegAppAcadId => 
         SymbolUtilityServices.GetRegAppAcadId(db);
      public ObjectId TextStyleStandardId => 
         SymbolUtilityServices.GetTextStyleStandardId(db);
      public ObjectId LayerDefpointsId => 
         SymbolUtilityServices.GetLayerDefpointsId(db);
      public ObjectId LayerZeroId => 
         SymbolUtilityServices.GetLayerZeroId(db);
      public bool IsCompatibilityMode => 
         SymbolUtilityServices.IsCompatibilityMode(db);
      
   }

}




