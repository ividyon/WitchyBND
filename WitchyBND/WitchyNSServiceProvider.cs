using System;
using System.IO;
using System.Linq;
using System.Threading;
#if MACOS
using AppKit;
using Foundation;
using ObjCRuntime;

namespace WitchyBND;

public class WitchyNSServiceProvider : NSObject
{
    private string[] SelectedItemPaths { [Export("selectedItemPaths")] get; set; }

    private static Class GetClass(Type type) => new Class(Class.GetHandle(type));

    [Export("witchyShortcut:userData:error:")]
    public void WitchyShortcut(NSPasteboard pboard, NSString userData, out NSString? error)
    {
        error = null;
        Interlocked.Increment(ref Program.totalServices);
        Interlocked.Increment(ref Program.runningServices);
        if (pboard.CanReadObjectForClasses(new[] { GetClass(typeof(NSUrl)) }, null))
        {
            var urls = pboard.ReadObjectsForClasses(new[] { GetClass(typeof(NSUrl)) }, null).OfType<NSUrl>();
            SelectedItemPaths = urls.Where(u => u.IsFileUrl || u.HasDirectoryPath).Select(u => u.Path).ToArray()!;
            if (urls.Any(u => u.HasDirectoryPath))
            {
                if (SelectedItemPaths.Count() != 1)
                    error = new NSString("Error: only one directory may be parsed at a time");
                else if (!Directory.Exists(SelectedItemPaths.First()))
                    error = new NSString("Error: directory does not exist");
                else if (Directory.GetFiles(SelectedItemPaths.First(), "_witchy*.xml", SearchOption.TopDirectoryOnly).Length <= 0)
                    error = new NSString("Error: directory must contain a witchy xml file for repacking");
            }
        }
        else
            error = new NSString("Error: cannot read passed object(s)");

        pboard.ClearContents();

        if (error != null)
        {
            new NSAlert() { MessageText = error }.RunModal();
            Interlocked.Decrement(ref Program.runningServices);
            return;
        }

        Program.Relaunch(SelectedItemPaths);
    }

    [Export("witchyMenu:userData:error:")]
    public void WitchyMenu(NSPasteboard pboard, NSString userData, out NSString? error)
    {
        error = null;
        Interlocked.Increment(ref Program.totalServices);
        Interlocked.Increment(ref Program.runningServices);
        if (pboard.CanReadObjectForClasses(new[] { GetClass(typeof(NSUrl)) }, null))
        {
            var urls = pboard.ReadObjectsForClasses(new[] { GetClass(typeof(NSUrl)) }, null).OfType<NSUrl>();
            SelectedItemPaths = urls.Where(u => u.IsFileUrl || u.HasDirectoryPath).Select(u => u.Path).ToArray()!;
            if (urls.Any(u => u.HasDirectoryPath))
            {
                if (SelectedItemPaths.Count() != 1)
                    error = new NSString("Error: only one directory may be parsed at a time");
                else if (!Directory.Exists(SelectedItemPaths.First()))
                    error = new NSString("Error: directory does not exist");
                else if (Directory.GetFiles(SelectedItemPaths.First(), "_witchy*.xml", SearchOption.TopDirectoryOnly).Length <= 0)
                    error = new NSString("Error: directory must contain a witchy xml file for repacking");
            }
        }
        else
            error = new NSString("Error: cannot read passed object(s)");

        pboard.ClearContents();

        if (error != null)
        {
            new NSAlert() { MessageText = error }.RunModal();
            Interlocked.Decrement(ref Program.runningServices);
            return;
        }

        NSMenu witchyMenu = new NSMenu() { AutoEnablesItems = false };

        bool bnd = SelectedItemPaths.Any(p => p.Contains(".matbinbnd") || p.Contains(".mtdbnd") || p.Contains(".ffxbnd") || p.Contains(".anibnd"));
        bool dcx = SelectedItemPaths.Any(p => p.Contains(".dcx"));

        NSMenuItem processMenuItem = new NSMenuItem("Process here", new Selector(nameof(processMenuItemOnClick)), "") { Target = this };
        witchyMenu.AddItem(processMenuItem);

        NSMenuItem processRecursiveMenuItem = new NSMenuItem("Process here (Recursive)", new Selector(nameof(processRecursiveMenuItemOnClick)), "") { Target = this };
        witchyMenu.AddItem(processRecursiveMenuItem);

        if (dcx)
        {
            NSMenuItem processDcxMenuItem = new NSMenuItem("Process here (DCX compression)", new Selector(nameof(processDcxMenuItemOnClick)), "") { Target = this };
            witchyMenu.AddItem(processDcxMenuItem);
        }

        NSMenu processMoreMenu = new NSMenu() { AutoEnablesItems = false };
        NSMenuItem processMoreMenuItem = new NSMenuItem("Process...", null, "") { Submenu = processMoreMenu };
        witchyMenu.AddItem(processMoreMenuItem);

        if (bnd)
        {
            NSMenuItem processBndMenuItem = new NSMenuItem("Process here (Standard BND)", new Selector(nameof(processBndMenuItemOnClick)), "") { Target = this };
            processMoreMenu.AddItem(processBndMenuItem);
        }

        NSMenuItem processToMenuItem = new NSMenuItem("Process to...", new Selector(nameof(processToMenuItemOnClick)), "") { Target = this };
        processMoreMenu.AddItem(processToMenuItem);

        if (bnd)
        {
            NSMenuItem processBndToMenuItem = new NSMenuItem("Process to... (Standard BND)", new Selector(nameof(processBndToMenuItemOnClick)), "") { Target = this };
            processMoreMenu.AddItem(processBndToMenuItem);
        }

        NSMenuItem processRecursiveToMenuItem = new NSMenuItem("Process to... (Recursive)", new Selector(nameof(processRecursiveToMenuItemOnClick)), "") { Target = this };
        processMoreMenu.AddItem(processRecursiveToMenuItem);

        if (dcx)
        {
            NSMenuItem processDcxToMenuItem = new NSMenuItem("Process to... (DCX compression)", new Selector(nameof(processDcxToMenuItemOnClick)), "") { Target = this };
            processMoreMenu.AddItem(processDcxToMenuItem);
        }

        witchyMenu.AddItem(NSMenuItem.SeparatorItem);

        NSMenuItem watchMenuItem = new NSMenuItem("Watch for changes", new Selector(nameof(watchMenuItemOnClick)), "") { Target = this };
        witchyMenu.AddItem(watchMenuItem);


        NSMenuItem watchRecursiveMenuItem = new NSMenuItem("Watch for changes (Recursive)", new Selector(nameof(watchRecursiveMenuItemOnClick)), "") { Target = this };
        witchyMenu.AddItem(watchRecursiveMenuItem);

        witchyMenu.AddItem(NSMenuItem.SeparatorItem);

        NSMenuItem configMenuItem = new NSMenuItem("Configure WitchyBND", new Selector(nameof(configMenuItemOnClick)), "") { Target = this };
        witchyMenu.AddItem(configMenuItem);

        var mouseLocation = NSEvent.CurrentMouseLocation;
        if (!witchyMenu.PopUpMenu(null, mouseLocation, null))
            Interlocked.Decrement(ref Program.runningServices);
    }

    [Export(nameof(watchRecursiveMenuItemOnClick))]
    private void watchRecursiveMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--mode", "Watch", "--recursive" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(watchMenuItemOnClick))]
    private void watchMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--mode", "Watch" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(configMenuItemOnClick))]
    private void configMenuItemOnClick()
    {
        Program.Relaunch(Array.Empty<string>());
    }

    [Export(nameof(processMenuItemOnClick))]
    private void processMenuItemOnClick()
    {
        Program.Relaunch(SelectedItemPaths);
    }

    [Export(nameof(processToMenuItemOnClick))]
    private void processToMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--location", "prompt" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processDcxMenuItemOnClick))]
    private void processDcxMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--dcx" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processDcxToMenuItemOnClick))]
    private void processDcxToMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--location", "prompt", "--dcx" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processBndMenuItemOnClick))]
    private void processBndMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--bnd" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processBndToMenuItemOnClick))]
    private void processBndToMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--location", "prompt", "--bnd" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processRecursiveMenuItemOnClick))]
    private void processRecursiveMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--recursive" }.Concat(SelectedItemPaths).ToArray());
    }

    [Export(nameof(processRecursiveToMenuItemOnClick))]
    private void processRecursiveToMenuItemOnClick()
    {
        Program.Relaunch(new[] { "--location", "prompt", "--recursive" }.Concat(SelectedItemPaths).ToArray());
    }
}
#endif