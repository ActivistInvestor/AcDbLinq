/// DBObjectFilterExample.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.


using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.Diagnostics;

/// Alternate pattern that uses a custom Transaction
/// as the invocation target for Database extension 
/// methods provided by methods of this library.

namespace Autodesk.AutoCAD.DatabaseServices.Extensions
{
   public static class DisposableWrapperExtensions
   {
      /// <summary>
      /// Replaces one DisposableWrapper with another.
      /// </summary>
      /// <param name="wrapper"></param>
      /// <param name="replacement"></param>
      /// <exception cref="InvalidOperationException"></exception>
      public static void ReplaceWith(this DisposableWrapper wrapper, DisposableWrapper replacement)
      {
         Assert.IsNotNullOrDisposed(wrapper, nameof(wrapper));
         Assert.IsNotNullOrDisposed(replacement, nameof(replacement));
         if(replacement.UnmanagedObject.ToInt64() > 0)
            throw new InvalidOperationException("Invalid replacmement");
         bool autoDelete = wrapper.AutoDelete;
         IntPtr ptr = wrapper.UnmanagedObject;
         if(ptr.ToInt64() < 1)
            throw new InvalidOperationException("Invalid wrapper");
         Interop.DetachUnmanagedObject(wrapper);
         Interop.DetachUnmanagedObject(replacement);
         Interop.AttachUnmanagedObject(replacement, ptr, autoDelete);
      }

      public static void TryDispose(this DisposableWrapper wrapper)
      {
         if(wrapper != null && !wrapper.IsDisposed)
            wrapper.Dispose();
      }

   }
}




