/// EntityExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Extension methods targeting the Entity class.

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime.Diagnostics;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{

   /// <summary>
   /// This class is merely an example showing how to write 
   /// 'Linq-compatible' extension methods to perform common 
   /// operations on multiple entities that are enumerated by 
   /// IEnumerable<Entity>.
   /// </summary>

   public static partial class EntityExtensions
   {
      /// <summary>
      /// Erases all entities in the sequence. 
      /// They should be open for write. 
      /// </summary>

      public static int Erase(this IEnumerable<DBObject> objects, bool erase = true)
      {
         Assert.IsNotNull(objects, nameof(objects));
         int count = 0;
         foreach(DBObject obj in objects)
         {
            obj.Erase(erase);
            count++;
         }
         return count;
      }

      /// <summary>
      /// Explodes each block reference in the input sequence
      /// to its owner space, and optionally collects and returns 
      /// the ObjectIds of all objects created by the operation.
      /// </summary>
      /// <param name="entities">A sequence of BlockReferences</param>
      /// <param name="erase">A value indicating if the source
      /// objects are to be erased.</param>
      /// <param name="collect">A value indicating if newly-
      /// created objects resulting from exploding the source
      /// collection are to be collected and returned.</param>
      /// <returns>A DBObjectCollection containing the ObjectIds
      /// of all objects created by the operation</returns>

      public static ObjectIdCollection ExplodeToOwnerSpace(
         this IEnumerable<BlockReference> entities, 
         out int count,
         bool erase = false,
         bool collect = false)
      {
         Assert.IsNotNull(entities, nameof(entities));
         Database db = null;
         ObjectIdCollection ids = new ObjectIdCollection();
         count = 0;
         if(entities.Any())
         {
            if(collect)
            {
               db = entities.TryGetDatabase(true);
               db.ObjectAppended += objectAppended;
            }
            try
            {
               int cnt = 0;
               foreach(BlockReference br in entities)
               {
                  br.ExplodeToOwnerSpace();
                  ++cnt;
                  if(erase && br.IsWriteEnabled)
                     br.Erase(true);
               }
               count = cnt;
            }
            finally
            {
               if(collect)
               {
                  db.ObjectAppended -= objectAppended;
               }
            }
         }
         return ids;

         void objectAppended(object sender, ObjectEventArgs e)
         {
            ids.Add(e.DBObject.ObjectId);
         }
      }

      /// <summary>
      /// An overload of ExplodeToOwnerSpace() that doesn't
      /// report the number of objects exploded.
      /// </summary>

      public static ObjectIdCollection ExplodeToOwnerSpace(
         this IEnumerable<BlockReference> entities,
         bool erase = false,
         bool collect = false)
      {
         int count = 0;
         return ExplodeToOwnerSpace(entities, out count, erase, collect);
      }


      /// <summary>
      /// Explode all entities in the sequence and collect
      /// the ObjectIds of the resulting objects.
      /// </summary>
      /// <param name="entities"></param>
      /// <param name="erase">A value indicating if the
      /// object to be exploded should be erased</param>
      /// <returns>A DBObjectCollection containing the
      /// entities produced by exploding the input</returns>

      public static DBObjectCollection Explode(this IEnumerable<Entity> entities, bool erase = false)
      {
         DBObjectCollection result = new DBObjectCollection();
         Explode(entities, result, erase);
         return result;
      }

      /// <summary>
      /// Explodes all entities in the sequence and adds
      /// the resulting objects to the DBObjectCollection
      /// argument.
      /// </summary>
      /// <param name="entities">The input sequence of
      /// entities to be exploded.</param>
      /// <param name="output">A DBObjectCollection to
      /// which all objects created by exploding the input
      /// sequence are added.</param>
      /// <param name="erase">A value indicating if the
      /// entities to be exploded should be erased. The
      /// entities are erased only if they are currently 
      /// open for write.</param>

      public static int Explode(this IEnumerable<Entity> entities, 
         DBObjectCollection output, bool erase = true)
      {
         Assert.IsNotNull(entities, nameof(entities));
         Assert.IsNotNull(output, nameof(output));
         int count = 0;
         foreach(var entity in entities)
         {
            entity.Explode(output);
            ++count;
            if(erase && entity.IsWriteEnabled)
               entity.Erase(true);
         }
         return count;
      }

      /// <summary>
      /// Attempts to explode all entities in the sequence and 
      /// adds the resulting objects to the DBObjectCollection
      /// passed as the output argument.
      /// 
      /// </summary>
      /// <param name="entities">The input sequence of
      /// entities to be exploded.</param>
      /// <param name="output">A DBObjectCollection to
      /// which all objects created by exploding the input
      /// sequence are added.</param>
      /// <param name="erase">A value indicating if the
      /// entities to be exploded should be erased. The
      /// entities are exploded only if they are currently 
      /// open for write.</param>
      /// <returns>The number of entities that were not
      /// exploded.</returns>

      public static int TryExplode(this IEnumerable<Entity> entities,
         DBObjectCollection output, bool erase = true)
      {
         Assert.IsNotNull(entities, nameof(entities));
         Assert.IsNotNull(output, nameof(output));
         int cnt = 0;
         foreach(var entity in entities)
         {
            try
            {
               entity.Explode(output);
            }
            catch(Autodesk.AutoCAD.Runtime.Exception)
            {
               ++cnt;
               continue;
            }
            if(erase && entity.IsWriteEnabled)
               entity.Erase(true);
         }
         return cnt;
      }


      /// <summary>
      /// Get the combined geometric extents of a sequence 
      /// of entities:
      /// </summary>
      /// <param name="entities"></param>
      /// <returns></returns>

      public static Extents3d GeometricExtents(this IEnumerable<Entity> entities)
      {
         Assert.IsNotNull(entities, nameof(entities));
         if(entities.Any())
         {
            Extents3d extents = entities.First().GeometricExtents;
            foreach(var entity in entities.Skip(1))
               extents.AddExtents(entity.GeometricExtents);
            return extents;
         }
         return new Extents3d();
      }
   }
}
