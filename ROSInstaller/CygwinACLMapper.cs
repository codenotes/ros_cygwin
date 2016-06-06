using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ROSInstaller
{
    class CygwinACLMapper
    {
        private SecurityIdentifier _WorldSID;
        private SecurityIdentifier _GroupSID;
        private SecurityIdentifier _UserSID;
        private readonly StreamWriter _LogStream;

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }

        public CygwinACLMapper(StreamWriter logStream)
        {
            _LogStream = logStream;
            _LogStream?.WriteLine("Obtaining SIDs for user and group...");
            _WorldSID = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

            IntPtr processToken;
            var currentProc = GetCurrentProcess();
            _LogStream?.WriteLine($"Current process handle: {currentProc:x}");
            if (OpenProcessToken(currentProc, 8 /*TOKEN_QUERY*/, out processToken))
            {
                int bufferSize = 4096;
                IntPtr pBuffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    int done;
                    if (GetTokenInformation(processToken, TOKEN_INFORMATION_CLASS.TokenUser, pBuffer, bufferSize, out done))
                    {
                        IntPtr pSid = Marshal.ReadIntPtr(pBuffer);
                        if ((ulong)pSid >= (ulong)pBuffer && (ulong)pSid < ((ulong)pBuffer + (ulong)bufferSize))
                        {
                            _LogStream?.WriteLine("Creating user SecurityIdentifier from raw SID...");
                            _UserSID = new SecurityIdentifier(pSid);
                            _LogStream?.WriteLine("Current user SID: " + _UserSID);
                        }
                        else
                            _LogStream?.WriteLine($"GetTokenInformation(TokenUser) returned an invalid SID pointer: 0x{pSid:x}, with buffer at 0x{pBuffer}, size {bufferSize}");
                    }
                    else
                        _LogStream?.WriteLine("GetTokenInformation(TokenUser) failed - error " + Marshal.GetLastWin32Error());

                    if (GetTokenInformation(processToken, TOKEN_INFORMATION_CLASS.TokenPrimaryGroup, pBuffer, bufferSize, out done))
                    {
                        IntPtr pSid = Marshal.ReadIntPtr(pBuffer);
                        if ((ulong)pSid >= (ulong)pBuffer && (ulong)pSid < ((ulong)pBuffer + (ulong)bufferSize))
                        {
                            _LogStream?.WriteLine("Creating group SecurityIdentifier from raw SID...");
                            _GroupSID = new SecurityIdentifier(pSid);
                            _LogStream?.WriteLine("Current group SID: " + _GroupSID);
                        }
                        else
                            _LogStream?.WriteLine($"GetTokenInformation(TokenPrimaryGroup) returned an invalid SID pointer: 0x{pSid:x}, with buffer at 0x{pBuffer}, size {bufferSize}");
                    }
                    else
                        _LogStream?.WriteLine("GetTokenInformation(TokenUser) failed - error " + Marshal.GetLastWin32Error());
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuffer);
                    CloseHandle(processToken);
                }
            }
            else
                _LogStream?.WriteLine("OpenProcessToken() failed - error " + Marshal.GetLastWin32Error());

            if (_UserSID == null)
            {
                _LogStream?.WriteLine("Retrieving current user SID via a fallback method...");

                var currentIdentity = WindowsIdentity.GetCurrent();
                if (currentIdentity == null)
                    throw new Exception("Failed to query current user identity");

                _LogStream?.WriteLine($"Current user: {currentIdentity.Name}, SID = {currentIdentity.User}");
                _UserSID = currentIdentity.User;
                _GroupSID = null;
            }
        }

        static FileSystemRights TranslateUnixMode(uint mode, bool isOwner)
        {
            uint result = 0x120080;
            if (isOwner)
                result |= 0x0D0100;
            if ((mode & 1) != 0)
                result |= 0x000020;
            if ((mode & 2) != 0)
                result |= 0x000116;
            if ((mode & 4) != 0)
                result |= 0x000009;
            return (FileSystemRights)result;
        }

        public void ApplyFileMode(string targetPath, uint mode, bool isDirectory)
        {
            FileSystemSecurity security;
            if (isDirectory)
                security = new DirectorySecurity();
            else
                security = new FileSecurity();

            _LogStream?.WriteLine($"Creating a security object for {targetPath}...");

            security.SetAccessRuleProtection(true, false);
            _LogStream?.WriteLine($"Setting user permission...");
            security.AddAccessRule(new FileSystemAccessRule(_UserSID, TranslateUnixMode((mode >> 6) & 7, true), AccessControlType.Allow));
            if (_GroupSID != null)
            {
                _LogStream?.WriteLine($"Setting group permission...");
                security.AddAccessRule(new FileSystemAccessRule(_GroupSID, TranslateUnixMode((mode >> 3) & 7, false), AccessControlType.Allow));
            }
            _LogStream?.WriteLine($"Setting world permission...");
            security.AddAccessRule(new FileSystemAccessRule(_WorldSID, TranslateUnixMode((mode >> 0) & 7, false), AccessControlType.Allow));
            _LogStream?.WriteLine($"Setting owner...");
            security.SetOwner(_UserSID);

            _LogStream?.WriteLine($"Applying new permissions...");

            if (isDirectory)
                Directory.SetAccessControl(targetPath, (DirectorySecurity)security);
            else
                File.SetAccessControl(targetPath, (FileSecurity)security);
            _LogStream?.WriteLine($"Successfully applied permissions for {targetPath}.");
        }
    }
}
