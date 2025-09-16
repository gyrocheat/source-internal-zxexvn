using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class KeyHelper
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);
        /// <summary>
        /// Checks if a specific key is currently pressed down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key is down, otherwise false.</returns>
        public static bool IsKeyDown(Keys key)
        {
            // The high-order bit is set if the key is down
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }
    }
}
