using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.IO;
using System.Drawing;

namespace LanceurRaccourcis
{
    [SupportedOSPlatform("windows")]
    public class ConfigForm : Form
    {
        // AJOUTÉ le '?' à tous les champs non-initialisés dans le constructeur direct
        private TextBox? pathTextBox;
        private CheckBox? startupCheckBox;
        private Button? browseButton;
        private Button? saveButton;
        private const string APP_NAME = "LanceurRaccourcis";

        // AJOUTÉ le '?' à l'événement
        public event Action<string, bool>? ConfigurationSaved;

        public ConfigForm(string currentPath, bool isStartupChecked)
        {
            InitializeComponents(currentPath, isStartupChecked);
            // L'avertissement CS8618 disparaît car les champs sont initialisés dans InitializeComponents
        }

        private void InitializeComponents(string currentPath, bool isStartupChecked)
        {
            this.Text = "Configuration";
            this.Size = new Size(500, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label pathLabel = new Label
            {
                Text = "Dossier des raccourcis:",
                Location = new Point(20, 20),
                Width = 150
            };
            this.Controls.Add(pathLabel);

            pathTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 350,
                Text = currentPath
            };
            this.Controls.Add(pathTextBox);

            browseButton = new Button
            {
                Text = "Parcourir...",
                Location = new Point(380, 43),
                Width = 90
            };
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            startupCheckBox = new CheckBox
            {
                Text = "Démarrer automatiquement avec Windows",
                Location = new Point(20, 85),
                Width = 350,
                Checked = isStartupChecked
            };
            this.Controls.Add(startupCheckBox);

            saveButton = new Button
            {
                Text = "Enregistrer",
                Location = new Point(190, 120),
                Width = 100
            };
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);
        }

        private void BrowseButton_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionnez le dossier contenant vos raccourcis";
                // Utilisation de '?' pour l'accès conditionnel
                dialog.SelectedPath = pathTextBox?.Text ?? string.Empty;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (pathTextBox != null)
                    {
                        pathTextBox.Text = dialog.SelectedPath;
                    }
                }
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            Logger.Log("Sauvegarde de la configuration depuis ConfigForm");
            // Utilisation de 'pathTextBox?.Text' pour la nullité
            string path = pathTextBox?.Text ?? string.Empty;

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    Logger.Log($"Dossier créé: {path}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erreur lors de la création du dossier {path}", ex);
                    MessageBox.Show($"Erreur lors de la création du dossier:\n{ex.Message}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Gestion du démarrage automatique
            SetStartup(startupCheckBox?.Checked ?? false);

            // Déclenche l'événement
            ConfigurationSaved?.Invoke(path, startupCheckBox?.Checked ?? false);

            MessageBox.Show("Configuration enregistrée avec succès!", "Succès",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }

        // ... (IsInStartup et SetStartup omis car pas de changements majeurs liés à la nullité)
        // ...

        private void SetStartup(bool enable)
        {
            Logger.Log($"Configuration du démarrage automatique: {enable}");
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        string exePath = Application.ExecutablePath;
                        key?.SetValue(APP_NAME, $"\"{exePath}\"");
                        Logger.Log("Démarrage automatique activé");
                    }
                    else
                    {
                        key?.DeleteValue(APP_NAME, false);
                        Logger.Log("Démarrage automatique désactivé");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur lors de la configuration du démarrage automatique", ex);
                MessageBox.Show($"Erreur lors de la configuration du démarrage automatique:\n{ex.Message}",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}