using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveScheduler
{
    class WMIReturnParser
    {
        public static String Parse(Int32 ReturnCode)
        {
            switch (ReturnCode)
            {
                case 0:
                    return "Successful Completion";
                case 1:
                    return "Request not supported";
                case 2:
                    return "Access Denied";
                case 8:
                    return "Unknown Failure";
                case 9:
                    return "Path not found";
                case 21:
                    return "Invalid parameter";
                case 22:
                    return "Service not started";
                default:
                    return "Unrecognised response";
            }
        }
    }
}
