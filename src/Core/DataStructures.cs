﻿using System.Runtime.InteropServices;

namespace FF8SND
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FmtAudioTracks
    {
        public uint NumberOfTracks;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] Unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FmtFileHeader
    {
        public uint Length;
        public uint Offset;
        public byte Loop;
        public byte Count;
        public byte Unk1;
        public byte Unk2;
        public uint Start;
        public uint End;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ADPCMCOEFSET
    {
        public ushort Coef1;
        public ushort Coef2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WAVEFORMATEX
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ADPCMWAVEFORMAT
    {
        public WAVEFORMATEX waveFormatEx;
        public ushort SamplesPerBlock;
        public ushort NumCoef;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public ADPCMCOEFSET[] Coef;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiffChunk
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Id;
        public uint Size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Format;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FormatChunk
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Id;
        public uint Size;
        public ADPCMWAVEFORMAT ADPCM;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DataChunk
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Id;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LoopChunk
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Id;
        public uint Size;
        public uint Start;
        public uint End;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct AudioFile
    {
        public FmtFileHeader fmtHeader;
        public RiffChunk riffChunk;
        public FormatChunk formatChunk;
        public LoopChunk loopChunk;
        public DataChunk dataChunk;
        public byte[] Data;
    }
}
