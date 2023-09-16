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
    [Guid("CCE90C57-0A92-4CB7-8E9B-0CFA92138AE9")]
    [ProgId("WitchyBND.Shell.ContextMenu")]
    public class ContextMenu : SharpContextMenu
    {
        public static readonly string WitchyPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".", "WitchyBND.exe");
        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            ToolStripMenuItem witchyMenu = new ToolStripMenuItem
            {
                Text = "WitchyBND",
                Image = Image.FromFile(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Assets", "context.png"), true),
            };
            var count = SelectedItemPaths.Count();
            bool plural = count > 1;
            bool tooMany = count > 15;
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
                    Text = plural
                        ? $"Process {count} selected items..."
                        : $"Process {count} selected item..."
                };
                processMenuItem.Click += ProcessMenuItemOnClick;
                witchyMenu.DropDownItems.Add(processMenuItem);

                if (SelectedItemPaths.Any(p => p.EndsWith(".dcx")))
                {
                    ToolStripMenuItem processDcxMenuItem = new ToolStripMenuItem
                    {
                        Text = plural
                            ? $"Process {count} selected items (Decompress DCX)..."
                            : $"Process {count} selected item (Decompress DCX)..."
                    };
                    processDcxMenuItem.Click += ProcessDcxMenuItemOnClick;
                    witchyMenu.DropDownItems.Add(processDcxMenuItem);
                }

                var allFilesMenuItem = new ToolStripMenuItem
                {
                    Text = "Process all files in folder..."
                };
                allFilesMenuItem.Click += AllFilesMenuItemOnClick;
                witchyMenu.DropDownItems.Add(allFilesMenuItem);
            }

            menu.Items.Add(witchyMenu);
            return menu;
        }

        private void AllFilesMenuItemOnClick(object sender, EventArgs e)
        {
            if (SelectedItemPaths.Count() > 0)
                Process.Start(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "WitchyBND.exe"), $"{Path.GetDirectoryName(SelectedItemPaths.First())}\\*.*");
        }

        private void ProcessDcxMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, $"--dcx {string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\""))}");
        }

        private void ProcessMenuItemOnClick(object sender, EventArgs e)
        {
            Process.Start(WitchyPath, string.Join(" ", SelectedItemPaths.Select(p => $"\"{p}\"")));
        }
    }

}