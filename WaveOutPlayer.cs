using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ShiaPrayerTimes
{
    public static class AppLogger
    {
        private static readonly object _logLock = new object();

        public static void Log(string category, string message)
        {
            try
            {
                string dir = AppSettings.GetConfigDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "app.log");

                lock (_logLock)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 200 * 1024)
                    {
                        string bak = Path.Combine(dir, "app.log.old");
                        if (File.Exists(bak)) File.Delete(bak);
                        File.Move(path, bak);
                    }
                    string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}{3}",
                        DateTime.Now, category, message, Environment.NewLine);
                    File.AppendAllText(path, line, System.Text.Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Pure native Windows audio player using Media Foundation (IMFSourceReader -> PCM)
    /// and winmm.dll waveOutOpen / waveOutWrite. Streams directly to the Windows audio mixer
    /// independently of WPF/DWM/DirectShow desktop state (works reliably while locked or screen off).
    /// </summary>
    public class WaveOutPlayer
    {
        private const int MF_SDK_VERSION = 0x00020070;
        private const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = -3;
        private const int MF_SOURCE_READERF_ENDOFSTREAM = 2;
        private const int WAVE_MAPPER = -1;
        private const int WAVE_FORMAT_PCM = 1;
        private const int WHDR_DONE = 0x00000001;

        private static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        private static readonly Guid MFMediaType_Audio = new Guid("73647561-0000-0010-8000-00AA00389B71");
        private static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        private static readonly Guid MFAudioFormat_PCM = new Guid("00000001-0000-0010-8000-00aa00389b71");
        private static readonly Guid MF_MT_AUDIO_NUM_CHANNELS = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a");
        private static readonly Guid MF_MT_AUDIO_SAMPLES_PER_SECOND = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
        private static readonly Guid MF_MT_AUDIO_BITS_PER_SAMPLE = new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFStartup(int version, int dwFlags);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFCreateMediaType(out IMFMediaType ppMFType);

        [DllImport("mfreadwrite.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int MFCreateSourceReaderFromURL(string pwszURL, IntPtr pAttributes, out IMFSourceReader ppSourceReader);

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, int fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hWaveOut, uint dwVolume);

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555")]
        private interface IMFMediaType
        {
            void _01(); void _02(); void _03(); void _04(); void _05(); void _06(); void _07(); void _08(); void _09(); void _10();
            void _11(); void _12(); void _13(); void _14(); void _15(); void _16(); void _17(); void _18();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            void _20(); void _21();
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
            void _23(); void _24(); void _25(); void _26(); void _27(); void _28(); void _29(); void _30();
            void _31(); void _32(); void _33(); void _34(); void _35();
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("70AE66F2-C809-4E4F-8915-BDCB406B7993")]
        private interface IMFSourceReader
        {
            [PreserveSig] int GetStreamSelection(int dwStreamIndex, out bool pfSelected);
            [PreserveSig] int SetStreamSelection(int dwStreamIndex, bool fSelected);
            [PreserveSig] int GetNativeMediaType(int dwStreamIndex, int dwMediaTypeIndex, out IMFMediaType ppMediaType);
            [PreserveSig] int GetCurrentMediaType(int dwStreamIndex, out IMFMediaType ppMediaType);
            [PreserveSig] int SetCurrentMediaType(int dwStreamIndex, IntPtr pdwReserved, IMFMediaType pMediaType);
            [PreserveSig] int SetCurrentPosition(ref Guid guidTimeFormat, IntPtr varPosition);
            [PreserveSig] int ReadSample(int dwStreamIndex, int dwControlFlags, out int pdwActualStreamIndex, out int pdwStreamFlags, out long pllTimestamp, out IMFSample ppSample);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
        private interface IMFSample
        {
            void _01(); void _02(); void _03(); void _04(); void _05(); void _06(); void _07(); void _08(); void _09(); void _10();
            void _11(); void _12(); void _13(); void _14(); void _15(); void _16(); void _17(); void _18(); void _19(); void _20();
            void _21(); void _22(); void _23(); void _24(); void _25(); void _26(); void _27(); void _28(); void _29(); void _30();
            void _31(); void _32(); void _33(); void _34(); void _35(); void _36(); void _37(); void _38();
            [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
        private interface IMFMediaBuffer
        {
            [PreserveSig] int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);
            [PreserveSig] int Unlock();
            [PreserveSig] int GetCurrentLength(out int pcbCurrentLength);
        }

        private class PinnedWaveChunk : IDisposable
        {
            public byte[] Data;
            public GCHandle DataHandle;
            public IntPtr HeaderPtr;
            public bool Prepared;
            public bool InFlight;

            public PinnedWaveChunk(byte[] bytes)
            {
                Data = bytes;
                DataHandle = GCHandle.Alloc(Data, GCHandleType.Pinned);
                HeaderPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WAVEHDR)));
                var hdr = new WAVEHDR
                {
                    lpData = DataHandle.AddrOfPinnedObject(),
                    dwBufferLength = (uint)Data.Length,
                    dwBytesRecorded = 0,
                    dwUser = IntPtr.Zero,
                    dwFlags = 0,
                    dwLoops = 0,
                    lpNext = IntPtr.Zero,
                    reserved = IntPtr.Zero
                };
                Marshal.StructureToPtr(hdr, HeaderPtr, false);
            }

            public bool IsDone()
            {
                if (!InFlight) return true;
                var hdr = (WAVEHDR)Marshal.PtrToStructure(HeaderPtr, typeof(WAVEHDR));
                return (hdr.dwFlags & WHDR_DONE) != 0;
            }

            public void Dispose()
            {
                if (HeaderPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(HeaderPtr);
                    HeaderPtr = IntPtr.Zero;
                }
                if (DataHandle.IsAllocated)
                {
                    DataHandle.Free();
                }
            }
        }

        private volatile bool _stopRequested = false;
        private volatile int _volumePercent = 85;
        private volatile bool _isMuted = false;
        private IntPtr _activeWaveOut = IntPtr.Zero;
        private readonly object _stateLock = new object();
        private Thread _playThread;
        private int _playbackSessionId = 0;

        public event Action<int, bool> PlaybackFinished; // (sessionId, stoppedByUser)

        public void SetVolume(int volumePercent, bool isMuted)
        {
            _volumePercent = Math.Max(0, Math.Min(100, volumePercent));
            _isMuted = isMuted;
            lock (_stateLock)
            {
                if (_activeWaveOut != IntPtr.Zero)
                {
                    ApplyWaveVolume(_activeWaveOut);
                }
            }
        }

        private void ApplyWaveVolume(IntPtr hWaveOut)
        {
            try
            {
                int vol = _isMuted ? 0 : _volumePercent;
                ushort chVol = (ushort)Math.Round(0xFFFF * (vol / 100.0));
                uint dwVol = (uint)chVol | ((uint)chVol << 16);
                waveOutSetVolume(hWaveOut, dwVol);
            }
            catch { }
        }

        public bool PlayAsync(string filePath, int volumePercent, bool isMuted, out int sessionId)
        {
            Stop();

            _volumePercent = Math.Max(0, Math.Min(100, volumePercent));
            _isMuted = isMuted;
            _stopRequested = false;

            sessionId = Interlocked.Increment(ref _playbackSessionId);
            int mySession = sessionId;

            // Validate Media Foundation can open & configure the file synchronously before claiming success
            IMFSourceReader reader = null;
            IMFMediaType mediaType = null;
            bool mfStarted = false;

            try
            {
                int hrStart = MFStartup(MF_SDK_VERSION, 0);
                if (hrStart != 0)
                {
                    AppLogger.Log("WaveOut", "MFStartup failed hr=0x" + hrStart.ToString("X"));
                    return false;
                }
                mfStarted = true;

                int hrReader = MFCreateSourceReaderFromURL(filePath, IntPtr.Zero, out reader);
                if (hrReader != 0 || reader == null)
                {
                    AppLogger.Log("WaveOut", "MFCreateSourceReaderFromURL failed hr=0x" + hrReader.ToString("X") + " file=" + filePath);
                    MFShutdown();
                    return false;
                }

                int hrMt = MFCreateMediaType(out mediaType);
                if (hrMt != 0 || mediaType == null)
                {
                    Marshal.ReleaseComObject(reader);
                    MFShutdown();
                    return false;
                }

                Guid maj = MF_MT_MAJOR_TYPE, aud = MFMediaType_Audio;
                Guid sub = MF_MT_SUBTYPE, pcm = MFAudioFormat_PCM;
                Guid ch = MF_MT_AUDIO_NUM_CHANNELS, sr = MF_MT_AUDIO_SAMPLES_PER_SECOND, bps = MF_MT_AUDIO_BITS_PER_SAMPLE;
                mediaType.SetGUID(ref maj, ref aud);
                mediaType.SetGUID(ref sub, ref pcm);
                mediaType.SetUINT32(ref ch, 2);
                mediaType.SetUINT32(ref sr, 44100);
                mediaType.SetUINT32(ref bps, 16);

                int hrSet = reader.SetCurrentMediaType(MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, mediaType);
                Marshal.ReleaseComObject(mediaType);
                mediaType = null;

                if (hrSet != 0)
                {
                    AppLogger.Log("WaveOut", "SetCurrentMediaType failed hr=0x" + hrSet.ToString("X"));
                    Marshal.ReleaseComObject(reader);
                    MFShutdown();
                    return false;
                }

                // Release pre-check reader and re-open cleanly inside dedicated audio MTA thread
                Marshal.ReleaseComObject(reader);
                reader = null;
                MFShutdown();
                mfStarted = false;
            }
            catch (Exception ex)
            {
                AppLogger.Log("WaveOut", "Pre-check exception: " + ex.Message);
                if (mediaType != null) Marshal.ReleaseComObject(mediaType);
                if (reader != null) Marshal.ReleaseComObject(reader);
                if (mfStarted) MFShutdown();
                return false;
            }

            _playThread = new Thread(() => StreamThreadProc(filePath, mySession))
            {
                IsBackground = true,
                Name = "WaveOutAudioThread"
            };
            _playThread.SetApartmentState(ApartmentState.MTA);
            _playThread.Start();
            return true;
        }

        public void Stop()
        {
            _stopRequested = true;
            Thread t = _playThread;
            if (t != null && t.IsAlive && Thread.CurrentThread != t)
            {
                t.Join(1200);
            }
        }

        private void StreamThreadProc(string filePath, int sessionId)
        {
            bool mfStarted = false;
            IMFSourceReader reader = null;
            IMFMediaType mediaType = null;
            IntPtr hWaveOut = IntPtr.Zero;
            var activeChunks = new List<PinnedWaveChunk>();
            bool stoppedByUser = false;

            try
            {
                if (MFStartup(MF_SDK_VERSION, 0) != 0) return;
                mfStarted = true;

                if (MFCreateSourceReaderFromURL(filePath, IntPtr.Zero, out reader) != 0 || reader == null) return;
                if (MFCreateMediaType(out mediaType) != 0 || mediaType == null) return;

                Guid maj = MF_MT_MAJOR_TYPE, aud = MFMediaType_Audio;
                Guid sub = MF_MT_SUBTYPE, pcm = MFAudioFormat_PCM;
                Guid ch = MF_MT_AUDIO_NUM_CHANNELS, sr = MF_MT_AUDIO_SAMPLES_PER_SECOND, bps = MF_MT_AUDIO_BITS_PER_SAMPLE;
                mediaType.SetGUID(ref maj, ref aud);
                mediaType.SetGUID(ref sub, ref pcm);
                mediaType.SetUINT32(ref ch, 2);
                mediaType.SetUINT32(ref sr, 44100);
                mediaType.SetUINT32(ref bps, 16);

                if (reader.SetCurrentMediaType(MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, mediaType) != 0) return;
                Marshal.ReleaseComObject(mediaType);
                mediaType = null;

                var wfx = new WAVEFORMATEX
                {
                    wFormatTag = WAVE_FORMAT_PCM,
                    nChannels = 2,
                    nSamplesPerSec = 44100,
                    wBitsPerSample = 16,
                    nBlockAlign = 4,
                    nAvgBytesPerSec = 44100 * 4,
                    cbSize = 0
                };

                // Retry briefly in case USB audio device is still waking after S3 Sleep resume
                int openRes = -1;
                for (int attempt = 0; attempt < 8 && !_stopRequested; attempt++)
                {
                    openRes = waveOutOpen(out hWaveOut, WAVE_MAPPER, ref wfx, IntPtr.Zero, IntPtr.Zero, 0);
                    if (openRes == 0 && hWaveOut != IntPtr.Zero) break;
                    AppLogger.Log("WaveOut", "waveOutOpen attempt " + attempt + " failed code=" + openRes);
                    Thread.Sleep(300);
                }

                if (openRes != 0 || hWaveOut == IntPtr.Zero)
                {
                    AppLogger.Log("WaveOut", "waveOutOpen failed permanently code=" + openRes);
                    return;
                }

                lock (_stateLock) { _activeWaveOut = hWaveOut; }
                ApplyWaveVolume(hWaveOut);
                AppLogger.Log("WaveOut", "Started streaming PCM via waveOut: " + Path.GetFileName(filePath));

                const int maxQueuedBuffers = 6;

                while (!_stopRequested)
                {
                    // Reclaim completed chunks
                    for (int i = activeChunks.Count - 1; i >= 0; i--)
                    {
                        if (activeChunks[i].IsDone())
                        {
                            if (activeChunks[i].Prepared)
                            {
                                waveOutUnprepareHeader(hWaveOut, activeChunks[i].HeaderPtr, Marshal.SizeOf(typeof(WAVEHDR)));
                            }
                            activeChunks[i].Dispose();
                            activeChunks.RemoveAt(i);
                        }
                    }

                    if (activeChunks.Count >= maxQueuedBuffers)
                    {
                        Thread.Sleep(25);
                        continue;
                    }

                    int idx, flags;
                    long ts;
                    IMFSample sample;
                    int hrRead = reader.ReadSample(MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, out idx, out flags, out ts, out sample);
                    if (hrRead != 0 || (flags & MF_SOURCE_READERF_ENDOFSTREAM) != 0 || sample == null)
                    {
                        if (sample != null) Marshal.ReleaseComObject(sample);
                        break;
                    }

                    IMFMediaBuffer buf = null;
                    byte[] pcmBytes = null;
                    try
                    {
                        sample.ConvertToContiguousBuffer(out buf);
                        if (buf != null)
                        {
                            IntPtr ptr;
                            int maxLen, curLen;
                            if (buf.Lock(out ptr, out maxLen, out curLen) == 0 && curLen > 0)
                            {
                                pcmBytes = new byte[curLen];
                                Marshal.Copy(ptr, pcmBytes, 0, curLen);
                                buf.Unlock();
                            }
                        }
                    }
                    finally
                    {
                        if (buf != null) Marshal.ReleaseComObject(buf);
                        Marshal.ReleaseComObject(sample);
                    }

                    if (pcmBytes == null || pcmBytes.Length == 0) continue;

                    var chunk = new PinnedWaveChunk(pcmBytes);
                    int hdrSize = Marshal.SizeOf(typeof(WAVEHDR));
                    if (waveOutPrepareHeader(hWaveOut, chunk.HeaderPtr, hdrSize) == 0)
                    {
                        chunk.Prepared = true;
                        if (waveOutWrite(hWaveOut, chunk.HeaderPtr, hdrSize) == 0)
                        {
                            chunk.InFlight = true;
                            activeChunks.Add(chunk);
                        }
                        else
                        {
                            waveOutUnprepareHeader(hWaveOut, chunk.HeaderPtr, hdrSize);
                            chunk.Dispose();
                        }
                    }
                    else
                    {
                        chunk.Dispose();
                    }
                }

                // Wait for queued audio buffers to finish (or stop immediately if requested)
                while (activeChunks.Count > 0)
                {
                    if (_stopRequested)
                    {
                        stoppedByUser = true;
                        waveOutReset(hWaveOut);
                        break;
                    }

                    bool anyPending = false;
                    for (int i = activeChunks.Count - 1; i >= 0; i--)
                    {
                        if (activeChunks[i].IsDone())
                        {
                            if (activeChunks[i].Prepared)
                            {
                                waveOutUnprepareHeader(hWaveOut, activeChunks[i].HeaderPtr, Marshal.SizeOf(typeof(WAVEHDR)));
                            }
                            activeChunks[i].Dispose();
                            activeChunks.RemoveAt(i);
                        }
                        else
                        {
                            anyPending = true;
                        }
                    }

                    if (anyPending) Thread.Sleep(30);
                }

                if (_stopRequested) stoppedByUser = true;
                AppLogger.Log("WaveOut", "Playback ended (stoppedByUser=" + stoppedByUser + "): " + Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                AppLogger.Log("WaveOut", "StreamThread error: " + ex.Message);
            }
            finally
            {
                if (hWaveOut != IntPtr.Zero)
                {
                    try { waveOutReset(hWaveOut); } catch { }
                    foreach (var c in activeChunks)
                    {
                        try
                        {
                            if (c.Prepared) waveOutUnprepareHeader(hWaveOut, c.HeaderPtr, Marshal.SizeOf(typeof(WAVEHDR)));
                        }
                        catch { }
                        c.Dispose();
                    }
                    activeChunks.Clear();
                    lock (_stateLock) { _activeWaveOut = IntPtr.Zero; }
                    try { waveOutClose(hWaveOut); } catch { }
                }
                else
                {
                    foreach (var c in activeChunks) c.Dispose();
                    activeChunks.Clear();
                }

                if (mediaType != null) Marshal.ReleaseComObject(mediaType);
                if (reader != null) Marshal.ReleaseComObject(reader);
                if (mfStarted) MFShutdown();

                if (PlaybackFinished != null)
                {
                    PlaybackFinished(sessionId, stoppedByUser);
                }
            }
        }
    }
}
