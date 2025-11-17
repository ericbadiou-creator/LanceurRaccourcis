using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Text.Json;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;

namespace LanceurRaccourcis
{
    [SupportedOSPlatform("windows")]
    public class NetworkDriveForm : Form
    {
        private DataGridView? driveGridView;
        private Button? refreshButton;
        private Button? disconnectButton;
        private Button? addButton;
        private Button? mountButton;
        private Button? editButton;
        private Button? deleteButton;
        private Button? closeButton;
        private ContextMenuStrip? driveContextMenu;
        private ToolStripMenuItem? contextEditItem;
        private ToolStripMenuItem? contextDeleteItem;
        private ToolStripMenuItem? contextMountItem;
        private ToolStripMenuItem? contextDismountItem;
        private string configFilePath;

        public event Action<List<NetworkDriveConfig>>? NetworkDrivesSaved;

        public NetworkDriveForm(string configPath)
        {
            configFilePath = configPath;
            InitializeComponents();
            LoadNetworkDrives();
        }

        private void InitializeComponents()
        {
            this.Text = "Gestionnaire de lecteurs réseau";
            this.Size = new Size(900, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LanceurRaccourcis.icon.ico");
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
                else
                {
                    this.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                this.Icon = SystemIcons.Application;
            }

            driveGridView = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(870, 350),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            driveGridView.SelectionChanged += (s, e) => UpdateActionButtons();
            driveGridView.CellMouseDown += DriveGridView_CellMouseDown;

            driveGridView.Columns.Add("Lecteur", "Lecteur");
            driveGridView.Columns.Add("Description", "Description");
            driveGridView.Columns.Add("Server", "Server");
            driveGridView.Columns.Add("Chemin", "Chemin réseau");
            driveGridView.Columns.Add("Utilisateur", "Utilisateur");
            driveGridView.Columns.Add("Password", "Password");
            driveGridView.Columns.Add("Lancer au Demarrage", "Lancer au Demarrage");
            driveGridView.Columns.Add("Etat", "État");

            driveContextMenu = new ContextMenuStrip();
            contextEditItem = new ToolStripMenuItem("✏️ Modifier", null, EditButton_Click);
            contextDeleteItem = new ToolStripMenuItem("🗑️ Supprimer", null, DeleteButton_Click);
            contextMountItem = new ToolStripMenuItem("📌 Monter", null, MountButton_Click);
            contextDismountItem = new ToolStripMenuItem("📍 Démonter", null, DismountButton_Click);

            driveContextMenu.Items.AddRange(new ToolStripItem[]
            {
                contextEditItem,
                contextDeleteItem,
                new ToolStripSeparator(),
                contextMountItem,
                contextDismountItem
            });

            driveGridView.ContextMenuStrip = driveContextMenu;

            this.Controls.Add(driveGridView);

            refreshButton = new Button
            {
                Text = "🔄 Actualiser",
                Location = new Point(10, 370),
                Width = 100
            };
            refreshButton.Click += (s, e) => LoadNetworkDrives();
            this.Controls.Add(refreshButton);

            addButton = new Button
            {
                Text = "➕ Ajouter",
                Location = new Point(120, 370),
                Width = 100
            };
            addButton.Click += (s, e) => ShowAddNetworkDriveForm();
            this.Controls.Add(addButton);

            editButton = new Button
            {
                Text = "✏️ Modifier",
                Location = new Point(230, 370),
                Width = 100
            };
            editButton.Click += EditButton_Click;
            this.Controls.Add(editButton);

            deleteButton = new Button
            {
                Text = "🗑️ Supprimer",
                Location = new Point(340, 370),
                Width = 100
            };
            deleteButton.Click += DeleteButton_Click;
            this.Controls.Add(deleteButton);

            mountButton = new Button
            {
                Text = "📌 Monter",
                Location = new Point(550, 370),
                Width = 100
            };
            mountButton.Click += MountButton_Click;
            this.Controls.Add(mountButton);
            
            disconnectButton = new Button
            {
                Text = "📍 Démonter",
                Location = new Point(660, 370),
                Width = 100
            };
            disconnectButton.Click += DismountButton_Click;
            this.Controls.Add(disconnectButton);

            closeButton = new Button
            {
                Text = "❌ Fermer",
                Location = new Point(770, 370),
                Width = 100
            };
            closeButton.Click += (s, e) => Close();
            this.Controls.Add(closeButton);


        }

        private void LoadNetworkDrives()
        {
            if (driveGridView == null) return;

            string? previouslySelectedDrive = null;
            if (driveGridView.SelectedRows.Count > 0)
            {
                previouslySelectedDrive = driveGridView.SelectedRows[0].Cells["Lecteur"].Value?.ToString();
            }

            DataGridViewColumn? sortedColumn = driveGridView.SortedColumn;
            SortOrder sortOrder = driveGridView.SortOrder;

            driveGridView.SuspendLayout();

            driveGridView.Rows.Clear();

            try
            {
                // Charger depuis la configuration JSON
                var networkDrivesConfig = LoadNetworkDrivesConfig();

                if (networkDrivesConfig != null && networkDrivesConfig.Count > 0)
                {
                    // Obtenir les lecteurs actuellement montés
                    var mountedDrives = GetMountedNetworkDrives();

                    foreach (var config in networkDrivesConfig)
                    {
                        string driveLetter = config.DriveLetter ?? "N/A";
                        string Description = config.Description ?? "N/A";
                        string Server = config.Server ?? "N/A";
                        string NetworkPath = config.NetworkPath ?? "N/A";
                        string Username = config.Username ?? "N/A";
                        string Password = config.Password ?? "N/A";
                        bool LancerDemarrage = config.LancerDemarrage;
                        

                        // Vérifier si le lecteur est monté
                        string status = mountedDrives.Contains(driveLetter) ? "Connecté" : "Déconnecté";

                        driveGridView.Rows.Add(driveLetter, Description, Server, NetworkPath, Username, Password, LancerDemarrage, status);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des lecteurs réseau:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                driveGridView.ResumeLayout();
            }

            if (sortedColumn != null && sortOrder != SortOrder.None)
            {
                ListSortDirection direction = sortOrder == SortOrder.Descending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                try
                {
                    driveGridView.Sort(sortedColumn, direction);
                }
                catch
                {
                    // Ignore si le tri échoue (ex: colonne supprimée)
                }
            }

            driveGridView.ClearSelection();

            bool selectionRestored = false;
            if (!string.IsNullOrEmpty(previouslySelectedDrive))
            {
                foreach (DataGridViewRow row in driveGridView.Rows)
                {
                    string rowDrive = row.Cells["Lecteur"].Value?.ToString() ?? string.Empty;
                    if (string.Equals(rowDrive, previouslySelectedDrive, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        driveGridView.CurrentCell = row.Cells[0];
                        selectionRestored = true;
                        break;
                    }
                }
            }

            if (!selectionRestored && driveGridView.Rows.Count > 0)
            {
                driveGridView.Rows[0].Selected = true;
                driveGridView.CurrentCell = driveGridView.Rows[0].Cells[0];
            }

            UpdateActionButtons();
        }

        private List<NetworkDriveConfig> LoadNetworkDrivesConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string jsonString = File.ReadAllText(configFilePath);
                    AppConfig? config = JsonSerializer.Deserialize<AppConfig>(jsonString);
                    return config?.NetworkDrives ?? new List<NetworkDriveConfig>();
                }
            }
            catch { }
            return new List<NetworkDriveConfig>();
        }

        private List<string> GetMountedNetworkDrives()
        {
            var mountedDrives = new List<string>();

            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_LogicalDisk WHERE DriveType = 4"))
                {
                    foreach (var drive in searcher.Get())
                    {
                        string driveLetter = drive["Name"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(driveLetter))
                        {
                            mountedDrives.Add(driveLetter);
                        }
                    }
                }
            }
            catch { }

            return mountedDrives;
        }



        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (driveGridView == null || driveGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur à modifier.",
                    "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = driveGridView.SelectedRows[0];
            string driveLetter = selectedRow.Cells["Lecteur"].Value?.ToString() ?? "";
            string Description = selectedRow.Cells["Description"].Value?.ToString() ?? "";
            string Server = selectedRow.Cells["Server"].Value?.ToString() ?? "";            
            string networkPath = selectedRow.Cells["Chemin"].Value?.ToString() ?? "";
            string username = selectedRow.Cells["Utilisateur"].Value?.ToString() ?? "";
            string Password = selectedRow.Cells["Password"].Value?.ToString() ?? "";
            bool LancerDemarrage = selectedRow.Cells["Lancer au Demarrage"].Value is true;
            string status = selectedRow.Cells["Etat"].Value?.ToString() ?? string.Empty;

            if (string.Equals(status, "Connecté", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Déconnexion requise avant modification du lecteur {driveLetter}");
                string? errorMessage;
                bool ok = NetworkDriveHelper.UnmapNetworkDrive(driveLetter, true, out errorMessage);
                if (!ok)
                {
                    Logger.LogError($"Impossible de déconnecter {driveLetter} avant modification", new Exception(errorMessage ?? "Erreur inconnue"));
                    MessageBox.Show($"Impossible de déconnecter {driveLetter} :\n{errorMessage}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Logger.Log($"Lecteur {driveLetter} déconnecté pour modification");
                selectedRow.Cells["Etat"].Value = "Déconnecté";
                SaveCurrentDrives();
                UpdateActionButtons();
            }

            using (EditNetworkDriveForm editForm = new EditNetworkDriveForm(driveLetter, Description, Server,
                networkPath, username, Password, LancerDemarrage))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    selectedRow.Cells["Lecteur"].Value = editForm.DriveLetter;
                    selectedRow.Cells["Description"].Value = editForm.Description;
                    selectedRow.Cells["Server"].Value = editForm.Server;
                    selectedRow.Cells["Chemin"].Value = editForm.NetworkPath;
                    selectedRow.Cells["Utilisateur"].Value = editForm.Username;
                    selectedRow.Cells["Password"].Value = editForm.Password;
                    selectedRow.Cells["Lancer au Demarrage"].Value = editForm.LancerDemarrage;
                    SaveCurrentDrives();
                    LoadNetworkDrives();
                }
            }
        }

        private void MountButton_Click(object? sender, EventArgs e)
        {
            if (driveGridView == null || driveGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur à monter.",
                    "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = driveGridView.SelectedRows[0];
            string driveLetter = selectedRow.Cells["Lecteur"].Value?.ToString() ?? "";
            string networkPath = selectedRow.Cells["Chemin"].Value?.ToString() ?? "";
            string username = selectedRow.Cells["Utilisateur"].Value?.ToString() ?? "";
            string password = selectedRow.Cells["Password"].Value?.ToString() ?? "";

            if (string.IsNullOrEmpty(driveLetter) || string.IsNullOrEmpty(networkPath))
                return;

            try
            {
                Logger.Log($"Demande de montage manuel du lecteur {driveLetter} -> {networkPath}");
                string? errorMessage;
                bool ok = NetworkDriveHelper.MapNetworkDrive(driveLetter, networkPath,
                    string.IsNullOrWhiteSpace(username) || username == "N/A" ? null : username,
                    string.IsNullOrWhiteSpace(password) || password == "N/A" ? null : password,
                    false, out errorMessage);
                if (ok)
                {
                    Logger.Log($"Lecteur {driveLetter} monté manuellement avec succès");
                    MessageBox.Show($"Lecteur {driveLetter} monté avec succès!", "Succès",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadNetworkDrives();
                }
                else
                {
                    Logger.LogError($"Erreur de montage manuel pour {driveLetter}", new Exception(errorMessage ?? "Erreur inconnue"));
                    MessageBox.Show($"Erreur de montage:\n{errorMessage}", "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception lors du montage manuel de {driveLetter}", ex);
                MessageBox.Show($"Erreur:\n{ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DismountButton_Click(object? sender, EventArgs e)
        {
            if (driveGridView == null || driveGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur à démonter.",
                    "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = driveGridView.SelectedRows[0];
            string driveLetter = selectedRow.Cells["Lecteur"].Value?.ToString() ?? "";

            if (string.IsNullOrEmpty(driveLetter))
                return;

            DialogResult result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir démonter {driveLetter} ?",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Logger.Log($"Demande de démontage manuel du lecteur {driveLetter}");
                    string? errorMessage;
                    bool ok = NetworkDriveHelper.UnmapNetworkDrive(driveLetter, true, out errorMessage);
                    if (ok)
                    {
                        Logger.Log($"Lecteur {driveLetter} démonté manuellement");
                        MessageBox.Show("Lecteur démonté avec succès!", "Succès",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadNetworkDrives();
                    }
                    else
                    {
                        Logger.LogError($"Erreur lors du démontage manuel de {driveLetter}", new Exception(errorMessage ?? "Erreur inconnue"));
                        MessageBox.Show($"Erreur lors du démontage:\n{errorMessage}",
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erreur lors du démontage manuel de {driveLetter}", ex);
                    MessageBox.Show($"Erreur lors du démontage:\n{ex.Message}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (driveGridView == null || driveGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur à supprimer.",
                    "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = driveGridView.SelectedRows[0];
            string driveLetter = selectedRow.Cells["Lecteur"].Value?.ToString() ?? "";
            string status = selectedRow.Cells["Etat"].Value?.ToString() ?? string.Empty;

            DialogResult result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer la configuration de {driveLetter} ?\nCette action ne supprimera pas la connexion active.",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (string.Equals(status, "Connecté", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Déconnexion du lecteur {driveLetter} avant suppression");
                    string? errorMessage;
                    bool ok = NetworkDriveHelper.UnmapNetworkDrive(driveLetter, true, out errorMessage);
                    if (!ok)
                    {
                        Logger.LogError($"Échec de la déconnexion de {driveLetter} avant suppression", new Exception(errorMessage ?? "Erreur inconnue"));
                        MessageBox.Show($"Impossible de déconnecter {driveLetter} :\n{errorMessage}",
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Logger.Log($"Lecteur {driveLetter} déconnecté avant suppression");
                }

                driveGridView.Rows.RemoveAt(selectedRow.Index);
                SaveCurrentDrives();
                MessageBox.Show("Lecteur supprimé de la configuration!", "Succès",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateActionButtons();
            }
        }

        private void ShowAddNetworkDriveForm()
        {
            using (AddNetworkDriveForm addForm = new AddNetworkDriveForm())
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    // Ajouter à la liste sans monter
                    string? driveLetter = addForm.DriveLetter;
                    string? Description = addForm.Description;
                    string? Server = addForm.Server;
                    string? networkPath = addForm.NetworkPath;
                    string? username = addForm.Username;
                    string? Password = addForm.Password;
                    bool? LancerDemarrage = addForm.LancerDemarrage;
                    
                    if (driveGridView != null && driveLetter != null && Description != null && Server  != null
                        && networkPath != null && username != null && Password != null && LancerDemarrage != null)
                    {
                        int rowIndex = driveGridView.Rows.Add(driveLetter, Description, Server, networkPath, username, Password, LancerDemarrage, "Déconnecté");
                        driveGridView.ClearSelection();
                        driveGridView.Rows[rowIndex].Selected = true;
                    }

                    SaveCurrentDrives();
                    MessageBox.Show("Lecteur réseau ajouté à la configuration!", "Succès",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateActionButtons();
                }
            }
        }

        private void SaveCurrentDrives()
        {
            if (driveGridView == null) return;

            var drives = new List<NetworkDriveConfig>();

            foreach (DataGridViewRow row in driveGridView.Rows)
            {
                drives.Add(new NetworkDriveConfig
                {
                    DriveLetter = row.Cells["Lecteur"].Value?.ToString(),
                    Description = row.Cells["Description"].Value?.ToString(),
                    Server = row.Cells["Server"].Value?.ToString(),
                    NetworkPath = row.Cells["Chemin"].Value?.ToString(),
                    Username = row.Cells["Utilisateur"].Value?.ToString(),
                    Password = row.Cells["Password"].Value?.ToString(),
                    LancerDemarrage = row.Cells["Lancer au Demarrage"].Value is true
                });
            }

            NetworkDrivesSaved?.Invoke(drives);
            UpdateActionButtons();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                driveGridView?.Dispose();
                refreshButton?.Dispose();
                disconnectButton?.Dispose();
                addButton?.Dispose();
                mountButton?.Dispose();
                editButton?.Dispose();
                deleteButton?.Dispose();
                driveContextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdateActionButtons()
        {
            bool canMount = false;
            bool canDismount = false;
            bool hasSelection = false;

            if (driveGridView != null && driveGridView.SelectedRows.Count > 0)
            {
                var selectedRow = driveGridView.SelectedRows[0];
                string status = selectedRow.Cells["Etat"].Value?.ToString() ?? string.Empty;
                bool isConnected = string.Equals(status, "Connecté", StringComparison.OrdinalIgnoreCase);
                canMount = !isConnected;
                canDismount = isConnected;
                hasSelection = true;
            }

            if (mountButton != null) mountButton.Enabled = canMount;
            if (disconnectButton != null) disconnectButton.Enabled = canDismount;

            if (editButton != null) editButton.Enabled = hasSelection;
            if (deleteButton != null) deleteButton.Enabled = hasSelection;

            if (contextMountItem != null) contextMountItem.Enabled = canMount;
            if (contextDismountItem != null) contextDismountItem.Enabled = canDismount;
            if (contextEditItem != null) contextEditItem.Enabled = hasSelection;
            if (contextDeleteItem != null) contextDeleteItem.Enabled = hasSelection;
        }
        private void DriveGridView_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (driveGridView == null) return;

            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < driveGridView.Rows.Count)
            {
                driveGridView.ClearSelection();
                driveGridView.Rows[e.RowIndex].Selected = true;
                if (e.ColumnIndex >= 0)
                {
                    driveGridView.CurrentCell = driveGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
                else
                {
                    driveGridView.CurrentCell = driveGridView.Rows[e.RowIndex].Cells[0];
                }
                UpdateActionButtons();
            }
        }
    }
}