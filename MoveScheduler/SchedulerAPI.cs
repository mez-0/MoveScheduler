using Microsoft.Win32.TaskScheduler;
using System;
using System.Net;
using System.Security;

// tl;dr: https://dahall.github.io/TaskScheduler/html/R_Project_TaskScheduler.htm

namespace MoveScheduler
{
    class SchedulerAPI
    {
        public static Boolean Execute
            (
                String Target,
                String Command,
                String Domain = "",
                String Username = "",
                String Password = "",
                String taskName = "",
                String taskDescription = "",
                Boolean Onstartup = false,
                Boolean Wakeup = false,
                Int32 TimeFromNow = 2,
                Boolean RunAsSystem = false,
                String DiffDomain = null,
                String DiffUser = null,
                String DiffPassword = null,
                Boolean ListJobs = false,
                String JobToDelete = null,
                Int32 DeleteAfter = 10
            )
        {

            SecureString secureString = null;

            if(taskName == null)
            {
                taskName = Utils.RandomString();
            }

            if(taskDescription == null)
            {
                taskDescription = Utils.RandomString();
            }

            Logger.Print(Logger.STATUS.INFO, "Connecting to: " + Target);

            if (Domain != null && Username != null && Password != null)
            {
                String Auth = Domain + "\\" + Username;
                Logger.Print(Logger.STATUS.INFO, "Authenticating as: " + Auth + ":" + Password);
                secureString = new NetworkCredential("", Password).SecurePassword;
            }
            else
            {
                Logger.Print(Logger.STATUS.INFO, "Authenticating as: " + Environment.UserDomainName + "\\" + Environment.UserName);
            }

            Boolean Response = Manager(
                        Target,
                        taskName,
                        taskDescription,
                        Command,
                        Domain,
                        Username,
                        secureString,
                        Onstartup,
                        Wakeup,
                        TimeFromNow,
                        RunAsSystem,
                        DiffDomain,
                        DiffUser,
                        DiffPassword,
                        ListJobs,
                        JobToDelete,
                        DeleteAfter
                    );
            if (!Response)
            {
                return false;
            }

            if (ListJobs || JobToDelete != null) { return true; }

            using (TaskService ts = new TaskService($"\\\\{Target}", Username, Domain, Password))
            {
                Task[] tasks = ts.FindAllTasks(new System.Text.RegularExpressions.Regex("."), true);
                Boolean isFound = false;
                
                foreach (Task task in tasks)
                {
                    TaskPrincipal princ = task.Definition.Principal;
                    if (task.Name.Contains(taskName))
                    {
                        Logger.Print(Logger.STATUS.GOOD, $"Task: '{task.Name}' User: '{princ.UserId}' Enabled: {task.Enabled} Next Run: {task.NextRunTime}");
                        isFound = true;
                        break;
                    }
                }
                if (!isFound)
                {
                    Logger.Print(Logger.STATUS.ERROR, "Task was not created.");
                }
            }
            return true;
        }
        private static Boolean Manager
            (
                string Target,
                string TaskName,
                string TaskDescription,
                string Cmd,
                string DomainName,
                string UserName,
                SecureString secureString,
                Boolean Onstartup,
                Boolean Wakeup,
                Int32 TimeFromNow,
                Boolean RunAsSystem,
                String DiffDomain,
                String DiffUser,
                String DiffPassword,
                Boolean ListJobs,
                String JobToDelete,
                Int32 DeleteAfter
            )
        {
            string Password = new NetworkCredential("", secureString).Password;

            if (ListJobs && JobToDelete == null)
            {
                using (TaskService taskService = new TaskService(Target, UserName, DomainName, Password))
                {
                    try
                    {
                        TaskFolder taskFolder = taskService.RootFolder;
                        foreach (Task task in taskFolder.AllTasks)
                        {
                            Logger.Print(Logger.STATUS.GOOD, task.Name);
                            Console.WriteLine("Active: " + task.IsActive);
                            Console.WriteLine("Last run time: " + task.LastRunTime);
                            Console.WriteLine("Next run time: " + task.NextRunTime);
                            Console.WriteLine();
                        }
                        return true;

                    }
                    catch (Exception e)
                    {
                        Logger.Print(Logger.STATUS.ERROR, e.Message);
                        return false;
                    }
                }
            }
            else if(!ListJobs && JobToDelete != null)
            {
                try
                {
                    using (TaskService taskService = new TaskService(Target, UserName, DomainName, Password))
                    {
                        TaskFolder taskFolder = taskService.RootFolder;
                        taskFolder.DeleteTask(JobToDelete);
                        Logger.Print(Logger.STATUS.GOOD, JobToDelete + " deleted.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Print(Logger.STATUS.ERROR, e.Message);
                    return false;
                }
            }
            else if(ListJobs && JobToDelete != null)
            {
                Logger.Print(Logger.STATUS.ERROR, "Choose either delete or list.");
                return false;
            }
            else if(ListJobs == false && Cmd == null)
            {
                Logger.Print(Logger.STATUS.ERROR, "Please specify a command or delete//list!");
                return false;
            }

            if(RunAsSystem == true)
            {
                if(DiffDomain != null || DiffUser != null || DiffPassword != null)
                {
                    Logger.Print(Logger.STATUS.ERROR, "Cannot create a task as SYSTEM and a different user");
                    return false;
                }
            }

            string Command = Cmd.Split(' ')[0]; // powershell.exe 
            string Args = Cmd.Replace(Command, ""); // everything else

            Logger.Print(Logger.STATUS.INFO, "Command: " + Command);
            Logger.Print(Logger.STATUS.INFO, "Arguments: " + Args);

            // Get the task service 
            try
            {
                using (TaskService taskService = new TaskService(Target, UserName, DomainName, Password))
                {
                    TaskDefinition taskDefinition = taskService.NewTask();
                    taskDefinition.RegistrationInfo.Description = TaskDescription;
                    taskDefinition.RegistrationInfo.Author = UserName;
                    taskDefinition.Settings.Hidden = true;
                    taskDefinition.Settings.StartWhenAvailable = true;
                    taskDefinition.Settings.Enabled = true;
                    taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.FromSeconds(DeleteAfter);
                    Logger.Print(Logger.STATUS.INFO, "Creating new task: " + TaskName);

                    // Create a trigger that will fire the task
                    // https://docs.microsoft.com/en-us/windows/win32/taskschd/trigger-types
                    // https://github.com/dahall/TaskScheduler/blob/master/TaskService/Trigger.cs
                    taskDefinition.Actions.Add(new ExecAction(Command, Args, null));

                    taskDefinition.Settings.WakeToRun = Wakeup; // literally wakes the computer up when its due to run

                    if (taskDefinition.Settings.WakeToRun == true)
                    {
                        Logger.Print(Logger.STATUS.INFO, "WakeToRun is enabled!");
                    }

                    if (Onstartup)
                    {
                        taskDefinition.Triggers.Add(new BootTrigger());
                    }

                    DateTime RemoteServerTime = Utils.RemoteServerTime(Target, DomainName, UserName, Password);
                    DateTime StartTime = RemoteServerTime.AddMinutes(TimeFromNow);

                    Logger.Print(Logger.STATUS.INFO, "Local Time is: " + DateTime.Now.ToString());
                    Logger.Print(Logger.STATUS.INFO, "Remote Time is: " + RemoteServerTime);
                    Logger.Print(Logger.STATUS.INFO, "Time to add is: " + TimeFromNow);
                    Logger.Print(Logger.STATUS.INFO, $"Remote start time is {TimeFromNow} minute(s) from now: " + StartTime);

                    TimeTrigger timeTrigger = new TimeTrigger();
                    timeTrigger.StartBoundary = StartTime;
                    timeTrigger.EndBoundary = StartTime.AddSeconds(10);
                    taskDefinition.Triggers.Add(timeTrigger);
                    Logger.Print(Logger.STATUS.INFO, "Will trigger at: " + StartTime.ToString());

                    if (taskService.HighestSupportedVersion > new Version(1, 2))
                    {
                        taskDefinition.Principal.LogonType = TaskLogonType.Password; // use a password
                        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest; // run with highest privs
                        Logger.Print(Logger.STATUS.INFO, "Running with highest privs!");
                    }

                    Logger.Print(Logger.STATUS.INFO, "Adding to root folder... ");

                    if (DiffDomain != null && DiffUser != null && DiffPassword != null)
                    {
                        Logger.Print(Logger.STATUS.INFO, "Registering as: " + DiffDomain + "\\" + DiffUser);
                        taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
                        taskService.RootFolder.RegisterTaskDefinition
                            (
                                TaskName,
                                taskDefinition,
                                TaskCreation.CreateOrUpdate,
                                DiffDomain + "\\" + DiffUser,
                                DiffPassword,
                                TaskLogonType.Password
                            );
                        return true;
                    }

                    if (RunAsSystem)
                    {
                        try
                        {
                            Logger.Print(Logger.STATUS.INFO, "Registering as NT AUTHORITY\\SYSTEM");
                            taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
                            taskService.RootFolder.RegisterTaskDefinition
                                (
                                    TaskName,
                                    taskDefinition,
                                    TaskCreation.CreateOrUpdate,
                                    "SYSTEM",
                                    null,
                                    TaskLogonType.ServiceAccount
                                );
                            return true;
                        }
                        catch (Exception e)
                        {
                            Logger.Print(Logger.STATUS.ERROR, e.Message);
                            return false;
                        }
                    }
                    else
                    {
                        taskService.RootFolder.RegisterTaskDefinition
                            (
                                TaskName,
                                taskDefinition,
                                TaskCreation.Create,
                                DomainName + "\\" + UserName,
                                Password,
                                TaskLogonType.Password
                            );
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Print(Logger.STATUS.ERROR, e.Message);
                return false;
            }
        }
        Boolean ListTasks()
        {

            return true;
        }
    }
}
