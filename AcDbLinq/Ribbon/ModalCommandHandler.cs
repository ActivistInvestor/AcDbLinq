
/// ModalCommandHandler.cs
/// 
/// ActivistInvestor / Tony T
/// 
/// Distributed under the terms of the MIT license


using System;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Extensions;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Ribbon.Extensions;

#pragma warning disable CS0612 // Type or member is obsolete

namespace Autodesk.Windows.Extensions
{
   public abstract class ModalCommandHandler : ICommand
   {
      public event EventHandler CanExecuteChanged;

      public ModalCommandHandler()
      {
         RibbonEventManager.QueryCanExecute = true;
         IsModal = true;
      }

      /// <summary>
      /// Indicates if the RibbonCommandItem associated
      /// with the instance should be disabled when there
      /// is an active command.
      /// </summary>
      public virtual bool IsModal { get; set; }   

      /// <summary>
      /// If IsModal is true, this enables the command only 
      /// when there is an active document that is quiescent:
      /// </summary>
      
      public virtual bool CanExecute(object parameter)
      {
         return IsModal ? RibbonEventManager.IsQuiescentDocument : true;
      }

      public abstract void Execute(object parameter);

      protected static Editor Editor =>
         Application.DocumentManager.MdiActiveDocument?.Editor;
   }

}