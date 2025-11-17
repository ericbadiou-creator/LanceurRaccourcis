using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Drawing;

namespace LanceurRaccourcis
{
    [SupportedOSPlatform("windows")]
    public class EditNetworkDriveForm : Form
    {
        private TextBox? letterTextBox;
        private TextBox? pathTextBox;
        private TextBox? descriptionBox;
        private TextBox? serverBox;
        private TextBox? usernameTextBox;
        private CheckBox? identifiersCheckBox;
        private TextBox? passwordTextBox;
        private CheckBox? lancerDemarrageBox;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? DriveLetter { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? Description { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? Server { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? NetworkPath { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? Username { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? Password { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool? LancerDemarrage { get; set; }

        public EditNetworkDriveForm(string driveLetter, string Description, string Server, string networkPath,
            string username, string Password, bool LancerDemarrage)
        {
            InitializeComponents(driveLetter, Description, Server, networkPath, username, Password, LancerDemarrage);
        }

        private void InitializeComponents(string driveLetter, string Description, string Server, string networkPath,
            string username, string Password, bool LancerDemarrage)
        {
            this.Text = "Modifier un lecteur réseau";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int position = 15;

            Label letterLabel = new Label { Text = "Lettre du lecteur:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(letterLabel);

            letterTextBox = new TextBox { Location = new Point(120, position), Width = 50, Text = driveLetter, ReadOnly = true };
            this.Controls.Add(letterTextBox);

            position += 30;

            Label DescriptionLabel = new Label { Text = "Description:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(DescriptionLabel);

            descriptionBox = new TextBox { Location = new Point(120, position), Width = 260, Text = Description };
            this.Controls.Add(descriptionBox);

            position += 30;

            Label ServeurLabel = new Label { Text = "Serveur:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(ServeurLabel);

            serverBox = new TextBox { Location = new Point(120, position), Width = 260, Text = Server };
            this.Controls.Add(serverBox);

            position += 30;

            Label pathLabel = new Label { Text = "Chemin réseau:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(pathLabel);

            pathTextBox = new TextBox { Location = new Point(120, position), Width = 260, Text = networkPath };
            this.Controls.Add(pathTextBox);

            position += 30;

            bool useCredentials = (!string.IsNullOrEmpty(username) && username != "N/A") || (!string.IsNullOrEmpty(Password) && Password != "N/A");

            identifiersCheckBox = new CheckBox
            {
                Text = "Utiliser identifiants",
                Location = new Point(10, position),
                Width = 150,                
                Checked = useCredentials
            };
            identifiersCheckBox.CheckedChanged += (s, e) =>
            {
                if (usernameTextBox != null) usernameTextBox.Enabled = identifiersCheckBox.Checked;
                if (passwordTextBox != null) passwordTextBox.Enabled = identifiersCheckBox.Checked;
            };
            this.Controls.Add(identifiersCheckBox);

            position += 30;

            Label userLabel = new Label { Text = "Utilisateur:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(userLabel);

            usernameTextBox = new TextBox
            {
                Location = new Point(120, position),
                Width = 260,
                Text = (!string.IsNullOrEmpty(username) && username != "N/A") ? username : "",
                Enabled = identifiersCheckBox.Checked
            };
            this.Controls.Add(usernameTextBox);
            position += 30;

            Label passLabel = new Label { Text = "Mot de passe:", Location = new Point(10, position), Width = 100 };
            this.Controls.Add(passLabel);

            passwordTextBox = new TextBox
            {
                Location = new Point(120, position),
                Width = 260,
                Text = (!string.IsNullOrEmpty(Password) && Password != "N/A") ? Password : "",
                Enabled = identifiersCheckBox.Checked
            };
            this.Controls.Add(passwordTextBox);

            position += 30;
            lancerDemarrageBox = new CheckBox
            {
                Text = "Lancer au demarrage",
                Location = new Point(10, position),
                Width = 150,
                Checked = LancerDemarrage
            };
            this.Controls.Add(lancerDemarrageBox);

            position += 30;

            Button okButton = new Button { Text = "Enregistrer", Location = new Point(140, position), Width = 100 };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            Button cancelButton = new Button { Text = "Annuler", Location = new Point(250, position), Width = 100, DialogResult = DialogResult.Cancel };
            this.Controls.Add(cancelButton);
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            string letter = letterTextBox?.Text?.Trim() ?? "";
            string path = pathTextBox?.Text?.Trim() ?? "";
            string description = descriptionBox?.Text?.Trim() ?? "";
            string server = serverBox?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(letter) || string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Veuillez remplir tous les champs requis.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DriveLetter = letter;
            Description = description;
            Server = server;
            NetworkPath = path;
            Username = (identifiersCheckBox?.Checked ?? false) ? (usernameTextBox?.Text ?? "") : "";
            Password = (identifiersCheckBox?.Checked ?? false) ? (passwordTextBox?.Text ?? "") : "";
            LancerDemarrage = (lancerDemarrageBox?.Checked ?? false) ? true : false;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                letterTextBox?.Dispose();
                pathTextBox?.Dispose();
                usernameTextBox?.Dispose();
                identifiersCheckBox?.Dispose();
                passwordTextBox?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}