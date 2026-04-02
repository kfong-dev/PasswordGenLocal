using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGenLocal.Core
{
    public enum CharsetMode
    {
        Web = 0,
        Nix = 1,
        Azure = 2
    }

    public static class PasswordGenerator
    {
        // Web: alphanumeric + !#$%&()*+-./:;=?@_~
        private const string CS_WEB = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+-./:;=?@_~";
        // NIX/POSIX: alphanumeric + !#%+,-./:=@_~
        private const string CS_NIX = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#%+,-./:=@_~";
        // Azure: full 95 printable ASCII (0x20..0x7E)
        private static readonly string CS_AZURE = BuildAzureCharset();

        private static string BuildAzureCharset()
        {
            var sb = new StringBuilder(95);
            for (int c = 0x20; c <= 0x7E; c++)
                sb.Append((char)c);
            return sb.ToString();
        }

        private static string GetCharset(CharsetMode mode) => mode switch
        {
            CharsetMode.Web   => CS_WEB,
            CharsetMode.Nix   => CS_NIX,
            CharsetMode.Azure => CS_AZURE,
            _                 => CS_WEB
        };

        /// <summary>
        /// Returns a human-readable description of the generation settings,
        /// e.g. "Web Password (16 characters)". Used as the entry label and in UI columns.
        /// </summary>
        public static string FriendlyName(CharsetMode mode, int length)
        {
            string typeName = mode switch
            {
                CharsetMode.Web   => "Web Password",
                CharsetMode.Nix   => "POSIX Password",
                CharsetMode.Azure => "Active Directory Password",
                _                 => "Password"
            };
            return $"{typeName} ({length} characters)";
        }

        /// <summary>Returns the short display label for a charset mode (e.g. "Web/OAuth").</summary>
        public static string CharsetLabel(CharsetMode mode) => mode switch
        {
            CharsetMode.Web   => "Web/OAuth",
            CharsetMode.Nix   => "NIX/POSIX",
            CharsetMode.Azure => "Azure AD/Kerberos",
            _                 => "Unknown"
        };

        /// <summary>
        /// Returns the abbreviated label for use in grid "Settings" columns,
        /// e.g. "Web (16 characters)". More compact than FriendlyName.
        /// </summary>
        public static string ColumnLabel(CharsetMode mode, int length) => mode switch
        {
            CharsetMode.Web   => $"Web ({length} characters)",
            CharsetMode.Nix   => $"POSIX ({length} characters)",
            CharsetMode.Azure => $"Kerberos (AD) ({length} characters)",
            _                 => $"Password ({length} characters)"
        };

        /// <summary>
        /// Generates a cryptographically strong password using rejection sampling (no modular bias).
        /// Fresh CSPRNG state every call.
        /// </summary>
        public static string Generate(CharsetMode mode, int length)
        {
			if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 1");
			if (length > 128) throw new ArgumentOutOfRangeException(nameof(length), "Length must be <= 128");
            string charset = GetCharset(mode);
            int charsetLen = charset.Length;
            // Find the largest multiple of charsetLen that fits in a byte (for rejection sampling)
            int maxValid = (256 / charsetLen) * charsetLen;

            char[] result = new char[length];
            using var rng = RandomNumberGenerator.Create();
            byte[] buf = new byte[length * 4]; // over-provision for rejection

            int filled = 0;
            while (filled < length)
            {
                rng.GetBytes(buf);
                foreach (byte b in buf)
                {
                    if (b < maxValid)
                    {
                        result[filled++] = charset[b % charsetLen];
                        if (filled == length) break;
                    }
                }
                // If not enough after one pass, loop again (very rare)
            }

            return new string(result);
        }
    }
}
