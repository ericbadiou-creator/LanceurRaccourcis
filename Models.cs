using System.Collections.Generic;

namespace LanceurRaccourcis
{
    // Modèle de données pour la sauvegarde JSON
    public class AppConfig
    {
        public string? ShortcutsPath { get; set; }
        public bool RunAtStartup { get; set; }

        public List<NetworkDriveConfig>? NetworkDrives { get; set; } = new List<NetworkDriveConfig>();
        public int? LogRetentionDays { get; set; }
        public string? LogDirectory { get; set; }
    }

    public class NetworkDriveConfig
    {
        public string? DriveLetter { get; set; }
        public string? Description { get; set; }
        public string? Server { get; set; }
        public string? NetworkPath { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool LancerDemarrage { get; set; } = true;
    }
}