namespace MeterFramework.AlmaMeter
{

    /// <summary>
    /// Тип устройств, которые могут подключаться к хабу.
    /// </summary>
    public enum EnumAMDeviceType : byte
    {
        /// <summary>
        /// Устройство не установлено
        /// </summary>
        No = 0,

        /// <summary>
        /// Осциллограф цифровой В-320 
        /// </summary>
        B320_Oscilloscope = 1,

        /// <summary>
        /// Генератора сигналов произвольной формы В-330 
        /// </summary>
        B330_AFG = 2,

        /// <summary>
        /// Анализатор-генератор цифровых сигналов В-340 
        /// </summary>
        B340_LAG = 3,

        /// <summary>
        /// Аналогово-цифровой преобразователь В-380 
        /// </summary>
        B380_ADC = 4,

        /// <summary>
        /// Аналогово-цифровой преобразователь В-360-S (подключение сигналов от полномостовых и полумостовых схем включения)
        /// </summary>
        B360_Strain = 5,

        /// <summary>
        /// Аналогово-цифровой преобразователь В-360-I (датчики типа ICP (пьезоэлектрические акселерометры, микрофоны))
        /// </summary>
        B361_ICP = 6,
        
        /// <summary>
        /// Датчик температуры
        /// </summary>
        B390_Thermo = 7,

        /// <summary>
        /// Мультиметр-АЦП B-382 (Для измерения напряжения и тока: 2*V(напряжение), 2*I(ток))
        /// </summary>
        B382_Multimeter = 8,
        
        /// <summary>
        /// Самописец для Могилёвлифтмаш
        /// </summary>
        B362 = 9,

        /// <summary>
        /// Мультиметр-АЦП B-385 (Аналог B-382+USB)
        /// </summary>
        B385_Multimeter = 10,

        /// <summary>
        /// Неизвестное устройство
        /// </summary>
        Unknown = 11
    };    

    public class AMHelper
    {
        static readonly ushort[] _PIDs = new ushort[]{0,0x320,0x330,0x340,0x380,0x360,0x361,0x390,0x382,0x362,0x385,0};
        public static ushort PID(EnumAMDeviceType devType)
        {
            return _PIDs[(int)devType];
        }

        public static EnumAMDeviceType DeviceType(ushort pid)
        {
            for(int i=1;i<=10;i++)
                if(pid==_PIDs[i])
                    return (EnumAMDeviceType)i;
            return EnumAMDeviceType.Unknown;
        }
    }
}
