using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniLib;

namespace UniLibTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "UniLib Tests";

            PersistentSaveFile sf = new PersistentSaveFile("save_persistent.sav");

            Console.WriteLine(sf.LoadEntries().Count);

            Console.ReadKey();
        }
    }
}
