using Doki.Extensions;
using Doki.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie
{
    public class Archive
    {
        private string Path { get; set; }

        public Dictionary<string, ArchiveFile> Files { get; set; }
        public bool Valid = false;

        public Archive(string path)
        {
            Path = path;
            Files = new Dictionary<string, ArchiveFile>();

            using (var stream = File.OpenRead(path))
            {
                if (!Expect(stream, "RPA-3.0 ")) return;

                var indexOffset = long.Parse(GetString(stream, 16), NumberStyles.HexNumber);
                if (!Expect(stream, " ")) return;

                var key = int.Parse(GetString(stream, 8), NumberStyles.HexNumber);

                stream.Position = indexOffset;
                var indexBytes = new byte[stream.Length - stream.Position];
                stream.Read(indexBytes, 0, indexBytes.Length);
                var pythonObj = Unpickler.UnpickleZlibBytes(indexBytes);

                if (pythonObj.Type != PythonObj.ObjType.DICTIONARY)
                    return;

                foreach (var entry in pythonObj.Dictionary)
                {
                    var name = entry.Key.String;
                    var rawOffset = entry.Value.List[0].Tuple[0].ToInt();
                    var rawLength = entry.Value.List[0].Tuple[1].ToInt();
                    var offset = rawOffset ^ key;
                    var length = rawLength ^ key;

                    Files[name] = new ArchiveFile(Path, GetFile(length, offset), name);
                }

                Valid = true;
            }
        }

        private byte[] GetFile(int length, int offset)
        {
            byte[] bytes = new byte[length];
            using (var stream = File.OpenRead(Path))
            {
                stream.Position = offset;
                stream.Read(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        private bool Expect(Stream stream, string s)
        {
            byte[] bytes = new byte[s.Length];
            stream.Read(bytes, 0, bytes.Length);
            for (var i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] != s[i])
                {
                    return false;
                }
            }
            return true;
        }

        private string GetString(Stream stream, int count)
        {
            byte[] bytes = new byte[count];
            stream.Read(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
