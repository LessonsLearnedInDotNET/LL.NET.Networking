using System;

namespace CommonLib
{
    public class TCP_Message : EventArgs
    {
        public byte[] Message { get; set; }
        public int Length { get; set; }
        public string Address { get; set; }
        public Guid ID { get; set; }

        public TCP_Message(byte[] message)
        {
            Message = message;
            Length = message.Length;
        }
        public TCP_Message(byte[] message, string address, Guid id)
        {
            Message = message;
            Length = message.Length;
            Address = address;
            ID = id;
        }
    }
}
