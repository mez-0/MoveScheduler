using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Management;
using System.Text;

namespace MoveScheduler
{
    class Utils
    {
        public static DateTime RemoteServerTime(String Target, String Domain = null, String Username = null, String Password = null)
        {
            DateTime r;

            ConnectionOptions options = new ConnectionOptions();
            if (Domain != null && Username != null && Password != null)
            {
                String Auth = Domain + "\\" + Username;
                options.Username = Auth;
                options.Password = Password;
            }
            
            String Scope = $"\\\\{Target}\\root\\cimv2";

            ManagementScope managementScope = new ManagementScope(Scope, options);

            SelectQuery timeQuery = new SelectQuery(@"SELECT * FROM Win32_LocalTime");
            ManagementObjectSearcher timeQuerySearcher = new ManagementObjectSearcher(managementScope, timeQuery);
            try
            {
                foreach (ManagementObject mo in timeQuerySearcher.Get())
                {
                    String t = $"{mo["Day"]}/{mo["Month"]}/{mo["Year"]} {mo["Hour"]}:{mo["Minute"]}:{mo["Second"]}";
                    r = DateTime.Parse(t);
                    return r;
                }
            }
            catch (Exception e)
            {
                Logger.Print(Logger.STATUS.INFO, "Failed to get remote DateTime, using local system time.");
                r = DateTime.Now;
                return r;
            }
            return DateTime.Now;
        }
        public static string DateTimetoUTC(DateTime dateParam)
        {
            string buffer = dateParam.ToString("********HHmmss.ffffff");
            TimeSpan tickOffset = TimeZone.CurrentTimeZone.GetUtcOffset(dateParam);
            buffer += (tickOffset.Ticks >= 0) ? '+' : '-';
            buffer += (Math.Abs(tickOffset.Ticks) / System.TimeSpan.TicksPerMinute).ToString("d3");
            return buffer;
        }
        public static String RandomString()
        {
            return Guid.NewGuid().ToString().Split('-')[0];
        }
    }
}
