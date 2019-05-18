using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;


namespace BECSLibrary.Transport
{
    public class TCPIPTransport : ITransport
    {
        public TCPIPTransport()
        {
            Timeout = 600;
            _IPAddress = IPAddress.None;
            _Port = 0;
        }

        private readonly object _Locker = new object();
        public object Locker { get { return _Locker; } }

        private IPAddress _IPAddress;
        private uint _Port;

        public IPAddress IPAddress
        {
            get { return _IPAddress; }
            set
            {
                if (_IPAddress == value)
                    return;
                _IPAddress = value;
                Disconnect();
            }
        }

        public uint Port
        {
            get { return _Port; }
            set
            {
                if (_Port == value)
                    return;
                _Port = value;
                Disconnect();
            }
        }

        /// <summary>
        /// Максимальное время ожидание IO операций в мсек.
        /// </summary>
        public int Timeout { get; set; }

        #region == Socket =====================================================

        private Socket _Socket = null;

        private void SocketCreate()
        {
            //System.Diagnostics.Debug.Assert(_Socket == null, "Сокет уже создан");
            SocketDisconnect();
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void SocketConfigure()
        {
            System.Diagnostics.Debug.Assert(_Socket != null, "Сокет не создан");
            // Don't allow another socket to bind to this port.
            _Socket.ExclusiveAddressUse = true;

            // The socket not will linger for 1 seconds after Socket.Close is called.
            _Socket.LingerState = new LingerOption(false, 0);

            // Disable the Nagle Algorithm for this tcp socket.
            _Socket.NoDelay = true;

            // Set the receive buffer size to 8k
            _Socket.ReceiveBufferSize = 8192;

            // Set the timeout for synchronous receive methods to 
            // 1 second (1000 milliseconds.)
            _Socket.ReceiveTimeout = Timeout;

            // Set the send buffer size to 8k.
            _Socket.SendBufferSize = 8192;

            // Set the timeout for synchronous send methods
            // to 1 second (1000 milliseconds.)			
            _Socket.SendTimeout = Timeout;

            // Set the Time To Live (TTL) to 42 router hops.
            //tcpSocket.Ttl = 42;            
        }

        private void PingServer()
        {
            int pingTimeout = Timeout / 2;
            Ping pinger = new Ping();
            PingReply reply = pinger.Send(IPAddress, pingTimeout);
            bool pingAble = reply.Status == IPStatus.Success;
            if (!pingAble)
                throw new Exception("За время " + pingTimeout.ToString() + " мс. PingTest не выполнен");
        }

        private void SocketConnectExc()
        {
            if (SocketConnected)
            {
                return;
            }

            //Почему то этот метод некорректно работает?
            //PingServer();//on error throw exception

            SocketCreate();
            SocketConfigure();

            // Connect using a timeout  // Чтобы не ждать слишком долго (как это происходит по умолчанию)
            IAsyncResult asyncResult = _Socket.BeginConnect(IPAddress, (int)Port, null, null);
            bool success = asyncResult.AsyncWaitHandle.WaitOne(Timeout);
            if (!success)
            {
                _Socket.EndConnect(asyncResult);
                SocketDisconnect();
                throw new TimeoutException(string.Format("За время {0} мс. соединение не установлено", Timeout.ToString()));
            }
            _Socket.EndConnect(asyncResult);
        }

        private bool SocketConnect()
        {
            try
            {
                SocketConnectExc();
            }
            catch
            {
                //MessageLib.OutputException(exception);
                return false;
            }
            return _Socket.Connected;
        }

        /// <summary>
        /// Разрывает связь с Сокетом
        /// </summary>
        private void SocketDisconnect()
        {
            try
            {
                if (_Socket != null)
                {
                    _Socket.Shutdown(SocketShutdown.Both);
                    _Socket.Close();
                }
            }
            catch
            {
                //MessageLib.OutputException(exception);
            }

            _Socket = null;
        }

        static readonly byte[] emptyData = new byte[1];
        /// <summary>
        /// Определяет, установлено ли подключение 
        /// </summary>
        private bool SocketConnected
        {
            get
            {
                if (_Socket == null)
                    return false;
                return _Socket.Connected;
            }
        }

        private void SocketSend(byte[] data, int offset, int size)
        {
            int sended = _Socket.Send(data, offset, size, SocketFlags.None);
            if (sended != size)
                throw new IOException(string.Format("Ошибка передачи данных: Передано только {0} из {1}", sended, size));
        }

        private int SocketReceive(byte[] data, int offset, int count)
        {
            // Send data to the server.
            return _Socket.Receive(data, offset, count, SocketFlags.None);
        }

        #endregion == Socket ==================================================

        #region == ITransportProtocol Members =================================

        /// <summary>
        /// Определяет, установлено ли подключение 
        /// </summary>
        public bool Connected
        {
            get { lock (_Locker) { return SocketConnected; } }
        }

        public void Connect()
        {
            lock (_Locker) { SocketConnectExc(); }
        }

        public void Disconnect()
        {
            lock (_Locker) { SocketDisconnect(); }
        }

        public void Send(byte[] buffer, int offset, int size)
        {
            lock (_Locker) { SocketSend(buffer, offset, size); }
        }

        public int Read(byte[] buffer, int offset, int size)
        {
            lock (_Locker) { return SocketReceive(buffer, offset, size); }
        }

        public void DiscardInBuffer()
        {
            lock (_Locker)
            {
                byte[] dummy = null;
                while (_Socket.Available > 0) // free input buffer
                {
                    Array.Resize(ref dummy, 256);
                    SocketReceive(dummy, 0, Math.Min(dummy.Length, _Socket.Available));
                }
            }
        }
        #endregion == ITransportProtocol Members ==============================
    }
}
