using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PasswordGenLocal.Core
{
    public enum JoinType
    {
        Standalone,
        Workgroup,
        DomainJoined,
        EntraJoined,
        DomainAndEntraJoined
    }

    public static class EnterpriseDetector
    {
        // NetGetJoinInformation from netapi32.dll
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetGetJoinInformation(
            string? server,
            out IntPtr nameBuffer,
            out int bufferType);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        private const int NERR_Success        = 0;
        private const int NetSetupUnjoined    = 0;
        private const int NetSetupWorkgroupName = 2;
        private const int NetSetupDomainName  = 3;

        public static JoinType Detect()
        {
            bool domainJoined = false;
            bool entraJoined  = false;

            // Single call to NetGetJoinInformation covers both the AD-domain check and
            // the workgroup fallback — the bufferType value is retained for both uses.
            int joinBufferType = NetSetupUnjoined;
            try
            {
                IntPtr nameBuffer = IntPtr.Zero;
                int result = NetGetJoinInformation(null, out nameBuffer, out int bufferType);
                if (nameBuffer != IntPtr.Zero)
                    NetApiBufferFree(nameBuffer);

                if (result == NERR_Success)
                {
                    joinBufferType = bufferType;
                    domainJoined   = bufferType == NetSetupDomainName;
                }
            }
            catch { /* P/Invoke unavailable or access denied */ }

            // Check Entra ID (Azure AD) join via registry
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo",
                    writable: false);
                if (key != null)
                {
                    string[] subkeys = key.GetSubKeyNames();
                    entraJoined = subkeys.Length > 0;
                }
            }
            catch { /* registry access denied or key missing */ }

            if (domainJoined && entraJoined) return JoinType.DomainAndEntraJoined;
            if (domainJoined)               return JoinType.DomainJoined;
            if (entraJoined)                return JoinType.EntraJoined;

            // Workgroup check reuses the bufferType captured above — no second P/Invoke needed.
            if (joinBufferType == NetSetupWorkgroupName)
                return JoinType.Workgroup;

            return JoinType.Standalone;
        }

        public static string Describe(JoinType jt) => jt switch
        {
            JoinType.Standalone            => "Standalone (not joined to any domain or directory)",
            JoinType.Workgroup             => "Workgroup member (peer network, no domain)",
            JoinType.DomainJoined          => "Active Directory domain-joined",
            JoinType.EntraJoined           => "Entra ID (Azure AD) joined",
            JoinType.DomainAndEntraJoined  => "Hybrid-joined (AD domain + Entra ID)",
            _                              => "Unknown"
        };
    }
}
