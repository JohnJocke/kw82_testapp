#region Copyright (c) 2010, Michael Kelly
/* 
 * Copyright (c) 2010, Michael Kelly
 * michael.e.kelly@gmail.com
 * http://michael-kelly.com/
 * 
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the organization nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */
#endregion License
using System.Collections.Generic;
using System.Linq;
using System.Text;
using J2534DotNet;
using System.Threading;
using System;

namespace Sample
{
    public delegate void LogText(object sender, LogEntryEventArgs e);
    public delegate void ProgressBar(object sender, ProgressBarEventArgs e);

    public class LogEntryEventArgs : EventArgs
    {
        private readonly string message;

        public LogEntryEventArgs(string arg)
        {
            this.message = arg;
        }
        public string Message
        {
            get { return this.message; }
        }
    }

    public class ProgressBarEventArgs : EventArgs
    {
        private readonly int progress;

        public ProgressBarEventArgs(int arg)
        {
            this.progress = arg;
        }
        public int Progress
        {
            get { return this.progress; }
        }
    }

    public class ObdComm
    {
        private IJ2534 m_j2534Interface;
        ProtocolID m_protocol;
        int m_deviceId;
        int m_channelId;
        bool m_isConnected;
        J2534Err m_status;
        private Thread m_thread;

        public event ProgressBar ProgressEvent;
        public event LogText LogEvent;

        private readonly object m_KW82WriteLock = new object();
        private Queue<List<byte>> m_KW82WriteQueue = new Queue<List<byte>>();

        private readonly object m_KW82BlockLock = new object();

        private List<string> m_KW82DTCs = new List<string>();

        public List<string> KW82DTCs
        {
            get
            {
                lock (m_KW82BlockLock)
                {
                    return m_KW82DTCs.ToList();
                }
            }
        }

        private string m_KW82ECUIdentification = "Not identified";
        public string KW82ECUIdentification
        {
            get
            {
                lock (m_KW82BlockLock)
                {
                    return m_KW82ECUIdentification;
                }
            }
            set {
                lock (m_KW82BlockLock)
                {
                    m_KW82ECUIdentification = value;
                }
            }
        }

        public ObdComm(IJ2534 j2534Interface)
        {
            m_j2534Interface = j2534Interface;
            m_isConnected = false;
            m_protocol = ProtocolID.ISO15765;
            m_status = J2534Err.STATUS_NOERROR;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        private void WriteLog(string text)
        {
            LogEvent(null, new LogEntryEventArgs(String.Format(text)));
        }

        public bool StartSession(byte session, byte baud)
        {
            WriteLog("Starting session");
            List<byte> value = new List<byte>();
            ProtocolTransaction(new List<byte> { 0x10, session, baud }, m_protocol, ref value);
            if (value.Count == 4 && value[1] == 0x50 && value[2] == session && value[3] == baud)
            {
                List<SConfig> configs = new List<SConfig>();
                SConfig s;
                s.Parameter = 0x01; //DATA_RATE

                switch (baud)
                {
                    case 0x01:
                        s.Value = 9600;
                        break;
                    case 0x02:
                        s.Value = 19200;
                        break;
                    case 0x03:
                        s.Value = 38400;
                        break;
                    case 0x04:
                        s.Value = 57600;
                        break;
                    case 0x05:
                        s.Value = 115200;
                        break;
                    default:
                        WriteLog("Unsupported baudrate!");
                        return false;
                }

                configs.Add(s);

                m_status = m_j2534Interface.SetConfig(m_channelId, ref configs);
                if (J2534Err.STATUS_NOERROR == m_status)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool StartSession(byte session)
        {
            WriteLog("Starting session");
            List<byte> value = new List<byte>();
            ProtocolTransaction(new List<byte> { 0x10, session }, m_protocol, ref value);
            if (value.Count == 3 && value[1] == 0x50 && value[2] == session)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool StopSession()
        {
            WriteLog("Stopping session");
            List<byte> value = new List<byte>();
            ProtocolTransaction(new List<byte> { 0x20 }, m_protocol, ref value);
            if (value.Count == 2 && value[1] == 0x60)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool KW82StopSession()
        {
            WriteLog("Stopping session");

            List<byte> endDiagnosis = new List<byte> { 0x02, 0xB2, 0x00, 0xB4 };
            for (int k = 0; k < 5; ++k)
            {
                SendMessage(endDiagnosis, ProtocolID.ISO9141);
                Thread.Sleep(20);
            }
            m_j2534Interface.ClearRxBuffer(m_channelId);


            return true;
        }
        public bool IsConnected()
        {
            return m_isConnected;
        }

        public bool DetectProtocol()
        {
            // possible return values:
            //  ProtocolID.ISO15765; // CAN
            //  ProtocolID.ISO9141;  // ISO-K
            //  ProtocolID.J1850PWM;  // J1850PWM
            //  ProtocolID.J1850VPW;  // J1850VPW
            m_deviceId = 0;
            m_status = m_j2534Interface.Open(ref m_deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
                return false;
            if (ConnectIso14230(true))
            {
                m_protocol = ProtocolID.ISO14230;
                m_isConnected = true;
            }
            else
            {
                return false;
            }
            return true;
        }

        public bool OpenIso9141(byte ecuAddress)
        {
            // possible return values:
            //  ProtocolID.ISO15765; // CAN
            //  ProtocolID.ISO9141;  // ISO-K
            //  ProtocolID.J1850PWM;  // J1850PWM
            //  ProtocolID.J1850VPW;  // J1850VPW
            m_deviceId = 0;
            m_status = m_j2534Interface.Open(ref m_deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
                return false;
            if (ConnectIso9141(ecuAddress, true))
            {
                m_protocol = ProtocolID.ISO9141;
                m_isConnected = true;
            }
            else
            {
                return false;
            }
            return true;
        }

        public J2534Err GetLastError(ref string errorDescription)
        {
            m_j2534Interface.GetLastError(ref errorDescription);
            return m_status;
        }

        public bool ConnectIso9141(byte ecuAddress, bool doInit)
        {
            byte kw1 = 0;
            byte kw2 = 0;

            m_status = m_j2534Interface.Connect(m_deviceId, ProtocolID.ISO9141, ConnectFlag.ISO9141_NO_CHECKSUM, 8192, ref m_channelId);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            PassThruMsg maskMsg = new PassThruMsg();
            PassThruMsg patternMsg = new PassThruMsg();
            PassThruMsg flowControlMsg = new PassThruMsg();
            int filterId = 0;

            byte i;
            for (i = 0; i < 1; i++)
            {
                maskMsg.ProtocolID = ProtocolID.ISO9141;
                maskMsg.TxFlags = 0;
                maskMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                patternMsg.ProtocolID = ProtocolID.ISO9141;
                patternMsg.TxFlags = 0;
                patternMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                flowControlMsg.ProtocolID = 0;
                flowControlMsg.TxFlags = 0;
                flowControlMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                m_status = m_j2534Interface.StartMsgFilter(m_channelId, FilterType.PASS_FILTER, ref maskMsg, ref patternMsg, ref filterId);
                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    //m_j2534Interface.Disconnect(m_channelId);
                    return false;
                }
            }

            if (doInit)
            {
                //end KW82 diagnostic session just in case
                List<byte> endDiagnosis = new List<byte> { 0x02, 0xB2, 0x00, 0xB4 };
                for (int k = 0; k < 10; ++k)
                {
                    SendMessage(endDiagnosis, ProtocolID.ISO9141);
                    Thread.Sleep(20);
                }
                m_j2534Interface.ClearRxBuffer(m_channelId);

                WriteLog("Connecting ECU with 5-baud init");

                List<SConfig> configs = new List<SConfig>();
                SConfig s;

                s.Parameter = (int)J2534Parameter.W0;
                s.Value = 500;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.W1;
                s.Value = 500;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.W2;
                s.Value = 500;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.W3;
                s.Value = 500;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.W4;
                s.Value = 500;
                configs.Add(s);

                s.Parameter = (int)J2534Parameter.P1_MAX;
                s.Value = 1;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.P3_MIN;
                s.Value = 0;
                configs.Add(s);
                s.Parameter = (int)J2534Parameter.P4_MIN;
                s.Value = 0;
                configs.Add(s);

                s.Parameter = (int)J2534Parameter.FIVE_BAUD_MOD;
                s.Value = 1;
                configs.Add(s);

                m_j2534Interface.SetConfig(m_channelId, ref configs);

                m_status = m_j2534Interface.FiveBaudInit(m_channelId, ecuAddress, ref kw1, ref kw2);

                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    //m_j2534Interface.Disconnect(m_channelId);
                    return false;
                }
            }

            WriteLog("Connected to ECU, keywords: " + $"{kw1:X2} {kw2:X2}");

            m_thread = new Thread(new ThreadStart(KW82Handler));
            m_thread.Start();

            return true;
        }

        public bool ConnectIso14230(bool doInit)
        {
            List<byte> value = new List<byte>();

            m_status = m_j2534Interface.Connect(m_deviceId, ProtocolID.ISO14230, ConnectFlag.NONE, BaudRate.ISO14230, ref m_channelId);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            PassThruMsg maskMsg = new PassThruMsg();
            PassThruMsg patternMsg = new PassThruMsg();
            PassThruMsg flowControlMsg = new PassThruMsg();
            int filterId = 0;

            byte i;
            //for (i=0; i < 8; i++)
            for (i = 0; i < 1; i++)
            {
                maskMsg.ProtocolID = ProtocolID.ISO14230;
                maskMsg.TxFlags = 0;
                maskMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                patternMsg.ProtocolID = ProtocolID.ISO14230;
                patternMsg.TxFlags = 0;
                patternMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                flowControlMsg.ProtocolID = ProtocolID.ISO14230;
                flowControlMsg.TxFlags = 0;
                flowControlMsg.Data = new byte[] { 0x00, 0x00, 0x00, 0x00};

                m_status = m_j2534Interface.StartMsgFilter(m_channelId, FilterType.PASS_FILTER, ref maskMsg, ref patternMsg, ref flowControlMsg, ref filterId);
                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    m_j2534Interface.Disconnect(m_channelId);
                    return false;
                }
            }

            if (doInit)
            {
                WriteLog("Connecting ECU with fast init");
                PassThruMsg txMsg = new PassThruMsg();
                PassThruMsg rxMsg = new PassThruMsg();
                txMsg.ProtocolID = ProtocolID.ISO14230;
                txMsg.Data = new byte[] { 0x81, 0x11, 0xF1, 0x81 };
                m_status = m_j2534Interface.FastInit(m_channelId, txMsg, ref rxMsg);
                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    m_j2534Interface.Disconnect(m_channelId);
                    return false;
                }
            }

            WriteLog("Connected to ECU");
            return true;
        }

        public bool ConnectIso15765()
        {
            List<byte> value = new List<byte>();

            m_status = m_j2534Interface.Connect(m_deviceId, ProtocolID.ISO15765, ConnectFlag.NONE, BaudRate.ISO15765, ref m_channelId);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            PassThruMsg maskMsg = new PassThruMsg();
            PassThruMsg patternMsg = new PassThruMsg();
            PassThruMsg flowControlMsg = new PassThruMsg();
            int filterId = 0;

	        byte i;
	        //for (i=0; i < 8; i++)
            for (i = 0; i < 1; i++)
	        {
                maskMsg.ProtocolID = ProtocolID.ISO15765;
                maskMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;
                maskMsg.Data = new byte[]{0xff,0xff,0xff,0xff};

                patternMsg.ProtocolID = ProtocolID.ISO15765;
                patternMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;
                patternMsg.Data = new byte[]{0x00,0x00,0x07,(byte)(0xE8 + i)};

                flowControlMsg.ProtocolID = ProtocolID.ISO15765;
                flowControlMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;
                flowControlMsg.Data = new byte[]{0x00,0x00,0x07,(byte)(0xE0 + i)};

                m_status = m_j2534Interface.StartMsgFilter(m_channelId, FilterType.FLOW_CONTROL_FILTER, ref maskMsg, ref patternMsg, ref flowControlMsg, ref filterId);
                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    m_j2534Interface.Disconnect(m_channelId);
                    return false;
                }
	        }
            
            if(!ReadObdPid(0x01,0x00,ProtocolID.ISO15765, ref value))
            {
                m_j2534Interface.Disconnect(m_channelId);
		        return false;
	        }
	        return true;
        }

        public bool Disconnect()
        {
            m_status = m_j2534Interface.Close(m_deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
            {
                return false;
            }
            return true;
        }

        private void checksum(ref List<byte> data)
        {
            byte sum = 0;
            int i = 0;
            for (i = 0; i < data.Count; ++i)
            {
                sum += data[i];
            }
            data.Add(sum);
        }

        private bool ReadObdPid(byte pid, byte mode, ProtocolID protocolId, ref List<byte> value)
        {
            return ProtocolTransaction(new List<byte> { pid, mode }, protocolId, ref value);
        }

        private bool SendMessages(List<PassThruMsg> msgs, int msgCount)
        {
            int timeout = 0;

            int numMsgs = msgCount;
            m_status = m_j2534Interface.WriteMsgs(m_channelId, msgs, ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }
            return true;
        }

        private bool SendMessage(List<byte> data, ProtocolID protocolId)
        {
            PassThruMsg txMsg = new PassThruMsg();
            int timeout;

            Console.Write("TX: ");
            for (int k = 0; k < data.Count; ++k)
            {
                Console.Write(data[k].ToString("X") + " ");
            }
            Console.Write("\r\n");

            txMsg.ProtocolID = protocolId;
            switch (protocolId)
            {
                case ProtocolID.ISO15765:

                case ProtocolID.J1850PWM:
                case ProtocolID.J1850VPW:
                case ProtocolID.ISO9141:
                case ProtocolID.ISO14230:
                    txMsg.TxFlags = TxFlag.NONE;

                    //data.Insert(0, (byte)data.Count);
                    //checksum(ref data);
                    txMsg.Data = data.ToArray();
                    timeout = 0;
                    break;
                default:
                    return false;
            }

            //m_j2534Interface.ClearRxBuffer(m_channelId);

            int numMsgs = 1;
            m_status = m_j2534Interface.WriteMsgs(m_channelId, ref txMsg, ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }
            return true;
        }

        private bool ReadMessages(ProtocolID protocolId, ref List<PassThruMsg> msgs, int msgCount)
        {
            List<PassThruMsg> rxMsgs = new List<PassThruMsg>();
            int timeout = 100;
            int numMsgs = 2 * msgCount;

            m_status = m_j2534Interface.ReadMsgs(m_channelId, ref rxMsgs, ref numMsgs, timeout);

            if (rxMsgs.Count > 0)
            {
                msgs = rxMsgs;
                return true;
            }
            return false;
        }

        private bool ReadMessage(ProtocolID protocolId, ref List<byte> value)
        {
            return ReadMessage(protocolId, ref value, 400);
        }

        private bool ReadMessage(ProtocolID protocolId, ref List<byte> value, int timeout)
        {
            List<PassThruMsg> rxMsgs = new List<PassThruMsg>();
            int numMsgs = 1;

            m_status = m_j2534Interface.ReadMsgs(m_channelId, ref rxMsgs, ref numMsgs, timeout);

            if (m_status != J2534Err.STATUS_NOERROR)
            {
                Console.WriteLine("ReadMsgs error: " + m_status.ToString());
            }

            Console.WriteLine($"ReadMsgs count {rxMsgs.Count}, RxStatus {rxMsgs[0].RxStatus}");

            if (rxMsgs.Count > 0 && rxMsgs[0].RxStatus == RxStatus.NONE)
            {
                value = rxMsgs[0].Data.ToList();
                Console.Write("RX: ");
                for (int k = 0; k < value.Count; ++k)
                {
                    Console.Write(value[k].ToString("X") + " ");
                }
                Console.Write("\r\n");
                return true;
            }
            return false;
        }

        private bool ProtocolTransaction(List<byte> data, ProtocolID protocolId, ref List<byte> value)
        {
            if (!SendMessage(data, protocolId))
            {
                return false;
            }

            if (!ReadMessage(protocolId, ref value))
            {
                return false;
            }

            return true;
        }

        private void KW82Handler()
        {
            while (true)
            {
                List<byte> data = new List<byte>();
                if (ReadMessage(m_protocol, ref data, 500))
                {
                    ParseKW82Message(data);

                    lock (m_KW82WriteQueue)
                    {
                        if (m_KW82WriteQueue.Count > 0)
                        {
                            List<byte> write = m_KW82WriteQueue.Dequeue();
                            SendMessage(write, m_protocol);
                        }
                    }
                }
            }
        }

        private void ParseKW82Message(List<byte> data)
        {
            if (data == null || data.Count < 4)
            {
                Console.WriteLine("KW82 invalid message length");
                return;
            }

            byte size = data[0];
            byte command = data[1];
            byte[] payload = data.Skip(2).Take(data.Count - 4).ToArray();
            ushort receivedChecksum = BitConverter.ToUInt16(data.Skip(data.Count - 2).ToArray(), 0);

            ushort calculatedChecksum = CalculateChecksum(data, data.Count - 2);

            if (receivedChecksum != calculatedChecksum)
            {
                Console.WriteLine($"KW82 checksum mismatch, rec {receivedChecksum:X}, calc {calculatedChecksum:X}");
                return;
            }

            switch (command)
            {
                case 0xA0:
                    Console.WriteLine("KW82 ECU identification");
                    m_KW82ECUIdentification = Encoding.ASCII.GetString(payload);
                    break;
                case 0xA2:
                    KW82ParseDTC(payload);
                    break;
                default:
                    Console.WriteLine("KW82 unknown command");
                    break;
            }
        }

        private ushort CalculateChecksum(List<byte> data, int length)
        {
            ushort checksum = 0;
            for (int i = 0; i < length; i++)
            {
                checksum += data[i];
            }

            checksum = (ushort)((checksum >> 8) | (checksum << 8));
            return checksum;
        }

        private void KW82ParseDTC(byte[] payload)
        {
            lock (m_KW82BlockLock)
            {
                m_KW82DTCs.Clear();

                for (int i = 0; i < payload.Length - 1; ++i)
                {
                    if (payload[i] == 0xFF || payload[i] == 0)
                    {
                        continue;
                    }

                    m_KW82DTCs.Add(payload[i].ToString("X2"));
                }
            }
        }

        public void KW82ReadDTC()
        {
            List<byte> msg = new List<byte> { 0x02, 0x12, 0x00, 0x14 };
            lock (m_KW82WriteQueue)
            {            
                m_KW82WriteQueue.Enqueue(msg);
            }
        }

        public void KW82ReadIdentification()
        {
            List<byte> msg = new List<byte> { 0x02, 0x10, 0x00, 0x12 };
            lock (m_KW82WriteQueue)
            {
                m_KW82WriteQueue.Enqueue(msg);
            }
        }

    }

}

