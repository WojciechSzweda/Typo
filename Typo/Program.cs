using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Typo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var hook = new KeyboardHook())
            {
                Application.Run();
            }
        }
    }
}
