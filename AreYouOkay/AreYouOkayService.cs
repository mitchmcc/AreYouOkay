using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;

//--------------------------------------------------------------------------------------------
//
// Module:  AreYouOkayService.cs
//
// Version        Date   Who    Description
//
// 01.00.02     12/28/12 mjm    Added code to support session connect. 
//                              Fixed bug in midnight detection.
//                              Added support for alternate email addresses (comma-separated)
// 01.00.03     12/29/12 mjm    Added code to debug crash "failed to process session event". 
//
// 01.00.04     12/30/12 mjm    Added code to ignore session change event reasons we don't care about. 
//
// 01.00.05     12/31/12 mjm    Added new thread code for LogonChecker work. 
//
// 01.00.06     01/01/13 mjm    Added configurable timeout (sleepSeconds). 
//
// 01.00.07     01/03/13 mjm    Added debug hook to clear map for testing. 
//
// 01.00.08     11/20/13 mjm    Made all debug output dependent on 'debug' flag. 
//                              Added 'okaystatus'bat' to scripts.
//
//--------------------------------------------------------------------------------------------

namespace AreYouOkay
{
    public partial class AreYouOkayService : ServiceBase
    {
        LogonChecker lc = null;
        private static EventLogger log = null;
        private static bool startDebugger = false;
        private bool debug = true;
        private String baseDir = null;
        private string alternateEmails = "";
        private int sleepSeconds = Constants.DEFAULT_SLEEP_SECS;

        public int SleepSeconds
        {
            get { return sleepSeconds; }
            set { sleepSeconds = value; }
        }

        public string AlternateEmails
        {
            get { return alternateEmails; }
            set { alternateEmails = value; }
        }

        private bool sendConnect = true;

        public bool SendConnect
        {
            get { return sendConnect; }
            set { sendConnect = value; }
        }

        private bool sendUnlock = true;

        public bool SendUnlock
        {
            get { return sendUnlock; }
            set { sendUnlock = value; }
        }

        public String BaseDir
        {
            get { return baseDir; }
            set { baseDir = value; }
        }

        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        String toAddress = "";

        public String ToAddress
        {
            get { return toAddress; }
            set { toAddress = value; }
        }

        String toName = "";

        public String ToName
        {
            get { return toName; }
            set { toName = value; }
        }
        String fromAddress = "";

        public String FromAddress
        {
            get { return fromAddress; }
            set { fromAddress = value; }
        }
        String fromName = "AreYouOkay Service";

        public String FromName
        {
            get { return fromName; }
            set { fromName = value; }
        }
        String fromPassword = "";

        public String FromPassword
        {
            get { return fromPassword; }
            set { fromPassword = value; }
        }
        String subject = Constants.DEFAULT_SUBJECT;

        public String Subject
        {
            get { return subject; }
            set { subject = value; }
        }
        String smtpHost = Constants.DEFAULT_SMTP_SERVER;

        public String SmtpHost
        {
            get { return smtpHost; }
            set { smtpHost = value; }
        }
        int smtpPort = Constants.DEFAULT_SMTP_PORT;

        public int SmtpPort
        {
            get { return smtpPort; }
            set { smtpPort = value; }
        }


        public AreYouOkayService()
        {
            string locus = "AreYouOkayService";

            InitializeComponent();
            CanHandleSessionChangeEvent = true;

            log = new EventLogger(Constants.LOG_NAME, Constants.SERVICE_NAME);

            try
            {
                if (startDebugger)
                {
                    try
                    {
                        System.Diagnostics.Debugger.Launch();
                    }
                    catch (Exception e)
                    {
                        log.DebugOut(locus, String.Format("WARNING: could not start debugger, error: {0}", e.Message), Constants.ERROR_START_DEBUGGER_FAIL, Constants.FILTER_ANY);
                    }
                }
            }
            catch (Exception e)
            {
                log.ErrorOut(locus, String.Format("ERROR: could not start debugger, error: {0}", e.Message), Constants.ERROR_START_DEBUGGER_FAIL);
            }


        }  // end constructor

        protected override void OnStart(string[] args)
        {
            string locus = "OnStart";

            try
            {
                bool okayToStart = true;
                string lastMissingParameter = "";
                StringBuilder parms = new StringBuilder();

                //string path = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
                //log.DebugOut(locus, String.Format("Current directory: {0}", path), Constants.EVENT_ID_STARTUP, Constants.FILTER_ALL);

                sendUnlock = Properties.Settings.Default.SendUnlock;

                sendConnect = Properties.Settings.Default.SendConnect;

                Debug = Properties.Settings.Default.Debug;

                alternateEmails = Properties.Settings.Default.AlternateEmails;

                sleepSeconds = Properties.Settings.Default.SleepSeconds;

                baseDir = Properties.Settings.Default.BaseDirectory;

                if (baseDir == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "BaseDir";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                toAddress = Properties.Settings.Default.ToAddress;

                if (toAddress == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "ToAddress";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                fromAddress = Properties.Settings.Default.FromAddress;

                if (fromAddress == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "FromAddress";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                fromName = Properties.Settings.Default.FromName;

                if (fromName == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "FromName";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                fromPassword = Properties.Settings.Default.FromPassword;

                if (fromPassword == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "FromPassword";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                toName = Properties.Settings.Default.ToName;

                subject = Properties.Settings.Default.Subject;

                smtpHost = Properties.Settings.Default.SmtpHost;

                if (smtpHost == "")
                {
                    okayToStart = false;
                    lastMissingParameter = "SmtpHost";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                smtpPort = Properties.Settings.Default.SmtpPort;

                if (smtpPort == 0)
                {
                    okayToStart = false;
                    lastMissingParameter = "SmtpPort";

                    if (parms.Length > 0)
                    {
                        parms.Append(",");
                    }
                    parms.Append(lastMissingParameter);
                }

                alternateEmails = Properties.Settings.Default.AlternateEmails;

                // Make sure everything we need is configured okay before proceeding

                if (!okayToStart)
                {
                    log.ErrorOut(locus, String.Format("ERROR: missing configuration parameter(s): {0}", parms.ToString()), Constants.ERROR_MISSING_PARAMETER);
                    throw new ConfigurationErrorsException("ERROR: missing configuration parameter(s)");
                }

                if (debug)
                {
                    log.DebugOut(locus, String.Format("{0} Version {1} starting", Constants.SERVICE_NAME, Constants.VERSION), Constants.EVENT_ID_STARTUP, Constants.FILTER_ALL);
                }

                lc = new LogonChecker();

                lc.ToAddress = ToAddress;
                lc.FromAddress = FromAddress;
                lc.FromPassword = FromPassword;
                lc.ToName = ToName;
                lc.FromName = FromName;
                lc.SmtpHost = SmtpHost;
                lc.SmtpPort = SmtpPort;
                lc.LogToFile = true;
                lc.BaseDir = BaseDir;
                lc.Subject = subject;
                lc.Debug = debug;
                lc.SendUnlock = sendUnlock;
                lc.SendConnect = sendConnect;
                lc.AlternateEmails = alternateEmails;

                // Now start the work

                lc.Start();
            }
            catch (Exception e)
            {
                log.ErrorOut(locus, String.Format("ERROR: caught exception during startup: {0}", e.Message), Constants.ERROR_STARTUP_EXCEPTION);
                this.ExitCode = Constants.ERROR_STARTUP_EXCEPTION;
                this.Stop();
            }

        }  // end OnStart

        protected override void OnStop()
        {
            string locus = "OnStop";

            log.DebugOut(locus, String.Format("{0} stop service requested", Constants.SERVICE_NAME), Constants.EVENT_ID_SHUTDOWN, Constants.FILTER_ALL);

            if (lc != null)
            {
                lc.Stop();
            }

        }  // end OnStop

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);

            lc.logSessionChangeEvent(changeDescription);
        }
    }
}
