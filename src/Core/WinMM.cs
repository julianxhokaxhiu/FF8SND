﻿using System;
using System.Runtime.InteropServices;

namespace FF8SND.Core
{
    public class WinMM
    {
        public const UInt32 WINMM_SND_SYNC = 0;
        public const UInt32 WINMM_SND_MEMORY = 4;

        [DllImport("Winmm.dll")]
        public static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);
    }
}
