using System;

namespace PasswordGenLocal.Core
{
    public class StoreEntry
    {
        public Guid Id { get; set; }
        public string Label { get; set; }    // backing field for the "Username" display column
        public string Note { get; set; }     // user-editable; max 95 printable ASCII (0x20-0x7E)
        public DateTime Timestamp { get; set; }
        public CharsetMode Mode { get; set; }
        public int Length { get; set; }
        public string Password { get; set; }

        public StoreEntry()
        {
            Id       = Guid.NewGuid();
            Label    = string.Empty;
            Note     = string.Empty;
            Password = string.Empty;
        }
    }
}
