using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LanceurRaccourcis
{
    internal static class NetworkDriveHelper
    {
        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string? lpLocalName;
            public string? lpRemoteName;
            public string? lpComment;
            public string? lpProvider;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE lpNetResource, string? lpPassword, string? lpUserName, int dwFlags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        public static bool MapNetworkDrive(string localName, string remoteName, string? username, string? password, bool persistent, out string? error)
        {
            error = null;
            var nr = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpLocalName = localName,
                lpRemoteName = remoteName,
                lpProvider = null,
                dwScope = 0,
                dwDisplayType = 0,
                dwUsage = 0
            };

            int flags = persistent ? CONNECT_UPDATE_PROFILE : 0;
            int result = WNetAddConnection2(ref nr, password, username, flags);
            if (result == 0)
            {
                return true;
            }

            try
            {
                error = new Win32Exception(result).Message;
            }
            catch
            {
                error = $"Code d'erreur: {result}";
            }
            return false;
        }

        public static bool UnmapNetworkDrive(string localName, bool force, out string? error)
        {
            error = null;
            int result = WNetCancelConnection2(localName, 0, force);
            if (result == 0)
            {
                return true;
            }
            try
            {
                error = new Win32Exception(result).Message;
            }
            catch
            {
                error = $"Code d'erreur: {result}";
            }
            return false;
        }
    }
}