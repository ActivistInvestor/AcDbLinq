using Autodesk.AutoCAD.Runtime.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{

   /// <summary>
   /// This is merely an example showing how to write 
   /// 'Linq-friendly' extension methods to perform 
   /// common operations on multiple entities expressed 
   /// as an IEnumerable<T>.
   /// </summary>

   public static partial class EntityExtensions
   {
      /// <summary>
      /// Erases all entities in the sequence. 
      /// They should be open for write.
      /// </summary>

      public static int Erase(this IEnumerable<Entity> entities, bool erase = true)
      {
         Assert.IsNotNull(entities, nameof(entities));
         int count = 0;
         foreach(Entity entity in entities)
         {
            entity.Erase(erase);
            count++;
         }
         return count;
      }
   }
}
