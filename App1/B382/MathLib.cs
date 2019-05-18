using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BECSLibrary
{
    public static class MathLib
    {
        #region == Bound ======================================================

        public static int Bound(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static uint Bound(uint value, uint min, uint max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static double Bound(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static float Bound(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static decimal Bound(decimal value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static short Bound(short value, short min, short max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static ushort Bound(ushort value, ushort min, ushort max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static byte Bound(byte value, byte min, byte max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static long Bound(long value, long min, long max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        #endregion == Bound ===================================================

        #region == Round ======================================================

        public static byte RoundToByte(double val)
        {
            return (byte)Math.Round(val, MidpointRounding.AwayFromZero);
        }

        public static int RoundToInt32(double val)
        {
            return (int)Math.Round(val, MidpointRounding.AwayFromZero);
        }

        #endregion == Round ===================================================

        public static void Swap<T>(ref T value1, ref T value2)
        {
            T temp = value1;
            value1 = value2;
            value2 = temp;
        }

        #region == Interpolate ================================================

        public static double Interpolate(double x, double x1, double val1, double x2, double val2)
        {
#if DEBUG
            #region /Проверка аргументов/
            if ((x2 - x1) == 0.0)
                throw new ArgumentException("x2-x1 = 0");
            #endregion
#endif

            return val1 + (x - x1) * (val2 - val1) / (x2 - x1);
        }

        public static float Interpolate(float x, float x1, float val1, float x2, float val2)
        {
#if DEBUG
            #region /Проверка аргументов/
            if ((x2 - x1) == 0.0)
                throw new ArgumentException("x2-x1 = 0");
            #endregion
#endif

            return val1 + (x - x1) * (val2 - val1) / (x2 - x1);
        }       

        public static double Interpolate(double x, double[] arrayX, double[] arrayY)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayX == null || arrayX.Length < 2)
                throw new ArgumentNullException("arrayX");
            if (arrayY == null || arrayY.Length < 2)
                throw new ArgumentNullException("arrayY");
            if (arrayX.Length != arrayY.Length)
                throw new ArgumentException("arrayX.Length != arrayY.Length", "arrayX,arrayY");
            #endregion
#endif
            int pos = Array.BinarySearch<double>(arrayX, x);

            if (pos >= 0)
                return arrayY[pos];

            //pos = ~pos;
            //if (pos == 0)// x находится в начале массива, скорректируем pos - возмем первую пару чисел
            //    pos++;
            //else if (pos == arrayX.Length)// x находится в конце массива, скорректируем pos - возмем последнюю пару чисел
            //    pos--;

            pos = Bound(~pos, 1, arrayY.Length - 1);
            return Interpolate(x, arrayX[pos - 1], arrayY[pos - 1], arrayX[pos], arrayY[pos]);
        }

        public static float Interpolate(float x, float[] arrayX, float[] arrayY)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayX == null || arrayX.Length < 2)
                throw new ArgumentNullException("arrayX");
            if (arrayY == null || arrayY.Length < 2)
                throw new ArgumentNullException("arrayY");
            if (arrayX.Length != arrayY.Length)
                throw new ArgumentException("arrayX.Length != arrayY.Length", "arrayX,arrayY");
            #endregion
#endif
            int pos = Array.BinarySearch<float>(arrayX, x);

            if (pos >= 0)
                return arrayY[pos];

            //pos = ~pos;
            //if (pos == 0)// x находится в начале массива, скорректируем pos - возмем первую пару чисел
            //    pos++;
            //else if (pos == arrayX.Length)// x находится в конце массива, скорректируем pos - возмем последнюю пару чисел
            //    pos--;

            pos = Bound(~pos, 1, arrayY.Length - 1);
            return Interpolate(x, arrayX[pos - 1], arrayY[pos - 1], arrayX[pos], arrayY[pos]);
        }

        public static double Interpolate(double x, double xmin, double xmax, double[] arrayY)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayY == null || arrayY.Length < 1)
                throw new ArgumentNullException("arrayY");
            if (xmin >= xmax)
                throw new ArgumentException("xmin >= xmax", "xmin,xmax");
            #endregion
#endif
            if (arrayY.Length == 1)
                return arrayY[0];

            double dx = (xmax - xmin) / (double)(arrayY.Length - 1);

            int pos = (int)((x - xmin) / dx) + 1;

            pos = Bound(pos, 1, arrayY.Length - 1);
            return Interpolate(x, xmin + dx * (pos - 1), arrayY[pos - 1], xmin + dx * pos, arrayY[pos]);
        }

        public static float Interpolate(float x, float xmin, float xmax, float[] arrayY)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayY == null || arrayY.Length < 1)
                throw new ArgumentNullException("arrayY");
            if (xmin >= xmax)
                throw new ArgumentException("xmin >= xmax", "xmin,xmax");
            #endregion
#endif
            if (arrayY.Length == 1)
                return arrayY[0];

            float dx = (xmax - xmin) / (float)(arrayY.Length - 1);

            int pos = (int)((x - xmin) / dx) + 1;

            pos = Bound(pos, 1, arrayY.Length - 1);
            return Interpolate(x, xmin + dx * (pos - 1), arrayY[pos - 1], xmin + dx * pos, arrayY[pos]);
        }

        public static double Interpolate(double x, double[] arrayX, double ymin, double ymax)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayX == null || arrayX.Length < 2)
                throw new ArgumentNullException("arrayX");
            if (ymin >= ymax)
                throw new ArgumentException("ymin >= ymax", "ymin,ymax");
            #endregion
#endif
            double dy = (ymax - ymin) / (double)(arrayX.Length - 1);

            int pos = Array.BinarySearch<double>(arrayX, x);

            if (pos >= 0)
                return ymin + dy * pos;

            pos = Bound(~pos, 1, arrayX.Length - 1);
            return Interpolate(x, arrayX[pos - 1], ymin + dy * (pos - 1), arrayX[pos], ymin + dy * pos);
        }

        public static float Interpolate(float x, float[] arrayX, float ymin, float ymax)
        {
#if DEBUG
            #region /Проверка аргументов/
            if (arrayX == null || arrayX.Length < 2)
                throw new ArgumentNullException("arrayX");
            if (ymin >= ymax)
                throw new ArgumentException("ymin >= ymax", "ymin,ymax");
            #endregion
#endif
            float dy = (ymax - ymin) / (float)(arrayX.Length - 1);

            int pos = Array.BinarySearch<float>(arrayX, x);

            if (pos >= 0)
                return ymin + dy * pos;

            pos = Bound(~pos, 1, arrayX.Length - 1);
            return Interpolate(x, arrayX[pos - 1], ymin + dy * (pos - 1), arrayX[pos], ymin + dy * pos);
        }

        #endregion == Interpolate =============================================

        public static bool AlmostEquals(double left, double right, int bitCount)
        {
            long leftAsBits = ToBits2Complement(left);
            long rightAsBits = ToBits2Complement(right);
            long floatingPointRepresentationsDiff = Math.Abs(leftAsBits - rightAsBits);
            //return (floatingPointRepresentationsDiff <= representationTolerance); 
            return (floatingPointRepresentationsDiff <= (1L << bitCount));
        }

        public static long ToBits2Complement(double value)
        {
            /*long valueAsLong = *(long*)&value;
            if (valueAsLong < 0)
                valueAsLong = (long)(0x8000000000000000 - (ulong)valueAsLong);
                
            return valueAsLong;*/
            return 0;
        }

        public static void SetBit(ref uint value, int bit, bool bitValue)
        {
            uint mask = 1u << bit;
            if (bitValue)
                value |= mask;
            else
                value &= ~mask;
        }

        public static bool CheckBit(uint value, int bit)
        {
            return ((value >> bit) & 1u) == 1u;
        }

        /// <summary>
        /// Возвращает указанные биты в числе
        /// </summary>
        /// <param name="value">число</param>
        /// <param name="start">позиция первого бита</param>
        /// <param name="count">кол-во бит</param>
        /// <returns>биты</returns>
        public static uint GetBits(uint value, int start, int count)
        {
            #region /Проверка аргументов/
            if (start < 0 || start > 31)
                throw new ArgumentOutOfRangeException("start");
            if (count < 1 || start + count > 32)
                throw new ArgumentOutOfRangeException("count");
            #endregion

            if (count > 31)
                return value;

            uint mask = ((1u << count) - 1);
            return (value >> start) & mask;
        }

        /// <summary>
        /// Устанавливает указанные биты в новое значение
        /// </summary>
        /// <param name="value">число</param>
        /// <param name="start">позиция первого бита</param>
        /// <param name="count">кол-во бит</param>
        /// <param name="newValue">новое значение</param>        
        public static void SetBits(ref uint value, int start, int count, uint newValue)
        {
            #region /Проверка аргументов/
            if (start < 0 || start > 31)
                throw new ArgumentOutOfRangeException("start");
            if (count < 1 || start + count > 32)
                throw new ArgumentOutOfRangeException("count");
            #endregion

            if (count > 31)
            {
                value = newValue;
                return;
            }

            uint mask = ((1u << count) - 1);
            value &= ~(mask << start);
            value |= ((newValue & mask) << start);
        }
    }

    //public static class MathLib<T>
    //{
    //    public delegate T DelegateValueGetter(int index);

    //    public static int QuickSearchL(DelegateValueGetter getter, T x, int first, int last)
    //    {
    //        if (last > first)
    //            return -1;

    //        IComparer<T> comparer = Comparer<T>.Default;

    //        if (comparer.Compare(x, getter(last)) >= 0)
    //            return last;
    //        if (comparer.Compare(x, getter(first)) < 0)
    //            return -1;

    //        return InnerQuickSearchL(getter, x, first, last);
    //    }

    //    private static int InnerQuickSearchL(DelegateValueGetter getter, T x, int first, int last)
    //    {
    //        if (last - first < 2)
    //            return first;

    //        int mid = first + (last - first); // устойчиво к переполнению

    //        if (Comparer<T>.Default.Compare(x, getter(mid)) <= 0)
    //            return InnerQuickSearchL(getter, x, first, mid);
    //        else
    //            return InnerQuickSearchL(getter, x, mid, last);
    //    }

    //    public static int QuickSearchR(DelegateValueGetter getter, T x, int first, int last)
    //    {
    //        if (last > first)
    //            return -1;

    //        IComparer<T> comparer = Comparer<T>.Default;

    //        if (comparer.Compare(x, getter(last)) > 0)
    //            return -1;
    //        if (comparer.Compare(x, getter(first)) <= 0)
    //            return first;

    //        return InnerQuickSearchR(getter, x, first, last);
    //    }

    //    private static int InnerQuickSearchR(DelegateValueGetter getter, T x, int first, int last)
    //    {
    //        if (last - first <= 1)
    //            return last;

    //        int mid = first + (last - first); // устойчиво к переполнению

    //        if (Comparer<T>.Default.Compare(x, getter(mid)) < 0)
    //            return InnerQuickSearchR(getter, x, first, mid);
    //        else
    //            return InnerQuickSearchR(getter, x, mid, last);
    //    }
    //}

    public class DoubleApproximateComparer : IEqualityComparer<double>
    {

        #region IEqualityComparer<double> Members

        const int bit = 16;

        public bool Equals(double x, double y)
        {
            if (x == y)
                return true;
            return MathLib.AlmostEquals(x, y, 16);
        }

        public int GetHashCode(double obj)
        {
            return (int)(MathLib.ToBits2Complement(obj) >> 32);
        }

        #endregion
    }

    public class Interpolator
    {
        /*public Interpolator(double xmin, double xmax, string data)
            :this(xmin,xmax,StringLib.ToDoubleArray(data))
        { }*/

        public Interpolator(double xmin, double xmax, double[] data)
        {
            if (data==null||data.Length<1)
                throw new ArgumentNullException("data");
            _y = data;
            _xmin = xmin;
            _xmax = xmax;

            _ymin = _y.Min();
            _ymax = _y.Max();
        }

        double[] _y;
        double _xmin;
        double _xmax;

        double _ymin;
        double _ymax;
        
        public double this[int i]
        {
            get { return _y[i]; }
        }

        public double this[double x]
        {
            get { return MathLib.Interpolate(x,_xmin,_xmax,_y); }
        }

        public int Count
        {
            get { return _y.Length; }
        }

        public double X(int i)
        {
            if (Count == 0)
                return (_xmax + _xmin) * 0.5;
            double dx = (_xmax - _xmin) / (double)(Count - 1);
            return i * dx;
        }
        public double Xmin { get { return _xmin; } }
        public double Xmax { get { return _xmax; } }

        public double Ymin { get { return _ymin; } }
        public double Ymax { get { return _ymax; } }
    }
}
