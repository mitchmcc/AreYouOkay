using System;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Management;
using System.IO;
using System.Net;
using System.Net.Mail;

//--------------------------------------------------------------------------------------------
//
// Module:  LogonChecker.cs
//
// Date     Who     Description
//
// 12/27/12 mjm     Added hook to send email for session unlock.  Added support for 
//                  multiple users and alternate email addresses.
//
//
//--------------------------------------------------------------------------------------------

namespace AreYouOkay
{
    class LogonChecker
    {
        private readonly ManagementClass _wmiComputerSystem = new ManagementClass("Win32_ComputerSystem");
        private bool fakeEvent = false;
        private static EventLogger log = null;
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

        private Dictionary<string, string> sendMap = new Dictionary<string, string>();

        String baseDir = @"C:\AreYouOkay";
        String fileName = @"AreYouOkay.txt";

        private bool debugClear = false;

        private bool debug = false;

        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        public String BaseDir
        {
            get { return baseDir; }
            set { baseDir = value; }
        }
        String outputFile;
        System.IO.StreamWriter file = null;
        //bool sent = false;

        bool sendConnect = true;

        public bool SendConnect
        {
            get { return sendConnect; }
            set { sendConnect = value; }
        }

        bool sendUnlock = true;

        public bool SendUnlock
        {
            get { return sendUnlock; }
            set { sendUnlock = value; }
        }

        bool logToFile = true;

        public bool LogToFile
        {
            get { return logToFile; }
            set { logToFile = value; }
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
        String fromName = "";

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
        String subject = "logon event";

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

        public LogonChecker()
        {
            string locus = "LogonChecker";

            outputFile = baseDir + @"\" + fileName;

            // Check for valid msi

            if (_wmiComputerSystem == null)
            {
                logFileWrite(locus, String.Format("ERROR: _wsiComputerSystem object is null"));
            }

            if (logToFile)
            {
                if (!File.Exists(outputFile))
                {
                    file = File.CreateText(outputFile);
                }
                else
                {
                    file = File.AppendText(outputFile);
                }

                logFileWrite(locus, String.Format("AreYouOkay Service Version {0} starting up", Constants.VERSION));
            }

            log = new EventLogger(Constants.LOG_NAME, Constants.SERVICE_NAME);

        }  // end constructor

        public void Start()
        {
            ThreadStart job = new ThreadStart(Run);
            Thread thread = new Thread(job);
            thread.Start();

        }  // end Start

        public void Run()
        {
            string locus = "Run";

            // wait forever, but wake up every 5 minutes

            while (true)
            {
                // We check to see if the hour changed, before and after our sleep.  If it did, and if it
                // transitioned from 23 to 00, we just passed midnight, and will reset our 'sent' flag.

                int lastHour = DateTime.Now.Hour;

                Thread.Sleep(sleepSeconds * 1000);                         // convert seconds to microseconds

                // Check to see when we pass over from hour 23 to hour 0

                int thisHour = DateTime.Now.Hour;

                if (debug)
                {
                    logFileWrite(locus, String.Format("This hour: {0} last hour: {1}", thisHour, lastHour));
                }

                if ((thisHour < lastHour) || (clearMap(thisHour, lastHour)))
                {
                    // Reset flag to send email next time

                    if (debug)
                    {
                        logFileWrite(locus, "Clearing 'sent' map");
                    }

                    sendMap.Clear();
                }

            }  // end while

        }  // end Run

        private bool clearMap(int thisHour, int lastHour)
        {
            if (!debugClear)
            {
                return false;
            }

            // for debugging only

            if (thisHour == 12)
            {
                return true;
            }

            return false;
        }

        public void Stop()
        {
            string locus = "Stop";

            logFileWrite(locus, "AreYouOkay Service stopping");

        }  // end Stop

        public void logSessionChangeEvent(SessionChangeDescription chg)
        {
            string locus = "logSessionChangeEvent";

            if (debug)
            {
                logFileWrite(locus, String.Format("Session change event: {0}, reason: {1}", chg.ToString(), chg.Reason.ToString()));
            }

            bool ignore = true;

            switch (chg.Reason)
            {
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                case SessionChangeReason.ConsoleConnect:
                    ignore = false;
                    break;

                default:
                    break;

            }  // end switch

            if (ignore)
            {
                if (debug)
                {
                    logFileWrite(locus, String.Format("Ignoring session change event: {0}, reason: {1}", chg.ToString(), chg.Reason.ToString()));
                }

                return;
            }

            string user = null;

            try
            {
                foreach (ManagementObject currentObject in _wmiComputerSystem.GetInstances())
                {
                    user = currentObject.Properties["UserName"].Value.ToString().Trim();
                }
            }
            catch (Exception e)
            {
                // On Windows 7 we get some events that do not have a UserName object, and we were crashing.... so now catch it
                // and try to figure out which one.

                logFileWrite(locus, String.Format("ERROR: caught exception getting UserName object, error message: {0}", e.Message));
                logFileWrite(locus, String.Format("Session change type: {0}", chg.ToString()));
                return;
            }

            if (user == null)
            {
                if (debug)
                {
                    logFileWrite(locus, String.Format("WARNING: Skipping session change event: {0} no user found in map", chg.Reason));
                }

                return;
            }

            // Check to see if we have already sent for this user

            if (sendMap.ContainsKey(user))
            {
                if (debug)
                {
                    logFileWrite(locus, String.Format("Skipping session change event: {0} for user {1} found in map", chg.Reason, user));
                }

                return;
            }

            switch (chg.Reason)
            {
                case SessionChangeReason.SessionLogon:

                    processEmail(user, "logon");

                    break;

                case SessionChangeReason.SessionUnlock:

                    if (sendUnlock)
                    {
                        processEmail(user, "logon (unlock)");
                    }

                    break;

                case SessionChangeReason.ConsoleConnect:

                    if (sendConnect)
                    {
                        processEmail(user, "logon (connect)");
                    }

                    break;

                default:
                    if (debug)
                    {
                        logFileWrite(locus, String.Format("Got session change event: {0} for user {1}", chg.Reason, user));
                    }

                    if (fakeEvent)
                    {
                        if (debug)
                        {
                            logFileWrite(locus, "sending fake logon event");
                        }

                        processEmail(user, "fake logon event");
                    }

                    break;

            }  // end switch


        }  // end logSessionChangeEvent

        private void processEmail(string user, string eventToSend)
        {
            string locus = "processEmail";

            logFileWrite(locus, String.Format("Got {0} event for user {1}", eventToSend, user));

            if (!sendMap.ContainsKey(user))
            {
                //sent = true;

                // Create map entry for this user

                sendMap.Add(user, DateTime.Now.ToString("g"));

                var from = new MailAddress(fromAddress, fromName);
                var to = new MailAddress(toAddress, toName);
                String body = String.Format("Got {0} event for user {1}", eventToSend, user);

                if (debug)
                {
                    logFileWrite(locus, String.Format("fromAddr: {0}, fromName: {1}, toAddress: {2}, toName: {3}", fromAddress, fromName, toAddress, toName));
                    logFileWrite(locus, String.Format("fromPassword: {0}", fromPassword));
                    logFileWrite(locus, String.Format("smtpHost: {0}, smtpPort: {1}", SmtpHost, SmtpPort));
                }

                var smtp = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(from.Address, fromPassword),
                    Timeout = 20000
                };

                MailMessage message = new MailMessage(from, to);

                // Check for extra emails if any were provided

                if (alternateEmails.Length > 0)
                {
                    string[] emails = alternateEmails.Split(',');

                    foreach (string addr in emails)
                    {
                        if (debug)
                        {
                            logFileWrite(locus, String.Format("Added alt email addr: {0}", addr));
                        }

                        message.To.Add(addr);
                    }
                }

                message.Subject = subject;
                message.Body = body;

                try
                {
                    smtp.Send(message);
                    logFileWrite(locus, "Sent email for user: " + user);
                }
                catch (Exception e)
                {
                    log.ErrorOut(locus, String.Format("ERROR: could not send email: {0}", e.Message), Constants.ERROR_SMTP_EXCEPTION);
                }

            }  // end if sendEmail and not sent
            else
            {
                if (debug)
                {
                    logFileWrite(locus, "Email already sent today");
                }
            }

        }  // end processEmail


        private void logFileWrite(string locus, string text)
        {
            if (logToFile)
            {
                file.WriteLine(String.Format("{0}: ({1}) {2}", DateTime.Now.ToString("G"), locus, text));
                file.Flush();
            }

        }  // end logFileWrite

    }  // end class

}  // end namespace
