using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Notification
{
   sealed public class NotificationsService : INotifyPropertyChanged
   {
      private readonly static NotificationsService _instance;
      private readonly HashSet<NotificationManager> notificationManagers;
      private readonly static object monitor = new object();
      private NotificationManager _systemNotifManager;
      private int count;
      public event PropertyChangedEventHandler PropertyChanged;
      private readonly object countMonitor = new object();

      private readonly NotificationChannel notificationChannelSystem;
      private const string SYSTEM = "System";

      private readonly IDictionary<SystemNotificationID, Notification> dictSystemNotif;

      static NotificationsService()
      {
            _instance = new NotificationsService();
            _instance._systemNotifManager = new NotificationManager(typeof(NotificationsService));
            _instance.notificationManagers.Add(_instance._systemNotifManager);
            _instance._systemNotifManager.CreateNotificationChannel(_instance.notificationChannelSystem);
            foreach (var kvp in _instance.dictSystemNotif)
            {
               _instance._systemNotifManager.Notify((int)kvp.Key, kvp.Value);
            }
      }

      public enum SystemNotificationID
      {
         OpticalOutputDiagnosis
      }

      private NotificationsService()
      {
         notificationManagers = new HashSet<NotificationManager>();
         dictSystemNotif = new Dictionary<SystemNotificationID, Notification>() {
            { SystemNotificationID.OpticalOutputDiagnosis, new Notification.NotificationBuilder().SetChannelID(SYSTEM).SetDeletable(false).SetTitle(Properties.Resources.OpticalOutputDiagnosisNotificationTitle).SetMessage(Properties.Resources.OpticalOutputDiagnosisNotificationMessage).SetVisible(false).Build() }
         };

         notificationChannelSystem = new NotificationChannel(SYSTEM, SYSTEM);
      }

      public void SetSystemNotifVisibility(SystemNotificationID systemNotificationID, bool visibility)
      {
         lock (dictSystemNotif)
         {
            var notif = dictSystemNotif[systemNotificationID];
            if (notif != null)
            {
               notif.Visible = visibility;
            }
         }
      }

      public static NotificationsService GetNotificationsService()
      {
            return _instance;
      }

      public int Count
      {
         get
         {

            lock (countMonitor)
            {
               return count;
            }
         }
         private set
         {
            lock (countMonitor)
            {
               if (count != value)
               {
                  count = value;
                  NotificationsServiceCountChanged(nameof(Count));
               }
            }
         }
      }

      public ICollection<NotificationManager> NotificationManagers
      {
         get
         {
            List<NotificationManager> tmpListNotifManager = new List<NotificationManager>();
            lock (_instance)
            {
               foreach (var nm in notificationManagers)
               {
                  if (nm == _systemNotifManager) continue;
                  tmpListNotifManager.Add((NotificationManager)nm.Clone());
               }
            }
            return new ReadOnlyCollection<NotificationManager>(tmpListNotifManager);
         }
      }

      public ICollection<Notification> Notifications
      {
         get
         {
            var notifList = new List<Notification>();
            lock (_instance)
            {
               foreach (var nm in notificationManagers)
               {
                  var tmpNotifications = nm.Notifications;
                  foreach (var notif in tmpNotifications)
                  {
                     notifList.Add(notif);
                  }
               }
            }

            return new ReadOnlyCollection<Notification>(notifList);
         }
      }

      private void NotificationsServiceCountChanged(string info)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
      }
      public class NotificationManager : ICloneable
      {
         private static readonly NotificationsService notificationsService;
         private readonly Type context;
         private readonly IDictionary<NotificationChannel, IDictionary<int, Notification>> notifications;

         static NotificationManager()
         {
            notificationsService = GetNotificationsService();
         }

         private NotificationManager(Type context, IDictionary<NotificationChannel, IDictionary<int, Notification>> notifications)
         {
            this.context = context;
            this.notifications = notifications;
         }


         internal NotificationManager(Type context) : this(context, new Dictionary<NotificationChannel, IDictionary<int, Notification>>())
         {
         }

         internal ICollection<Notification> Notifications
         {
            get
            {
               var notifList = new List<Notification>();
               lock (notifications)
               {
                  var listListNotif = notifications.Where(ncn => ncn.Key.Visible == true).Select(kvp => kvp.Value.Select(idNotif => idNotif.Value).Where(n => n.Visible == true).ToList()).ToList();
                  listListNotif.ForEach(listNotif => listNotif.ForEach(notifList.Add));
               }
               return new ReadOnlyCollection<Notification>(notifList);
            }
         }

         internal static NotificationManager GetNotificationManager(object context)
         {

            var nm = new NotificationManager(context.GetType());
            lock (_instance)
            {
               if (notificationsService.notificationManagers.Add(nm))
               {
                  return nm;
               }
               else
               {
                  return notificationsService.notificationManagers.Where(x => x.Equals(nm)).First();
               }
            }
         }

         public override bool Equals(object obj)
         {
            if (obj == null || GetType() != obj.GetType())
            {
               return false;
            }

            NotificationManager nm = (NotificationManager)obj;

            return this.context == nm.context;
         }

         public override int GetHashCode()
         {
            var hashCode = 723747906;
            hashCode = hashCode * -1521134295 + context.GetHashCode();
            return hashCode;
         }

         public void Notify(int id, Notification notification)
         {

            if (notification == null)
            {
               throw new ArgumentNullException("notification should not be null.");
            }
            NotificationChannel channelTmp = new NotificationChannel(notification.ChannelID);
            lock (notifications)
            {
               if (notifications.TryGetValue(channelTmp, out var notificationDict))
               {
                  notification.NotificationDeleted += Notification_NotificationDeleted;
                  notification.OnNotificationVisibleChanged += Notification_OnNotificationVisibleChanged;
                  int add = 0;
                  if (!notificationDict.Remove(id))
                  {
                     add = 1;
                  }
                  notificationDict.Add(id, notification);

                  if (channelTmp.Visible && notification.Visible)
                  {
                     notificationsService.Count += add;
                  }
               }
               else
               {
                  throw new KeyNotFoundException("Notification Channel id : " + notification.ChannelID + "not found.");
               }
            }
         }

         /// <summary>
         /// return the id of a notification
         /// </summary>
         /// <param name="notification"></param>
         /// <returns>the id of a notification or null if it does not exist in this notificationManager</returns>
         public int? GetNotificationId(Notification notification)
         {

            var channelIdNotiftmp = notifications.Where(kvp => kvp.Key.ChannelID == notification.ChannelID).FirstOrDefault();
            if (channelIdNotiftmp.Key == null)
            {
               return null;
            }
            var idNotif = channelIdNotiftmp.Value.Where(kvp => kvp.Value == notification).FirstOrDefault();
            if (idNotif.Value == null)
            {
               return null;
            }

            return idNotif.Key;
         }

         private void Notification_OnNotificationVisibleChanged(object sender, PropertyChangedEventArgs e)
         {
            Notification notif = (Notification)sender;
            NotificationChannel channelTmp = new NotificationChannel(notif.ChannelID);
            lock (notifications)
            {
               var channel = notifications.Where(kvp => kvp.Key.Equals(channelTmp)).Select(kvp => kvp.Key).FirstOrDefault();
               if (channel != null)
               {
                  if (channel.Visible)
                  {
                     if (notif.Visible)
                     {
                        notificationsService.Count += 1;
                     }
                     else
                     {
                        notificationsService.Count -= 1;
                     }
                  }
               }
            }
         }

         private void Notification_NotificationDeleted(object sender, PropertyChangedEventArgs e)
         {

            Notification notif = (Notification)sender;
            NotificationChannel channelTmp = new NotificationChannel(notif.ChannelID);
            int key;
            lock (notifications)
            {
               notifications.TryGetValue(channelTmp, out var notifDict);
               key = notifDict.Where(p => p.Value.Equals(notif) && p.Value.Deleted).First().Key;
            }
            Close(key, notif);
         }

         public bool Close(string ChannelID, int id)
         {
            if (string.IsNullOrEmpty(ChannelID)) return false;
            KeyValuePair<int, Notification> IdNotif;
            lock (notifications)
            {
               var dicIdNotif = notifications.Where(kvp => kvp.Key.ChannelID == ChannelID).Select(kvp => kvp.Value).FirstOrDefault();
               if (dicIdNotif == null)
               {
                  return false;
               }
               IdNotif = dicIdNotif.Where(dic => dic.Key == id).FirstOrDefault();
            }
            return Close(IdNotif.Key, IdNotif.Value);
         }


         private bool Close(int id, Notification notif)
         {
            bool isClosed = false;
            if (notif == null)
            {
               return isClosed;
            }

            using (notif)
            {
               notif.NotificationDeleted -= Notification_NotificationDeleted;
               notif.OnNotificationVisibleChanged -= Notification_OnNotificationVisibleChanged;
               NotificationChannel channelTmp = new NotificationChannel(notif.ChannelID);
               lock (notifications)
               {
                  if (notifications.TryGetValue(channelTmp, out var notifDict))
                  {
                     isClosed = notifDict.Remove(id);
                     if (channelTmp.Visible && notif.Visible)
                     {
                        notificationsService.Count -= 1;
                     }
                  }
                  return isClosed;
               }
            }
         }

         public void CreateNotificationChannel(NotificationChannel notificationChannel)
         {
            if (notificationChannel == null)
            {
               throw new ArgumentNullException("notificationChannel must not be null");
            }
            lock (notifications)
            {
               if (notifications.ContainsKey(notificationChannel))
               {
                  return;
               }
            }
            notificationChannel.VisibilityChanged += NotificationChannel_VisibilityChanged;
            lock (notifications)
            {
               notifications.Add(notificationChannel, new Dictionary<int, Notification>());
            }
         }

         public bool DeleteNotificationChannel(string channelID)
         {

            NotificationChannel channelTmp = new NotificationChannel(channelID);
            lock (notifications)
            {
               channelTmp = notifications.Keys.Where(x => x.Equals(channelTmp)).First();
            }
            if (channelTmp != null)
            {
               channelTmp.VisibilityChanged -= NotificationChannel_VisibilityChanged;

               List<KeyValuePair<int, Notification>> lstNotif;
               lock (notifications)
               {
                  notifications.TryGetValue(channelTmp, out var dictIdNotif);
                  lstNotif = dictIdNotif.ToList();
               }
               foreach (var kv in lstNotif)
               {
                  Close(kv.Key, kv.Value);
               }

               lock (notifications)
               {
                  notifications.Remove(channelTmp);
               }
               return true;
            }
            else
            {
               return false;
            }

         }

         private void NotificationChannel_VisibilityChanged(object sender, PropertyChangedEventArgs e)
         {
            NotificationChannel notifchanTmp = (NotificationChannel)sender;
            lock (notifications)
            {
               notifications.TryGetValue(notifchanTmp, out IDictionary<int, Notification> dictIdNotif);
               int nbVisibleNotif = dictIdNotif.Where(kvp => kvp.Value.Visible == true).Count();

               if (notifchanTmp.Visible)
               {
                  notificationsService.Count += nbVisibleNotif;
               }
               else
               {
                  notificationsService.Count -= nbVisibleNotif;
               }
            }
         }

         public object Clone()
         {
            IDictionary<NotificationChannel, IDictionary<int, Notification>> cloneNotifChanDict = new Dictionary<NotificationChannel, IDictionary<int, Notification>>();
            lock (notifications)
            {
               notifications.Select<KeyValuePair<NotificationChannel, IDictionary<int, Notification>>, KeyValuePair<NotificationChannel, IDictionary<int, Notification>>>(kv => { return new KeyValuePair<NotificationChannel, IDictionary<int, Notification>>((NotificationChannel)kv.Key.Clone(), new Dictionary<int, Notification>(kv.Value)); })
               .ToList().ForEach(cloneNotifChanDict.Add);
            }
            return new NotificationManager(context, new ReadOnlyDictionary<NotificationChannel, IDictionary<int, Notification>>(cloneNotifChanDict));
         }
      }

      public class NotificationChannel : ICloneable
      {
         public static readonly string DEFAULT_CHANNEL_ID = "miscellaneous";
         private readonly string id;
         private readonly Optional<string> name;
         private bool visible = true;
         public event PropertyChangedEventHandler VisibilityChanged;

         public NotificationChannel(string id, string name = null)
         {
            if (string.IsNullOrEmpty(id))
            {
               throw new ArgumentException("the id of a NotificationChannel could not be null or empty");
            }
            this.id = id;

            this.name = Optional<string>.OfNullable(name);
         }

         public string ChannelID
         {
            get => id;
         }

         public string Name => name.OrElse(null);

         public bool Visible
         {
            get => visible;
            set
            {
               if (value != visible)
               {
                  visible = value;
                  NotificationChannelVisibilityChanged(nameof(Visible));
               }
            }
         }

         // override object.Equals
         public override bool Equals(object obj)
         {

            if (obj == null || GetType() != obj.GetType())
            {
               return false;
            }

            NotificationChannel nc = (NotificationChannel)obj;
            return nc.id == id;
         }

         // override object.GetHashCode
         public override int GetHashCode()
         {
            var hashCode = 723747906;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            return hashCode;

         }

         private void NotificationChannelVisibilityChanged(string info)
         {
            VisibilityChanged?.Invoke(this, new PropertyChangedEventArgs(info));
         }

         public object Clone()
         {
            NotificationChannel clone = new NotificationChannel(id, Name);
            clone.visible = visible;
            return clone;
         }
      }
   }

}
