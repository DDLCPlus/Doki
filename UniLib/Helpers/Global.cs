using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniLib.Helpers
{
    public static class Global
    {
        public static byte[] StreamToByteArray(this Stream inputStream)
        {
            if (inputStream is MemoryStream ms)
                return ms.ToArray();

            using (var memoryStream = new MemoryStream())
            {
                inputStream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
