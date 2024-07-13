/// RibbonEventManager.cs
/// 
/// ActivistInvestor / Tony T
/// 
/// Distributed under the terms of the MIT license
/// 

using System;
using System.ComponentModel;
using System.Extensions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System.Diagnostics.Extensions;

namespace Autodesk.AutoCAD.EditorInput.Extensions
{

   [DefaultBindingProperty(nameof(IsQuiescentDocument))]
   public class EditorStateView : IDisposable, INotifyPropertyChanged
   {
      static DocumentCollection docs = Application.DocumentManager;
      static EditorStateView instance;
      private bool disposed;
      bool observing = false;
      static Cached<bool> quiescent = new Cached<bool>(GetIsQuiescent);

      EditorStateView()
      {
         SetIsObserving(true);
      }

      event PropertyChangedEventHandler propertyChanged = null;

      public event PropertyChangedEventHandler PropertyChanged
      {
         add
         {
            propertyChanged += value;
         }
         remove
         {
            propertyChanged -= value;
         }
      }

      public static EditorStateView Instance
      {
         get
         {
            if(instance == null)
               instance = new EditorStateView();
            return instance;
         }
      }

      void SetIsObserving(bool value)
      {
         if(value ^ observing && !isQuitting)
         {
            observing = value;
            if(value)
            {
               docs.DocumentLockModeChanged += documentLockModeChanged;
               docs.DocumentActivated += documentActivated;
               docs.DocumentDestroyed += documentDestroyed;
               Application.QuitWillStart += quit;
            }
            else
            {
               docs.DocumentLockModeChanged -= documentLockModeChanged;
               docs.DocumentActivated -= documentActivated;
               docs.DocumentDestroyed -= documentDestroyed;
               Application.QuitWillStart -= quit;
            }
            quiescent.Invalidate();
         }
      }

      void InvalidateQuiescentState()
      {
         quiescent.Invalidate();
         NotifyIsQuiescentDocumentChanged();
      }

      void NotifyIsQuiescentDocumentChanged()
      {
         propertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(nameof(IsQuiescentDocument)));
      }

      /// <summary>
      /// Note: Returns false if there is no active document
      /// </summary>

      public bool IsQuiescentDocument
      {
         get
         {
            return quiescent.Value; 
         }
      }

      static bool GetIsQuiescent()
      {
         Document doc = docs.MdiActiveDocument;
         if(doc != null)
         {
            return doc.Editor.IsQuiescent
               && !doc.Editor.IsDragging
               && (doc.LockMode() & DocumentLockMode.NotLocked)
                   == DocumentLockMode.NotLocked;
         }
         return false;
      }


      /// <summary>
      /// Handlers of driving events:
      /// 
      /// These events signal that the effective-quiescent state
      /// may have changed. 
      /// 
      /// They are only used when the PropertyChanged event 
      /// is being observed.
      /// </summary>

      void documentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
      {
         if(e.Document == docs.MdiActiveDocument && !e.GlobalCommandName.ToUpper().Contains("ACAD_DYNDIM"))
            InvalidateQuiescentState();
      }

      void documentActivated(object sender, DocumentCollectionEventArgs e)
      {
         InvalidateQuiescentState();
      }

      void documentDestroyed(object sender, DocumentDestroyedEventArgs e)
      {
         InvalidateQuiescentState();
      }

      void quit(object sender, EventArgs e)
      {
         try
         {
            isQuitting = true;
            SetIsObserving(false);
         }
         catch
         {
         }
      }

      bool isQuitting = false;

      public bool IsQuitting => isQuitting;

      public void Dispose()
      {
         if(!disposed)
         {
            this.disposed = true;
            if(!isQuitting)
            {
               SetIsObserving(false);
            }
         }
         GC.SuppressFinalize(this);
      }

      //struct Cached<T>
      //{
      //   bool dirty = true;
      //   Func<T> update;
      //   T value;

      //   public Cached(Func<T> update)
      //   {
      //      Assert.IsNotNull(update, nameof(update));
      //      this.update = update;
      //      Invalidate();
      //   }

      //   public Cached(T initialValue, Func<T> update)
      //   {
      //      Assert.IsNotNull(update, nameof(update));
      //      this.update = update;
      //      this.value = initialValue;
      //      this.dirty = false;
      //   }

      //   public void Invalidate()
      //   {
      //      this.dirty = true;
      //   }

      //   public T Value
      //   {
      //      get
      //      {
      //         if(dirty)
      //         {
      //            value = update();
      //            dirty = false;
      //         }
      //         return value;
      //      }
      //   }

      //   public bool IsValid => !dirty;

      //   public static implicit operator T(Cached<T> value) => value.Value;
      //}

   }
}

