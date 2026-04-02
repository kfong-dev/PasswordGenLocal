using System;
using System.Collections.Generic;

namespace PasswordGenLocal.Core
{
    public class LpgeStore
    {
        public string StoreName { get; set; }     // user-set, independent from filename
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public bool TrackAndClear { get; set; }   // per-store setting
        public List<StoreEntry> Entries { get; set; }
        // Runtime-only — not persisted. StoreManager uses a private DTO for serialization,
        // so these properties are never written to or read from the .bin file.
        public bool IsDirty { get; set; }
        public string? FilePath { get; set; }      // null until first save
        public string? CachedPassword { get; set; } // held in memory while tab is open; cleared on close

        public LpgeStore()
        {
            StoreName = string.Empty;
            Entries = new List<StoreEntry>();
        }
    }
}
