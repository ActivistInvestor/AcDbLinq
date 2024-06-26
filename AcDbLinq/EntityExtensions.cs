/// EntityExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Extension methods targeting the Entity class.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
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
            Assert.IsNotNullOrDisposed(obj, nameof(obj));
            obj.Erase(erase);
            count++;
         }
         return count;
      }

      /// <summary>
      /// Explode all entities in the sequence and collect
      /// the ObjectIds of the resulting objects.
      /// </summary>
      /// <param name="entities"></param>
      /// <returns></returns>

      public static DBObjectCollection Explode(this IEnumerable<Entity> entities)
      {
         Assert.IsNotNull(entities, nameof(entities));
         DBObjectCollection result = new DBObjectCollection();
         foreach(var entity in entities)
         {
            entity.Explode(result);
         }
         return result;
      }

      /// <summary>
      /// Get the combined geometric extents of a sequence of entities:
      /// </summary>
      /// <param name="entities"></param>
      /// <returns></returns>

      public static Extents3d GeometricExtents(this IEnumerable<Entity> entities)
      {
         Assert.IsNotNull(entities, nameof(entities));
         Extents3d extents = new Extents3d(Point3d.Origin, Point3d.Origin);
         foreach(var entity in entities)
         {
            extents.AddExtents(entity.GeometricExtents);
         }
         return extents;
      }
   }
}
