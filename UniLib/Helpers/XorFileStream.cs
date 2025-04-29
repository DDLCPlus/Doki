using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniLib.Helpers
{
    /// <summary>
    /// UnityPS.dll - XorFileStream, all credits goto their respective authors.
    /// </summary>
    public class XorFileStream : Stream
    {
        public XorFileStream(string path, FileMode mode, FileAccess access, byte privateKey = 40)
        {
            this.m_Key = privateKey;
            this.m_FileStream = new FileStream(path, mode, access);
        }

        private void PerformXor(ref byte[] array, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                array[i] ^= this.m_Key;
            }
        }

        public override void Flush()
        { 
            this.m_FileStream.Flush();
        }

        public override int Read(byte[] array, int offset, int count)
        {
            int result = this.m_FileStream.Read(array, offset, count);
            this.PerformXor(ref array, offset, count);
            return result;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            this.PerformXor(ref array, offset, count);
            this.m_FileStream.Write(array, offset, count);
        }

        public override int ReadByte()
        {
            return (int)((byte)(this.m_FileStream.ReadByte() ^ (int)this.m_Key));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.m_FileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.m_FileStream.SetLength(value);
        }

        public override void WriteByte(byte value)
        {
            this.m_FileStream.WriteByte((byte)(value ^ this.m_Key));
        }

        public override bool CanRead
        {
            get
            {
                return this.m_FileStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.m_FileStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.m_FileStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.m_FileStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.m_FileStream.Position;
            }
            set
            {
                this.m_FileStream.Position = value;
            }
        }

        public FileStream m_FileStream;

        private byte m_Key;
    }
}
