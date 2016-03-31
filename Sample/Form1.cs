using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using J2534DotNet;
using System.Threading;
using System.IO;

namespace Sample
{
    public partial class Form1 : Form
    {
        private int m_J2534;
        private StreamWriter m_logfile;

        private event LogText LogEvent;
        private event ProgressBar ProgressEvent;

        ObdComm m_comm;

        public Form1()
        {
            InitializeComponent();

            LogEvent += new LogText(LogHandler);
            ProgressEvent += new ProgressBar(ProgressHandler);

            DateTime thisDay = DateTime.Today;
            Directory.CreateDirectory("Logs");
            m_logfile = File.AppendText("Logs\\KW82Log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
            m_logfile.AutoFlush = true;

            // Find all of the installed J2534 passthru devices
            List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
            if (availableJ2534Devices.Count == 0)
            {
                WriteLog("Could not find any installed J2534 devices.");
                return;
            }

            foreach (J2534Device d in availableJ2534Devices)
            {
                comboBoxJ2534.Items.Add(d.Name);
            }
            //comboBoxJ2534.SelectedIndex = comboBoxJ2534.Items.Count - 2;
            comboBoxJ2534.SelectedIndex = 0;
        }

        private void ProgressHandler(object sender, ProgressBarEventArgs e)
        {
            //run on UI thread
            this.Invoke((MethodInvoker)delegate
            {
                progressBar1.Value = e.Progress;
            });
        }

        private void LogHandler(object sender, LogEntryEventArgs e)
        {
            //run on UI thread
            this.Invoke((MethodInvoker)delegate
            {
                m_logfile.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " : " + e.Message);
                textBoxLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " : " + e.Message + Environment.NewLine);
                textBoxLog.SelectionStart = textBoxLog.Text.Length;
                textBoxLog.ScrollToCaret();
            });
        }

        private void WriteLog(string text)
        {
            LogEvent(null, new LogEntryEventArgs(String.Format(text)));
        }

        private void buttonReadInfo_Click(object sender, EventArgs e)
        {
            if (m_comm == null)
            {
                J2534 passThru = new J2534();

                m_J2534 = comboBoxJ2534.SelectedIndex;

                List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
                passThru.LoadLibrary(availableJ2534Devices[m_J2534]);

                m_comm = new ObdComm(passThru);
                m_comm.ProgressEvent += new ProgressBar(ProgressHandler);
                m_comm.LogEvent += new LogText(LogHandler);

                if (!m_comm.OpenIso9141(Convert.ToByte(textBox1.Text, 16)))
                {
                    string error = "";
                    J2534Err ret = m_comm.GetLastError(ref error);
                    WriteLog(String.Format("Error connecting to device. Error: {0}, {1}", ret, error));
                    m_comm.Disconnect();
                    return;
                }
            }

            Thread.Sleep(300);

            WriteLog("ECU identification: " + m_comm.KW82ECUIdentification);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (m_comm == null)
            {
                WriteLog("No connection to ECU.");
                return;
            }

            m_comm.KW82ReadDTC();

            Thread.Sleep(300);

            List <string> DTCs = m_comm.KW82DTCs;

            WriteLog("DTC count: " + DTCs.Count);
            foreach (string dtc in DTCs)
            {
                WriteLog("DTC: 0x" + dtc);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (m_comm == null)
            {
                WriteLog("No connection to ECU.");
                return;
            }

            m_comm.KW82ECUIdentification = "";

            m_comm.KW82ReadIdentification();

            Thread.Sleep(300);

            WriteLog("ECU identification: " + m_comm.KW82ECUIdentification);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (m_comm == null)
            {
                WriteLog("No connection to ECU.");
                return;
            }

            m_comm.KW82StopSession();
        }
    }
}
