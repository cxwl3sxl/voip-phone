using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    partial class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                WorkAsParent();
            }
            else
            {
                WorkAsClient(args);
            }
        }
    }
}
