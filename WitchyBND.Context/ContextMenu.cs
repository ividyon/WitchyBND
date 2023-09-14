using System.Runtime.InteropServices;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace WitchyBND.Context;

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
[ProgId("WitchyBND.ContextMenu")]
public class ContextMenu : SharpContextMenu
{
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
        };

        ToolStripMenuItem processMenuItem = new ToolStripMenuItem
        {
            Text = SelectedItemPaths.Count() > 1 ? $"Process {SelectedItemPaths.Count()} selected items..." : "Process selection..."
        };

        witchyMenu.DropDownItems.Add(processMenuItem);

        if (SelectedItemPaths.Any(p => p.EndsWith(".dcx")))
        {
            ToolStripMenuItem ProcessDcxMenuItem = new ToolStripMenuItem
            {
                Text = "Process selection (Decompress DCX)..."
            };
            witchyMenu.DropDownItems.Add(ProcessDcxMenuItem);
        }

        menu.Items.Add(witchyMenu);
        return menu;
    }
}