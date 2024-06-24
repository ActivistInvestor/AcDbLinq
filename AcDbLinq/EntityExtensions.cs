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
   /// common operations on multiple entities that take
   /// form of an IEnumerable<DBObject>.
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
   }
}
