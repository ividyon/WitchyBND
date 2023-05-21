using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace Yabber.Context
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Console.Write(
                $"{assembly.GetName().Name} {assembly.GetName().Version}\n\n" +
                "This program will register Yabber.exe and Yabber.DCX.exe\n" +
                "so that they can be run by right-clicking on a file or folder.\n" +
                "Enter R to register, U to unregister, or anything else to exit.\n" +
                "> ");
            string choice = Console.ReadLine().ToUpper();
            Console.WriteLine();

            if (choice == "R" || choice == "U")
            {
                try
                {
                    RegistryKey classes = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
                    if (choice == "R")
                    {
                        string yabberPath = Path.GetFullPath("Yabber.exe");
                        RegistryKey yabberFileKey = classes.CreateSubKey("*\\shell\\yabber");
                        RegistryKey yabberFileCommand = yabberFileKey.CreateSubKey("command");
                        yabberFileKey.SetValue(null, "Yabber");
                        yabberFileCommand.SetValue(null, $"\"{yabberPath}\" \"%1\"");
                        RegistryKey yabberDirKey = classes.CreateSubKey("directory\\shell\\yabber");
                        RegistryKey yabberDirCommand = yabberDirKey.CreateSubKey("command");
                        yabberDirKey.SetValue(null, "Yabber");
                        yabberDirCommand.SetValue(null, $"\"{yabberPath}\" \"%1\"");

                        string dcxPath = Path.GetFullPath("Yabber.DCX.exe");
                        RegistryKey dcxFileKey = classes.CreateSubKey("*\\shell\\yabberdcx");
                        RegistryKey dcxFileCommand = dcxFileKey.CreateSubKey("command");
                        dcxFileKey.SetValue(null, "Yabber.DCX");
                        dcxFileCommand.SetValue(null, $"\"{dcxPath}\" \"%1\"");

                        Console.WriteLine("Programs registered!");
                    }
                    else if (choice == "U")
                    {
                        classes.DeleteSubKeyTree("*\\shell\\yabber", false);
                        classes.DeleteSubKeyTree("directory\\shell\\yabber", false);
                        classes.DeleteSubKeyTree("*\\shell\\yabberdcx", false);
                        Console.WriteLine("Programs unregistered.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Operation failed; try running As Administrator. Reason:\n{ex}");
                }

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }
    }
}
