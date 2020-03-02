
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;


namespace Notification
{
   public class Notification : IDisposable
   {
      private readonly Bitmap icon;
      private readonly string title;
      private readonly string message;
      private readonly string channelID;
      private bool visible;
      private readonly bool deletable;
      public event PropertyChangedEventHandler NotificationDeleted;
      public event PropertyChangedEventHandler OnNotificationVisibleChanged;
      public bool deleted = false;
      private Notification(Optional<Bitmap> icon, Optional<string> title, Optional<string> message, Optional<string> channelID, Optional<bool> visible, Optional<bool> deletable)
      {
         this.title = title.OrElse(string.Empty);
         this.message = message.OrElse(string.Empty);
         this.icon = icon.OrElse(null);
         this.channelID = channelID.OrElse(string.Empty);
         this.visible = visible.OrElse(true);
         this.deletable = deletable.OrElse(true);
      }

      public Bitmap Icon
      {
         get => icon;
      }

      public string Title
      {
         get => title;
      }

      public string Message
      {
         get => message;
      }

      public string ChannelID
      {
         get => channelID;
      }

      public bool Visible
      {
         get => visible;
         set {
            if (visible != value)
            {
               visible = value;
               NotifyNotificationVisibleChanged(nameof(Visible));
            }
         } 
      }

      public bool Deletable
      {
         get => deletable;
      }

      public bool Deleted
      {
         get => deleted;
      }

      public void Delete()
      {
         if (Deletable)
         {
            deleted = true;
            NotifyNotificationDeleted(nameof(Deleted));
         }
      }

      private void NotifyNotificationDeleted(string info)
      {
         NotificationDeleted?.Invoke(this, new PropertyChangedEventArgs(info));
      }

      private void NotifyNotificationVisibleChanged(string info)
      {
         OnNotificationVisibleChanged?.Invoke(this, new PropertyChangedEventArgs(info));
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
            icon?.Dispose();
         }
      }

      public override string ToString()
      {
         return "Channel ID : " + channelID + " Title : " + title + " Message : " + message;
      }

      // override object.Equals
      public override bool Equals(object obj)
      {
         if (obj == null || GetType() != obj.GetType())
         {
            return false;
         }

         Notification notif = (Notification)obj;

         return title == notif.Title && message == notif.message && icon == notif.icon && channelID == notif.channelID;
      }

      public override int GetHashCode()
      {
         var hashCode = 723747906;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(title);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(message);
         hashCode = hashCode * -1521134295 + icon.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(channelID);
         return hashCode;
      }

      public class NotificationBuilder
      {
         private Optional<Bitmap> icon = Optional<Bitmap>.Empty();
         private Optional<string> title = Optional<string>.Empty();
         private Optional<string> message = Optional<string>.Empty();
         private Optional<string> channelID = Optional<string>.Empty();
         private Optional<bool> visible = Optional<bool>.Empty();
         private Optional<bool> deletable = Optional<bool>.Empty();

         public NotificationBuilder()
         {

         }

         public NotificationBuilder SetIcon(Bitmap icon)
         {
            this.icon = Optional<Bitmap>.Of(icon);
            return this;
         }

         public NotificationBuilder SetTitle(string title)
         {
            this.title = Optional<string>.Of(title);
            return this;
         }

         public NotificationBuilder SetMessage(string message)
         {
            this.message = Optional<string>.Of(message);
            return this;
         }

         public NotificationBuilder SetChannelID(string channelID)
         {
            if (string.IsNullOrEmpty(channelID))
            {
               throw new ArgumentException("channelID should not be null or empty.");
            }
            this.channelID = Optional<string>.Of(channelID);
            return this;
         }

         public NotificationBuilder SetVisible(bool visible)
         {
            this.visible = Optional<bool>.Of(visible);
            return this;
         }

         public NotificationBuilder SetDeletable(bool deletable)
         {
            this.deletable = Optional<bool>.Of(deletable);
            return this;
         }

         public Notification Build()
         {
            return new Notification(icon, title, message, channelID, visible, deletable);
         }
      }



   }

   public class NotificationEventArgs : EventArgs
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="NotificationEventArgs"/> class.
      /// </summary>
      /// <param name="notification">The notification.</param>
      public NotificationEventArgs(Notification notification)
      {
         Notification = notification;
      }

      /// <summary>
      /// The new state.
      /// </summary>
      public Notification Notification
      {
         get;
      }
   }
}
