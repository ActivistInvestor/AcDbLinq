﻿/// DBObjectFilterExample.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Example code showing how to use/extend the
/// DBObjectFilter and various other classes from 
/// the AcDbLinq library.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Extensions;
using System.Linq.Extensions;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Extensions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Extensions;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.Runtime;

namespace AutoCAD.AcDbLinq.Examples
{
   /// The docs for DBObjectFilter showed a simple example of
   /// a DBObjectFilter that filters entities based on if the
   /// layer they reside on/reference is locked.
   /// 
   /// DBObjectFilter is not only applicable to entities and
   /// LayerTableRecords. The generic arguments are what allow
   /// it to be used for many similar use cases as well.
   /// 
   /// The following example filters BlockReferences by their
   /// "effective name". The filter resolves anonymous dynamic 
   /// blocks to their dynamic block definition, allowing its 
   /// name to be filtered against for both references to the
   /// dynamic block definition, and references to anonymous
   /// variations of it. This example will include references
   /// to all blocks having names that start with "DESK":
   ///
   /// <code>
   /// 
   ///   var deskFilter = new DBObjectFilter<BlockReference, BlockTableRecord>(
   ///      blkref => blkref.DynamicBlockTableRecord, 
   ///      block => block.Name.Matches("DESK*")
   ///   );
   ///      
   /// </code>
   /// Note that this time the generic arguments are different.
   /// The objects being queried are block references, and the
   /// objects used to determine if a block reference satisfies
   /// the query criteria is the referenced BlockTableRecord. 
   /// 
   /// Also note that the first delegate passed to the constructor
   /// returns the DynamicBlockTableRecord's property value for every 
   /// block reference, which means that it resolves not to anonymous 
   /// blocks, but rather to the defining dynamic block definition, 
   /// that holds the 'effective name' of all references to the block,
   /// including references to anonymous blocks. 
   /// 
   /// So, we see another problem that is solved by the DBObjectFilter,
   /// which is that it can implicitly resolve anonymous dynamic block
   /// references to the dynamic block definition. You can of course,
   /// have it resolve to anonymous block definitions, by simply using
   /// the BlockTableRecord property in the second delegate, instead of
   /// the DynamicBlockTableRecord property. There are legitimate use
   /// cases for both options, and so we can define specializations of
   /// DBObjectFilter that specifically-targets BlockReferences and 
   /// BlockTableRecords, creating two versions, one that resolves to
   /// dynamic blocks, and one that resolves to anonymous blocks:
   /// 
   /// A specialization of DBObjectFilter that resolves dynamic block 
   /// references to anonymous blocks:
   ///

   public class StaticBlockFilter : DBObjectFilter<BlockReference, BlockTableRecord>
   {
      public StaticBlockFilter(Expression<Func<BlockTableRecord, bool>> predicate)
         : base(blockref => blockref.BlockTableRecord, predicate)
      {
      }
   }

   /// And a second variant that resolves anonymous dynamic 
   /// block references to the dynamic block definition:

   public class BlockFilter : DBObjectFilter<BlockReference, BlockTableRecord>
   {
      public BlockFilter(Expression<Func<BlockTableRecord, bool>> predicate)
        : base(blockref => blockref.DynamicBlockTableRecord, predicate)
      {
      }
   }

   /// <summary>
   /// A specialization of DBObjectFilter that filters entities 
   /// based on properties of the layer they reference/reside on:
   /// </summary>

   public class LayerFilter<T> : DBObjectFilter<T, LayerTableRecord> where T : Entity
   {
      public LayerFilter(Expression<Func<LayerTableRecord, bool>> predicate)
         : base(e => e.LayerId, predicate)
      {
      }
   }

   /// <summary>
   /// What's not obvious from the above examples, is how efficient
   /// the DBObjectFilter is at doing its job. For example, when using 
   /// the filter that excludes entities on locked layers, it has to 
   /// open each LayerTableRecord <em>only once</em>, regardless of how
   /// many entities reference that layer. The result of applying the 
   /// caller-supplied predicate to each LayerTableRecord is cached, and 
   /// subsequently used whenever the locked state of that same layer 
   /// is requested.
   /// 
   /// In the example that filters blocks by name, each BlockTableRecord
   /// must be opened and its name tested <em>only once</em>, regardless
   /// of how many insertions of the same block are encountered. 
   /// 
   /// That means that instead of having to perform a wildcard comparison 
   /// for each block reference, the comparison is performed only once for 
   /// each block definition, and the result is cached for subsequent use 
   /// with other references to the same block.
   /// 
   /// This example uses the BlockFilter to collect all insertions of 
   /// blocks in model space whose names start with "DESK":
   /// </summary>

   public static class DBObjectFilterExamples
   {
      /// <summary>
      /// An example that finds and selects all block insertions
      /// in model space having names that start with "DESK":
      /// </summary>

      [CommandMethod("SELECTDESKS", CommandFlags.Redraw)]
      public static void FindAndSelectDeskBlocks()
      {
         // Define a filter that collects all insertions of blocks
         // having names that start with "DESK":

         var deskFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

         // We'll use a DocumentTransaction to simplify the operation.
         // The default constructor uses the active document:

         using(var tr = new DocumentTransaction())
         {
            // Rather than having to write dozens of lines of
            // code, using the BlockFilter and a helper method
            // from this library, in two lines of code we can
            // collect all block references in model space whose
            // block name starts with "DESK". That will include
            // references to anonymous dynamic blocks as well,
            // which is what complicates most other conventional
            // means of achieving the objective:

            var desks = tr.GetModelSpaceObjects<BlockReference>().Where(deskFilter);

            // Get the ObjectIds of the resulting block references:

            var ids = desks.Select(br => br.ObjectId).ToArray();

            tr.Editor.WriteMessage($"\nFound {ids.Length} DESK blocks.");

            // Select the resulting block references:

            if(ids.Length > 0)
               tr.Editor.SetImpliedSelection(ids);

            tr.Commit();
         }
      }

      /// <summary>
      /// A slightly modified example that erases the
      /// resulting objects:
      /// </summary>

      [CommandMethod("ERASEDESKS", CommandFlags.Redraw)]
      public static void EraseDesks()
      {
         using(var doc = new DocumentTransaction())
         {
            var deskFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
            var desks = doc.GetModelSpaceObjects<BlockReference>().Where(deskFilter);

            int cnt = 0;
            foreach(BlockReference blockref in desks.UpgradeOpen())
            {
               blockref.Erase();
               ++cnt;
            }
            doc.Commit();

            doc.Editor.WriteMessage($"Found and erased {cnt} DESK blocks");
         }
      }

      /// <summary>
      /// The following variations of the above two commands introduce 
      /// a second DBObjectFilter that excludes all block references on 
      /// locked layers. The two filters are joined in a logical 'and' 
      /// operation allowing them to work as a single, complex filter.
      /// </summary>

      [CommandMethod("SELECTDESKS2")]
      public static void SelectDesks2()
      {
         /// A DocumentTransaction combines the functionality of
         /// a transaction and the broad range of operations
         /// provided by the extension methods of the Database 
         /// class that are included in this library. 
         /// 
         /// All extension methods that target the Database class
         /// are also instance members of DocumentTransaction.

         using(var doc = new DocumentTransaction())
         {
            // Define a filter that collects all insertions of blocks
            // having names that start with "DESK":

            var filter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

            /// Add a child filter to the block filter that 
            /// excludes block references on locked layers:

            filter.Add<LayerTableRecord>(ent => ent.LayerId,
               layer => !layer.IsLocked);

            // Define the filtered sequence. The filter instance
            // applies both the block and layer filter criteria:

            var desks = doc.GetModelSpaceObjects<BlockReference>().Where(filter);

            // Get the ObjectIds of the resulting block references:

            var ids = desks.Select(br => br.ObjectId).ToArray();
            doc.Editor.WriteMessage($"\nFound {ids.Length} DESK blocks.");

            // Select the resulting block references:
            if(ids.Length > 0)
               doc.Editor.SetImpliedSelection(ids);

            doc.Commit();
         }
      }

      /// <summary>
      /// The next example demonstrates the 'composability' 
      /// aspects of the DBObjectFilter class. What it does
      /// is similar to the previous example, but uses the
      /// Add<T>() method to simplify the addition of the
      /// layer filter and its criteria. The Add<T>() method
      /// is the recommended way to use multiple filters, as
      /// it is much easier to implement and makes the code
      /// logic easier to understand.
      /// 
      /// Composability is what allows runtime conditions to 
      /// determine what critiera is used to filter/query 
      /// objects.
      /// 
      /// The example erases all uniformly-scaled insertions of 
      /// blocks in model space having names starting with "DESK", 
      /// that reside on unlocked layers whose names start with 
      /// "FURNITURE".
      /// 
      /// This example shows how to add an additional condition 
      /// to the query criteria used to qualify objects to be
      /// erased (in this case, that they must reside on a layer 
      /// whose name starts with "FURNITURE", in addition to the 
      /// layer being unlocked).
      /// 
      /// DocumentTransaction:
      /// 
      /// Also note that because this command is registered to run
      /// in the application context, implicit document locking and
      /// unlocking is fully-automated by the DocumentTransaction.
      /// </summary>

      [CommandMethod("ERASEDESKS2", CommandFlags.Session)]
      public static void EraseDesks2()
      {
         // Define a filter that includes only block 
         // references having names starting with 'DESK',
         // using the BlockFilter specialization defined 
         // above:

         var filter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

         /// Add a complex condition to the block filter, that 
         /// includes only block references residing on unlocked 
         /// layers having names that start with "FURNITURE". 
         /// 
         /// The first delegate argument is passed a BlockReference, 
         /// and must return the ObjectId of the object to which the
         /// second predicate argument is applied to, to determine if
         /// the BlockReference satisfies the filter criteria.

         filter.Add<LayerTableRecord>(br => br.LayerId,
            layer => !layer.IsLocked && layer.Name.Matches("FURNITURE*"));

         /// The next line adds a 'simple' condition to the block 
         /// filter, that excludes all non-uniformly scaled block 
         /// references.
         /// 
         /// Simple conditions are predicates that are applied
         /// directly to the type of objects being filtered (the
         /// TFiltered generic argument), while complex conditions
         /// are ones with predicates that are applied to an object
         /// that's referenced by the objects being filtered. 
         /// 
         /// This line adds a simple condition to the filter:
         
         filter.Predicate.Add(br => br.ScaleFactors.IsProportional());
         
         using(var tr = new DocumentTransaction())
         {
            /// In the transaction-centric programming model, 
            /// instance methods of a transaction do what one 
            /// might otherwise use extension methods targeting 
            /// the Database class for. This works, because the
            /// DatabaseTransaction class encapsulates a Database, 
            /// and DatabaseTransaction is itself a Transaction.
            /// 
            /// Collect the block references satisifying the 
            /// filter criteria:

            var desks = tr.GetModelSpaceObjects<BlockReference>().Where(filter);

            /// Erase the collected block references using the 
            /// Erase() extension method from EntityExtensions.cs:

            int count = desks.UpgradeOpen().Erase();

            tr.Commit();

            tr.Editor.WriteMessage($"\nFound and erased {count} DESK blocks");
         }
      }

      /// <summary>
      /// This example is similar to the above ones, except 
      /// that it explodes the filtered block references.
      /// 
      /// Notice that because the command is registered using
      /// the CommandFlags.Session flag, it will run in the 
      /// application context, and the DocumentTransaction will
      /// implicitly lock and unlock the document.
      /// </summary>

      [CommandMethod("EXPLODEDESKS", CommandFlags.Session)]
      public static void ExplodeDesks()
      {
         // Define a filter that includes only block 
         // references having names starting with 'DESK',
         // using the BlockFilter specialization defined 
         // above:

         var filter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

         /// Next, we add a complex criteria to the block filter, 
         /// that includes only block references on layers having 
         /// names that start with "FURNITURE". 
         /// 
         /// The first delegate argument is passed a BlockReference, 
         /// and must return the ObjectId of the object to which the
         /// second predicate argument is applied to. The generic
         /// argument type specifies the type of the argument to the
         /// second predicate, and is the type of the object used to 
         /// determine if each BlockReference satisfies the filter 
         /// criteria, which in this case is LayerTableRecord.

         /// This adds a child filter requiring that block 
         /// references reside on layers having names that
         /// start with "FURNITURE":
         
         var layerFilter = filter.Add<LayerTableRecord>(
            br => br.LayerId,
            layer => layer.Name.Matches("FURNITURE*"));

         /// Add a second criteria to the child layer filter 
         /// added above, that restricts the resulting set of
         /// block references to only those residing on layers
         /// that are not locked

         filter.Add<LayerTableRecord>(
            bref => bref.LayerId,
            (layer => !layer.IsLocked));

         // The above operation can also be accomplished
         // more easily if the caller knows that a compatible
         // filter already exists in the filter graph, and
         // they have a reference to it:

         // layerFilter.Criteria.Add(layer => !layer.IsLocked);

         /// The layerFilter variable above is a separate filter
         /// that is a child of the BlockFilter created above.
         /// Because the layerFilter is a child of the block filter,
         /// the layerFilter's criteria is incorporated into the
         /// block filter's criteria, and hence, the layerFilter 
         /// isn't used directly in subsequent query/filtering,
         /// only the BlockFilter is needed to perform filtering.

         /// Simple verses complex criteria:
         /// 
         /// Simple criteria are predicates that are applied
         /// directly to the objects being filtered (whose type
         /// is the DBObjectFilter's TFiltered generic argument).
         /// 
         /// Complex criteria are ones having predicates that 
         /// are applied to another object that's referenced by 
         /// the objects being filtered (the TCriteria generic
         /// argument). 
         /// 
         /// The first argument to a DBObjectFilter is a function 
         /// that returns the ObjectId of the referenced object, 
         /// from an object being filtered. The BlockFilter and 
         /// the layer filter that was added to it above are two 
         /// examples of complex criteria.
         /// 
         /// Simple criteria have predicates taking a TFiltered
         /// type as their argument, while complex criteria have 
         /// predicates that take a TCriteria type as arguments.
         /// 
         /// Simple criteria can easily be added to an existing 
         /// DBObjectFilter using it's Predicate property, as 
         /// shown below.
         ///
         /// The next line adds a simple criteria to the block 
         /// filter, that excludes non-uniformly scaled block 
         /// references. This specific criteria is typically-
         /// used when the resulting block references are to be
         /// exploded as this command does.

         filter.Predicate.Add(blkref => blkref.ScaleFactors.IsProportional());

         /// The above call to the Add() method adds the supplied 
         /// predicate to the filter critiera, in a logical 'and' 
         /// operation with the existing criteria. The default 
         /// logical operation is Logical.And, but it can also be 
         /// specified explicitly using an overload of the Add() 
         /// method.

         /// At this point, we can examine the filter and the
         /// predicate expressions that were combined using the
         /// above method calls, using the Dump() method:

         Write(filter.Dump("Root filter"));

         /// Because this command runs in the application context,
         /// the DocumentTransaction will automatically lock and
         /// unlock the document:

         using(var tr = new DocumentTransaction())
         {
            /// Define the filtered sequence of BlockReferences:
            
            var desks = tr.GetCurrentSpaceObjects<BlockReference>(filter);

            /// Explode and erase the block references enumerated by the
            /// above filtered sequence. They consist of all uniformly-
            /// scaled DESK blocks that are on unlocked layers whose names 
            /// start with "FURNITURE". Here we use the ExplodeToOwnerSpace
            /// extension method (which can be found in EntityExtensions.cs), 
            /// which will also erase the original block references:

            int count = 0;
            desks.UpgradeOpen().ExplodeToOwnerSpace(out count, true);

            Write($"Exploded {count} DESK blocks.");

            tr.Commit();
         }
      }

      /// <summary>
      /// A variation of the above method sans commentary, that 
      /// adds the second criteria for the layer filter using the
      /// Criteria property's Add() method, and manually adds the 
      /// explode results to the current space block. The result 
      /// should be identical to the above method.
      /// 
      /// Other differences:
      /// 
      /// This method utilizes a number of extension methods that
      /// are included in this library.
      /// 
      /// The EnsureDispose() method is a helper that will ensure
      /// that all elements in a DBObjectCollection are properly-
      /// disposed when the object returned by EnsureDispose() is
      /// disposed. That will happen even if an exception causes
      /// flow to exit the using() block prematurely before all of
      /// the elements in the DBObjectCollection were operated on.
      /// 
      /// The Append() method of the DatabaseTransaction appends
      /// multiple newly-created entities to the current space 
      /// (or to another specified space). It also automates calls 
      /// to AddNewlyCreatedDBObject() for the caller.
      /// </summary>
      
      [CommandMethod("EXPLODEDESKS2", CommandFlags.Session)]
      public static void ExplodeDesks2()
      {
         var filter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
         filter.Predicate.Add(br => br.ScaleFactors.IsProportional());
         var layerFilter = filter.Add<LayerTableRecord>(br => br.LayerId,
            layer => layer.Name.Matches("FURNITURE*"));
         layerFilter.Criteria.Add(layer => !layer.IsLocked);
         using(var tr = new DocumentTransaction())
         {
            var desks = tr.GetCurrentSpaceObjects<BlockReference>(filter);
            DBObjectCollection fragments = new DBObjectCollection();
            using(fragments.EnsureDispose())
            {
               int count = desks.UpgradeOpen().Explode(fragments, true);
               if(fragments.Count > 0)
               {
                  tr.Append(fragments);
                  Write($"Found and exploded {count} DESK blocks");
               }
            }
            tr.Commit();
         }
      }

      /// <summary>
      /// Further demonstrating the transaction-centric programming
      /// model embraced by this library, this command will count and 
      /// display the number of insertions of every user-defined block 
      /// in the model space of the active document.
      /// 
      /// Two versions of this command are provided. One accesses 
      /// block references through BlockTableRecords, and the other 
      /// directly scans model space. Both should yield identical
      /// results.
      /// 
      /// </summary>

      [CommandMethod("BTCOUNTBLOCKS")]
      public static void CountBlocksByBlockTable()
      {
         using(var tr = new DocumentTransaction())
         {
            var idModel = tr.ModelSpaceBlockId;

            var map = tr.GetNamedObjects<BlockTableRecord>()
               .Where(btr => btr.IsUserBlock())
               .SelectMany(btr => btr.GetBlockReferences(tr))
               .Where(br => br.BlockId == idModel)
               .CountAllBy(br => br.DynamicBlockTableRecord)
               .Select(p => (tr.GetObject<BlockTableRecord>(p.Key).Name, p.Value))
               .OrderBy(p => p.Name);

            Write("\n");

            int total = 0;
            foreach((string name, int count) item in map)
            {
               Write("{0,-12} {1,4}", item.name, item.count);
               total += item.count;
            }
            Write("-------------------------\n{0,-16} {1,4}", "Total", total);

            Application.DisplayTextScreen = true;

            tr.Commit();
         }
      }

      [CommandMethod("MSCOUNTBLOCKS")]
      public static void CountBlocksByModelSpace()
      {
         using(var tr = new DocumentTransaction())
         {
            // Define a BlockFilter that includes 
            // only user-defined blocks:

            var filter = new BlockFilter(btr => btr.IsUserBlock());

            var map = tr.GetModelSpaceObjects<BlockReference>()
               .Where(filter)
               .CountAllBy(br => br.DynamicBlockTableRecord)
               .Select(p => (tr.GetObject<BlockTableRecord>(p.Key).Name, p.Value))
               .OrderBy(p => p.Name);

            Write("\n");

            int total = 0;
            foreach((string name, int count) tuple in map)
            {
               Write("{0,-12} {1,4}", tuple.name, tuple.count);
               total += tuple.count;
            }
            Write("-------------------------\n{0,-16} {1,4}", "Total", total);

            Application.DisplayTextScreen = true;

            tr.Commit();
         }
      }

      /// <summary>
      /// An example showing the use of the GetNamedObject<>() method:
      /// 
      /// This example uses GetNamedObject() to open a Group named 
      /// "Group1"; a LayerTableRecord having the name "PHONES"; a
      /// DBVisualStyle named "Conceptual"; and an MLineStyle named
      /// "Standard". 
      /// 
      /// Note that GetNamedObject() returns items from SymbolTables
      /// as well as built-in DBDictionaries.
      /// </summary>

      [CommandMethod("GETNAMEDOBJECTEXAMPLE")]
      public static void GetNamedObjectExample()
      {
         using(var tr = new DocumentTransaction())
         {
            var group1 = tr.GetNamedObject<Group>("Group1");
            tr.Editor.WriteMessage($"\nGroup: {group1.ToString()}");

            var layer = tr.GetNamedObject<LayerTableRecord>("PHONES");
            tr.Editor.WriteMessage($"\nLayer: {layer.ToString()}");

            var visualStyle = tr.GetNamedObject<DBVisualStyle>("Conceptual");
            tr.Editor.WriteMessage($"\nVisual style: {visualStyle}");

            var mlStyle = tr.GetNamedObject<MlineStyle>("Standard");
            tr.Editor.WriteMessage($"\nMLineStyle: {mlStyle}");

            tr.Commit();


         }
      }

      [CommandMethod("GETNAMEDOBJECSEXAMPLE")]
      public static void GetNamedObjectsExample()
      {
         using(var tr = new DocumentTransaction())
         {
            // Display the names of all layers in the active document:

            Write("Layers:\n");
            foreach(LayerTableRecord ltr in tr.GetNamedObjects<LayerTableRecord>())
            {
               Write($"  {ltr.Name}");
            }

            // Display the names of all Visual styles defined in the active document:

            Write("\nVisual Styles:\n");
            foreach(DBVisualStyle vs in tr.GetNamedObjects<DBVisualStyle>())
            {
               Write($"  {vs.Name}");
            }

            // Display the names of all Layouts defined in the active document:

            Write("\nLayouts:\n");
            foreach(Layout layout in tr.GetNamedObjects<Layout>())
            {
               Write($"  {layout.LayoutName}");
            }

            tr.Commit();
         }
      }

      static void Write(string fmt, params object[] args)
      {
         Application.DocumentManager.MdiActiveDocument?.
            Editor.WriteMessage("\n" + fmt, args);
      }

   }
}



