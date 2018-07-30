using System;
using System.IO;
using System.Diagnostics;
using WixSharp;
using installer.src.utils;

namespace installer
{
    class Program
    {
        static void Main(string[] args)
        {
            var distrUrl = $@"{AppUtils.AppDir()}\..\..\..\installer_output";

            if (Directory.Exists(distrUrl))
                Directory.Delete(distrUrl, true);

            var netVersions = new string[2, 2] { { "2.0", "NET2=\"#1\"" }, { "4.0", "NET4A=\"#1\" OR NET4B=\"#1\"" } };

            for (int i = 0; i < netVersions.GetLength(0); ++i)
                Build(distrUrl, netVersions[i, 0], netVersions[i, 1]);

            foreach (var tempFile in Directory.GetFiles(distrUrl, "*.wixpdb"))
                System.IO.File.Delete(tempFile);
        }

        static void Build(string distrUrl, string netVersion, string networkExistCondition)
        {
            var appDir = AppUtils.AppDir();
            var appName = "Убийца нежелательных окон 1С";

            var releaseDir = $@"{appDir}\..\..\..\bin\x86\Release";

            if (Directory.Exists(releaseDir))
                Directory.Delete(releaseDir, true);

            string solutionUrl = $@"{appDir}\..\..\..\KillerOfUnwantedWindows1C.sln";
            string projectUrl = $@"{appDir}\..\..\..\KillerOfUnwantedWindows1C.csproj";
            string projectBackupUrl = Path.ChangeExtension(projectUrl, ".bak");

            if (System.IO.File.Exists(projectBackupUrl))
            {
                System.IO.File.Copy(projectBackupUrl, projectUrl, true);
                System.IO.File.Delete(projectBackupUrl);
            }

            System.IO.File.Copy(projectUrl, projectBackupUrl);
            var projectFileText = System.IO.File.ReadAllText(projectUrl);
            var newProjectFileText = projectFileText.Replace("<TargetFrameworkVersion>v2.0</TargetFrameworkVersion>",
                $"<TargetFrameworkVersion>v{netVersion}</TargetFrameworkVersion>");
            System.IO.File.WriteAllText(projectUrl, newProjectFileText);

            Process.Start(@"c:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe",
                $"/build Release {solutionUrl}").WaitForExit();

            System.IO.File.Copy(projectBackupUrl, projectUrl, true);
            System.IO.File.Delete(projectBackupUrl);

            Process.Start(@"x:\Utils\sign_sha1_sha256.cmd", $@"{appDir}\..\..\..\bin\x86\Release\KillerOfUnwantedWindows1C.exe").WaitForExit();

            var project = new Project(
                appName,
                new LaunchCondition(networkExistCondition, $"Пожалуйста, установите .NET Framework {netVersion} или выберите другой установщик программы для уже установленной у вас версии .NET Framework."),
                new Dir(
                    @"%ProgramFiles%\Killer of unwanted windows 1C",
                    new WixSharp.File($@"{appDir}\..\..\..\bin\x86\Release\config.xml"),
                    new WixSharp.File(
                        $@"{appDir}\..\..\..\bin\x86\Release\KillerOfUnwantedWindows1C.exe",
                        new FileShortcut(appName, @"%Startup%") { Arguments = "-r" }
                    ),
                    new WixSharp.File($@"{appDir}\..\..\..\LICENSE"),
                    new WixSharp.File($@"{appDir}\..\..\..\readme.txt")
                ),
                new Property("ALLUSERS", "1"),
                new RegValueProperty("NET2", Microsoft.Win32.RegistryHive.LocalMachine,
                    @"Software\Microsoft\NET Framework Setup\NDP\v2.0.50727", "Install", "0"),
                new RegValueProperty("NET4A", Microsoft.Win32.RegistryHive.LocalMachine,
                    @"Software\Microsoft\NET Framework Setup\NDP\v4.0\Client", "Install", "0"),
                new RegValueProperty("NET4B", Microsoft.Win32.RegistryHive.LocalMachine,
                    @"Software\Microsoft\NET Framework Setup\NDP\v4\Client", "Install", "0")
            );

            project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");
            project.ControlPanelInfo.Manufacturer = "Vladimir Milkin (helpme1c.ru)";
            project.ControlPanelInfo.HelpLink = "https://helpme1c.ru";
            project.ControlPanelInfo.ProductIcon = $@"{appDir}\..\..\..\images\killer.ico";
            project.UI = WUI.WixUI_InstallDir;
            project.PreserveTempFiles = true;
            project.PreserveDbgFiles = true;
            project.MajorUpgrade = new MajorUpgrade()
            {
                AllowDowngrades = true,
                AllowSameVersionUpgrades = false
            };
            project.Language = "ru-RU";

            var partsOfVersion = netVersion.Split(new char[] { '.' });
            int major = int.Parse(partsOfVersion[0] + partsOfVersion[1]);
            int minor = partsOfVersion.Length > 2 ? int.Parse(partsOfVersion[2]) : 0;

            var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            project.Version = new Version(major, minor, appVersion.Build, appVersion.Revision);
            project.LicenceFile = $@"{appDir}\..\..\..\LICENSE.rtf";
            project.RebootSupressing = RebootSupressing.ReallySuppress;

            var msiUrl = $@"{distrUrl}\setup_for_net_{netVersion.Replace(".", "_")}.msi";

            Compiler.BuildMsi(project, msiUrl);

            Process.Start(@"x:\Utils\sign_sha1.cmd", msiUrl).WaitForExit();
        }
    }
}