using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniLib.Helpers;

namespace UniLib
{
    public class CyFile
    {
        private XorFileStream XorFileStream { get; set; }

        private string Path { get; set; }

        public CyFile(string path, byte privateKey = 40)
        {
            Path = path;
            XorFileStream = new XorFileStream(path, FileMode.Open, FileAccess.Read, privateKey);
        }

        public bool IsValidCyFile()
        {
            byte[] magicHeader = new byte[] { 0x7D, 0x46, 0x41, 0x5C, 0x51, 0x6E, 0x7B, 0x28, 0x28, 0x28, 0x28, 0x2F, 0x1D, 0x06, 0x50, 0x06, 0x50, 0x28, 0x1A, 0x18, 0x19, 0x11, 0x06, 0x1C, 0x06, 0x1A, 0x18, 0x4E, 0x19, 0x28, 0x28, 0x28, 0x28, 0x28 };
            byte[] expectedHeader = new byte[magicHeader.Length];

            int bytesRead = XorFileStream.m_FileStream.Read(expectedHeader, 0, expectedHeader.Length);

            return bytesRead == expectedHeader.Length && expectedHeader.SequenceEqual(magicHeader);
        }

        public byte[] GetEncrypted() => XorFileStream.m_FileStream.StreamToByteArray();

        public byte[] GetDecrypted() => XorFileStream.StreamToByteArray();

        public void DecryptToFile(string filePath) => File.WriteAllBytes(filePath, GetDecrypted());

        public void DecryptToFile() => File.WriteAllBytes(Path, GetDecrypted());

        public void EncryptToFile(string filePath) => File.WriteAllBytes(filePath, GetEncrypted());

        public void EncryptToFile() => File.WriteAllBytes(Path, GetEncrypted());
    }
}
