using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace WitchyBND.Shell
{
// <summary>
// The SubMenuExtension is an example shell context menu extension,
// implemented with SharpShell. It loads the menu dynamically
// files.
//
// Adapted from: https://www.codeproject.com/Articles/1035998/NET-Shell-Extensions-Adding-submenus-to-Shell-Cont
// </summary>
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    [Guid("CCE90C57-0A92-4CB7-8E9B-0CFA92138AE9")]
    [ProgId("WitchyBND.Shell.WitchyContextMenu")]
    public class WitchyContextMenu : SharpContextMenu
    {
        public static readonly string WitchyPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".", "WitchyBND.exe");
        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var count = SelectedItemPaths.Count();
            bool plural = count > 1;
            bool tooMany = count > 15;

            var menu = new ContextMenuStrip();

            ToolStripMenuItem witchyShortcut = new ToolStripMenuItem
            {
                Text = "WitchyBND",
                Image = Image.FromFile(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Assets", "context.png"), true),
            };

            if (!tooMany)
            {
                if (SelectedItemPaths.Count() == 1 && Directory.Exists(SelectedItemPaths.First()))
                {
                    if (Directory.GetFiles(SelectedItemPaths.First(), "_witchy*.xml", SearchOption.TopDirectoryOnly)
                            .Length > 0)
                    {
                        witchyShortcut.Click += ProcessMenuItemOnClick;
                        menu.Items.Add(witchyShortcut);
                    }
                }
                else
                {
                    witchyShortcut.Click += ProcessMenuItemOnClick;
                    menu.Items.Add(witchyShortcut);
                }
            }

            ToolStripMenuItem witchyMenu = new ToolStripMenuItem
            {
                Text = "WitchyBND...",
                Image = Image.FromFile(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Assets", "context.png"), true),
            };
            if (tooMany)
            {
                ToolStripMenuItem tooManyMenuItem = new ToolStripMenuItem
                {
                    Text = "Use 'Send to' to process 15+ items",
                    ForeColor = SystemColors.GrayText,
                };
                witchyMenu.DropDownItems.Add(tooManyMenuItem);
            }
            else
            {
                ToolStripMenuItem processMenuItem = new ToolStripMenuItem
                {
                    Text = "Process here"
                };
                processMenuItem.Click += ProcessMenuItemOnClick;
                witchyMenu.DropDownItems.Add(processMenuItem);

                ToolStripMenuItem processRecursiveMenuItem = new ToolStripMenuItem
                {
                    Text = "Process here (Recursive)"
                };
                processRecursiveMenuItem.Click += ProcessRecursiveMenuItemOnClick;
                witchyMenu.DropDownItems.Add(processRecursiveMenuItem);

                if (SelectedItemPaths.Any(p => p.EndsWith(".dcx")))
                {
                    ToolStripMenuItem processDcxMenuItem = new ToolStripMenuItem
                    {
                        Text = "Process here (DCX compression)"
                    };
                    processDcxMenuItem.Click += ProcessDcxMenuItemOnClick;
                    witchyMenu.DropDownItems.Add(processDcxMenuItem);
                }

                ToolStripMenuItem processToMenuItem = new ToolStripMenuItem
                {
                    Text = "Process to..."
                };
                processToMenuItem.Click += ProcessToMenuItemOnClick;
                witchyMenu.DropDownItems.Add(processToMenuItem);

                ToolStripMenuItem processRecursiveToMenuItem = new ToolStripMenuItem
                {
                    Text = "Process to... (Recursive)"
                };
                processRecursiveToMenuItem.Click += ProcessRecursiveToMenuItemOnClick;
                witchyMenu.DropDownItems.Add(processRecursiveToMenuItem);

                if (SelectedItemPaths.Any(p => p.EndsWith(".dcx")))
                {
                    ToolStripMenuItem processDcxToMenuItem = new ToolStripMenuItem
                    {
                        Text = "Process to... (DCX compression)"
                    };
                    processDcxToMenuItem.Click += ProcessDcxToMenuItemOnClick;
                    witchyMenu.DropDownItems.Add(processDcxToMenuItem);
                }
            }

            menu.Items.Add(witchyMenu);
            return menu;
        }

        private void ProcessMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\"")));
        }
        private void ProcessToMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--location prompt {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        private void ProcessDcxMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--dcx {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        private void ProcessDcxToMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--location prompt --dcx {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        private void ProcessRecursiveMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--recursive {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        private void ProcessRecursiveToMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--location prompt --recursive {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        // private void AllFilesMenuItemOnClick(object sender, EventArgs e)
        // {
        //     if (SelectedItemPaths.Count() > 0)
        //         Process.Start(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "WitchyBND.exe"), $"{Path.GetDirectoryName(SelectedItemPaths.First())}\\*.*");
        // }

    }

}