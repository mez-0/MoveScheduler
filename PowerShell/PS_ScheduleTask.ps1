# https://stackoverflow.com/a/35777432
# https://stackoverflow.com/a/29337370

$Target = "10.10.11.113"

$Domain = "avatar.local"
$User = "iroh"
$Password = "3JyE63D%xu!4mBwnTHtvY8bhU2Z2r^M"

$Command = "powershell.exe"
$Arguments = "-e aaa"

$TaskName = "ps name"
$TaskDesc = "ps desc"

$ExecuteTime = (Get-Date).AddDays(1)
$ExpireTime = $ExecuteTime.AddMinutes(2)

$ShedService = new-object -comobject "Schedule.Service"
$ShedService.Connect($Target, $User, $Domain, $Password)

$Task = $ShedService.NewTask(0)
$Task.RegistrationInfo.Description = "$TaskDesc"
$Task.Settings.Enabled = $true
$Task.Settings.AllowDemandStart = $true
$Task.Settings.DeleteExpiredTaskAfter = "PT0S"
$Task.Settings.ExecutionTimeLimit = "PT1H"

$trigger = $task.triggers.Create(1) # Creates a "One time" trigger
#    TASK_TRIGGER_EVENT     0
#    TASK_TRIGGER_TIME      1
#    TASK_TRIGGER_DAILY     2
#    TASK_TRIGGER_WEEKLY    3
#    TASK_TRIGGER_MONTHLY   4
#    TASK_TRIGGER_MONTHLYDOW    5
#    TASK_TRIGGER_IDLE      6
#    TASK_TRIGGER_REGISTRATION  7
#    TASK_TRIGGER_BOOT      8
#    TASK_TRIGGER_LOGON     9
#    TASK_TRIGGER_SESSION_STATE_CHANGE  11

$trigger.StartBoundary = $ExecuteTime.ToString("yyyy-MM-dd'T'HH:mm:ss")
$trigger.EndBoundary = $ExpireTime.ToString("yyyy-MM-dd'T'HH:mm:ss")
$trigger.Enabled = $true

$Action = $Task.Actions.Create(0)
$action.Path = $Command
$action.Arguments = $Arguments

Try {$taskFolder = $ShedService.GetFolder("\$taskpath")}
catch {$taskFolder = $ShedService.GetFolder("\").CreateFolder("$taskpath")}

$result = $taskFolder.RegisterTaskDefinition("$TaskName",$Task,6,"System",$null,5)