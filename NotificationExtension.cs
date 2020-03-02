using System.Linq;

namespace Notification
{
   public static class NotificationExtension
   {
      public static NotificationManager GetNotificationManager(this object context)
      {
         return NotificationManager.GetNotificationManager(context);
      }
   }
}
