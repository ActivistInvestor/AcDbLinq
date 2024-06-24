/// DBObjectFilterExample.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Example code showing how to use/extend the
/// DBObjectFilter class.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   /// DBObjectFilter is not only applicable to entities and
   /// LayerTableRecords. The generic arguments are what allow
   /// it to be used for many similar use cases as well.
   /// 
   /// The following example filters BlockReferences by their
   /// "effective name". The filter resolves anonymous dynamic 
   /// blocks to their dynamic block definition, allowing its 
   /// name to be filtered against for both references to the
   /// original dynamic block, and references to any anonymous
   /// 'variations' of it. This example will include references
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
   /// A version that resolves dynamic block references to 
   /// anonymous blocks:
   ///

   public class StaticBlockFilter : DBObjectFilter<BlockReference, BlockTableRecord>
   {
      public StaticBlockFilter(Expression<Func<BlockTableRecord, bool>> predicate)
         : base(blockref => blockref.BlockTableRecord, predicate)
      {
      }
   }

   /// And a second variant that resolves dynamic references
   /// to the dynamic block definition:
   /// 

   public class BlockFilter : DBObjectFilter<BlockReference, BlockTableRecord>
   {
      public BlockFilter(Expression<Func<BlockTableRecord, bool>> predicate)
        : base(blockref => blockref.DynamicBlockTableRecord, predicate)
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

   public static class DynamicBlockFilterExample
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

         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(Transaction tr = new OpenCloseTransaction())
         {
            // Rather than having to write dozens of lines of
            // code, using the BlockFilter and a helper method
            // from this library, in ONE LINE OF CODE, we can
            // collect all block references in model space whose
            // block name starts with "DESK":

            var desks = db.GetModelSpaceObjects<BlockReference>(tr).Where(deskFilter);

            // Get the ObjectIds of the resulting block references:

            var ids = desks.Select(br => br.ObjectId).ToArray();
            doc.Editor.WriteMessage($"\nFound {ids.Length} DESK blocks.");

            // Select the resulting block references:
            if(ids.Length > 0)
               doc.Editor.SetImpliedSelection(ids);
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
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(Transaction tr = doc.TransactionManager.StartTransaction())
         {
            var deskFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
            var desks = db.GetModelSpaceObjects<BlockReference>(tr).Where(deskFilter);

            int cnt = 0;
            foreach(BlockReference blockref in desks.UpgradeOpen())
            {
               blockref.Erase();
               ++cnt;
            }
            tr.Commit();

            doc.Editor.WriteMessage($"Found and erased {cnt} DESK blocks");
         }
      }

      /// <summary>
      /// The following variations of the above two commands introduce 
      /// a second filter that excludes all block references on locked
      /// layers. The two filters are joined in a logical 'and' operation 
      /// allowing them to work as a single, complex filter.
      /// </summary>

      [CommandMethod("SELECTDESKS2")]
      public static void SelectDesks2()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(Transaction tr = doc.TransactionManager.StartTransaction())
         {

            // Define a filter that collects all insertions of blocks
            // having names that start with "DESK":

            var blockFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

            /// Define a filter that excludes block references on 
            /// locked layers:

            var layerFilter = new DBObjectFilter<BlockReference, LayerTableRecord>(
               btr => btr.LayerId, layer => !layer.IsLocked);

            // Logically-join the two filters using the And() method:
            
            var desks = db.GetModelSpaceObjects<BlockReference>(tr)
               .Where(blockFilter.And(layerFilter));

            // Get the ObjectIds of the resulting block references:
            
            var ids = desks.Select(br => br.ObjectId).ToArray();
            doc.Editor.WriteMessage($"\nFound {ids.Length} DESK blocks.");

            // Select the resulting block references:
            if(ids.Length > 0)
               doc.Editor.SetImpliedSelection(ids);
            tr.Commit();
         }
      }

      /// <summary>
      /// Erases all insertions of blocks having names starting
      /// with "DESK" in model space, that are not on a locked
      /// layer.
      /// </summary>

      [CommandMethod("ERASEDESKS2")]
      public static void EraseDesks2()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(Transaction tr = doc.TransactionManager.StartTransaction())
         {
            /// Define a filter that includes only block references
            /// having names starting with 'DESK':
            
            var blockFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
            
            /// Define a filter that excludes block references on 
            /// locked layers:
            
            var layerFilter = new DBObjectFilter<BlockReference, LayerTableRecord>(
               btr => btr.LayerId, layer => !layer.IsLocked);

            /// Add an additional condition to the LayerFilter
            /// predicate that's applied to each LayerTableRecord, 
            /// requiring the layer's name to start with "FURNITURE":

            layerFilter.Expression.And(layer => layer.Name.Matches("FURNITURE*"));

            // Logically-join the blockFilter and
            // layerFilter using the And() method:

            var filter = blockFilter.And(layerFilter);

            // At this point, the blockFilter and layerFilter are
            // both obsolete, and the result of And() (assigned to
            // the 'filter' variable above) represents the logical
            // union of the two filters. Hence, each BlockReference
            // must now satisfy the conditions of both filters in
            // order to be included in the result.
            
            var desks = db.GetModelSpaceObjects<BlockReference>(tr).Where(filter);

            /// Erase all DOOR blocks that are not on a locked layer,
            /// this time using the Erase() extension method from 
            /// EntityExtensions.cs:
            
            int count = desks.UpgradeOpen().Erase();

            tr.Commit();

            doc.Editor.WriteMessage($"Found and erased {count} DESK blocks");
         }
      }

      [CommandMethod("GETOBJ")]
      public static void GetNamedObjects()
      {
         using(var tr = new OpenCloseTransaction())
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            var group1 = db.GetNamedObject<Group>("Group1", tr);
            doc.Editor.WriteMessage($"\n{group1.ToString()}");
            var layer = db.GetNamedObject<LayerTableRecord>("PHONES", tr);
            doc.Editor.WriteMessage($"\n{layer.ToString()}");

            tr.Commit();

         }
      }

   }

}



