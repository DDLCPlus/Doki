using Doki.Renpie.Rpyc;
using System;
using System.IO;

namespace RpycTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] contents = File.ReadAllBytes("script-ch40.rpyc");

            RpycFile rpyc = new RpycFile(contents);

            Console.WriteLine(rpyc.Valid);

            Console.ReadKey();
        }
    }
}
