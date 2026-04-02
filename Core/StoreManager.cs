using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PasswordGenLocal.Core
{
    public static class StoreManager
    {
        // Magic bytes: ASCII "LPGE"
        private static readonly byte[] MAGIC = { 0x4C, 0x50, 0x47, 0x45 };
        private const ushort VERSION = 1;
        private const byte ALGO_SHA3_384 = 0x01;
        private const byte ALGO_SHA512   = 0x02;

        // Checked once at startup
        public static readonly bool UseSha3_384 = SHA3_384.IsSupported;
        public static string HashAlgorithmLabel => UseSha3_384 ? "SHA3-384" : "SHA-512";

        private const int SALT_LEN   = 32;
        private const int NONCE_LEN  = 12;
        private const int TAG_LEN    = 16;
        private const int HASH_FIELD = 64;
        private const int PBKDF2_ITER = 210_000;
        private const int KEY_LEN    = 32;

        // ── JSON DTOs ─────────────────────────────────────────────────────────

        private class EntryDto
        {
            [JsonPropertyName("id")]     public string Id      { get; set; } = "";
            [JsonPropertyName("label")]  public string Label   { get; set; } = "";
            [JsonPropertyName("note")]   public string Note    { get; set; } = "";
            [JsonPropertyName("ts")]     public string Ts      { get; set; } = "";
            [JsonPropertyName("mode")]   public int Mode       { get; set; }
            [JsonPropertyName("length")] public int Length     { get; set; }
            [JsonPropertyName("pwd")]    public string Pwd     { get; set; } = "";
        }

        private class SettingsDto
        {
            [JsonPropertyName("trackAndClear")] public bool TrackAndClear { get; set; }
        }

        private class StoreDto
        {
            [JsonPropertyName("v")]        public int Version      { get; set; }
            [JsonPropertyName("name")]     public string Name      { get; set; } = "";
            [JsonPropertyName("created")]  public string Created   { get; set; } = "";
            [JsonPropertyName("modified")] public string Modified  { get; set; } = "";
            [JsonPropertyName("settings")] public SettingsDto Settings { get; set; } = new();
            [JsonPropertyName("entries")]  public List<EntryDto> Entries { get; set; } = new();
        }

        // ── Compute integrity hash ─────────────────────────────────────────────

        // Used by SaveStore: always writes with the runtime's best algorithm.
        private static byte[] ComputeHash(byte[] data)
        {
            if (UseSha3_384)
            {
                byte[] h48 = SHA3_384.HashData(data);
                byte[] result = new byte[64];
                h48.CopyTo(result, 0);
                // last 16 bytes remain zero padding
                return result;
            }
            else
            {
                return SHA512.HashData(data);
            }
        }

        // Used by LoadStore / VerifyIntegrity: honours the algorithm the file was
        // actually written with, regardless of what the current runtime supports.
        private static byte[] ComputeHashForAlgoId(byte[] data, byte algoId)
        {
            if (algoId == ALGO_SHA3_384)
            {
                byte[] h48 = SHA3_384.HashData(data);
                byte[] result = new byte[64];
                h48.CopyTo(result, 0);
                return result;
            }
            // ALGO_SHA512 or any future unknown id falls back to SHA-512
            return SHA512.HashData(data);
        }

        // ── SaveStore ──────────────────────────────────────────────────────────

        public static void SaveStore(LpgeStore store, string path, string passphrase)
        {
            // Build JSON payload
            var dto = new StoreDto
            {
                Version  = VERSION,
                Name     = store.StoreName,
                Created  = store.Created.ToString("O"),
                Modified = DateTime.Now.ToString("O"),
                Settings = new SettingsDto { TrackAndClear = store.TrackAndClear },
                Entries  = new List<EntryDto>()
            };
            foreach (var e in store.Entries)
            {
                dto.Entries.Add(new EntryDto
                {
                    Id     = e.Id.ToString(),
                    Label  = e.Label,
                    Note   = e.Note,
                    Ts     = e.Timestamp.ToString("O"),
                    Mode   = (int)e.Mode,
                    Length = e.Length,
                    Pwd    = e.Password
                });
            }
            byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto));

            // Derive key
            byte[] salt = RandomNumberGenerator.GetBytes(SALT_LEN);
            byte[] key  = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(passphrase),
                salt, PBKDF2_ITER, HashAlgorithmName.SHA512, KEY_LEN);

            // Encrypt
            byte[] nonce      = RandomNumberGenerator.GetBytes(NONCE_LEN);
            byte[] ciphertext = new byte[json.Length];
            byte[] tag        = new byte[TAG_LEN];
            using (var aes = new AesGcm(key, TAG_LEN))
            {
                aes.Encrypt(nonce, json, ciphertext, tag);
            }

            // Assemble file (without hash)
            byte algoId = UseSha3_384 ? ALGO_SHA3_384 : ALGO_SHA512;
            using var ms = new MemoryStream();
            ms.Write(MAGIC);
            ms.Write(BitConverter.GetBytes(VERSION));
            ms.WriteByte(algoId);
            ms.Write(salt);
            ms.Write(nonce);
            ms.Write(BitConverter.GetBytes(ciphertext.Length));
            ms.Write(ciphertext);
            ms.Write(tag);

            byte[] bodyBytes = ms.ToArray();
            byte[] hash = ComputeHash(bodyBytes);

            // Final buffer
            using var full = new MemoryStream();
            full.Write(bodyBytes);
            full.Write(hash);
            byte[] fileBytes = full.ToArray();

 // Atomic write — SMB/network share safe
string tmp = path + ".tmp";
try
{
    File.WriteAllBytes(tmp, fileBytes);

    if (File.Exists(path))
    {
        // Pass null as backup path — avoids creating .bak files that
        // accumulate on network shares and require directory write permission
        File.Replace(tmp, path, destinationBackupFileName: null);
    }
    else
    {
        File.Move(tmp, path);
    }
}
catch (Exception ex)
{
    // Clean up orphaned .tmp before surfacing the error —
    // without this, failed saves leave encrypted data on the share
    try
    {
        if (File.Exists(tmp)) File.Delete(tmp);
    }
    catch { /* best-effort cleanup */ }

    throw new IOException($"Store save failed: {ex.Message}", ex);
}
		}
        // ── LoadStore ──────────────────────────────────────────────────────────

        public static (LpgeStore? store, string error) LoadStore(string path, string passphrase)
        {
            if (!File.Exists(path))
                return (null, "File not found.");

            byte[] fileBytes;
            try { fileBytes = File.ReadAllBytes(path); }
            catch (Exception ex) { return (null, $"Cannot read file: {ex.Message}"); }

            // Auto-backup before parsing
            try
            {
                string bak = path + ".bak";
                File.Copy(path, bak, overwrite: true);
            }
            catch { /* best-effort */ }

            // Minimum size: 4+2+1+32+12+4+0+16+64 = 135
            if (fileBytes.Length < 135)
                return (null, "File is too small to be a valid LPGE store.");

            // Check magic
            for (int i = 0; i < 4; i++)
                if (fileBytes[i] != MAGIC[i])
                    return (null, "Invalid file format (magic mismatch).");

            // Peek algo byte at offset 6 (magic[4] + version[2]) before integrity check
            // so we verify using the algorithm the file was actually written with.
            int hashStart = fileBytes.Length - HASH_FIELD;
            byte[] body       = fileBytes[..hashStart];
            byte[] storedHash = fileBytes[hashStart..];

            byte algoIdPeek = body[6];
            if (algoIdPeek != ALGO_SHA3_384 && algoIdPeek != ALGO_SHA512)
                return (null, $"Unknown integrity algorithm (0x{algoIdPeek:X2}) in store file.");
            if (algoIdPeek == ALGO_SHA3_384 && !SHA3_384.IsSupported)
                return (null, "This store was created with SHA3-384 integrity, which is not " +
                              "available on this system. Windows 11 Build 22000 or later is required.");

            // Verify integrity hash using the algorithm recorded in the file
            byte[] computed = ComputeHashForAlgoId(body, algoIdPeek);
            if (!CryptographicOperations.FixedTimeEquals(computed, storedHash))
                return (null, "Integrity check failed. File may be corrupted or tampered with.");

            // Parse header
            int pos = 4;
            ushort version = BitConverter.ToUInt16(body, pos); pos += 2;
            if (version != 1)
                return (null, $"Unsupported store version: {version}.");

            byte algoId = body[pos]; pos += 1;

            byte[] salt = body[pos..(pos + SALT_LEN)]; pos += SALT_LEN;
            byte[] nonce = body[pos..(pos + NONCE_LEN)]; pos += NONCE_LEN;
            int cipherLen = BitConverter.ToInt32(body, pos); pos += 4;
            if (cipherLen < 0 || pos + cipherLen + TAG_LEN > body.Length)
                return (null, "File structure is invalid (length mismatch).");

            byte[] ciphertext = body[pos..(pos + cipherLen)]; pos += cipherLen;
            byte[] tag = body[pos..(pos + TAG_LEN)];

            // Derive key
            byte[] key;
            try
            {
                key = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(passphrase),
                    salt, PBKDF2_ITER, HashAlgorithmName.SHA512, KEY_LEN);
            }
            catch (Exception ex) { return (null, $"Key derivation failed: {ex.Message}"); }

            // Decrypt
            byte[] plain;
            try
            {
                plain = new byte[cipherLen];
                using var aes = new AesGcm(key, TAG_LEN);
                aes.Decrypt(nonce, ciphertext, tag, plain);
            }
            catch (AuthenticationTagMismatchException)
            {
                return (null, "Incorrect passphrase or file is corrupted.");
            }
            catch (Exception ex)
            {
                return (null, $"Decryption failed: {ex.Message}");
            }

            // Parse JSON
            StoreDto? dto;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                dto = JsonSerializer.Deserialize<StoreDto>(plain, opts);
                if (dto == null) return (null, "Failed to parse store payload.");
            }
            catch (Exception ex) { return (null, $"JSON parse error: {ex.Message}"); }

            // Build LpgeStore
            var store = new LpgeStore
            {
                StoreName    = dto.Name,
                Created      = ParseDate(dto.Created),
                Modified     = ParseDate(dto.Modified),
                TrackAndClear = dto.Settings?.TrackAndClear ?? false,
                FilePath     = path,
                IsDirty      = false,
                Entries      = new List<StoreEntry>()
            };

            foreach (var e in dto.Entries)
            {
                store.Entries.Add(new StoreEntry
                {
                    Id        = Guid.TryParse(e.Id, out var g) ? g : Guid.NewGuid(),
                    Label     = e.Label,
                    Note      = e.Note,
                    Timestamp = ParseDate(e.Ts),
                    Mode      = (CharsetMode)e.Mode,
                    Length    = e.Length,
                    Password  = e.Pwd
                });
            }

            return (store, string.Empty);
        }

        // ── VerifyIntegrity ────────────────────────────────────────────────────

        public static bool VerifyIntegrity(string path)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                if (fileBytes.Length < 135) return false;
                int hashStart = fileBytes.Length - HASH_FIELD;
                byte[] body   = fileBytes[..hashStart];
                byte[] stored = fileBytes[hashStart..];

                byte algoId = body[6];
                if (algoId == ALGO_SHA3_384 && !SHA3_384.IsSupported) return false;

                byte[] computed = ComputeHashForAlgoId(body, algoId);
                return CryptographicOperations.FixedTimeEquals(computed, stored);
            }
            catch { return false; }
        }

        private static DateTime ParseDate(string s)
        {
            if (DateTime.TryParse(s, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToLocalTime();
            return DateTime.Now;
        }
    }
}
