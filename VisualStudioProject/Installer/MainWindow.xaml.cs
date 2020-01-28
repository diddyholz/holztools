using Installer.Properties;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Installer
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        private const string guid = "{1B3CB67C-6ADF-46DA-8740-752AFC4BA540}";

        private static MainWindow activeWindow;

        private static bool updateApplication = false;

        private static string applicationFullName = "";

        private string windowTitle = "Installer";

        public enum InstallerState
        {
            ChoosingInstallationOptions = 0,
            ConfirmingUninstall,
            Installing,
            Uninstalling
        }

        public MainWindow()
        {
            activeWindow = this;
            InitializeComponent();

            DataContext = this;

            //check if the arguments contain -update
            if (App.Args != null)
            {
                foreach (string s in App.Args)
                {
                    if (s == "-update")
                    {
                        UpdateApplication = true;

                        //get the installation directory
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + guid, true))
                        {
                            installerOptionsControl.InstallationDirectory = key.GetValue("InstallLocation") as string;
                        }

                        //close the program 
                        foreach (Process p in Process.GetProcesses())
                        {
                            if(p.ProcessName.Contains(ProgramName))
                            { 
                                p.Kill();
                                Thread.Sleep(100);
                            }
                        }

                        SetInstallerState(InstallerState.Installing);
                        return;
                    }
                }
            }

            //check if the application is already installed
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + guid, true))
            {
                //start the uninstaller if the application is already installed
                if (key == null)
                {
                    SetInstallerState(InstallerState.ChoosingInstallationOptions);
                }
                else
                {
                    SetInstallerState(InstallerState.ConfirmingUninstall);
                }
            }
        }

        public void SetInstallerState(InstallerState state)
        {
            switch (state)
            {
                case InstallerState.ChoosingInstallationOptions:
                    //make the installer options control visible and hide every other control
                    installerOptionsControl.Visibility = Visibility.Visible;
                    progressControl.Visibility = Visibility.Collapsed;
                    confirmUninstallControl.Visibility = Visibility.Collapsed;

                    WindowTitle = "Configure your installation";
                    break;

                case InstallerState.Installing:
                    //make the progress control visible and hide every other control
                    progressControl.Visibility = Visibility.Visible;
                    installerOptionsControl.Visibility = Visibility.Collapsed;
                    confirmUninstallControl.Visibility = Visibility.Collapsed;

                    WindowTitle = "Installing";

                    //start the installation
                    new Task(installProgram).Start();
                    break;

                case InstallerState.ConfirmingUninstall:
                    //make the uninstaller control visible and hide every other control
                    confirmUninstallControl.Visibility = Visibility.Visible;
                    installerOptionsControl.Visibility = Visibility.Collapsed;
                    progressControl.Visibility = Visibility.Collapsed;

                    WindowTitle = "Confirm Removal";

                    break;

                case InstallerState.Uninstalling:
                    //make the progress control visible and hide every other control
                    progressControl.Visibility = Visibility.Visible;
                    installerOptionsControl.Visibility = Visibility.Collapsed;
                    confirmUninstallControl.Visibility = Visibility.Collapsed;

                    progressControl.UninstallApplication = true;

                    WindowTitle = "Uninstalling";

                    //start the removal
                    new Task(removeProgram).Start();
                    break;
            }
        }

        private void installProgram()
        {
            ResourceManager resourceClass = new ResourceManager(typeof(Resources));
            ResourceSet resourceSet = resourceClass.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            progressControl.Status = "Preparing Destination";

            if (Directory.Exists(InstallationDirectory))
            {
                //delete every file in the programs directory
                foreach (string fileName in Directory.GetFiles(InstallationDirectory))
                {
                    System.IO.File.Delete(fileName);
                }
            }

            int fileAmount = 0;

            //count files to copy
            foreach (DictionaryEntry entry in resourceSet)
            {
                fileAmount++;
            }

            int copiedFiles = 0;

            progressControl.Status = "Copying files";

            //copy every embedded file
            foreach (DictionaryEntry entry in resourceSet)
            {
                //create the correct string
                string fileName = ((string)entry.Key).Replace('_', '.');

                //dont copy txt files
                if (fileName.Contains(".txt"))
                    continue;

                //create the destination directory
                Directory.CreateDirectory(InstallationDirectory);

                byte[] bytes = resourceClass.GetObject(entry.Key as string) as byte[];

                //use an alternative way to convert the icon to bytes
                if (fileName.Contains(".ico"))
                {
                    Icon ico = entry.Value as Icon;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ico.Save(ms);

                        bytes = ms.ToArray();
                    }
                }

                //copy the file
                using (var bw = new BinaryWriter(System.IO.File.Open(InstallationDirectory + fileName, FileMode.OpenOrCreate)))
                {
                    bw.Write(bytes);
                }

                if (fileName.Contains(".exe"))
                    ApplicationFullName = InstallationDirectory + fileName;

                copiedFiles++;
                progressControl.Progress = (byte)((float)((float)((float)copiedFiles / (float)fileAmount) * 100) * 0.8);
            }

            progressControl.Status = "Setting registry keys";

            //creating the subkey for the program
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
            {
                key.CreateSubKey(guid);
            }

            //add all values
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + guid, true))
            {
                key.SetValue("DisplayName", ProgramName, RegistryValueKind.String);
                key.SetValue("Publisher", "diddyholz", RegistryValueKind.String);
                key.SetValue("InstallLocation", InstallationDirectory, RegistryValueKind.String);
                key.SetValue("DisplayIcon", Path.Combine(InstallationDirectory, "icon.ico"));
                key.SetValue("UninstallString", $@"C:\Windows\Installer\{guid}\Installer.exe");
            }

            progressControl.Progress = 85;

            //copy the installer to the windows installer directory
            Directory.CreateDirectory($@"C:\Windows\Installer\{guid}");

            if (System.IO.File.Exists($@"C:\Windows\Installer\{guid}\Installer.exe"))
                System.IO.File.Delete($@"C:\Windows\Installer\{guid}\Installer.exe");

            var data = System.IO.File.ReadAllBytes(AssemblyDirectory);
            using (var bw = new BinaryWriter(System.IO.File.Open($@"C:\Windows\Installer\{guid}\Installer.exe", FileMode.OpenOrCreate)))
            {
                bw.Write(data);
            }

            progressControl.Progress = 85;
            progressControl.Status = "Creating Shortcuts";

            //create all selected shortcuts if its not updating
            if (installerOptionsControl.CreateDesktopShortcut && !UpdateApplication)
                CreateShortcut(ProgramName, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ApplicationFullName, Path.Combine(InstallationDirectory, "icon.ico"));

            if (installerOptionsControl.CreateStartMenuShortcut && !UpdateApplication)
                CreateShortcut(ProgramName, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), ApplicationFullName, Path.Combine(InstallationDirectory, "icon.ico"));

            if (installerOptionsControl.AutoStart && !UpdateApplication)
                CreateShortcut(ProgramName, Environment.GetFolderPath(Environment.SpecialFolder.Startup), ApplicationFullName, Path.Combine(InstallationDirectory, "icon.ico"), "-startup");

            progressControl.Progress = 100;
            progressControl.Status = "Finished Installing";
        }

        private void removeProgram()
        {
            progressControl.Status = "Removing program directories";

            //get the program location
            string directory = "";
            string appName = "";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + guid, true))
            {
                directory = key.GetValue("InstallLocation") as string;
            }

            //close the program and delete the main directory
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains(ProgramName))
                {
                    p.Kill();
                    Thread.Sleep(100);
                }
            }

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);

            //delete appdata directory
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProgramName)))
                Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProgramName), true);

            progressControl.Progress = 50;
            progressControl.Status = "Removing shortcuts and clearing registry";

            //removing all shortcuts
            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ProgramName + ".lnk")))
                System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ProgramName + ".lnk"));

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), ProgramName + ".lnk")))
                System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), ProgramName + ".lnk"));

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ProgramName + ".lnk")))
                System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ProgramName + ".lnk"));

            //deleting registry subkey
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
            {
                key.DeleteSubKey(guid);
            }

            progressControl.Progress = 100;
            progressControl.Status = "Finished Removal";
        }

        private void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string iconLocation, string arguments = "")
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Launch " + ProgramName;
            shortcut.IconLocation = iconLocation;
            shortcut.TargetPath = targetFileLocation;
            shortcut.Arguments = arguments;
            shortcut.Save();
        }

        //events
        private void CloseAppBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeAppBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        //getters and setters
        public static MainWindow ActiveWindow
        {
            get { return activeWindow; }
        }

        public static string ProgramName
        {
            get { return "HolzTools"; }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().GetName().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                return Uri.UnescapeDataString(uri.Path);
            }
        }

        public static string ApplicationFullName
        {
            get { return applicationFullName; }
            set { applicationFullName = value; }
        }
        public bool UpdateApplication
        {
            get { return updateApplication; }
            set 
            {
                updateApplication = value;
                OnPropertyChanged("UpdateApplication");
            }
        }


        public string InstallationDirectory
        {
            get { return installerOptionsControl.InstallationDirectory; }
        }

        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                windowTitle = value;
                OnPropertyChanged("WindowTitle");
            }
        }

        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
