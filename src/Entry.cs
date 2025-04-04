﻿using FF8SND.Core;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;
using ATL;

namespace FF8SND
{
    public partial class Entry : Form
    {
        private string FF8Dir = string.Empty;
        private FmtFileHeader fmtHeader;
        private FmtAudioTracks fmtAudioTracks;
        private AudioFile[] audioList;

        public Entry()
        {
            InitializeComponent();

            // Use ID3v2.3
            ATL.Settings.ID3v2_tagSubVersion = 3;

            RegistryKey ret = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"Software\Square Soft, Inc\FINAL FANTASY VIII\1.00");
            if (ret != null)
            {
                FF8Dir = ret.GetValue("AppPath") + @"\Data\Sound";
                ret.Close();
            }
        }

        private bool parseAudioFile(string audioDat, string audioFmt)
        {
            FileStream fileFmt = File.OpenRead(audioFmt);
            FileStream fileDat = File.OpenRead(audioDat);

            fmtAudioTracks = fileFmt.ReadStruct<FmtAudioTracks>();
            audioList = new AudioFile[fmtAudioTracks.NumberOfTracks];

            for (int idx = 0; idx < audioList.Length; ++idx)
            {
                fmtHeader = fileFmt.ReadStruct<FmtFileHeader>();
                if (fmtHeader.Length == 0)
                {
                    fileFmt.SeekStruct<WAVEFORMATEX>();
                    continue;
                }

                // Save fmt Header info for later
                audioList[idx].fmtHeader = fmtHeader;

                // Set Header Info
                audioList[idx].riffChunk.Id = "RIFF".ToArray();
                audioList[idx].riffChunk.Size = 0;
                audioList[idx].riffChunk.Format = "WAVE".ToArray();

                // Set Format Info
                audioList[idx].formatChunk.Id = "fmt ".ToArray();
                audioList[idx].formatChunk.Size = (uint)Marshal.SizeOf(typeof(ADPCMWAVEFORMAT));
                audioList[idx].formatChunk.ADPCM = fileFmt.ReadStruct<ADPCMWAVEFORMAT>();

                // Set Loop Info
                audioList[idx].loopChunk.Id = "fflp".ToArray();
                audioList[idx].loopChunk.Size = (uint)Marshal.SizeOf(typeof(uint)) * 2;
                audioList[idx].loopChunk.Start = fmtHeader.Start;
                audioList[idx].loopChunk.End = fmtHeader.End;

                // Set Data Info
                audioList[idx].dataChunk.Id = "data".ToArray();
                audioList[idx].dataChunk.Size = fmtHeader.Length;
                audioList[idx].Data = new byte[audioList[idx].dataChunk.Size];
                fileDat.ReadExactly(audioList[idx].Data);

                // Finish saving some last info
                audioList[idx].riffChunk.Size = (uint)(Marshal.SizeOf(typeof(FormatChunk)) + Marshal.SizeOf(typeof(DataChunk)) + audioList[idx].dataChunk.Size - Marshal.SizeOf(typeof(RiffChunk)));
                if (fmtHeader.Loop > 0) audioList[idx].riffChunk.Size += (uint)(Marshal.SizeOf(typeof(LoopChunk)));
            }

            fileFmt.Close();
            fileDat.Close();

            return true;
        }

        private bool dumpAudioFile(string audioDat, string audioFmt)
        {
            FileStream fileFmt = File.OpenWrite(audioFmt);
            FileStream fileDat = File.OpenWrite(audioDat);

            for (int idx = 0; idx < audioList.Length; ++idx)
            {
                AudioFile audioFile = audioList[idx];

                // Write fmt header
                fileFmt.WriteStruct<FmtFileHeader>(audioFile.fmtHeader);

                // Write ADPCM data
                fileFmt.WriteStruct<ADPCMWAVEFORMAT>(audioFile.formatChunk.ADPCM);

                // Write audio data
                if (audioFile.Data != null) fileDat.Write(audioFile.Data, 0, audioFile.Data.Length);
            }

            fileFmt.Close();
            fileDat.Close();

            return true;
        }

        private void getWaveStream(Stream stream, int idx)
        {
            AudioFile audioFile = audioList[idx];

            stream.WriteStruct<RiffChunk>(audioFile.riffChunk);
            stream.WriteStruct<FormatChunk>(audioFile.formatChunk);
            stream.WriteStruct<DataChunk>(audioFile.dataChunk);

            if (audioFile.Data != null) stream.Write(audioFile.Data, 0, audioFile.Data.Length);

            if (audioFile.fmtHeader.Loop > 0)
            {
                var track = new Track(stream);

                var loopStart = audioFile.loopChunk.Start * (audioFile.formatChunk.ADPCM.waveFormatEx.Channels / 2.0);
                var loopEnd = audioFile.loopChunk.End * (audioFile.formatChunk.ADPCM.waveFormatEx.Channels / 2.0);

                // Add Wave loop points
                track.AdditionalFields["sample.NumSampleLoops"] = "1";
                track.AdditionalFields["sample.SampleLoop[0].Start"] = loopStart.ToString();
                track.AdditionalFields["sample.SampleLoop[0].End"] = loopEnd.ToString();

                // Add ID3 Tags
                track.AdditionalFields["LOOPSTART"] = loopStart.ToString();
                track.AdditionalFields["LOOPEND"] = loopEnd.ToString();

                track.Save();
            }
        }

        private void renderList()
        {
            for (int idx = 0; idx < audioList.Length; ++idx)
            {
                string[] item =
                {
                    (idx + 1).ToString(),
                    audioList[idx].dataChunk.Size.ToString(),
                    audioList[idx].formatChunk.ADPCM.waveFormatEx.SamplesPerSec.ToString(),
                    audioList[idx].fmtHeader.Loop.ToString()
                };

                lstView.Items.Add(new ListViewItem(item));
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "FF8 Sound file (audio.dat)|audio.dat|All files (*.*)|*.*";
            openFileDialog.DefaultExt = "dat";
            openFileDialog.FileName = "audio.dat";
            if (FF8Dir != string.Empty) openFileDialog.InitialDirectory = FF8Dir;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string audioDat = openFileDialog.FileName;
                string audioFmt = Path.ChangeExtension(audioDat, "fmt");

                parseAudioFile(audioDat, audioFmt);
                renderList();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("https://github.com/julianxhokaxhiu/FF8SND")
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            getWaveStream(ms, lstView.SelectedItems[0].Index);

            WinMM.PlaySound(ms.ToArray(), IntPtr.Zero, WinMM.WINMM_SND_SYNC | WinMM.WINMM_SND_MEMORY);
        }

        private void lstView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnExtract.Enabled = lstView.SelectedItems.Count > 0;
            btnPlay.Enabled = lstView.SelectedItems.Count == 1;
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                foreach(ListViewItem item in lstView.SelectedItems)
                {
                    FileStream fileOut = File.Open(folderDialog.SelectedPath + @"\" + (item.Index + 1).ToString() + @".wav", FileMode.Create);
                    getWaveStream(fileOut, item.Index);
                    fileOut.Close();
                }

                MessageBox.Show("Successfully exported the selected items.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void Entry_KeyUp(object sender, KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        lstView.Items.OfType<ListViewItem>().ToList().ForEach(item => item.Selected = true);
                        break;
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "FF8 Sound file (audio.dat)|audio.dat|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "dat";
            saveFileDialog.FileName = "audio.dat";
            if (FF8Dir != string.Empty) saveFileDialog.InitialDirectory = FF8Dir;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string audioDat = saveFileDialog.FileName;
                string audioFmt = Path.ChangeExtension(audioDat, "fmt");

                if (dumpAudioFile(audioDat, audioFmt))
                    MessageBox.Show("Audio files were successfully saved in:\n\n" + audioFmt + "\n" + audioDat, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
