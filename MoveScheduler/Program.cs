using System;
using CommandLine;
using CommandLine.Text;

namespace MoveScheduler
{
    public class Options
    {
        [Option('m', "method", Required = true, HelpText = "MoveScheduler method to execute with (Win32_ScheduledJob or SchedulerAPI)")]
        public String Method { get; set; }

        [Option('t', "target", Required = true, HelpText = "Target to create job on")]
        public String Target { get; set; }

        [Option('d', "domain", Required = false, HelpText = "Domain name to use for authentication (defaults to current user)")]
        public String Domain { get; set; }

        [Option('u', "username", Required = false, HelpText = "Username to use for authentication (defaults to current user)")]
        public String Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "Password to use for authentication (defaults to current user)")]
        public String Password { get; set; }

        [Option('c', "command", Required = false, HelpText = "Command to embed into job")]
        public String Command { get; set; }

        [Option("taskname", Required = false, HelpText = "Name to give task (Defaults to random string) (SchedulerAPI Only)")]
        public String TaskName { get; set; }

        [Option("taskdescription", Required = false, HelpText = "Description to give task (Defaults to random string) (SchedulerAPI Only)")]
        public String TaskDescription { get; set; }

        [Option("deleteafter", Required = false, HelpText = "Delete job after X seconds when executed (default 10 seconds) (SchedulerAPI Only)")]
        public Int32 DeleteAfter { get; set; }

        [Option("time", Required = false, HelpText = "Time until execution takes place (1 minute default)")]
        public Int32 Time { get; set; }

        [Option("onstartup", Required = false, HelpText = "Set an additional trigger for system startup (SchedulerAPI Only)")]
        public Boolean Onstartup { get; set; }

        [Option("wakeup", Required = false, HelpText = "Wake computer up when job is due (SchedulerAPI Only)")]
        public Boolean Wakeup { get; set; }

        [Option("system", Required = false, HelpText = "Elevate to SYSTEM (SchedulerAPI Only)")]
        public Boolean System { get; set; }

        [Option("diffdomain", Required = false, HelpText = "Domain for different user to run job (SchedulerAPI Only)")]
        public String DiffDomain { get; set; }

        [Option("diffuser", Required = false, HelpText = "User for different user to run job (SchedulerAPI Only)")]
        public String DiffUser { get; set; }

        [Option("diffpassword", Required = false, HelpText = "Password for different user to run job (SchedulerAPI Only)")]
        public String DiffPassword { get; set; }

        [Option("list", Required = false, HelpText = "List scheduled tasks (SchedulerAPI Only)")]
        public Boolean List { get; set; }

        [Option("delete", Required = false, HelpText = "Delete scheduled task (SchedulerAPI Only)")]
        public String Delete { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                 {
                    if (o.Method == "SchedulerAPI")
                    {
                         SchedulerAPI.Execute
                         (
                            o.Target,
                            o.Command,
                            o.Domain,
                            o.Username,
                            o.Password,
                            o.TaskName,
                            o.TaskDescription,
                            o.Onstartup,
                            o.Wakeup,
                            o.Time,
                            o.System,
                            o.DiffDomain,
                            o.DiffUser,
                            o.DiffPassword,
                            o.List,
                            o.Delete,
                            o.DeleteAfter
                         );
                    }
                    else if (o.Method == "Win32_ScheduledJob")
                    {
                        // will not accept >= 260 characters on the command
                         Win32_ScheduledJob.Execute
                         (
                             o.Target,
                             o.Command,
                             o.Time,
                             o.Domain,
                             o.Username,
                             o.Password
                         );
                     }
                    else if (o.Method == "PS_ScheduledTask")
                    {
                         o.Method = o.Method;
                    }
                    else
                    {
                         Logger.Print(Logger.STATUS.ERROR, o.Method + " is not a valid method!");
                    }
                 });
       }
    }
}
