using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BECSLibrary;
using BECSLibrary.Transport;
using MeterFramework.Core.ControlProtocols;

namespace MeterFramework.AlmaMeter
{
    public class AMDevice
    {
        public AMDevice(ITransport transport)
        {
            #region /Проверка аргументов/
            if (transport == null)
                throw new ArgumentNullException("transport");
            #endregion /Проверка аргументов/
            _Protocol = new RegistersProtocolW32R16(transport);
        }

        protected RegistersProtocolW32R16 _Protocol;
        public EnumAMDeviceType DeviceType { get; protected set; }
        public ushort DeviceID { get; protected set; }

        #region == EEPROM =====================================================

        protected int _EEPROMSize = 0;
        protected int _EEPROMBlockSize = 0;
        public int EEPROMSize { get { return _EEPROMSize; } }

        public byte[] ReadFromEEPROM(ushort address, int count)
        {
            #region /Проверка аргументов/
            Debug.Assert(_EEPROMBlockSize > 0 && _EEPROMSize > _EEPROMBlockSize, "Переменные EEPROM должны быть инициализированы");

            if (_Protocol.Transport == null)
                throw new ArgumentNullException("protocol", "Не задан транспорт для протокола");

            if (!_Protocol.Transport.Connected)
                throw new IOException("Связь с устройством не установлена");

            if (address >= _EEPROMSize)
                throw new ArgumentOutOfRangeException("address");

            if (count < 1 || (address + count) > _EEPROMSize)
                throw new ArgumentOutOfRangeException("count");
            #endregion /Проверка аргументов/

            byte[] result = new byte[count];

            int readCount = count / _EEPROMBlockSize; // Читать можно только кусками <= 256(0x100)
            int remainBytes = count % _EEPROMBlockSize;

            int start = 0;
            bool resetFIFO = true;
            for (int i = 0; i < readCount; i++)
            {
                ReadFromEEPROM(address, _EEPROMBlockSize, result, start, resetFIFO);
                if (DeviceType != EnumAMDeviceType.B390_Thermo)
                    resetFIFO = false;
                start += _EEPROMBlockSize;
                address += (ushort)_EEPROMBlockSize;
            }

            if (remainBytes > 0)
                ReadFromEEPROM(address, remainBytes, result, start, resetFIFO);

            return result;
        }

        private void ReadFromEEPROM(ushort address, int count, byte[] dest, int start, bool resetFIFO)
        {
            #region /Проверка аргументов/
            if (_Protocol.Transport == null)
                throw new ArgumentNullException("protocol", "Не задан транспорт для протокола");

            if (!_Protocol.Transport.Connected)
                throw new IOException("Связь с устройством не установлена");

            if (address >= _EEPROMSize)
                throw new ArgumentOutOfRangeException("address");

            if (dest == null)
                throw new ArgumentNullException("dest");

            if (start < 0 || start >= dest.Length)
                throw new ArgumentOutOfRangeException("start");

            if (count < 1 || count > _EEPROMBlockSize || (address + count) > _EEPROMSize || (start + count) > dest.Length)
                throw new ArgumentOutOfRangeException("count");
            #endregion /Проверка аргументов/

            if (resetFIFO)
            {
                // сбрасываем FIFO
                DoAction(EnumAction.Reset, false);
            }

            //конфигурируем доступ к eeprom
            W10Register w10 = new W10Register(W10Register.EnumEEPROMAction.Read, (ushort)count, address);
            _Protocol.PrepareWriteRequest(w10);

            R4RegisterBase r4 = new R4RegisterBase();

            double maxTryTime = 1; //сек
            DateTime startTime = DateTime.Now;
            while (true)
            {
                _Protocol.SendRequestAndReadRegister(r4);

                if ((DateTime.Now - startTime).TotalSeconds > maxTryTime)
                    throw new IOException("Превышено время ожидания завершения операции");

                if (!r4.EEPROMStatus)
                    throw new IOException("Ошибка записи/чтения EEPROM");

                if (r4.EEPROMReady)
                    break;
            }

            Register<ushort> r15 = new Register<ushort>(15, _EEPROMBlockSize);
            _Protocol.SendRequestAndReadRegister(r15);

            for (int i = 0; i < count; i++)
                dest[start + i] = (byte)r15.Data[i];
        }

        public void WriteToEEPROM(byte[] src, int count, ushort address)
        {
            #region /Проверка аргументов/
            Debug.Assert(_EEPROMBlockSize > 0 && _EEPROMSize > _EEPROMBlockSize, "Переменные EEPROM должны быть инициализированы");

            if (_Protocol.Transport == null)
                throw new ArgumentNullException("protocol", "Не задан транспорт для протокола");

            if (!_Protocol.Transport.Connected)
                throw new IOException("Связь с устройством не установлена");

            if (src == null)
                throw new ArgumentNullException("src");

            if (address >= _EEPROMSize)
                throw new ArgumentOutOfRangeException("address");

            if (count > src.Length)
                throw new ArgumentOutOfRangeException("count");

            if (count < 1 || (address + count) > _EEPROMSize)
                throw new ArgumentOutOfRangeException("count");
            #endregion /Проверка аргументов/

            int writeCount = count / _EEPROMBlockSize;
            int remainBytes = count % _EEPROMBlockSize;

            int start = 0;
            bool resetFIFO = true;
            for (int i = 0; i < writeCount; i++)
            {
                WriteToEEPROM(src, start, _EEPROMBlockSize, address, resetFIFO);
                if (DeviceType != EnumAMDeviceType.B390_Thermo)
                    resetFIFO = false;
                start += _EEPROMBlockSize;
                address += (ushort)_EEPROMBlockSize;
            }

            if (remainBytes > 0)
                WriteToEEPROM(src, start, remainBytes, address, resetFIFO);
        }

        public void WriteToEEPROM(byte[] src, ushort address)
        {
            WriteToEEPROM(src, src.Length, address);
        }

        private void WriteToEEPROM(byte[] src, int start, int count, ushort address, bool resetFIFO)
        {
            #region /Проверка аргументов/
            if (_Protocol.Transport == null)
                throw new ArgumentNullException("protocol", "Не задан транспорт для протокола");

            if (!_Protocol.Transport.Connected)
                throw new IOException("Связь с устройством не установлена");

            if (src == null)
                throw new ArgumentNullException("src");

            if (start < 0 || start >= src.Length)
                throw new ArgumentOutOfRangeException("start");

            if (address >= _EEPROMSize)
                throw new ArgumentOutOfRangeException("address");

            if (count < 1 || count > _EEPROMBlockSize || (address + count) > _EEPROMSize || (start + count) > src.Length)
                throw new ArgumentOutOfRangeException("count");
            #endregion /Проверка аргументов/

            _Protocol.ClearRequest();

            if (resetFIFO)
            {
                // сбрасываем FIFO
                DoAction(EnumAction.Reset, false);
            }

            Register<uint> w15 = new Register<uint>(15, _EEPROMBlockSize / 2); //2 ushort в 1 uint 

            for (int i = 0, j = 0; i < count; i += 2, j++)
            {
                uint val = src[start + i];
                if (i + 1 < count)
                    val += (uint)(src[start + i + 1] << 16);
                w15.Data[j] = val;
            }

            _Protocol.PrepareWriteRequest(w15);

            //конфигурируем доступ к eeprom
            W10Register w10 = new W10Register(W10Register.EnumEEPROMAction.Write, (ushort)count, address);
            _Protocol.PrepareWriteRequest(w10);

            R4RegisterBase r4 = new R4RegisterBase();

            double maxTryTime = 1; //сек
            DateTime startTime = DateTime.Now;
            while (true)
            {
                _Protocol.SendRequestAndReadRegister(r4);

                if ((DateTime.Now - startTime).TotalSeconds > maxTryTime)
                    throw new IOException("Превышено время ожидания завершения операции");

                if (!r4.EEPROMStatus)
                    throw new IOException("Ошибка записи/чтения EEPROM");

                if (r4.EEPROMReady)
                    break;
                Thread.Sleep(100);
            }
        }

        #endregion == EEPROM ==================================================


        #region == Работа с Хабом =============================================

        private EnumAMDeviceType[] GetHUBInfo()
        {
            SendCommandToHUB(0x04, 0x00, 0x01);
            Register<ushort> r3 = new Register<ushort>(3, 2);
            _Protocol.SendRequestAndReadRegister(r3);

            EnumAMDeviceType[] res = new EnumAMDeviceType[6];

            uint data = (uint)(r3.Data[1] << 16) + r3.Data[0];

            for (int i = 0; i < res.Length; i++)
                res[i] = (EnumAMDeviceType)((data >> (i * 4)) & 0xF);

            return res;
        }

        private void SendCommandToHUB(byte cmdCode, ushort cmdData, byte replyCode)
        {
            #region /Проверка аргументов/
            if (_Protocol.Transport == null)
                throw new ArgumentNullException("protocol", "Не задан транспорт для протокола");

            if (!_Protocol.Transport.Connected)
                throw new IOException("Связь с устройством не установлена");
            #endregion /Проверка аргументов/

            W14Register w14 = new W14Register(cmdCode, cmdData, replyCode);
            _Protocol.PrepareWriteRequest(w14);

            R4RegisterBase r4 = new R4RegisterBase();
            double maxTryTime = 3; //сек
            DateTime startTime = DateTime.Now;
            while (true)
            {
                _Protocol.SendRequestAndReadRegister(r4);

                if ((DateTime.Now - startTime).TotalSeconds > maxTryTime)
                    throw new IOException("Превышено время ожидания завершения операции");

                if (r4.HUBQueryTimeout)
                    throw new IOException("Ошибка записи/чтения EEPROM");

                if (r4.HUBQueryOK)
                    break;
            }
        }

        #endregion == Работа с Хабом ==========================================

        #region == УПРАВЛЕНИЕ =================================================

        public void ClearProtocol()
        {
            _Protocol.ClearRequest();
        }

        public void FlushProtocol()
        {
            _Protocol.SendRequest();
        }

        #region < Регистр R14: Индентификация устройства >

        static public R14Register GetIDs(ITransport transport)
        {
            #region /Проверка аргументов/
            if (transport == null)
                throw new ArgumentNullException("transport");
            #endregion /Проверка аргументов/

            RegistersProtocolW32R16 protocol = new RegistersProtocolW32R16(transport);

            protocol.ClearRequest();

            R14Register res = new R14Register();
            protocol.SendRequestAndReadRegister(res);

            return res;
        }

        public R14Register GetIDs()
        {
            R14Register res = new R14Register();
            _Protocol.SendRequestAndReadRegister(res);
            return res;
        }

        #endregion < Регистр R14: Индентификация устройства >

        #region < Регистр R6: Информация об текущем состоянии устройства >

        public R6Register GetInfo()
        {
            R6Register res = new R6Register();
            _Protocol.SendRequestAndReadRegister(res);
            return res;
        }

        #endregion < Регистр R6: Информация об текущем состоянии устройства >

        #region < Регистр W12: Управление >

        //W12 – LockerID[31..0]

        public void SetLockData(uint lockData, bool flush)
        {
            W12Register w12 = new W12Register(lockData);
            _Protocol.PrepareWriteRequest(w12);
            if (flush)
                FlushProtocol();
        }

        #endregion < Регистр W12: Управление >

        #region < Регистр W0: Управление >

        private enum EnumAction { Reset = 0x1 };

        protected void DoAction(Enum action, bool flush)
        {
            DoAction(new W0Register(action), flush);
        }

        protected void DoAction(W0Register action, bool flush)
        {
            _Protocol.PrepareWriteRequest(action);
            if (flush)
                _Protocol.SendRequest();
        }

        #endregion < Регистр W0: Управление >

        #region < Регистр W3: дискретизация >

        /// <summary>
        /// W3 – Период дискретизации в милисеундах
        /// </summary>
        class W3Register : Register<uint>
        {
            public W3Register(uint timeBase)
                : base(3, 1)
            {
                TimeBase = timeBase;
            }
            public W3Register()
                : this(10)
            { }

            public uint TimeBase
            {
                get { return Data[0]; }
                set { Data[0] = value; }
            }
        }

        public virtual double SamplingPeriodStep { get { throw new NotImplementedException(); } }
        public virtual uint SamplingPeriodMaxCode { get { throw new NotImplementedException(); } }
        public virtual uint SamplingPeriodMinCode { get { throw new NotImplementedException(); } }

        protected uint SamplingPeriodToCode(double period)
        {
            uint code = (uint)Math.Round(period / SamplingPeriodStep, MidpointRounding.AwayFromZero);
            code = MathLib.Bound(code, SamplingPeriodMinCode, SamplingPeriodMaxCode);
            return code;
        }

        /// <summary>
        /// Устанавливает период дискретизации
        /// </summary>
        /// <param name="period">период дискретизации</param>
        /// <param name="flush">true - сразу послать команду устройству</param>
        /// <returns>Установленное значение (из-за дискретизации может отличаться)</returns>
        /// <remarks>Для измерителя температуры: ждать 1 период перед вычиткой данных(Но не меньше 400 мс) надо наверху</remarks>
        public double SetSamplingPeriod(double period, bool flush)
        {
            uint code = SamplingPeriodToCode(period);

            SetSamplingPeriod(code);

            if (flush)
                FlushProtocol();

            return code * SamplingPeriodStep;
        }

        protected virtual void SetSamplingPeriod(uint code)
        {
            W3Register w3 = new W3Register(code);
            _Protocol.PrepareWriteRequest(w3);
        }

        #endregion < Регистр W3: дискретизация >

        #endregion == УПРАВЛЕНИЕ ==============================================
    }
}
