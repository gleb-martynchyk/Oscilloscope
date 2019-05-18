using System;
using System.Diagnostics;
using System.IO;
using BECSLibrary.Transport;

namespace MeterFramework.Core.ControlProtocols
{
    public class RegistersProtocolW32R16
    {
        public RegistersProtocolW32R16()
        {
        }

        public RegistersProtocolW32R16(ITransport transport)
        {
            _Transport = transport;
        }

        #region == Transport ==================================================

        private ITransport _Transport = null;

        public ITransport Transport
        {
            get { return _Transport; }
            set
            {
                Debug.Assert(value != null);
                _Transport = value;
            }
        }

        #endregion == Transport ===============================================

        #region == Read/Write Registers - Подготовка данных ===================

        const int WriteBufferSize = 3 * 1460;
        byte[] _WriteBuffer = new byte[WriteBufferSize];
        int _WritePos = 0;

        public void PrepareWriteRequest(byte regNo, uint value)
        {
            _WriteBuffer[_WritePos++] = (byte)(0x80 + (regNo & 0x1Fu));
            _WriteBuffer[_WritePos++] = (byte)(value & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((value >> 7) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((value >> 14) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((value >> 21) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((value >> 28) & 0x4Fu);
        }

        public void PrepareWriteRequest(Register<uint> reg)
        {
            foreach (uint value in reg.Data)
                PrepareWriteRequest(reg.Number, value);
        }

        public void PrepareReadRequest(byte regNo, int count)
        {
            _WriteBuffer[_WritePos++] = (byte)(0xC0 + (regNo & 0x1Fu));
            _WriteBuffer[_WritePos++] = (byte)(count & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((count >> 7) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((count >> 14) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((count >> 21) & 0x7Fu);
            _WriteBuffer[_WritePos++] = (byte)((count >> 28) & 0x4Fu);
        }

        public void PrepareReadRequest(Register<ushort> reg)
        {
            PrepareReadRequest(reg.Number, reg.Size);
        }

        #endregion == Read/Write Registers - Подготовка данных ================

        #region == Read/Write Registers - Передача данных =====================

        public void SendRequest()
        {
            if (_Transport == null)
                throw new IOException("Ошибка записи/чтения данных: Транспорт не задан");
            if (_WritePos < 1)
                return;
            if (_WritePos < 5)
                throw new IOException("Ошибка записи/чтения данных: Запрос неверен или очередь запросов маленького размера");

            _Transport.Send(_WriteBuffer, 0, _WritePos);
            ClearRequest();
        }

        public void ClearRequest()
        { _WritePos = 0; }

        public void SendRequestAndReadRegister(Register<ushort> reg)
        {
            PrepareReadRequest(reg);
            SendRequestAndReadData(reg.Data);
        }

        public void SendRequestAndReadData(ushort[] result)
        {
            int size = result.Length;
            const int bufsize = 1460;
            byte[] buffer = new byte[bufsize];
            ushort[] header = new ushort[2];

            lock (_Transport.Locker)
            {
                _Transport.Send(_WriteBuffer, 0, _WritePos);
                ClearRequest();

                int pos = 0;
                ushort packNum = 0;
                bool firstPack = true;
                while (true)
                {
                    int count = _Transport.Read(buffer, 0, buffer.Length);
                    if (count < 5)//В начале каэждого пакета 0xBEEF, 0xNNNN где NNNN - номер пакета
                        throw new IOException("Ошибка записи/чтения данных");

                    #region < Проверяем заголовок >
                    Buffer.BlockCopy(buffer, 0, header, 0, 2 * sizeof(ushort));

                    if (firstPack)
                    {
                        packNum = header[1];
                        firstPack = false;
                    }

                    if (header[0] != 0xBEEF || header[1] != packNum)
                        throw new IOException("Ошибка записи/чтения данных");

                    packNum++;
                    #endregion < Проверяем заголовок >

                    if (count != bufsize && (count - 4) != (size - pos) * 2)
                        throw new IOException("Ошибка записи/чтения данных");

                    Buffer.BlockCopy(buffer, 4, result, pos * 2, count - 4);
                    pos += (count - 4) / 2;
                    if (pos == size)
                        break;
                }
            }
        }

        public void SendRequestAndReadPacket(ref ushort[] result)
        {
            int size = result.Length;
            const int bufsize = 1460;
            byte[] buffer = new byte[bufsize];
            ushort[] header = new ushort[2];

            lock (_Transport.Locker)
            {
                _Transport.Send(_WriteBuffer, 0, _WritePos);
                ClearRequest();

                ushort packNum = 0;

                int count = _Transport.Read(buffer, 0, buffer.Length);

                if (count < 5)//В начале каэждого пакета 0xBEEF, 0xNNNN где NNNN - номер пакета
                    throw new IOException("Ошибка записи/чтения данных");

                #region < Проверяем заголовок >
                Buffer.BlockCopy(buffer, 0, header, 0, 2 * sizeof(ushort));

                packNum = header[1];

                if (header[0] != 0xBEEF)
                    throw new IOException("Ошибка записи/чтения данных");

                #endregion < Проверяем заголовок >

                Debug.Assert((count - 4) % 2 == 0);

                Array.Resize(ref result, (count - 4) / 2);

                Buffer.BlockCopy(buffer, 4, result, 0, count - 4);
            }

        }

        #endregion == Read/Write Registers - Передача данных ==================
    }
}
