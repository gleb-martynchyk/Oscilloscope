using System;
using System.Collections.Generic;
using System.Text;
using BECSLibrary;

namespace MeterFramework.Core.ControlProtocols
{
    public class Register<T>
    {
        public Register(byte number, T[] data)
        {
            Number = number;
            Data = data;
        }

        public Register(byte number, int size)
            : this(number, new T[size])
        { }

        public T[] Data { get; protected set; }
        public byte Number { get; private set; }
        public int Size { get { return Data.Length; } }         
    }   
}
