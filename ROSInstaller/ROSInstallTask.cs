using SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ROSInstaller
{
    class ROSInstallTask : ICodeProgress, IDisposable
    {
        public readonly string DestDir;
        Exception _TarException, _DecompException;
        Thread _DecompThread, _TarThread;

        Stream _TarStream;

        long _TotalSize, _Done;
        private AnonymousPipeServerStream _OutPipe;

        public bool HasExited
        {
            get { return !_DecompThread.IsAlive && !_TarThread.IsAlive; }
        }

        public string ErrorMessage
        {
            get
            {
                StringBuilder result = new StringBuilder();
                if (_DecompException != null)
                    result.AppendLine("Decompressing failed: " + _DecompException.Message);
                if (_TarException != null)
                    result.AppendLine("TAR Unpacking failed: " + _TarException.Message);
                return result.Length == 0 ? null : result.ToString();
            }
        }

        public ROSInstallTask(string destDir)
        {
            DestDir = destDir;
            _TarThread = new Thread(TarExtractThreadBody);
            _DecompThread = new Thread(DecompressionThreadBody);

            _OutPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            _TarStream = new FullReadAdapter(new AnonymousPipeClientStream(PipeDirection.In, _OutPipe.ClientSafePipeHandle));

            _DecompThread.Start();
            _TarThread.Start();
        }

        void TarExtractThreadBody()
        {
            try
            {
                CygwinTarUnpacker unpacker = new CygwinTarUnpacker(_TarStream, DestDir);
                Directory.CreateDirectory(DestDir);
                unpacker.Unpack(false, true);

                File.WriteAllText(Path.Combine(DestDir, "cygwin.bat"), "@set MYDIR=%~dp0\r\n@start %MYDIR%\\bin\\mintty.exe -i /Cygwin-Terminal.ico -\r\n");

                var bash = Path.Combine(DestDir, "bin", "bash.exe");
                var startInfo = new ProcessStartInfo(bash, "--login -c exit") { UseShellExecute = false, CreateNoWindow = true };
                var proc = Process.Start(startInfo);
                if (!proc.WaitForExit(20000))
                    proc.Kill();

                var profile = Path.Combine(DestDir, "home", Environment.UserName, ".bashrc");
                if (!File.Exists(profile))
                    throw new Exception(profile + " not found. Please run bash and update it manually");

                File.AppendAllText(profile, "\ncd /opt/ros/install_isolated\nCATKIN_SHELL=sh\n. ./setup.sh\nexport PATH=$PATH:/usr/local/lib:/opt/ros/install_isolated/lib\n");

                //The unpacker might have returned due to reading an empty record that may be followed by some padding records.
                //If we close the pipe without reading the padding records, the unpacking thread will see this as an error.
                byte[] data = new byte[512];
                for (;;)
                {
                    int done = _TarStream.Read(data, 0, data.Length);
                    if (done == 0)
                        break;
                    for (int i = 0; i < done; i++)
                        if (data[i] != 0)
                            throw new Exception("Unexpected data after the end of TAR stream:" + string.Format("{0:x2}", data[i]));
                }
            }
            catch (Exception ex)
            {
                _TarException = ex;
            }
            finally
            {
                _TarStream.Close();
            }
        }

        void DecompressionThreadBody()
        {
            try
            {
                FileStream inStream;
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    string fn = args[1];
                    long offset = 0;
                    int idx = fn.IndexOf('@');
                    if (idx != -1)
                    {
                        offset = long.Parse(fn.Substring(idx + 1));
                        fn = fn.Substring(0, idx);
                    }
                    inStream = new FileStream(fn, FileMode.Open, FileAccess.Read);
                    inStream.Seek(offset, SeekOrigin.Begin);
                }
                else
                {
                    inStream = new FileStream(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read);
                    inStream.Seek(88576, SeekOrigin.Begin);
                }

                byte[] properties = new byte[5];
                if (inStream.Read(properties, 0, 5) != 5)
                    throw (new Exception("input .lzma is too short"));

                var decoder = new SevenZip.Compression.LZMA.Decoder();
                decoder.SetDecoderProperties(properties);

                long outSize = 0;
                for (int i = 0; i < 8; i++)
                {
                    int v = inStream.ReadByte();
                    if (v < 0)
                        throw (new Exception("Can't Read size from LZMA header"));
                    outSize |= ((long)(byte)v) << (8 * i);
                }
                long compressedSize = inStream.Length - inStream.Position;
                _TotalSize = compressedSize;
                decoder.Code(inStream, _OutPipe, compressedSize, outSize, this);
            }
            catch (Exception ex)
            {
                _DecompException = ex;
            }
            finally
            {
                _OutPipe.Close();
            }
        }

        public void SetProgress(long inSize, long outSize)
        {
            _Done = inSize;
        }

        public void ApplyProgress(ProgressBar bar)
        {
            if (_TotalSize == 0)
                return;
            try
            {
                int val = (int)((_Done * bar.Maximum) / _TotalSize);
                bar.Value = Math.Min(val, bar.Maximum);
            }
            catch { }
        }

        public void Dispose()
        {
            _OutPipe.Dispose();
            _TarStream.Dispose();
        }

        internal void Abort()
        {
            _DecompThread.Abort();
            _TarThread.Abort();
            _OutPipe.Close();
            _TarStream.Close();
        }
    }
}
