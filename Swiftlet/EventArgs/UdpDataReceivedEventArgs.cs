using System;
using System.Net;

namespace Swiftlet
{
    public class UdpDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }

        public UdpDataReceivedEventArgs(byte[] data, IPEndPoint remoteEndPoint)
        {
            this.Data = data;
            this.RemoteEndPoint = remoteEndPoint;
        }
    }
}
