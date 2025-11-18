using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.Runtime.Versioning;

namespace LanceurRaccourcis
{
    [SupportedOSPlatform("windows")]
    public class BackupForm : Form
    {
        private DataGridView dgvBackups = null!;
        private ToolStrip toolStrip = null!;
        private ToolStripButton btnAdd = null!;
        private ToolStripButton btnModify = null!;
        private ToolStripButton btnDelete = null!;
        private ToolStripButton btnExecute = null!;
        private ToolStripButton btnManualSave = null!;
        private ToolStripButton btnSettings = null!;
        private ToolStripButton btnSchedule = null!;
        private ToolStripButton btnRestore = null!;
        private System.Windows.Forms.Timer? autoSaveTimer = null!;

        private const string BACKUP_CONFIG_FILE = "backup_config.json";
        private string configPath;
        private List<BackupEntry> backupEntries = new List<BackupEntry>();

        public BackupForm()
        {
            InitializeComponent();
            configPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath)!,
                BACKUP_CONFIG_FILE
            );
            LoadBackupConfiguration();
            SetupAutoSaveTimer();
        }

        private void InitializeComponent()
        {
            this.Text = "Saveugarde automatique des fichiers";
            this.Size = new Size(1050, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Shield;

            // Créer la barre d'outils
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                ImageScalingSize = new Size(32, 32)
            };

            // Boutons de la barre d'outils
            btnManualSave = CreateToolStripButton("💾", "Sauvegarde manuelle", OnManualSave);
            btnAdd = CreateToolStripButton("➕", "Ajouter", OnAdd);
            btnModify = CreateToolStripButton("✏️", "Modifier", OnModify);
            btnModify.Enabled = false; // Désactivé par défaut
            btnDelete = CreateToolStripButton("➖", "Supprimer", OnDelete);
            btnExecute = CreateToolStripButton("▶", "Exécuter", OnExecute);
            btnSettings = CreateToolStripButton("⚙", "Paramètres", OnSettings);
            btnSchedule = CreateToolStripButton("🕐", "Planification", OnSchedule);
            btnRestore = CreateToolStripButton("↶", "Restaurer", OnRestore);

            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnManualSave,
                new ToolStripSeparator(),
                btnAdd,
                btnModify,
                btnDelete,
                new ToolStripSeparator(),
                btnExecute,
                new ToolStripSeparator(),
                btnSettings,
                btnSchedule,
                btnRestore
            });

            // Créer le DataGridView
            dgvBackups = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = true,
                BackgroundColor = Color.White
            };

            // Définir les colonnes
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "Ind...", Width = 50, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Fichier à sauvegarder", FillWeight = 150, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Destination", HeaderText = "Lieu de sauvegarde", FillWeight = 150, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Jour", HeaderText = "Jour", Width = 50, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Heure", HeaderText = "Heure", Width = 50, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Minutes", HeaderText = "Minutes", Width = 60, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", Width = 80, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastBackup", HeaderText = "Dernière maj", Width = 130, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 80, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Heure2", HeaderText = "Heure", Width = 80, ReadOnly = true });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Error", HeaderText = "Erreur", Width = 80, ReadOnly = true });

            // Événements
            dgvBackups.CellDoubleClick += OnCellDoubleClick;
            dgvBackups.SelectionChanged += DgvBackups_SelectionChanged;

            // Ajouter les contrôles au formulaire
            this.Controls.Add(dgvBackups);
            this.Controls.Add(toolStrip);

            this.FormClosing += BackupForm_FormClosing;
        }

        private void DgvBackups_SelectionChanged(object? sender, EventArgs e)
        {
            // Activer le bouton Modifier seulement si exactement une ligne est sélectionnée
            btnModify.Enabled = dgvBackups.SelectedRows.Count == 1;
        }

        private ToolStripButton CreateToolStripButton(string text, string tooltip, EventHandler clickHandler)
        {
            var button = new ToolStripButton
            {
                Text = text,
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Font = new Font("Segoe UI", 16, FontStyle.Regular)
            };
            button.Click += clickHandler;
            return button;
        }

        private void LoadBackupConfiguration()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    backupEntries = JsonSerializer.Deserialize<List<BackupEntry>>(json) ?? new List<BackupEntry>();
                    RefreshGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la configuration:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveBackupConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(backupEntries, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde de la configuration:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid()
        {
            dgvBackups.Rows.Clear();
            int index = 1;
            foreach (var entry in backupEntries)
            {
                dgvBackups.Rows.Add(
                    index++,
                    entry.SourcePath ?? "",
                    entry.DestinationPath ?? "",
                    entry.DayInterval,
                    entry.HourInterval,
                    entry.MinuteInterval,
                    entry.ActionType ?? "COPIE",
                    entry.LastBackupDate?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                    entry.LastBackupDate?.ToString("dd/MM/yyyy") ?? "NON",
                    entry.LastBackupDate?.ToString("HH:mm") ?? "NON",
                    entry.Error
                );
            }
        }

        private void OnManualSave(object? sender, EventArgs e)
        {
            if (dgvBackups.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une entrée à sauvegarder.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgvBackups.SelectedRows)
            {
                if (row.Cells["Index"].Value != null)
                {
                    int index = (int)row.Cells["Index"].Value! - 1;
                    if (index >= 0 && index < backupEntries.Count)
                    {
                        ExecuteBackup(backupEntries[index], true);
                    }
                }
            }
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            var newEntry = new BackupEntry 
            { 
                Index = backupEntries.Count > 0 ? backupEntries.Max(x => x.Index) + 1 : 1 
            };
            
            using (var addForm = new BackupEntryForm(newEntry))
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    backupEntries.Add(addForm.BackupEntry);
                    RefreshGrid();
                    SaveBackupConfiguration();
                }
            }
        }

        private void OnModify(object? sender, EventArgs e)
        {
            // Vérifier qu'exactement une ligne est sélectionnée
            if (dgvBackups.SelectedRows.Count != 1)
            {
                MessageBox.Show("Veuillez sélectionner une seule entrée à modifier.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvBackups.SelectedRows[0];
            if (row.Cells["Index"].Value == null) return;

            int displayIndex = (int)row.Cells["Index"].Value!;
            int entryIndex = displayIndex - 1;

            if (entryIndex < 0 || entryIndex >= backupEntries.Count) return;

            var entry = backupEntries[entryIndex];
            using (var editForm = new BackupEntryForm(entry))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    backupEntries[entryIndex] = editForm.BackupEntry;
                    RefreshGrid();
                    SaveBackupConfiguration();
                }
            }
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (dgvBackups.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une entrée à supprimer.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer {dgvBackups.SelectedRows.Count} entrée(s) ?",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var indicesToRemove = new List<int>();
                foreach (DataGridViewRow row in dgvBackups.SelectedRows)
                {
                    if (row.Cells["Index"].Value != null)
                    {
                        int index = (int)row.Cells["Index"].Value! - 1;
                        indicesToRemove.Add(index);
                    }
                }

                indicesToRemove.Sort();
                indicesToRemove.Reverse();

                foreach (int index in indicesToRemove)
                {
                    if (index >= 0 && index < backupEntries.Count)
                    {
                        backupEntries.RemoveAt(index);
                    }
                }

                RefreshGrid();
                SaveBackupConfiguration();
            }
        }

        private void OnExecute(object? sender, EventArgs e)
        {
            if (dgvBackups.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une entrée à exécuter.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgvBackups.SelectedRows)
            {
                if (row.Cells["Index"].Value != null)
                {
                    int index = (int)row.Cells["Index"].Value! - 1;
                    if (index >= 0 && index < backupEntries.Count)
                    {
                        ExecuteBackup(backupEntries[index], true);
                    }
                }
            }
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            MessageBox.Show("Fonctionnalité Paramètres à implémenter",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnSchedule(object? sender, EventArgs e)
        {
            MessageBox.Show("Fonctionnalité Planification à implémenter",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnRestore(object? sender, EventArgs e)
        {
            MessageBox.Show("Fonctionnalité Restaurer à implémenter",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvBackups.Rows.Count) return;

            var row = dgvBackups.Rows[e.RowIndex];
            if (row.Cells["Index"].Value == null) return;

            int displayIndex = (int)row.Cells["Index"].Value!;
            int entryIndex = displayIndex - 1;

            if (entryIndex < 0 || entryIndex >= backupEntries.Count) return;

            var entry = backupEntries[entryIndex];
            using (var editForm = new BackupEntryForm(entry))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    backupEntries[entryIndex] = editForm.BackupEntry;
                    RefreshGrid();
                    SaveBackupConfiguration();
                }
            }
        }

        private void ExecuteBackup(BackupEntry entry, bool showmessagebox = false)
        {
            try
            {
                if (!File.Exists(entry.SourcePath) && !Directory.Exists(entry.SourcePath))
                {
                    entry.Error = true;
                    MessageBox.Show($"Le fichier source n'existe pas:\n{entry.SourcePath}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SaveBackupConfiguration();
                    RefreshGrid();
                    return;
                }

                if (entry.ActionType == "EXECUTER")
                {
                    // Pour EXECUTER, on lance simplement le programme
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = entry.SourcePath,
                        UseShellExecute = true
                    });

                    entry.LastBackupDate = DateTime.Now;
                    SaveBackupConfiguration();
                    RefreshGrid();

                    string sourceFileNameexe = Path.GetFileName(entry.SourcePath);
                    Logger.Log($"Programme exécuté avec succès:\n{sourceFileNameexe}");
                    if(showmessagebox)
                    {                    
                        MessageBox.Show($"Programme exécuté avec succès:\n{sourceFileNameexe}",
                            "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                // Pour COPIE et ZIP, on continue avec la logique de sauvegarde
                string destPath = entry.DestinationPath;

                // Ajouter la date et/ou l'heure au nom de destination si demandé
                if (entry.AddDateToDestination || entry.AddTimeToDestination)
                {
                    string dateTimeSuffix = "";
                    if (entry.AddDateToDestination)
                    {
                        dateTimeSuffix += DateTime.Now.ToString("yyyyMMdd");
                    }
                    if (entry.AddTimeToDestination)
                    {
                        if (!string.IsNullOrEmpty(dateTimeSuffix)) dateTimeSuffix += "_";
                        dateTimeSuffix += DateTime.Now.ToString("HHmmss");
                    }

                    // Ajouter le suffixe avant l'extension
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(destPath);
                    string extension = Path.GetExtension(destPath);
                    string directory = Path.GetDirectoryName(destPath) ?? "";
                    
                    // Pour ZIP, s'assurer que l'extension est .zip
                    if (entry.ActionType == "ZIP" && extension.ToLower() != ".zip")
                    {
                        extension = ".zip";
                    }
                    
                    destPath = Path.Combine(directory, $"{fileNameWithoutExt}_{dateTimeSuffix}{extension}");
                }
                else if (entry.ActionType == "ZIP")
                {
                    // S'assurer que le fichier de destination a l'extension .zip
                    if (!destPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        destPath += ".zip";
                    }
                }

                string destDirectory = Path.GetDirectoryName(destPath) ?? "";
                if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                if (entry.ActionType == "ZIP")
                {
                    // Supprimer le fichier ZIP existant s'il existe
                    if (File.Exists(destPath))
                    {
                        File.Delete(destPath);
                    }

                    // Créer l'archive ZIP
                    if (File.Exists(entry.SourcePath))
                    {
                        // Zipper un seul fichier
                        using (ZipArchive archive = ZipFile.Open(destPath, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile(entry.SourcePath, Path.GetFileName(entry.SourcePath));
                        }
                    }
                    else if (Directory.Exists(entry.SourcePath))
                    {
                        // Zipper un répertoire entier
                        ZipFile.CreateFromDirectory(entry.SourcePath, destPath);
                    }
                }
                else // COPIE
                {
                    if (File.Exists(entry.SourcePath))
                    {
                        File.Copy(entry.SourcePath, destPath, true);
                    }
                    else if (Directory.Exists(entry.SourcePath))
                    {
                        CopyDirectory(entry.SourcePath, destPath);
                    }
                }

                entry.LastBackupDate = DateTime.Now;
                SaveBackupConfiguration();
                RefreshGrid();

                string sourceFileName = Path.GetFileName(entry.SourcePath);
                string actionText = entry.ActionType == "ZIP" ? "compressée" : "copiée";
                Logger.Log($"Sauvegarde {actionText} avec succès:\n{sourceFileName}\n\nVers:\n{destPath}");
                if(showmessagebox)
                {                   
                    MessageBox.Show($"Sauvegarde {actionText} avec succès:\n{sourceFileName}\n\nVers:\n{destPath}",
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
            }
            catch (Exception ex)
            {
                entry.Error = true;
                SaveBackupConfiguration();
                RefreshGrid();
                 Logger.LogError($"Erreur lors de la sauvegarde:\n{ex.Message}", ex);
                if(showmessagebox)
                {
                    MessageBox.Show($"Erreur lors de la sauvegarde:\n{ex.Message}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private void SetupAutoSaveTimer()
        {
            autoSaveTimer = new System.Windows.Forms.Timer
            {
                Interval = 60000 // Vérifier toutes les minutes
            };
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (var entry in backupEntries)
            {
                if (ShouldExecuteBackup(entry, now))
                {
                    ExecuteBackup(entry, false);
                }
            }
        }

        private bool ShouldExecuteBackup(BackupEntry entry, DateTime now)
        {
            if (!entry.LastBackupDate.HasValue)
                return true;

            var lastBackup = entry.LastBackupDate.Value;
            var daysPassed = (now - lastBackup).TotalMinutes;

            double totalIntervalMinutes = (entry.DayInterval * 24 * 60) + 
                        (entry.HourInterval * 60) + entry.MinuteInterval;

            if (daysPassed < totalIntervalMinutes && entry.Error == true)
                return false;

            return true;
        }

        private void BackupForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            autoSaveTimer?.Stop();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Si l’utilisateur ferme la fenêtre avec la croix, on cache seulement
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                Logger.Log("BackupForm → Fenêtre masquée, le timer continue à tourner.");
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        public void ReloadConfiguration()
        {
            try
            {
                Logger.Log("BackupForm → Rechargement de la configuration...");

                // Exemple : si tu lis un fichier JSON de configuration :
                LoadBackupConfiguration();

                // Redémarre le timer automatique
                RestartAutoSaveTimer();

                Logger.Log("BackupForm → Configuration rechargée avec succès.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du rechargement de la configuration du BackupForm", ex);
            }
        }
    
        // 🔁 Redémarre le timer automatique avec les nouvelles valeurs
        private void RestartAutoSaveTimer()
        {
            try
            {
                Logger.Log("BackupForm → Redémarrage du timer de sauvegarde automatique...");

                if (autoSaveTimer != null)
                {
                    autoSaveTimer.Stop();
                    autoSaveTimer.Tick -= AutoSaveTimer_Tick;
                    autoSaveTimer.Dispose();
                    autoSaveTimer = null;
                }

                SetupAutoSaveTimer(); // ← ta méthode existante qui configure ton timer

                Logger.Log("BackupForm → Timer relancé avec la nouvelle configuration.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors du redémarrage du timer automatique", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoSaveTimer?.Dispose();
                dgvBackups?.Dispose();
                toolStrip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Classe pour représenter une entrée de sauvegarde
    public class BackupEntry
    {
        public int Index { get; set; } = 0;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int DayInterval { get; set; } = 7;
        public int HourInterval { get; set; } = 0;
        public int MinuteInterval { get; set; } = 0;
        public string ActionType { get; set; } = "COPIE";
        public bool AddDateToDestination { get; set; } = false;
        public bool AddTimeToDestination { get; set; } = false;
        public DateTime? LastBackupDate { get; set; }
        public bool Error { get; set; } = false;
    }

    // Formulaire pour ajouter/éditer une entrée
    [SupportedOSPlatform("windows")]
    public class BackupEntryForm : Form
    {
        private TextBox txtSourceFile = null!;
        private TextBox txtDestination = null!;
        private NumericUpDown numDay = null!;
        private NumericUpDown numHour = null!;
        private NumericUpDown numMinute = null!;
        private ComboBox cboAction = null!;
        private Button btnBrowseSourceFile = null!;
        private Button btnBrowseSourceFolder = null!;
        private Button btnBrowseDestFile = null!;
        private Button btnBrowseDestFolder = null!;
        private GroupBox grpDest = null!;
        private GroupBox grpDuration = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;
        private CheckBox chkAddDateToDestination = null!;
        private CheckBox chkAddTimeToDestination = null!;
        private NumericUpDown numIndex = null!;

        private int positionGrpDest = 0;
        private int tailleGrpDest = 0;
        private int tailleGrpDuration = 0;

        public BackupEntry BackupEntry { get; private set; }

        public BackupEntryForm(BackupEntry? entry = null)
        {
            BackupEntry = entry ?? new BackupEntry();
            InitializeComponent();
            LoadEntry();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration de la sauvegarde";
            this.Size = new Size(380, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            // Index
            Label lblIndex = new Label { Text = "Indice :", Left = 20, Top = y, Width = 80 };
            numIndex = new NumericUpDown { Left = 110, Top = y - 3, Width = 180, Minimum = 1, Maximum = 9999, ReadOnly = true };
            y += 35;

            // Action
            Label lblAction = new Label { Text = "Action :", Left = 20, Top = y, Width = 80 };
            cboAction = new ComboBox { Left = 110, Top = y - 3, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboAction.Items.AddRange(new[] { "COPIE", "ZIP", "EXECUTER" });
            cboAction.SelectedIndex = 0;
            cboAction.SelectedIndexChanged += CboAction_SelectedIndexChanged;
            y += 35;

            // GroupBox - Fichier origine
            GroupBox grpSource = new GroupBox { Text = "Fichier origine :", Left = 10, Top = y, Width = 345, Height = 95 };
            txtSourceFile = new TextBox { Left = 10, Top = 25, Width = 270, Parent = grpSource };
            btnBrowseSourceFolder = new Button { Text = "Répertoire", Left = 10, Top = 55, Width = 100, Parent = grpSource };
            btnBrowseSourceFile = new Button { Text = "Fichier", Left = 170, Top = 55, Width = 100, Parent = grpSource };
            btnBrowseSourceFolder.Click += BrowseSourceFolder;
            btnBrowseSourceFile.Click += BrowseSourceFile;
            this.Controls.Add(grpSource);
            y += 105;

            positionGrpDest = y;    
            tailleGrpDest = 155;
            // GroupBox - Fichier destination
            grpDest = new GroupBox { Text = "Fichier destination :", Left = 10, Top = y, Width = 345, Height = 145 };
            txtDestination = new TextBox { Left = 10, Top = 25, Width = 270, Parent = grpDest };
            btnBrowseDestFolder = new Button { Text = "Répertoire", Left = 10, Top = 55, Width = 100, Parent = grpDest };
            btnBrowseDestFile = new Button { Text = "Fichier", Left = 170, Top = 55, Width = 100, Parent = grpDest };
            chkAddDateToDestination = new CheckBox { Text = "Ajout Date à la destination", Left = 10, Top = 85, Width = 270, Parent = grpDest };
            chkAddTimeToDestination = new CheckBox { Text = "Ajout heure minute  à la destination", Left = 10, Top = 105, Width = 270, Parent = grpDest };
            btnBrowseDestFolder.Click += BrowseDestFolder;
            btnBrowseDestFile.Click += BrowseDestFile;
            this.Controls.Add(grpDest);
            y += tailleGrpDest;

            tailleGrpDuration = 80;
            // GroupBox - Durée retraitement
            grpDuration = new GroupBox { Text = "Durée retraitement :", Left = 10, Top = y, Width = 345, Height = 70 };
            numDay = new NumericUpDown { Left = 10, Top = 25, Width = 50, Minimum = 0, Maximum = 365, Parent = grpDuration };
            Label lblDays = new Label { Text = "Jours", Left = 65, Top = 28, Width = 50, Parent = grpDuration };
            numHour = new NumericUpDown { Left = 120, Top = 25, Width = 50, Minimum = 0, Maximum = 23, Parent = grpDuration };
            Label lblHours = new Label { Text = "Heures", Left = 175, Top = 28, Width = 50, Parent = grpDuration };
            numMinute = new NumericUpDown { Left = 230, Top = 25, Width = 50, Minimum = 0, Maximum = 59, Parent = grpDuration };
            Label lblMinutes = new Label { Text = "Min", Left = 285, Top = 28, Width = 50, Parent = grpDuration };
            this.Controls.Add(grpDuration);
            y += tailleGrpDuration;

            // Boutons
            btnOk = new Button { Text = "OK", Left = 70, Top = y, Width = 100, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Annuler", Left = 180, Top = y, Width = 100, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[] {
                lblIndex, numIndex,
                lblAction, cboAction,
                btnOk, btnCancel
            });

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void CboAction_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string selectedAction = cboAction.SelectedItem?.ToString() ?? "COPIE";
            bool isExecute = selectedAction == "EXECUTER";
            
            // Pour EXECUTER, on cache les options de destination
            txtDestination.Visible = !isExecute;
            btnBrowseSourceFolder.Visible = !isExecute;
            btnBrowseDestFolder.Visible = !isExecute;
            btnBrowseDestFile.Visible = !isExecute;
            chkAddDateToDestination.Visible = !isExecute;
            chkAddTimeToDestination.Visible = !isExecute;
            grpDest.Visible = !isExecute;

            grpDuration.Top = isExecute ? positionGrpDest : positionGrpDest+tailleGrpDest;
            btnOk.Top = isExecute ? positionGrpDest+tailleGrpDuration : positionGrpDest+tailleGrpDest+tailleGrpDuration;
            btnCancel.Top = btnOk.Top;

            this.Size = new Size(380, grpDuration.Bottom + 80);

            if (isExecute)
            {
                txtDestination.Text = "";
                chkAddDateToDestination.Checked = false;
                chkAddTimeToDestination.Checked = false;
            }
        }

        private void LoadEntry()
        {
            txtSourceFile.Text = BackupEntry.SourcePath;
            txtDestination.Text = BackupEntry.DestinationPath;
            numDay.Value = BackupEntry.DayInterval;
            numHour.Value = BackupEntry.HourInterval;
            numMinute.Value = BackupEntry.MinuteInterval;
            cboAction.SelectedItem = BackupEntry.ActionType;
            chkAddDateToDestination.Checked = BackupEntry.AddDateToDestination;
            chkAddTimeToDestination.Checked = BackupEntry.AddTimeToDestination;
            numIndex.Value = BackupEntry.Index > 0 ? BackupEntry.Index : 1;

            // Mettre à jour l'interface selon le type d'action
            CboAction_SelectedIndexChanged(null, EventArgs.Empty);
        }

        private void BrowseSourceFile(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Tous les fichiers (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtSourceFile.Text = ofd.FileName;
                }
            }
        }

        private void BrowseSourceFolder(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtSourceFile.Text = fbd.SelectedPath;
                }
            }
        }

        private void BrowseDestFile(object? sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Tous les fichiers (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    txtDestination.Text = sfd.FileName;
                }
            }
        }

        private void BrowseDestFolder(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDestination.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSourceFile.Text))
            {
                MessageBox.Show("Veuillez sélectionner un fichier source.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedAction = cboAction.SelectedItem?.ToString() ?? "COPIE";

            // Pour COPIE et ZIP, la destination est obligatoire
            if ((selectedAction == "COPIE" || selectedAction == "ZIP") && string.IsNullOrWhiteSpace(txtDestination.Text))
            {
                MessageBox.Show($"Veuillez sélectionner une destination pour l'action {selectedAction}.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BackupEntry.SourcePath = txtSourceFile.Text;
            BackupEntry.DestinationPath = txtDestination.Text;
            BackupEntry.DayInterval = (int)numDay.Value;
            BackupEntry.HourInterval = (int)numHour.Value;
            BackupEntry.MinuteInterval = (int)numMinute.Value;
            BackupEntry.ActionType = selectedAction;
            BackupEntry.AddDateToDestination = chkAddDateToDestination.Checked;
            BackupEntry.AddTimeToDestination = chkAddTimeToDestination.Checked;
            BackupEntry.Index = (int)numIndex.Value;
            BackupEntry.Error = false;
        }
    }
}
