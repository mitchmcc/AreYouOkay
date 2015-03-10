using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace AreYouOkay
{
    public class EventLogger
    {
        private string eventSource = null;
        private string name = null;
        private int filterValue = -1;        // filter out all but port, or else "-1" means "all"

        public EventLogger(string name, string source)
        {
            eventSource = source;
            this.name = name;

        }  // end constructor

        public EventLogger(string logName, string source, int filter)
        {
            eventSource = source;
            filterValue = filter;
            this.name = logName;

        }  // end constructor


        public bool ErrorOut(string locus, string errorText, int eventId)
        {
            string sEvent = "(" + locus + ") " + errorText;

            if (!EventLog.SourceExists(name))
                EventLog.CreateEventSource(name, eventSource);

            EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.Error, eventId);
            return true;

        }  // end log.ErrorOut

        public bool ErrorOut(int id, string locus, string errorText, int eventId)
        {
            string sEvent = "(" + locus + "-" + id + ") " + errorText;

            if (!EventLog.SourceExists(name))
                EventLog.CreateEventSource(name, eventSource);

            EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.Error, eventId);
            return true;

        }  // end log.ErrorOut


        public bool DebugOut(int id, string locus, string debugText, int eventId, int filter)
        {
            // If filter string is set but not "all" (-1), and it doesn't match, just return.

            if ((filterValue != -1) && (!(filter == filterValue)))
            {
                return false;
            }

            string sEvent = "(" + locus + "-" + id + ") " + debugText;

            if (!EventLog.SourceExists(name))
                EventLog.CreateEventSource(name, eventSource);

            EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.Information, eventId);
            return true;

        }  // end DebugOut

        public bool DebugOut(string locus, string debugText, int eventId, int filter)
        {
            // If filter string is set but not "all" (-1), and it doesn't match, just return.

            if ((filterValue != -1) && (!(filter == filterValue)))
            {
                return false;
            }

            string sEvent = "(" + locus + ") " + debugText;

            if (!EventLog.SourceExists(name))
                EventLog.CreateEventSource(name, eventSource);

            EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.Information, eventId);
            return true;

        }  // end DebugOut

    }  // end class EventLogger
}
