﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.FormHelpers
{
    public class GeneralToolStripMenuItem
    {
        public GeneralToolStripMenuItem() { }

        public GeneralToolStripMenuItem(string text) { Text = text; }

        public string Text { get; internal set; }
        public EventHandler Click { get; set; }
        public string ShortcutKeyDisplayString { get; internal set; }

        public List<GeneralToolStripMenuItem> DropDownItems { get; private set; } = new List<GeneralToolStripMenuItem>();

        public ToolStripMenuItem ToTsmi()
        {
            var tsmi = new ToolStripMenuItem(Text);
            tsmi.Click += Click;
            tsmi.ShortcutKeyDisplayString = ShortcutKeyDisplayString;

            foreach(var dropdownItem in DropDownItems)
            {
                tsmi.DropDownItems.Add(dropdownItem.ToTsmi());
            }

            return tsmi;
        }
    }

    public static class GeneralToolStripMenuItemExtensions
    {
        public static void Add(this List<GeneralToolStripMenuItem> items, string text, EventHandler click)
        {
            var item = new GeneralToolStripMenuItem();
            item.Text = text;
            item.Click += click;
            items.Add(item);
        }
    }
}
