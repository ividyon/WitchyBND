using System;
using System.IO;
using ShellLink;

namespace WitchyBND.Shell
{
    public static class SendTo
    {
        private static readonly string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);

        private static readonly string witchyPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "WitchyBND.exe");
        private static readonly string shortcutPath = Path.Combine(sendToPath, "WitchyBND.lnk");
        private static readonly string shortcutPathDcx = Path.Combine(sendToPath, "WitchyBND (Decompress DCX).lnk");

        public static void AddSendToShortcuts()
        {
            DeleteSendToShortcuts();
            Shortcut.CreateShortcut(witchyPath, "", Path.GetDirectoryName(witchyPath))
                .WriteToFile(shortcutPath);
            Shortcut.CreateShortcut(witchyPath, "--dcx", Path.GetDirectoryName(witchyPath), witchyPath, 0)
                .WriteToFile(shortcutPathDcx);
        }

        public static void DeleteSendToShortcuts()
        {
            if (File.Exists(shortcutPathDcx))
                File.Delete(shortcutPathDcx);
            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);
        }
    }
}