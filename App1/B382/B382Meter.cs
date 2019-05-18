using System;
using System.Diagnostics;
using System.IO;
using BECSLibrary;
using BECSLibrary.Transport;
using MeterFramework.Core.ControlProtocols;

namespace MeterFramework.AlmaMeter
{
    /// <summary>
    /// Диапазоны токовых каналов
    /// </summary>
    public enum EnumB382ChRange { I_2mA = 1, I_2A = 2 };
    [Serializable]
    public class B382Meter : AMDevice
    {
        
        public B382Meter(ITransport transport)
            : base(transport)
        {
            DeviceType = EnumAMDeviceType.B385_Multimeter;
            DeviceID = AMHelper.PID(DeviceType);

            _EEPROMSize = 2048;
            _EEPROMBlockSize = 256;

           //_ADCCalibrations = new CalibrationCollection(ChannelCount, GainCount);
        }

        public const int ChannelCount = 4;

        #region == Калибровки АЦП =============================================

        private const int GainCount = 2;
        //private CalibrationCollection _ADCCalibrations;

        //public void ReadCalibrations()
        //{
        //    ushort address = 0x50;
        //    byte[] eeprom = ReadFromEEPROM(address, 0xFF);

        //    CalibrationCollection.EnumParseResult res = _ADCCalibrations.ParseV1(eeprom);
        //     if (res != CalibrationCollection.EnumParseResult.OK)
        //        throw new IOException("Ошибка чтения EEPROM: код " + res.ToString());
        //}

        //public Calibration GetCalibration(int channel, int gain)
        //{
        //    return _ADCCalibrations.Get(channel, gain);
        //}

#endregion == Калибровки АЦП ==========================================

        #region == УПРАВЛЕНИЕ =================================================

        #region < Регистр W0: Управление >

        public enum EnumAction { Reset = 0x1, Run = 0x2, StopLogger = 0x10 };

        static private W0Register _W0Reset = new W0Register(EnumAction.Reset);
        static private W0Register _W0Run = new W0Register(EnumAction.Run);
        static private W0Register _W0StopLogger = new W0Register(EnumAction.StopLogger);

        /// <summary>
        /// Сбросить
        /// </summary>
        /// <param name="flush">true - сразу послать команду устройству</param>
        public void Reset(bool flush)
        {
            DoAction(_W0Reset, flush);
        }

        /// <summary>
        /// Запустить
        /// </summary>
        /// <param name="flush">true - сразу послать команду устройству</param>
        public void Start(bool flush)
        {
            DoAction(_W0Run, flush);
        }

        /// <summary>
        /// Остановить самописец
        /// </summary>
        /// <param name="flush">true - сразу послать команду устройству</param>
        public void StopLogger(bool flush)
        {
            DoAction(_W0StopLogger, flush);
        }

        #endregion < Регистр W0: Управление >

        #region < Регистр W1: Настройки>

        /// <summary>
        /// Config[20..0]
        /// [19] – Disable Ch4
        /// [18] – Enable Ch4
        /// [17] – Disable Ch3
        /// [16] – Enable Ch3
        /// 
        /// [7] – Disable Logger Mode
        /// [6] – Enable Logger Mode
        /// [3] – Disable Ch2
        /// [2] – Enable Ch2
        /// [1] – Disable Ch1
        /// [0] – Enable Ch1
        /// </summary>
        class W1Register : Register<uint>
        {
            public W1Register()
                : base(1, 1)
            {
                Data[0] = 0;
            }

            static int[] ChEnablingBits = new int[] { 0, 2, 16, 18 };
            static int[] ChDisablingBits = new int[] { 1, 3, 17, 19 };

            public void SetChState(int ch, bool enable)
            {
                MathLib.SetBit(ref Data[0], ChEnablingBits[ch], enable);
                MathLib.SetBit(ref Data[0], ChDisablingBits[ch], !enable);
            }

            public bool GetChState(int ch)
            {
                return MathLib.CheckBit(Data[0], ChEnablingBits[ch]);
            }

            public bool LoggerMode
            {
                get { return MathLib.CheckBit(Data[0], 6); }
                set
                {
                    MathLib.SetBit(ref Data[0], 6, value);
                    MathLib.SetBit(ref Data[0], 7, !value);
                }
            }
        }

        public void SetChState(int ch, bool enable, bool flush)
        {
            W1Register w1 = new W1Register();
            w1.SetChState(ch, enable);
            _Protocol.PrepareWriteRequest(w1);
            if (flush)
                FlushProtocol();
        }

        public void SetChStates(bool ch1enable, bool ch2enable, bool ch3enable, bool ch4enable, bool flush)
        {
            W1Register w1 = new W1Register();

            w1.SetChState(0, ch1enable);
            w1.SetChState(1, ch2enable);
            w1.SetChState(2, ch3enable);
            w1.SetChState(3, ch4enable);

            _Protocol.PrepareWriteRequest(w1);
            if (flush)
                FlushProtocol();
        }

        public void SetMeasMode(bool loggerMode, bool flush)
        {
            W1Register w1 = new W1Register();
            w1.LoggerMode = loggerMode;
            _Protocol.PrepareWriteRequest(w1);
            if (flush)
                FlushProtocol();
        }

        #endregion < Регистр W1: Настройки>

        #region < Регистр W3: дискретизация >

        public override double SamplingPeriodStep
        {
            get { return 1e-8; }
        }

        public override uint SamplingPeriodMinCode
        {
            get { return 1000; }
        }

        public override uint SamplingPeriodMaxCode
        {
            get { return uint.MaxValue; }
        }
        #endregion < Регистр W3: дискретизация >

        #region < Регистр W4 и W5: Сегмент >
        /// <summary>
        ///W4 – WorkLen[31..0]. –  С учетом постыстории. Длина полезного сигнала – [12..0]
        /// </summary>
        class W4Register : Register<uint>
        {
            public W4Register(uint measLength)
                : base(4, 1)
            {
                MeasLength = measLength;
            }
            public W4Register()
                : this(100)
            { }

            public const uint MaxFrameSize = (1 << (12 - 2)) - 1;// = (1<<24)/4ch

            public uint MeasLength
            {
                get { return Data[0]; }
                set { Data[0] = value; }
            }
        }
        /// <summary>
        /// W5 – Количество данных предыстории по каждому каналу
        /// </summary>
        class W5Register : Register<uint>
        {
            public W5Register(uint history)
                : base(5, 1)
            {
                History = history;
            }
            public W5Register()
                : this(0)
            { }

            public uint History
            {
                get { return Data[0]; }
                set { Data[0] = value; }
            }
        }

        public void SetSegment(int history, int size, bool flush)
        {
            uint usize = (uint)size;
            if (usize > W4Register.MaxFrameSize)
            {
                Debug.Fail("W4W5: мax. знач. = 1^12/(Число каналов)-1");
                usize = W4Register.MaxFrameSize;
            }

            W5Register w5 = new W5Register(0);
            W4Register w4 = new W4Register();
            if (history < 0)
            {
                w5.History = (uint)Math.Abs(history);
                if (w5.History > W4Register.MaxFrameSize)
                {
                    Debug.Fail("W4W5: мax. знач. = 1^12/(Число каналов)-1");
                    w5.History = W4Register.MaxFrameSize;
                }

                w4.MeasLength = Math.Min(0, usize - w5.History);
            }
            else
                w4.MeasLength = usize + (uint)history;

            _Protocol.PrepareWriteRequest(w4);
            _Protocol.PrepareWriteRequest(w5);

            if (flush)
                FlushProtocol();
        }

        #endregion < Регистр W4 и W5: Сегмент >

        #region < Регистр W6: Работа с памятью >

        /// <summary>
        /// W6 – LoadMC[31..0] – Адрес блока памяти для чтения 
        /// [31] – PointerSelect. 
        /// Если 0, то пишется адрес , с которого начнется чтение (как и было раньше).
        /// Если 1, то пишется указатель чтения, соответствующий количеству удачно вычитанных данных. Используется для определения момента переполнения памяти в режиме самописца.
        /// [11..0] – Адрес. Чтение инициировать больше не надо.
        /// </summary>
        class W6Register : Register<uint>
        {
            public W6Register()
                : base(6, 1)
            {
                //Address = address;
            }

            public void SetReadAddress(uint address)
            {
                MathLib.SetBits(ref Data[0], 0, 12, address);
                MathLib.SetBit(ref Data[0], 31, false);
            }

            public void SetReadPtr(uint address)
            {
                MathLib.SetBits(ref Data[0], 0, 12, address);
                MathLib.SetBit(ref Data[0], 31, true);
            }

            public bool IsReadAddress { get { return !MathLib.CheckBit(Data[0], 31); } }
            public bool IsReadPtr { get { return MathLib.CheckBit(Data[0], 31); } }
            public uint Address { get { return MathLib.GetBits(Data[0], 0, 12); } }
        }

        public void SetReadAddress(uint address, bool flush)
        {
            W6Register w6 = new W6Register();
            w6.SetReadAddress(address);

            _Protocol.PrepareWriteRequest(w6);

            if (flush)
                FlushProtocol();
        }

        public void PrepareForReading(uint address)
        {
            // Указываем адрес, откуда будем читать
            SetReadAddress((uint)address, false);

            R4RegisterBase r4;
            double maxTryTime = 0.3; //сек
            DateTime startTime = DateTime.Now;

            while (true)
            {
                r4 = GetStatus();

                if (r4.ReadMemBlockDone)
                    break;

                if ((DateTime.Now - startTime).TotalSeconds > maxTryTime)
                    throw new IOException("Превышено время ожидания завершения операции");
            }
        }

        public void SetReadPtr(uint address, bool flush)
        {
            W6Register w6 = new W6Register();
            w6.SetReadPtr(address);

            _Protocol.PrepareWriteRequest(w6);

            if (flush)
                FlushProtocol();
        }

        #endregion < Регистр W6: Работа с памятью >

        #region < Регистр W8: Диапазон канала >

        /// <summary>
        /// W8 – CurrentRange:
        // [0] – Set Ch3 range to 1mA 
        // [1] – Set Ch3 range to 1A
        // [2] – Set Ch4 range to 1mA 
        // [3] – Set Ch4 range to 1A

        /// </summary>
        public class W8Register : Register<uint>
        {
            public W8Register()
                : base(8, 1)
            {
                SetChRange(2, EnumB382ChRange.I_2A);
                SetChRange(3, EnumB382ChRange.I_2A);
            }

            public EnumB382ChRange GetChRange(int ch)
            {
                ch -= 2;
                if (ch < 0 || ch > 1)
                    throw new ArgumentOutOfRangeException("ch");

                return (EnumB382ChRange)MathLib.GetBits(Data[0], 2 * ch, 2);
            }

            public void SetChRange(int ch, EnumB382ChRange value)
            {
                ch -= 2;
                if (ch < 0 || ch > 1)
                    throw new ArgumentOutOfRangeException("ch");

                MathLib.SetBits(ref Data[0], 2 * ch, 2, (uint)value);
            }
        }

        public void SetChRange(EnumB382ChRange ch3Range, EnumB382ChRange ch4Range, bool flush)
        {
            W8Register w8 = new W8Register();

            w8.SetChRange(2,ch3Range);
            w8.SetChRange(3,ch4Range);

            _Protocol.PrepareWriteRequest(w8);
            if (flush)
                FlushProtocol();
        }

        #endregion < Регистр W2: Режим работы канала >

        #region < Регистр W9: Синхронизация>
        /// <summary>
        /// W9 - SyncCtrl
        /// [0] – Normal Mode
        /// [1] – Rising Edge
        /// [2] – FromHUB (0 – from channels)
        /// [4..3] – Channel Select
        /// [15...8] – Average count
        /// [31..16] – Threshold (in codes)
        /// </summary>
        public class W9Register : Register<uint>
        {
            public W9Register()
                : base(9, 1)
            {
                Data[0] = 0;
                NormalMode = true;
                ByPositiveEdge = true;
                SynchFromHUB = false;
                SynchFromChannel = 0;
                FilterSize = 1;
                Threshold = ushort.MaxValue / 2;
            }

            public bool NormalMode
            {
                get { return MathLib.CheckBit(Data[0], 0); }
                set { MathLib.SetBit(ref Data[0], 0, value); }
            }

            public bool ByPositiveEdge
            {
                get { return MathLib.CheckBit(Data[0], 1); }
                set { MathLib.SetBit(ref Data[0], 1, value); }
            }

            public bool SynchFromHUB
            {
                get { return MathLib.CheckBit(Data[0], 2); }
                set { MathLib.SetBit(ref Data[0], 2, value); }
            }

            public const uint FirstChannel = 0;
            public const uint LastChannel = 3;

            public uint SynchFromChannel
            {
                get { return MathLib.GetBits(Data[0], 3, 2); }
                set
                {
                    uint fixValue = value;
                    if (value > LastChannel)
                    {
                        Debug.Fail("W9: Каналы 0,1,2,3");
                        fixValue = MathLib.Bound(value, FirstChannel, LastChannel);
                    }
                    MathLib.SetBits(ref Data[0], 3, 2, fixValue);
                }
            }

            public byte FilterSize
            {
                get { return (byte)MathLib.GetBits(Data[0], 8, 8); }
                set { MathLib.SetBits(ref Data[0], 8, 8, value); }
            }

            public ushort Threshold
            {
                get { return (ushort)MathLib.GetBits(Data[0], 16, 16); }
                set { MathLib.SetBits(ref Data[0], 16, 16, value); }
            }
        }

        public void SetSynchSettings(bool normalMode, bool positiveEdge, bool fromHUB, uint ch, byte filterSize, ushort threshold, bool flush)
        {
            W9Register w9 = new W9Register();
            w9.NormalMode = normalMode;
            w9.ByPositiveEdge = positiveEdge;
            w9.SynchFromHUB = fromHUB;
            w9.SynchFromChannel = ch;
            w9.FilterSize = filterSize;
            w9.Threshold = threshold;
            _Protocol.PrepareWriteRequest(w9);
            if (flush)
                FlushProtocol();
        }


        #endregion < Регистр W9: Синхронизация>

        #region < Регистр R0: Данные >

        public void GetData(ushort[] buffer)
        {
            Register<ushort> r0 = new Register<ushort>(0, buffer);
            _Protocol.SendRequestAndReadRegister(r0);
        }

        public ushort[] GetData(int count)
        {
            ushort[] buffer = new ushort[count];
            GetData(buffer);
            return buffer;
        }

        #endregion < Регистр R0: Данные >

        #region < Регистр R4: Информация о текущем статусе устройства >

        public R4RegisterBase GetStatus()
        {
            R4RegisterBase res = new R4RegisterBase();
            _Protocol.SendRequestAndReadRegister(res);
            return res;
        }

        #endregion < Регистр R4: Информация о текущем статусе устройства >


        #endregion == УПРАВЛЕНИЕ ==============================================
    }
}
