$arguments = @{
    Command         = 'notepad.exe'  # replace 'someText' with meaningful text
    DaysOfMonth     = [UInt32](12345)  # replace 12345 with a meaningful value
    DaysOfWeek      = [UInt32](12345)  # replace 12345 with a meaningful value
    InteractWithDesktop = [Boolean](12345)  # replace 12345 with a meaningful value
    RunRepeatedly   = [Boolean](12345)  # replace 12345 with a meaningful value
    StartTime       = [DateTime](12345)  # replace 12345 with a meaningful value
}


Invoke-CimMethod -ClassName Win32_ScheduledJob -Namespace Root/CIMV2 -MethodName Create -Arguments $arguments |
Add-Member -MemberType ScriptProperty -Name ReturnValueFriendly -Passthru -Value {
  switch ([int]$this.ReturnValue)
  {
        0        {'Successful completion'}
        1        {'Not supported'}
        2        {'Access denied'}
        8        {'Unknown failure'}
        9        {'Path not found'}
        21       {'Invalid parameter'}
        22       {'Service not started'}
        default  {'Unknown Error '}
    }
}