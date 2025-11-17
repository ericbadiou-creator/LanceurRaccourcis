using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text.Json;
using System.Reflection; // AJOUTÉ pour System.Reflection.Assembly
using System.Runtime.Versioning;
using System.Management;
using System.Collections.Generic;
using System.ComponentModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using System.Runtime.InteropServices;



namespace LanceurRaccourcis
{   
    [SupportedOSPlatform("windows")]
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Logger.Log("=== Démarrage de l'application LanceurRaccourcis ===");

            if (OperatingSystem.IsWindows())
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            try
            {
                Application.Run(new TrayApplicationContext());
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur fatale dans Main", ex);
                throw;
            }
            finally
            {
                Logger.Log("=== Arrêt de l'application ===");
            }
        }
    }

    

    

    

    
}