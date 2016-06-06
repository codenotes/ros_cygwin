using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Serialization;

namespace ROSInstaller
{
    [Obfuscation(Exclude = true)]
    public class DirectoryStatistics
    {
        public int TotalFiles;
        public int TotalDirectories;
        public int TotalSymlinks;
        public long TotalBytes;
    }

    public class CygwinTarUnpacker
    {
        Stream _Stream;
        string _TargetDir;

        byte[] TempBuffer = new byte[1024 * 1024];

        public CygwinTarUnpacker(Stream stream, string targetDir, StreamWriter logStream)
        {
            _Stream = stream;
            _TargetDir = targetDir;
            _LogStream = logStream;
            _ACLMapper = new CygwinACLMapper(logStream);
        }

        static System.DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        long _BytesReadFromTARFile;

        DirectoryStatistics Statistics = new DirectoryStatistics();

        //WARNING: fileName can be set to null unless preparing to copy a new file
        public delegate void TarProgressHandler(bool isSymlinkPhase, string fileName, long fileSize, long bytesDone, int filesDone, long bytesDoneBeforeThisFile);
        public TarProgressHandler ProgressHandler;
        CygwinACLMapper _ACLMapper;
        private readonly StreamWriter _LogStream;

        byte[] DoReadHeader(out string fileName, out ulong fileSize, out byte linkIndicator)
        {
            byte[] header = new byte[512];
            int done = _Stream.Read(header, 0, 512);
            if (done == 0 || header[0] == 0)
            {
                fileName = null;
                fileSize = 0;
                linkIndicator = 0;
                return null;
            }

            _BytesReadFromTARFile += done;

            if (done != 512)
                throw new IOException("Incomplete TAR header");

            fileName = Encoding.ASCII.GetString(header, 0, 100).TrimEnd('\0');
            if (fileName == "")
                throw new Exception("Invalid file name in a TAR record");

            fileSize = Convert.ToUInt64(Encoding.ASCII.GetString(header, 124, 12).TrimEnd('\0', ' '), 8);
            linkIndicator = header[156];
            return header;
        }

        public delegate void PathFilter(string extractedDir, ref string relativePath);
        public event PathFilter TransformPath;

        bool UnpackTarRecord(bool ignoreFileCreationErrors, bool stripFirstComponent)
        {
            string fileName;
            ulong fileSize;
            byte linkIndicator;

            byte[] header = DoReadHeader(out fileName, out fileSize, out linkIndicator);
            if (header == null)
                return false;

            _LogStream?.WriteLine("Read TAR entry for " + fileName);

            if (linkIndicator == 'L' && fileName == "././@LongLink")
            {
                _LogStream?.WriteLine("Reading link contents...");

                int padding = 512 - (int)(fileSize % 512);
                byte[] longFileName = new byte[fileSize];
                if (_Stream.Read(longFileName, 0, (int)fileSize) != (int)fileSize)
                    throw new IOException("Incomplete long TAR name");

                if (padding != 512)
                    _Stream.Read(header, 0, padding);

                header = DoReadHeader(out fileName, out fileSize, out linkIndicator);
                if (header == null)
                    return false;

                fileName = Encoding.ASCII.GetString(longFileName, 0, longFileName.Length).TrimEnd('\0');
                _LogStream?.WriteLine($"Long entry name: {fileName}");
            }

            ulong rawTimestamp = Convert.ToUInt64(Encoding.ASCII.GetString(header, 136, 12).TrimEnd('\0', ' '), 8);
            DateTime timeStamp = UnixEpoch.AddSeconds(rawTimestamp);

            char lastChar = fileName[fileName.Length - 1];
            bool isDirectory = ((lastChar == '/') || (lastChar == '\\') || (linkIndicator == '5'));
            bool isHardLink = (linkIndicator == '1');
            bool isSymlink = (linkIndicator == '2');

            bool isUstar = (header[257] == 'u') && (header[258] == 's') && (header[259] == 't') && (header[260] == 'a') && (header[261] == 'r');

            if (isUstar && (header[345] != 0))  //Parse name prefix if needed
            {
                string namePrefix = Encoding.ASCII.GetString(header, 345, 155).TrimEnd('\0');
                if (!namePrefix.EndsWith("/"))
                    namePrefix += '/';
                if (namePrefix != "")
                    fileName = namePrefix + fileName;
            }

            uint mode = Convert.ToUInt32(Encoding.ASCII.GetString(header, 100, 8).TrimEnd('\0', ' '), 8);

            if (fileName == null)
                throw new Exception("Unexpected null file name in TAR archive");

            if (fileName.StartsWith("..") || fileName.StartsWith("\\") || fileName.StartsWith("/"))
                throw new Exception("Invalid file name in a TAR record");

            fileName = fileName.TrimStart('/', '\\');
            if (stripFirstComponent)
            {
                int idx = fileName.IndexOf('/');
                if (idx != -1)
                    fileName = fileName.Substring(idx + 1).TrimStart('/', '\\');
            }

            string targetPath = null;
            if (fileName != "")
            {
                fileName = fileName.Replace('/', '\\');
                if (TransformPath != null)
                    TransformPath(_TargetDir, ref fileName);
                targetPath = Path.Combine(_TargetDir, fileName);
            }

            _LogStream?.WriteLine($"Target file path: {targetPath}");

            if (ProgressHandler != null)
                ProgressHandler(false, targetPath, (long)fileSize, 0, Statistics.TotalFiles, Statistics.TotalBytes);

            if (isDirectory)
            {
                _LogStream?.WriteLine($"Target entry is a directory");

                if (targetPath != null)
                {
                    Statistics.TotalDirectories++;

                    if (fileSize != 0)
                    {
                        throw new Exception("Found a TAR directory record with non-zero data size:" + targetPath);
                    }

                    try
                    {
                        _LogStream?.WriteLine($"Creating {targetPath}");
                        Directory.CreateDirectory(targetPath);

                        _LogStream?.WriteLine($"Setting last write time on {targetPath}...");
                        Directory.SetLastWriteTimeUtc(targetPath, timeStamp);

                    }
                    catch (Exception ex)
                    {
                        _LogStream?.WriteLine($"Failed to create {targetPath} - {ex.Message}");

                        if (!ignoreFileCreationErrors)
                            throw;
                    }
                }
            }
            else if (isSymlink)
            {
                if (targetPath != null)
                {
                    Statistics.TotalSymlinks++;

                    string linkTarget = Encoding.ASCII.GetString(header, 157, 100).TrimEnd('\0');
                    _LogStream?.WriteLine($"{targetPath} is a symlink to {linkTarget}. Creating a Cygwin symlink...");
                    File.WriteAllText(targetPath, "!<symlink>" + linkTarget + "\0", Encoding.ASCII);
                    File.SetAttributes(targetPath, FileAttributes.System | FileAttributes.Normal);
                    File.SetLastWriteTimeUtc(targetPath, timeStamp);
                }
            }
            else if (isHardLink)
            {
                if (targetPath != null)
                {
                    string linkTarget = Encoding.ASCII.GetString(header, 157, 100).TrimEnd('\0');
                    _LogStream?.WriteLine($"{targetPath} is a hardlink to {linkTarget}. Creating an NTFS hardlink...");

                    if (stripFirstComponent)
                    {
                        int idx = linkTarget.IndexOf('/');
                        if (idx != -1)
                            linkTarget = linkTarget.Substring(idx + 1).TrimStart('/', '\\');
                    }

                    linkTarget = linkTarget.Replace('/', '\\');
                    if (TransformPath != null)
                        TransformPath(_TargetDir, ref linkTarget);
                    linkTarget = Path.Combine(_TargetDir, linkTarget);

                    if (File.Exists(linkTarget))
                    {
                        _LogStream?.WriteLine($"Hardlinking {targetPath} to {linkTarget}...");

                        CreateHardLink(targetPath, linkTarget, IntPtr.Zero);
                    }
                    else
                    {
                        _LogStream?.WriteLine($"Missing {linkTarget} - cannot create {targetPath}");

                        if (!ignoreFileCreationErrors)
                            throw new Exception("Missing hardlink source");
                    }
                }
            }
            else
            {
                FileStream fs = null;
                try
                {
                    _LogStream?.WriteLine($"Creating {targetPath}...");

                    if (targetPath != null)
                        fs = File.Create(targetPath);
                }
                catch(Exception ex)
                {
                    _LogStream?.WriteLine($"Failed to create {targetPath} - {ex.Message}");
                    if (!ignoreFileCreationErrors)
                        throw;
                }

                _LogStream?.WriteLine($"Writing contents for {targetPath}...");

                using (fs)
                    CopyStream(_Stream, fs, (long)fileSize);

                _LogStream?.WriteLine($"Done writing contents for {targetPath}.");

                Statistics.TotalFiles++;
                Statistics.TotalBytes += (long)fileSize;
                _BytesReadFromTARFile += (long)fileSize;

                int padding = 512 - (int)(fileSize % 512);
                if (padding != 512)
                    _Stream.Read(header, 0, padding);
                _BytesReadFromTARFile += padding;

                _LogStream?.WriteLine($"Setting last write time for {targetPath}...");
                File.SetLastWriteTimeUtc(targetPath, timeStamp);
            }

            if (targetPath != null && File.Exists(targetPath))
            {
                _LogStream?.WriteLine($"Applying security for {targetPath}...");
                _ACLMapper.ApplyFileMode(targetPath, mode, isDirectory);
                _LogStream?.WriteLine($"Done applying security for {targetPath}.");
            }

            return true;
        }

        void CopyStream(Stream source, Stream dest, long byteCount)
        {
            long done = 0;
            while (done < byteCount)
            {
                long remaining = byteCount - done;
                int todo = (int)Math.Min(remaining, TempBuffer.Length);

                if (source.Read(TempBuffer, 0, todo) != todo)
                    throw new IOException("Cannot read TAR archive body");
                if (dest != null)
                    dest.Write(TempBuffer, 0, todo);
                done += todo;

                if (ProgressHandler != null)
                    ProgressHandler(false, null, byteCount, done, Statistics.TotalFiles, Statistics.TotalBytes);
            }
        }

        public void Unpack(bool ignoreFileCreationErrors, bool stripFirstComponent)
        {
            for (int i = 0; ;i++)
            {
                if (!UnpackTarRecord(ignoreFileCreationErrors, stripFirstComponent))
                    break;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetFileAttributes(string lpFileName);

        static string CombineAndFixUnixPath(string first, string second)
        {
            Stack<string> path = new Stack<string>(first.Split(new char[]{'/'}, StringSplitOptions.RemoveEmptyEntries));
            
            foreach (string s in second.Split('/'))
            {
                if (s == "")
                    continue;
                if (s == ".." && path.Count > 0)
                    path.Pop();
                path.Push(s);
            }

            return "/" + string.Join("/", path.ToArray());
        }

        int DetermineMaxDirectedDepthOfRelPath(string path, bool backDepth)
        {
            int max = 0, depth = 0;
            foreach (string s in path.Split('/'))
            {
                if (s == "")
                    continue;
                if (s == "..")
                    depth++;
                else
                    depth--;

                if (backDepth)
                    max = Math.Max(max, depth);
                else
                    max = Math.Max(max, -depth);
            }
            return max;
        }

        public void SaveStatistics(string fn)
        {
            var ser = new XmlSerializer(typeof(DirectoryStatistics));
            using (var fs = File.Create(fn))
                ser.Serialize(fs, Statistics);
        }


        public void ExportStatistics(DirectoryStatistics exportedStatistics)
        {
            if (exportedStatistics == null)
                return;
            exportedStatistics.TotalBytes += Statistics.TotalBytes;
            exportedStatistics.TotalDirectories += Statistics.TotalDirectories;
            exportedStatistics.TotalFiles += Statistics.TotalFiles;
            exportedStatistics.TotalSymlinks += Statistics.TotalSymlinks;
        }
        
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }

}
