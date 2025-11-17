using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Drawing;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Management;

namespace LanceurRaccourcis
{
    [SupportedOSPlatform("windows")]
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon? trayIcon;
        private ConfigForm? configForm;

        private BackupForm? backupForm;
        private string shortcutsPath = string.Empty;
        private FileSystemWatcher? watcher;

        private System.Windows.Forms.Timer? rebuildTimer;
        private System.Windows.Forms.Timer? trayLeftClickTimer;
        private bool pendingTrayMenuOnLeftClick;
        private const int REBUILD_DELAY_MS = 300;

        private const string APP_NAME = "LanceurRaccourcis";
        private const string CONFIG_FILENAME = "config.json";
        private readonly string configFilePath;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public TrayApplicationContext()
        {
            Logger.Log("Initialisation de TrayApplicationContext");

            // Définir le chemin complet du fichier de configuration JSON
            configFilePath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath)!, // '!' : Affirme que le chemin n'est pas null
                CONFIG_FILENAME
            );

            LoadConfiguration();

            // --- Démarrage automatique de la sauvegarde ---
            try
            {
                backupForm = new BackupForm();
                backupForm.Hide(); // On ne veut pas afficher la fenêtre
                Logger.Log("Timer de sauvegarde automatique initialisé au démarrage.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de l'initialisation du module de sauvegarde automatique", ex);
            }

            // Monter automatiquement les lecteurs réseau
            MountStartupNetworkDrives();

            // Créer l'icône dans la barre des tâches
            try
            {
                trayIcon = new NotifyIcon()
                {
                    Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("LanceurRaccourcis.icon.ico")!),
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true,
                    Text = "Lanceur de Raccourcis"
                };
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du chargement de l'icône embarquée", ex);
                // Fallback si l'icône embarquée n'est pas trouvée
                trayIcon = new NotifyIcon()
                {
                    Icon = SystemIcons.Application,
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true,
                    Text = "Lanceur de Raccourcis (Erreur icône)"
                };
            }

            trayIcon.MouseClick += TrayIcon_MouseClick;
            trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;

            BuildContextMenu();

            if (Directory.Exists(shortcutsPath))
            {
                SetupFileWatcher();
            }

            trayLeftClickTimer = new System.Windows.Forms.Timer
            {
                Interval = SystemInformation.DoubleClickTime
            };
            trayLeftClickTimer.Tick += TrayLeftClickTimer_Tick;
        }

        private void LoadConfiguration()
        {
            Logger.Log("Chargement de la configuration");
            if (File.Exists(configFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(configFilePath);
                    AppConfig? config = JsonSerializer.Deserialize<AppConfig>(jsonString);

                    if (config != null && !string.IsNullOrEmpty(config.ShortcutsPath))
                    {
                        shortcutsPath = config.ShortcutsPath;
                        Logger.Log($"Chemin des raccourcis chargé: {shortcutsPath}");
                        Logger.Configure(config.LogDirectory, config.LogRetentionDays);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Erreur lors de la lecture du fichier de configuration", ex);
                }
            }

            // Chemin par défaut (si JSON inexistant ou lecture échouée)
            if (string.IsNullOrEmpty(shortcutsPath))
            {
                // Utilisation de '!' sur GetFolderPath pour lever l'avertissement CS8604
                shortcutsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)!,
                    "LanceurRaccourcis"
                );
                Logger.Log($"Utilisation du chemin par défaut: {shortcutsPath}");
            }

            if (!Directory.Exists(shortcutsPath))
            {
                try
                {
                    Directory.CreateDirectory(shortcutsPath);
                    Logger.Log($"Dossier créé: {shortcutsPath}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erreur lors de la création du dossier {shortcutsPath}", ex);
                }
            }
        }

        private void SaveConfiguration(string path, bool runAtStartup, List<NetworkDriveConfig>? networkDrives = null)
        {
            Logger.Log($"Sauvegarde de la configuration - Path: {path}, RunAtStartup: {runAtStartup}");
            int? existingRetention = null;
            string? existingLogDir = null;

            try
            {
                if (File.Exists(configFilePath))
                {
                    string jsonString = File.ReadAllText(configFilePath);
                    AppConfig? existing = JsonSerializer.Deserialize<AppConfig>(jsonString);
                    existingRetention = existing?.LogRetentionDays;
                    existingLogDir = existing?.LogDirectory;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de lecture de la configuration JSON", ex);
            }

            AppConfig config = new AppConfig
            {
                ShortcutsPath = path,
                RunAtStartup = runAtStartup,
                NetworkDrives = networkDrives ?? LoadNetworkDrivesConfig(),
                LogRetentionDays = existingRetention,
                LogDirectory = existingLogDir
            };

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configFilePath, jsonString);
                Logger.Log("Configuration sauvegardée avec succès");
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de l'enregistrement de la configuration JSON", ex);
                MessageBox.Show($"Erreur lors de l'enregistrement de la configuration JSON:\n{ex.Message}",
                    "Erreur de Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<NetworkDriveConfig> LoadNetworkDrivesConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string jsonString = File.ReadAllText(configFilePath);
                    AppConfig? config = JsonSerializer.Deserialize<AppConfig>(jsonString);
                    var drives = config?.NetworkDrives ?? new List<NetworkDriveConfig>();
                    Logger.Log($"Chargement de {drives.Count} lecteur(s) réseau depuis la configuration");
                    return drives;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du chargement des lecteurs réseau", ex);
            }
            return new List<NetworkDriveConfig>();
        }

        private void SetupFileWatcher()
        {
            Logger.Log("Configuration du FileSystemWatcher");
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            if (rebuildTimer == null)
            {
                rebuildTimer = new System.Windows.Forms.Timer { Interval = REBUILD_DELAY_MS, Enabled = false };
                rebuildTimer.Tick += (s, e) =>
                {
                    rebuildTimer.Stop();
                    BuildContextMenu();
                };
            }


            watcher = new FileSystemWatcher(shortcutsPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                Filter = "*.*"
            };

            watcher.Changed += OnFileEvent;
            watcher.Created += OnFileEvent;
            watcher.Deleted += OnFileEvent;
            watcher.Renamed += OnFileEvent;

            watcher.EnableRaisingEvents = true;
            Logger.Log("FileSystemWatcher activé");
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            Logger.Log($"Événement fichier détecté: {e.ChangeType} - {e.Name}");
            if (rebuildTimer != null)
            {
                rebuildTimer.Stop();
                rebuildTimer.Start();
            }
        }

        private void BuildContextMenu()
        {
            Logger.Log("Construction du menu contextuel");

            if (trayIcon == null) return; // Sécurité

            if (trayIcon.ContextMenuStrip == null)
            {
                trayIcon.ContextMenuStrip = new ContextMenuStrip();
            }

            if (trayIcon.ContextMenuStrip.InvokeRequired)
            {
                trayIcon.ContextMenuStrip.Invoke(new Action(BuildContextMenu));
                return;
            }

            trayIcon.ContextMenuStrip.Items.Clear();

            if (Directory.Exists(shortcutsPath))
            {
                Logger.Log($"Ajout des element de {shortcutsPath}");
                AddDirectoryItems(trayIcon.ContextMenuStrip.Items, shortcutsPath, true);
            }

            ToolStripMenuItem configItem = new ToolStripMenuItem("⚙ Configuration");
            configItem.Click += ShowConfiguration;
            trayIcon.ContextMenuStrip.Items.Add(configItem);

            ToolStripMenuItem openFolderItem = new ToolStripMenuItem("📁 Ouvrir le dossier des raccourcis");
            openFolderItem.Click += (s, e) => OpenShortcutsFolder();
            trayIcon.ContextMenuStrip.Items.Add(openFolderItem);

            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            
            ToolStripMenuItem backupItem = new ToolStripMenuItem("💾 Sauvegarde");
            backupItem.Click += ShowBackup;
            trayIcon.ContextMenuStrip.Items.Add(backupItem);

            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem networkDriveItem = new ToolStripMenuItem("🌐 Lecteur réseau");
            networkDriveItem.Click += ShowNetworkDrives;
            trayIcon.ContextMenuStrip.Items.Add(networkDriveItem);

            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ Quitter");
            exitItem.Click += Exit;
            trayIcon.ContextMenuStrip.Items.Add(exitItem);
        }
        
        private void ShowTrayMenu()
        {
            try
            {
                if (trayIcon?.ContextMenuStrip == null) return;

                if (trayIcon.ContextMenuStrip.InvokeRequired)
                {
                    trayIcon.ContextMenuStrip.Invoke(new Action(ShowTrayMenu));
                    return;
                }
                using (Form dummyForm = new Form())
                {
                    dummyForm.ShowInTaskbar = false;
                    dummyForm.FormBorderStyle = FormBorderStyle.None;
                    dummyForm.Width = 0;
                    dummyForm.Height = 0;
                    dummyForm.Opacity = 0;
                    dummyForm.Show();

                    SetForegroundWindow(dummyForm.Handle);

                    var position = Control.MousePosition;
                    trayIcon.ContextMenuStrip.Show(position);

                    // Fermer le formulaire après que le menu soit affiché
                    dummyForm.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de l'ouverture du menu du plateau", ex);
            }
        }

        private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    Logger.Log("MouseClick L: Démarrage de l'attente du simple clic.");
                    pendingTrayMenuOnLeftClick = true;
                    if (trayLeftClickTimer != null)
                    {
                        trayLeftClickTimer.Stop();
                        trayLeftClickTimer.Interval = SystemInformation.DoubleClickTime;
                        trayLeftClickTimer.Start();
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    Logger.Log("MouseClick R: Affichage immédiat du menu contextuel.");
                    pendingTrayMenuOnLeftClick = false;
                    trayLeftClickTimer?.Stop();
                    //ShowTrayMenu();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du double-clic sur l'icône de la zone de notification", ex);
            }
        }

        private void TrayIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    Logger.Log("MouseDoubleClick L: Annulation de l'action en attente et exécution du double-clic.");
                    pendingTrayMenuOnLeftClick = false;
                    trayLeftClickTimer?.Stop();
                    if (trayIcon != null)
                    {
                        // On se désabonne pour ignorer l'événement MouseClick qui arrive en trop
                        trayIcon.MouseClick -= TrayIcon_MouseClick; 
                    }
                    ShowNetworkDrives(sender, EventArgs.Empty);

                    if (trayIcon != null)
                    {
                        // La réactivation doit se faire de manière simple et directe.
                        trayIcon.MouseClick += TrayIcon_MouseClick;
                        Logger.Log("MouseClick réactivé immédiatement.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du double-clic sur l'icône de la zone de notification", ex);
            }
        }

        private void TrayLeftClickTimer_Tick(object? sender, EventArgs e)
        {
            Logger.Log("Tick Timer: Le minuteur a expiré.");
            trayLeftClickTimer?.Stop();
            if (pendingTrayMenuOnLeftClick)
            {
                Logger.Log("Tick Timer: pendingTrayMenuOnLeftClick est TRUE. Exécution du simple clic (ShowConfiguration).");
                pendingTrayMenuOnLeftClick = false;
                //ShowTrayMenu();
                ShowConfiguration(sender, EventArgs.Empty);
            }
            else
            {
                Logger.Log("Tick Timer: pendingTrayMenuOnLeftClick est FALSE. Action simple clic ignorée (annulée par un double-clic).");
            }
        }

        private void AddDirectoryItems(ToolStripItemCollection items, string directoryPath, bool isRoot = false)
        {
            try
            {
                // Ajouter les fichiers du répertoire actuel
                var files = Directory.GetFiles(directoryPath)
                    .Where(f => IsExecutableOrShortcut(f))
                    .OrderBy(f => Path.GetFileName(f));

                Logger.Log($"Ajout de {files.Count()} elements");

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    ToolStripMenuItem item = new ToolStripMenuItem(fileName);

                    // Essayer d'obtenir l'icône du fichier
                    try
                    {
                        Icon? icon = Icon.ExtractAssociatedIcon(file);
                        if (icon != null)
                        {
                            item.Image = icon.ToBitmap();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Erreur extraction icône pour {file}", ex);
                    }

                    string filePath = file;
                    item.Click += (s, e) => LaunchFile(filePath);
                    items.Add(item);
                }

                // Ajouter les sous-répertoires
                var directories = Directory.GetDirectories(directoryPath)
                    .OrderBy(d => Path.GetFileName(d));

                foreach (string directory in directories)
                {
                    string dirName = Path.GetFileName(directory);
                    ToolStripMenuItem dirItem = new ToolStripMenuItem("📁 " + dirName);

                    // Ajouter récursivement les éléments du sous-répertoire
                    AddDirectoryItems(dirItem.DropDownItems, directory);

                    // N'ajouter le menu que s'il contient des éléments
                    if (dirItem.DropDownItems.Count > 0)
                    {
                        items.Add(dirItem);
                    }
                }

                // Ajouter un séparateur après le contenu du répertoire racine
                if (isRoot && items.Count > 0)
                {
                    Logger.Log($"Ajout de séparateur");
                    items.Add(new ToolStripSeparator());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erreur lors de l'ajout des éléments de {directoryPath}", ex);
                ToolStripMenuItem errorItem = new ToolStripMenuItem($"Erreur: {ex.Message}");
                errorItem.Enabled = false;
                items.Add(errorItem);
            }
        }

        private bool IsExecutableOrShortcut(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return ext == ".exe" || ext == ".lnk" || ext == ".bat" || ext == ".cmd";
        }

        private void LaunchFile(string filePath)
        {
            Logger.Log($"Lancement du fichier: {filePath}");
            // Code de lancement avec explorer.exe (qui a fonctionné)
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                Logger.Log($"Fichier lancé avec succès: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erreur lors du lancement de {Path.GetFileName(filePath)}", ex);
                MessageBox.Show($"Erreur lors du lancement de {Path.GetFileName(filePath)}:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenShortcutsFolder()
        {
            Logger.Log($"Ouverture du dossier: {shortcutsPath}");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = shortcutsPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de l'ouverture du dossier", ex);
                MessageBox.Show($"Erreur lors de l'ouverture du dossier:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowConfiguration(object? sender, EventArgs e)
        {
            Logger.Log("Ouverture de la fenêtre de configuration");
            if (configForm == null || configForm.IsDisposed)
            {
                configForm = new ConfigForm(shortcutsPath, IsInStartup());

                configForm.ConfigurationSaved += (path, runAtStartup) =>
                {
                    SaveConfiguration(path, runAtStartup);

                    shortcutsPath = path;
                    SetupFileWatcher();
                    BuildContextMenu();
                };
            }
            configForm.Show();
            configForm.BringToFront();
        }

        private void Exit(object? sender, EventArgs e)
        {
            Logger.Log("Fermeture de l'application demandée");
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon?.Dispose();
                rebuildTimer?.Dispose();
                watcher?.Dispose();
                backupForm?.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool IsInStartup()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue(APP_NAME) != null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de la vérification du démarrage automatique", ex);
                return false;
            }
        }

        private NetworkDriveForm? networkDriveForm;

        private void ShowNetworkDrives(object? sender, EventArgs e)
        {
            Logger.Log("Ouverture du gestionnaire de lecteurs réseau");
            if (networkDriveForm == null || networkDriveForm.IsDisposed)
            {
                networkDriveForm = new NetworkDriveForm(configFilePath);
                networkDriveForm.NetworkDrivesSaved += (config) =>
                {
                    SaveConfiguration(shortcutsPath, IsInStartup(), config);
                };
            }
            networkDriveForm.Show();
            networkDriveForm.BringToFront();
        }

        private void MountStartupNetworkDrives()
        {
            Logger.Log("Début du montage automatique des lecteurs réseau");
            try
            {
                var networkDrivesConfig = LoadNetworkDrivesConfig();

                if (networkDrivesConfig == null || networkDrivesConfig.Count == 0)
                    return;

                int mountedCount = 0;
                foreach (var config in networkDrivesConfig)
                {
                    // Vérifier si le lecteur doit être monté au démarrage
                    if (!config.LancerDemarrage)
                    {
                        Logger.Log($"Lecteur {config.DriveLetter} - ignoré (LancerDemarrage = false)");
                        continue;
                    }

                    string driveLetter = config.DriveLetter ?? "";
                    string networkPath = config.NetworkPath ?? "";
                    string username = config.Username ?? "";
                    string password = config.Password ?? "";

                    if (string.IsNullOrEmpty(driveLetter) || string.IsNullOrEmpty(networkPath))
                    {
                        Logger.Log($"Lecteur ignoré - informations manquantes (Lettre: {driveLetter}, Path: {networkPath})");
                        continue;
                    }                    

                    // Vérifier si le lecteur n'est pas déjà monté
                    if (IsDriveMounted(driveLetter))
                    {
                        Logger.Log($"Lecteur {driveLetter} déjà monté");
                        continue;
                    }

                    Logger.Log($"Tentative de montage du lecteur {driveLetter} -> {networkPath}");
                    // Monter le lecteur                    
                    if (MountNetworkDrive(driveLetter, networkPath, username, password))
                    {
                        mountedCount++;
                        Logger.Log($"Lecteur {driveLetter} monté avec succès");
                    }
                }
                Logger.Log($"Montage automatique terminé - {mountedCount} lecteur(s) monté(s)");
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du montage automatique des lecteurs", ex);
            }
        }

        private bool IsDriveMounted(string driveLetter)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_LogicalDisk WHERE DriveType = 4"))
                {
                    foreach (var drive in searcher.Get())
                    {
                        string mountedDrive = drive["Name"]?.ToString() ?? "";
                        if (mountedDrive.Equals(driveLetter, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erreur lors de la vérification du montage de {driveLetter}", ex);
            }
            return false;
        }

        private bool MountNetworkDrive(string driveLetter, string networkPath, string username, string password)
        {
            try
            {
                bool persistent = false;
                string? errorMessage;
                bool ok = NetworkDriveHelper.MapNetworkDrive(driveLetter, networkPath,
                    string.IsNullOrWhiteSpace(username) || username == "N/A" ? null : username,
                    string.IsNullOrWhiteSpace(password) || password == "N/A" ? null : password,
                    persistent, out errorMessage);
                if (!ok)
                {
                    Logger.LogError($"Échec du montage de {driveLetter}", new Exception(errorMessage ?? "Erreur inconnue"));
                }
                return ok;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception lors du montage de {driveLetter}", ex);
                return false;
            }
        }

        private void ShowBackup(object? sender, EventArgs e)
        {
            Logger.Log("Ouverture de l'outil de sauvegarde");
            try
            {
                if (backupForm == null || backupForm.IsDisposed)
                {
                    // Si la fenêtre a été détruite (rare), on la recrée
                    backupForm = new BackupForm();
                    backupForm.Hide();
                    Logger.Log("BackupForm recréé (ancienne instance détruite)");
                }
                backupForm.Show();
                backupForm.BringToFront();
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de l'ouverture de l'outil de sauvegarde", ex);
                MessageBox.Show($"Erreur lors de l'ouverture de l'outil de sauvegarde:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}