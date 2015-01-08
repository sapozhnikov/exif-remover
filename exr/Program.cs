using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace exr
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                System.Console.WriteLine("Usage:\nexr.exe file1.jpg [file2.jpg] [file3.jpg] [...]");
            else
                foreach (string arg in args)
                {
                    FileCleaner.ProcessFile(arg);
                }
        }
    }
}
