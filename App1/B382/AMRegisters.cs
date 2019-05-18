using System;
using System.Diagnostics;
using BECSLibrary;
using MeterFramework.Core.ControlProtocols;

namespace MeterFramework.AlmaMeter
{
    /// <summary>
    /// Control[4..0] (Stop, Start, Reset, и т.д.)
    /// </summary>
    public class W0Register : Register<uint>
    {
        public W0Register(Enum action)
            : base(0, 1)
        {
            Data[0] = Convert.ToUInt32(action);
        }

        public uint Action
        {
            get { return Data[0]; }
            set { Data[0] = value; }
        }
    }
    
    /// <summary>
    /// W10 - AT24LC16Ctrl Работа с EEPROM
    /// [7..0] – DataCnt. 0 == 256. Сами данные напихиваются в W15 (Для B-390: DataCnt < 129)
    /// [18..8] – Address (Для B-390:[16..8])
    /// [30] – StartWrite
    /// [31] – StartRead (перед чтением сделать Reset, чтобы очистить Test FIFO)
    /// </summary>
    public class W10Register : Register<uint>
    {
        public enum EnumEEPROMAction { Write = 0x1, Read = 0x2 };

        public W10Register(EnumEEPROMAction action,ushort dataSize,ushort address)
            : base(10, 1)
        {
            Action = action;
            DataSize = dataSize;
            Address = address;
        }

        public EnumEEPROMAction Action
        {
            get { return (EnumEEPROMAction)MathLib.GetBits(Data[0], 30, 2); }
            set { MathLib.SetBits(ref Data[0], 30, 2, (uint)value); }
        }

        
        public const ushort MinDataSize = 0x1;
        public const ushort MaxDataSize = 0x100;

        public ushort DataSize
        {
            get
            {
                ushort val = (ushort)MathLib.GetBits(Data[0], 0, 8);
                if (val == 0)
                    val = 256;
                return val;
            }
            set
            {
                Debug.Assert(value <= MaxDataSize && value >= MinDataSize, "W10: размер данных = 1..256");
                ushort val = MathLib.Bound(value, MinDataSize, MaxDataSize);
                if (val == MaxDataSize)
                    val = 0;
                MathLib.SetBits(ref Data[0], 0, 8, val);
            }
        }

        public const ushort MaxAddress = 0x7FF;

        public ushort Address
        {
            get { return (ushort)MathLib.GetBits(Data[0], 8, 11); }
            set
            {
                Debug.Assert(value <= MaxAddress, "W10: макс. адресс = 2047(0x7FF)");
                MathLib.SetBits(ref Data[0], 8, 11, value);
            }
        }
    }


    /// <summary>
    /// W12 – LockerID[31..0]  – используется для обозначения, что плата кем то залочена. 0 – никем
    /// </summary>
    public class W12Register : Register<uint>
    {
        public W12Register(uint lockData)
            : base(12, 1)
        {
            LockData = lockData;
        }
        public W12Register()
            : this(0)
        { }

        public uint LockData
        {
            get { return Data[0]; }
            set { Data[0] = value; }
        }
    }

    /// <summary>
    /// W14 – HUBCommData|[31..0]
    /// </summary>
    public class W14Register : Register<uint>
    {
        public W14Register(byte cmdCode, ushort cmdData, byte replyCode)
            : base(14, 1)
        {
            CMDCode = cmdCode;
            CMDData = cmdData;
            ReplyCode = replyCode;
        }

        public W14Register()
            : base(14, 1)
        {
            Data[0] = 0;
        }

        public byte CMDCode
        {
            get { return (byte)MathLib.GetBits(Data[0], 0, 8); }
            set { MathLib.SetBits(ref Data[0], 0, 8, value); }
        }

        public ushort CMDData
        {
            get { return (ushort)MathLib.GetBits(Data[0], 8, 16); }
            set { MathLib.SetBits(ref Data[0], 8, 16, value); }
        }

        public byte ReplyCode
        {
            get { return (byte)MathLib.GetBits(Data[0], 24, 8); }
            set { MathLib.SetBits(ref Data[0], 24, 8, value); }
        }
    }

    /// <summary>
    /// R4 – Status[7..0][15..0]
    /// </summary>
    public class R4RegisterBase : Register<ushort>
    {
        public R4RegisterBase()
            : base(4, 8)
        { }

        public bool MemIsEnd
        {
            get { return MathLib.CheckBit(Data[0], 0); }
        }

        public bool ReadMemBlockDone
        {
            get { return MathLib.CheckBit(Data[0], 1); }
        }

        public bool MeasStarted
        {
            get { return MathLib.CheckBit(Data[0], 15); }
        }

        public uint PreReg
        {
            get { return Data[1] + (uint)(Data[2] << 16); }
        }

        public bool EEPROMReady
        {
            get { return MathLib.CheckBit(Data[5], 1); }
        }
        public bool EEPROMStatus
        {
            get { return MathLib.CheckBit(Data[5], 0); }
        }

        public bool HUBQueryOK
        {
            get { return MathLib.CheckBit(Data[4], 15); }
        }
        public bool HUBQueryTimeout
        {
            get { return MathLib.CheckBit(Data[4], 14); }
        }

        public bool LoggerIsOverflow
        {
            get { return MathLib.CheckBit(Data[5], 3); }
        }

        public uint WritePtr
        {
            get { return (Data[7] + (MathLib.CheckBit(Data[5], 2) ? 0x10000u : 0u)) & 0x1FFFF; }
        }
    }
    
    /// <summary>
    /// R6 – InfoReg[5 x 16]
    /// </summary>
    public class R6Register : Register<ushort>
    {
        public R6Register()
            : base(6, 5)
        { }

        public uint LockData
        {
            get { return (uint)(Data[4] << 16) + Data[3]; }
        }

        public ulong TickOnStart
        {
            get
            {
                ulong tick = ((ulong)Data[2] << 32) + ((ulong)Data[1] << 16) + Data[0];
                return tick;
            }
        }
    }

    /// <summary>
    /// R14 – IDs[3 x 16]
    /// </summary>
    public class R14Register : Register<ushort>
    {
        public R14Register()
            : base(14, 3)
        { }

        public ushort FPGAVersion
        {
            get { return Data[0]; }
        }

        public ushort TypeID
        {
            get { return Data[1]; }
        }

        public ushort Serial
        {
            get { return Data[2]; }
        }

    }
}
