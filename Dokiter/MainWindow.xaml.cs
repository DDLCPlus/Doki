using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Dokiter.Helpers;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Dokiter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            VersionLabel.Content = GlobalUtils.CurrentVersion;

            if (File.Exists("ddlcplus.config") && File.ReadAllLines("ddlcplus.config").Length > 0)
            {
                InstallButton.Content = "Uninstall";
                InstallButton.Click += Uninstall_Click;
                BrowseModsButton.IsEnabled = true;
                BrowseModsButton.Click += BrowseModsButton_Click;
            }
            else
                InstallButton.Click += Install_Click;
        }

        private void BrowseModsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not done yet! haha sorry");
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("ddlcplus.config"))
            {
                MessageBox.Show("Failed to locate DDLC+! Please make sure you didn't delete the configuration file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            GlobalUtils.DDLCPlusPath = File.ReadAllLines("ddlcplus.config")[0];

            if (GlobalUtils.DDLCPlusPath == null)
            {
                MessageBox.Show("Failed to locate DDLC+! Please make sure you didn't delete the configuration file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string managedDir = $"{GlobalUtils.DDLCPlusPath}\\Doki Doki Literature Club Plus_Data\\Managed";

            File.Delete($"{managedDir}\\UnityEngine.CoreModule.dll");
            File.Delete($"{managedDir}\\DDLC.dll");

            File.Move($"{managedDir}\\UnityEngine.CoreModule.bak", $"{managedDir}\\UnityEngine.CoreModule.dll");
            File.Move($"{managedDir}\\DDLC.bak", $"{managedDir}\\DDLC.dll");

            File.Delete($"{GlobalUtils.DDLCPlusPath}\\Doki.dll");
            File.Delete($"{GlobalUtils.DDLCPlusPath}\\0Harmony.dll");
            File.Delete($"{GlobalUtils.DDLCPlusPath}\\RenDisco.dll");

            File.Delete("ddlcplus.config");

            MessageBox.Show("Uninstallation completed! The original game files have been restored.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            InstallButton.Content = "Install";
            InstallButton.Click -= Uninstall_Click;
            InstallButton.Click += Install_Click;

            BrowseModsButton.IsEnabled = false;
            BrowseModsButton.Click -= BrowseModsButton_Click;
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "Select the DDLC+ executable",
                    Filter = "Executable Files (*.exe)|*.exe",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    string selectedFile = dialog.FileName;

                    if (Path.GetFileName(selectedFile).Equals("Doki Doki Literature Club Plus.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string? selectedDirectory = Path.GetDirectoryName(selectedFile);

                        if (selectedDirectory == null)
                        {
                            MessageBox.Show("Could not determine the base directory for the game.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!File.Exists($"Doki.dll"))
                        {
                            MessageBox.Show("Please make sure Doki.dll is in the same folder as the manager.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!File.Exists($"RenDisco.dll"))
                        {
                            MessageBox.Show("Please make sure RenDisco.dll is in the same folder as the manager.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!File.Exists($"0Harmony.dll"))
                        {
                            MessageBox.Show("Please make sure 0Harmony.dll is in the same folder as the manager.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        GlobalUtils.DDLCPlusPath = selectedDirectory;

                        MessageBox.Show($"DDLC+ directory set to: {GlobalUtils.DDLCPlusPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        if (!Directory.Exists($"{GlobalUtils.DDLCPlusPath}\\Doki Doki Literature Club Plus_Data\\Managed"))
                        {
                            MessageBox.Show("Could not determine the location of the Managed folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        File.Copy($"0Harmony.dll", $"{selectedDirectory}\\0Harmony.dll", overwrite: true);
                        File.Copy($"Doki.dll", $"{selectedDirectory}\\Doki.dll", overwrite: true);
                        File.Copy($"RenDisco.dll", $"{selectedDirectory}\\RenDisco.dll", overwrite: true);

                        string managedDir = $"{GlobalUtils.DDLCPlusPath}\\Doki Doki Literature Club Plus_Data\\Managed";

                        File.Copy($"{managedDir}\\UnityEngine.CoreModule.dll", $"{managedDir}\\UnityEngine.CoreModule.bak", overwrite: true);

                        File.Copy($"{managedDir}\\DDLC.dll", $"{managedDir}\\DDLC.bak", overwrite: true);

                        ModuleDefMD unityModule = ModuleDefMD.Load($"{managedDir}\\UnityEngine.CoreModule.dll");
                        ModuleDefMD ddlcModule = ModuleDefMD.Load($"{managedDir}\\DDLC.dll");
                        ModuleDefMD dokiModule = ModuleDefMD.Load($"{selectedDirectory}\\Doki.dll");

                        //to-do more stuff in DDLC.dll probably? depends on future of the mod loader
                        var renpyDialogueLine = ddlcModule.Types.First(t => t.Name == "RenpyDialogueLine");

                        renpyDialogueLine.Visibility = dnlib.DotNet.TypeAttributes.Public;

                        var bootLoaderType = dokiModule.Types.First(t => t.Name == "BootLoader");
                        var loadMethod = bootLoaderType.Methods.First(m => m.Name == "Load");

                        var importedLoadMethod = unityModule.Import(loadMethod);

                        // Find UnityEngine.Debug .cctor
                        var debugType = unityModule.Types.FirstOrDefault(t => t.Name == "Debug");
                        var cctor = debugType?.Methods.FirstOrDefault(m => m.Name == ".cctor");

                        if (cctor == null)
                        {
                            MessageBox.Show("Failed to find Debug::.cctor!", "Error (0x1A)", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Find types dynamically
                        var debugLogHandlerType = unityModule.Find("UnityEngine.DebugLogHandler", false);
                        var loggerType = unityModule.Find("UnityEngine.Logger", false);
                        var debugTypeType = unityModule.Find("UnityEngine.Debug", false);

                        // Find constructors and static field
                        var debugLogHandlerCtor = debugLogHandlerType.FindMethod(".ctor");
                        var loggerCtor = loggerType.FindMethods(".ctor").First(x => x.IsPublic);
                        var sLoggerField = debugTypeType.FindField("s_Logger");


                        // Inject IL into Debug::.cctor
                        var il = cctor.Body.Instructions;
                        il.Clear();

                        il.Add(Instruction.Create(OpCodes.Call, importedLoadMethod));        // Call Doki.BootLoader::Load()
                        il.Add(Instruction.Create(OpCodes.Newobj, debugLogHandlerCtor));     // new DebugLogHandler()
                        il.Add(Instruction.Create(OpCodes.Newobj, loggerCtor));              // new Logger(DebugLogHandler)
                        il.Add(Instruction.Create(OpCodes.Stsfld, sLoggerField));            // Debug.s_Logger = Logger
                        il.Add(Instruction.Create(OpCodes.Ret));                             // Return from .cctor

                        try
                        {
                            unityModule.Write($"{managedDir}\\UnityEngine.CoreModule_temp.dll");
                        }
                        catch (ModuleWriterException ex)
                        {
                            MessageBox.Show($"Failed to write module: {ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        ddlcModule.Write($"{managedDir}\\DDLC_temp.dll");

                        File.Delete($"{managedDir}\\UnityEngine.CoreModule.dll");
                        File.Delete($"{managedDir}\\DDLC.dll");

                        File.Move($"{managedDir}\\UnityEngine.CoreModule_temp.dll", $"{managedDir}\\UnityEngine.CoreModule.dll");
                        File.Move($"{managedDir}\\DDLC_temp.dll", $"{managedDir}\\DDLC.dll");

                        MessageBox.Show("Patched UnityEngine.CoreModule.dll and DDLC.dll! Doki has been installed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        File.WriteAllLines("ddlcplus.config",
                        [
                            GlobalUtils.DDLCPlusPath,
                            GlobalUtils.CurrentVersion
                        ]);

                        InstallButton.Content = "Uninstall";
                        InstallButton.Click -= Install_Click;
                        InstallButton.Click += Uninstall_Click;

                        BrowseModsButton.IsEnabled = true;
                        BrowseModsButton.Click += BrowseModsButton_Click;
                    }
                    else
                    {
                        MessageBox.Show("Please make sure you select the correct .exe (Should be: Doki Doki Literature Club Plus.exe)", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch(Exception x)
            {
                MessageBox.Show(x.ToString());
            }
        }
    }
}