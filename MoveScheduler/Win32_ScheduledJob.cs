using System;
using System.Data;
using System.Management;

namespace MoveScheduler
{
    // https://support.microsoft.com/en-gb/help/313565/how-to-use-the-at-command-to-schedule-tasks
    class Win32_ScheduledJob
    {
        public static Boolean Execute(String Target, String Command, Int32 TimeFromNow, String Domain = "", String Username = "", String Password = "")
        {
            if (Command.Length >= 260)
            {
                Logger.Print(Logger.STATUS.INFO, "The command is >= 260, the scheduler is likely to return error 21 and not launch the command.");
            }
            else if (Command == null)
            {
                Logger.Print(Logger.STATUS.ERROR, "Please specify a command!");
                return false;
            }

            ConnectionOptions options = new ConnectionOptions();
            if(Domain != null && Username != null && Password != null)
            {
                String Auth = Domain + "\\" + Username;
                options.Username = Auth;
                options.Password = Password;
                Logger.Print(Logger.STATUS.INFO, "Authenticating as: " + Auth + ":" + Password);
            }
            else
            {
                Logger.Print(Logger.STATUS.INFO, "Authenticating as: " + Environment.UserDomainName +"\\"+ Environment.UserName);
            }

            String Scope = $"\\\\{Target}\\root\\cimv2";

            Logger.Print(Logger.STATUS.INFO, "Connecting to: " + Scope);

            ManagementScope managementScope = new ManagementScope(Scope, options);

            try
            {
                managementScope.Connect();
                Logger.Print(Logger.STATUS.GOOD, "Connection successful!");
            }
            catch(Exception e)
            {
                Logger.Print(Logger.STATUS.ERROR, "Got error whilst connecting: " + e.Message);
                return false;
            }

            ObjectGetOptions objectGetOptions = new ObjectGetOptions();

            ManagementPath managementPath = new ManagementPath("Win32_ScheduledJob");

            ManagementClass managementClass = new ManagementClass(managementScope, managementPath, objectGetOptions);

            DateTime RemoteServerTime = Utils.RemoteServerTime(Target, Domain, Username, Password);

            if(TimeFromNow == 0)
            {
                TimeFromNow = 5;
            }

            string StartTime = Utils.DateTimetoUTC(RemoteServerTime.AddMinutes(TimeFromNow));

            Logger.Print(Logger.STATUS.INFO, "Local Time is: " + DateTime.Now.ToString());
            Logger.Print(Logger.STATUS.INFO, "Remote Time is: " + RemoteServerTime);
            Logger.Print(Logger.STATUS.INFO, "Time to add is: " + TimeFromNow);
            Logger.Print(Logger.STATUS.INFO, $"Remote start time is {TimeFromNow} minute(s) from now: " + StartTime);

            Logger.Print(Logger.STATUS.INFO, "Using command: " + Command);

            object[] cmdParams = { Command, StartTime, false, null, null, true, 100 };

            object outParams = managementClass.InvokeMethod("Create", cmdParams);

            Int32 WMIReturnCode = Int32.Parse(outParams.ToString());

            Logger.Print(Logger.STATUS.INFO, $"WMI Return Value: {WMIReturnCode} ({WMIReturnParser.Parse(WMIReturnCode)})");

            if (WMIReturnCode == 8)
            {
                Logger.Print(Logger.STATUS.INFO, "Return value 8 is usually returned because the remote host does not have the AT protocol enabled.");
                Logger.Print(Logger.STATUS.INFO, "This can be checked with following command and looking for EnableAt:");
                Console.WriteLine("Get-ItemProperty Registry::\"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Configuration\\");
            }

            else if(WMIReturnCode == 0)
            {
                Logger.Print(Logger.STATUS.GOOD, "Command executed successfully!");
            }

            return true;
        }
    }
}
