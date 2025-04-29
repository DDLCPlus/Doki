using Doki.Renpie.Rpyc;
using System;
using System.IO;
using System.Linq;

namespace RpycTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] contents = File.ReadAllBytes("script-ch40.rpyc");

            RpycFile rpyc = new RpycFile(contents);

            foreach(var b in rpyc.Labels.First().Value.Contents)
                Console.WriteLine(b.GetType());

            Console.ReadKey();
        }
    }
}
