using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AreYouOkay
{
    class Constants
    {

        public const int SUCCESS = 0;
        public const int ERROR = -1;

        public const int FILTER_ALL = 0;
        public const int FILTER_ANY = -1;

        public const int ERROR_BASE = 10000;
        public const int ERROR_UNKNOWN_COMMAND =        ERROR_BASE + 1;
        public const int ERROR_START_DEBUGGER_FAIL =    ERROR_BASE + 2;
        public const int ERROR_MISSING_PARAMETER =      ERROR_BASE + 3;
        public const int ERROR_SMTP_EXCEPTION =         ERROR_BASE + 4;
        public const int ERROR_STARTUP_EXCEPTION =      ERROR_BASE + 5;

        public const int EVENT_ID_BASE = 15000;
        public const int EVENT_ID_STARTUP =     EVENT_ID_BASE + 1;
        public const int EVENT_ID_SHUTDOWN =    EVENT_ID_BASE + 2;
        public const int EVENT_ID_PARSE_ERROR = EVENT_ID_BASE + 3;

        public const int DEFAULT_SMTP_PORT = 5000;
        public const string DEFAULT_SMTP_SERVER = "smtp.gmail.com";
        public const string DEFAULT_SUBJECT = "AreYouOkay event notification";
        public const int DEFAULT_SLEEP_SECS = 3600;                       // one hour
        public const int DEFAULT_MAX_LOG_SIZE = 8000;

        public const string SERVICE_NAME = "AreYouOkay";
        public const string LOG_NAME = "Application";
        public const string VERSION = "1.0.8";
    }
}
