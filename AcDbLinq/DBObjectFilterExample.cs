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
using Autodesk.AutoCAD.ApplicationServices.Extensions;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
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

         // We'll use a DocTransaction to simplify the operation.
         // The default constructor uses the active document:

         using(var tr = new DocTransaction())
         {
            // Rather than having to write dozens of lines of
            // code, using the BlockFilter and a helper method
            // from this library, in ONE LINE OF CODE, we can
            // collect all block references in model space whose
            // block name starts with "DESK":

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
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(var tr = new DocTransaction())
         {
            var deskFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
            var desks = tr.GetModelSpaceObjects<BlockReference>().Where(deskFilter);

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

         /// A DocTransaction combines the functionality of
         /// a transaction and the broad range of operations
         /// provided by the extension methods of the Database 
         /// class that are included in this library. 
         /// 
         /// All extension methods that target the Database
         /// class are also instance members of DocTransaction.

         using(var tr = new DocTransaction())
         {

            // Define a filter that collects all insertions of blocks
            // having names that start with "DESK":

            var blockFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));

            /// Define a filter that excludes block references on 
            /// locked layers:

            var layerFilter = new DBObjectFilter<BlockReference, LayerTableRecord>(
               btr => btr.LayerId, layer => !layer.IsLocked);

            // Use the And() method to add the layerFilter's
            // critieria to the block filter:

            var desks = tr.GetModelSpaceObjects<BlockReference>()
               .Where(blockFilter.And(layerFilter));

            // Note that the above use of the And() method to combine
            // the criteria of both filters is equivalent to this:

            desks = tr.GetModelSpaceObjects<BlockReference>()
               .Where(br => blockFilter.IsMatch(br) && layerFilter.IsMatch(br));

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
      /// with "DESK" in model space that reside on the layer 
      /// "FURNITURE", and are not on a locked layer:
      /// 
      /// This example shows how to add an additional condition 
      /// to the query criteria used to qualify objects to be
      /// erased (that they must reside on the layer "FURNITURE"
      /// in addition to the layer not being locked).
      /// 
      /// The example also uses a specialization of DBObjectFilter
      /// to filter entities based on properties of the layer they 
      /// reside on.
      /// </summary>

      [CommandMethod("ERASEDESKS2")]
      public static void EraseDesks2()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         using(var tr = new DocTransaction())
         {
            /// Define a filter that includes only block 
            /// references having names starting with 'DESK'.
            /// This filter is an instance of the BlockFilter
            /// specialization defined above:
            
            var blockFilter = new BlockFilter(btr => btr.Name.Matches("DESK*"));
            
            /// Define a filter that excludes block references 
            /// on locked layers, this time using the LayerFilter
            /// specialization:
            
            var layerFilter = new LayerFilter<BlockReference>(layer => !layer.IsLocked);

            /// Add an additional condition to the LayerFilter
            /// predicate that's applied to each LayerTableRecord, 
            /// requiring that the layer's name starts with "FURNITURE".
            /// 
            /// The net sum effect is that only BlockReferences residing 
            /// on an unlocked layer whose name starts with "FURNITURE"
            /// will be included.

            layerFilter.SourcePredicate.And(layer => layer.Name.Matches("FURNITURE*"));

            /// Add an additional condition to the blockFilter
            /// that excludes block references that are non-
            /// uniformly scaled. 
            /// 
            /// This criteria is applied to each BlockReference, while 
            /// the predicate supplied to the constructor is applied to 
            /// each BlockTableRecord. The DBObjectFilter class allows
            /// one to specify per-instance and per-reference criteria.

            blockFilter.And(br => br.BlockTransform.IsUniscaledOrtho());

            // Logically-join the blockFilter and the
            // layerFilter using the And() method:
            //
            // This causes the blockFilter to incorporate
            // its criteria with the layerFilter's criteria:

            blockFilter.And(layerFilter);

            // At this point, the layerFilter is obsolete, and the
            // blockFilter represents the logical union of itself
            // and the layerFilter, so the blockFilter must be used
            // to perform the query. With the two filters unioned
            // into a single filter, each BlockReference must satisfy
            // the criteria of both filters in order to be included
            // in the result.
            //
            // This example attempts to demonstrate the 'composablity'
            // provided by the DBObjectFilter class, allowing runtime
            // conditions to be used to determine what critiera is used
            // to filter objects.
            
            var desks = tr.GetModelSpaceObjects<BlockReference>().Where(blockFilter);

            /// Erase all uniformly-scaled DESK blocks that are on 
            /// a locked layer whose name starts with "FURNITURE",
            /// this time using the Erase() extension method from 
            /// EntityExtensions.cs:
            
            int count = desks.UpgradeOpen().Erase();

            tr.Commit();

            doc.Editor.WriteMessage($"Found and erased {count} DESK blocks");
         }
      }

      /// <summary>
      /// Example showing the use of the GetNamedObject<>() method:
      /// </summary>
      [CommandMethod("GETNAMEDOBJ")]
      public static void TestGetNamedObjects()
      {
         using(var tr = new DocTransaction())
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            var group1 = tr.GetNamedObject<Group>("Group1");
            doc.Editor.WriteMessage($"\n{group1.ToString()}");
            var layer = tr.GetNamedObject<LayerTableRecord>("PHONES");
            doc.Editor.WriteMessage($"\n{layer.ToString()}");

            tr.Commit();

         }
      }
   }

}



