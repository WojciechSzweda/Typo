using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
namespace Typo
{

    class KeyboardHook : IDisposable
    {
        private static double chanceNewKey = 0.07;
        private static double chanceNoKey = 0.02;

        static char[,] keyboard = new char[,]
       {
            {'1','2','3','4','5','6','7','8','9','0'},
            {'q','w','e','r','t','y','u','i','o','p'},
            {'a','s','d','f','g','h','j','k','l',';'},
            {'z','x','c','v','b','n','m',',','.','/'}
       };


        private static Random rng = new Random();
        private static SendInputApi sendInputApi = new SendInputApi();
        private static bool sendThroughApi = false;


        public KeyboardHook()
        {
            _hookID = SetHook(_proc);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var coord = keyboard.CoordinatesOf(char.ToLower((char)vkCode));
            var isInArray = (coord.Item1 != -1 && coord.Item2 != -1);
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !sendThroughApi && isInArray)
            {
                var chance = rng.NextDouble();
                if (chance < chanceNewKey)
                {
                    var newKey = NewNeighbourKey(coord);
                    sendInputApi.SendKey(newKey);
                    return (IntPtr)1;
                }
                else if (chance < chanceNewKey + chanceNoKey)
                    return (IntPtr)1;

                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            if (sendThroughApi)
            {
                sendThroughApi = false;
                return CallNextHookEx(_hookID, nCode, wParam, lParam);

            }
            if (!isInArray)
                return CallNextHookEx(_hookID, nCode, wParam, lParam);

            return (IntPtr)1;

        }

        public static void SendTroughApi()
        {
            //allowNext++;
            sendThroughApi = true;
        }

        static char NewNeighbourKey(Tuple<int, int> coord)
        {
            var newIndexX = Math.Max(0, Math.Min(10, coord.Item2 + (rng.NextDouble() <= 0.5 ? 1 : -1)));
            return keyboard[coord.Item1, newIndexX];
        }

        static char NewRandomKey()
        {
            return keyboard[rng.Next(keyboard.GetLength(0)), rng.Next(keyboard.GetLength(1))];
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;




    }
}