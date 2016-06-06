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
        private readonly StreamWriter _LogStream;

        public CygwinACLMapper(StreamWriter logStream)
        {
            _LogStream = logStream;
            _LogStream?.WriteLine("Obtaining SIDs for user and group...");
            _WorldSID = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var currentIdentity = WindowsIdentity.GetCurrent();
            if (currentIdentity == null)
                throw new Exception("Failed to query current user identity");

            _LogStream?.WriteLine($"Current user: {currentIdentity.Name}, SID = {currentIdentity.User}");
            _UserSID = currentIdentity.User;

            _LogStream?.WriteLine($"Machine name: {Environment.MachineName}, domain name: {Environment.UserDomainName}");
            if (StringComparer.InvariantCultureIgnoreCase.Compare(Environment.MachineName, Environment.UserDomainName) == 0 || string.IsNullOrEmpty(Environment.UserDomainName))
            {
                _LogStream?.WriteLine($"Non-domain machine: obtaining group SID from local directory...");

                using (DirectoryEntry machine = new DirectoryEntry("WinNT://" + Environment.MachineName))
                {
                    DirectoryEntry e = machine.Children.Find("None");
                    if (e?.Properties == null)
                        throw new Exception("Failed to obtain a DirectoryEntry for " + Environment.MachineName);

                    var sid = e.Properties["objectSid"];
                    if (sid == null)
                        throw new Exception("Failed to query objectSid property");

                    var rawSid = sid[0] as byte[];
                    if (rawSid == null)
                        throw new Exception($"objectSid returned an invalid value ({rawSid})");

                    _GroupSID = new SecurityIdentifier(rawSid, 0);
                    _LogStream?.WriteLine($"Group SID: {_GroupSID}");

                }
            }
            else
            {
                _LogStream?.WriteLine($"Domain-joined machine: obtaining group SID from LDAP://{Environment.UserDomainName}...");

                using (DirectoryEntry domain = new DirectoryEntry("LDAP://" + Environment.UserDomainName))
                {
                    DirectorySearcher searcher = new DirectorySearcher(domain);
                    searcher.Filter = string.Format("(&(objectClass=user) (cn= {0}))", Environment.UserName);
                    searcher.PropertiesToLoad.Add("primaryGroupID");
                    _LogStream?.WriteLine($"Searching for {searcher.Filter}...");
                    SearchResult searchresult = searcher.FindOne();
                    if (searchresult == null)
                        throw new Exception("Failed to identify the primary group for the current user");

                    var entry = searchresult.GetDirectoryEntry();
                    if (entry == null)
                        throw new Exception("Search for current user returned no directory entries");

                    var prop = entry.Properties["primaryGroupID"];
                    if (prop?.Value == null)
                        throw new Exception("Current user object does not define primaryGroupID");

                    var primaryGroupID = (int)prop.Value;
                    prop = entry.Properties["objectSid"];
                    if (prop?.Value == null)
                        throw new Exception("Current user object does not define objectSid");

                    byte[] objectSid = (byte[])prop.Value;

                    _LogStream?.WriteLine($"primaryGroupId = {primaryGroupID}");

                    StringBuilder escapedGroupSid = new StringBuilder();
                    BitConverter.GetBytes(primaryGroupID).CopyTo(objectSid, objectSid.Length - 4);
                    _GroupSID = new SecurityIdentifier(objectSid, 0);
                    _LogStream?.WriteLine($"Derived group SID = {_GroupSID}");
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

            _LogStream?.WriteLine($"Creating a security object for {targetPath}...");

            security.SetAccessRuleProtection(true, false);
            _LogStream?.WriteLine($"Setting user permission...");
            security.AddAccessRule(new FileSystemAccessRule(_UserSID, TranslateUnixMode((mode >> 6) & 7, true), AccessControlType.Allow));
            _LogStream?.WriteLine($"Setting group permission...");
            security.AddAccessRule(new FileSystemAccessRule(_GroupSID, TranslateUnixMode((mode >> 3) & 7, false), AccessControlType.Allow));
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
