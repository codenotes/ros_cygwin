using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
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

        public CygwinACLMapper()
        {
            _WorldSID = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var currentIdentity = WindowsIdentity.GetCurrent();
            _UserSID = currentIdentity.User;

            if (Environment.MachineName == Environment.UserDomainName)
            {
                using (DirectoryEntry machine = new DirectoryEntry("WinNT://" + Environment.MachineName))
                {
                    DirectoryEntry e = machine.Children.Find("None");
                    _GroupSID = new SecurityIdentifier(e.Properties["objectSid"][0] as byte[], 0);
                }
            }
            else
            {
                using (DirectoryEntry domain = new DirectoryEntry("LDAP://" + Environment.UserDomainName))
                {
                    DirectorySearcher searcher = new DirectorySearcher(domain);
                    searcher.Filter = string.Format("(&(objectClass=user) (cn= {0}))", Environment.UserName);
                    searcher.PropertiesToLoad.Add("primaryGroupID");
                    SearchResult searchresult = searcher.FindOne();
                    var entry = searchresult.GetDirectoryEntry();
                    var primaryGroupID = (int)entry.Properties["primaryGroupID"].Value;

                    byte[] objectSid = (byte[])entry.Properties["objectSid"].Value;

                    StringBuilder escapedGroupSid = new StringBuilder();
                    BitConverter.GetBytes(primaryGroupID).CopyTo(objectSid, objectSid.Length - 4);
                    _GroupSID = new SecurityIdentifier(objectSid, 0);
                }
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

            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(_UserSID, TranslateUnixMode((mode >> 6) & 7, true), AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(_GroupSID, TranslateUnixMode((mode >> 3) & 7, false), AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(_WorldSID, TranslateUnixMode((mode >> 0) & 7, false), AccessControlType.Allow));
            security.SetOwner(_UserSID);

            if (isDirectory)
                Directory.SetAccessControl(targetPath, (DirectorySecurity)security);
            else
                File.SetAccessControl(targetPath, (FileSecurity)security);
        }
    }
}
