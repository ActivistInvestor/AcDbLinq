/// AcConsole.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Supporting APIs for the AcDbLinq library.

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

/// A class that implements a proxy for diagnostic 
/// trace output, that can be used in-place of other
/// trace diagnostic functionlity that may not be
/// included with dependent code.
/// 
/// AcConsole simply routes trace output to the
/// AutoCAD console in lieu of other trace output 
/// functionality not included in this library.

namespace Autodesk.AutoCAD.Runtime
{
   public static class AcConsole
   {
      public static void Write(string fmt, params object[] args)
      {
         Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
            $"{fmt}\n", args);
      }

      public static void WriteLine(string fmt, params object[] args)
      {
         Write("\n" + fmt, args);
      }

      public static void TraceThread()
      {
         Write($"\nCurrent Thread = {Thread.CurrentThread.ManagedThreadId}");
      }

      public static void TraceModule(Type type = null)
      {
         Write($"\nUsing {(type ?? typeof(AcConsole)).Assembly.Location}");
      }

      public static void TraceContext()
      {
         var appctx = Application.DocumentManager.IsApplicationContext ?
            "Application" : "Document";
         Write($"\nContext: {appctx}");
      }

      public static void StackTrace()
      {
         Write(new StackTrace(1).ToString());
      }

      public static void TraceProperties(object target, string delimiter = "\n")
      {
         WriteLine(GetProperties(target, delimiter));
      }

      public static string GetProperties(object target, string delimiter = "\n")
      {
         StringBuilder sb = new StringBuilder();
         string targetstr = target?.ToString() ?? "(null)";
         sb.Append($"====[{targetstr}]====\n");
         if(target != null)
         {
            foreach(PropertyDescriptor prop in TypeDescriptor.GetProperties(target))
            {
               sb.Append(string.Format($"  {prop.Name} = {GetValue(target, prop)}{delimiter}"));
            }
         }
         return sb.ToString();
      }

      static string GetValue(object target, PropertyDescriptor pd)
      {
         object obj = null;
         try
         {
            obj = pd.GetValue(target);
            return obj != null ? obj.ToString() : "(null)";
         }
         catch(System.Exception ex)
         {
            return ex.Message;
         }
         finally
         {
            if(obj != null && Marshal.IsComObject(obj))
               Marshal.FinalReleaseComObject(obj);
         }
      }



   }

}
