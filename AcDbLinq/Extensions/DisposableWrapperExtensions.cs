/// DisposableWrapperExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.


using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.Diagnostics;

namespace Autodesk.AutoCAD.Runtime.Extensions
{
   public static class DisposableWrapperExtensions
   {
      /// <summary>
      /// Replaces one DisposableWrapper with another.
      /// 
      /// After replacement, the <paramref name="replacement"/> argument
      /// becomes the managed wrapper for the <paramref name="wrapper"/>'s
      /// UnmanagedObject, and all interaction with the native object must
      /// be through the replacment. The <paramref name="wrapper"/> argument
      /// is no-longer usable or valid after this method returns.
      /// </summary>
      /// <param name="wrapper">The DisposableWrapper that is to be replaced</param>
      /// <param name="replacement">The DisposableWrapper that is to replace the 
      /// <paramref name="wrapper"/> argument</param>
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
         Interop.SetAutoDelete(wrapper, false);
         wrapper.Dispose();
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




