using System;
namespace BECSLibrary.Transport
{
    public interface ITransport
    {
        void Connect();
        bool Connected { get; }
        void Disconnect();
        object Locker { get; }
        int Read(byte[] buffer, int offset, int size);
        void Send(byte[] buffer, int offset, int size);
        void DiscardInBuffer();
    }
}
