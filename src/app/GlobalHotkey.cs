using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Hotkeys
{
    public class GlobalHotkey
    {
        private int modifier;
        private int key;
        private IntPtr hWnd;
        private int id;
        private string command;
        private string app = "";

        public GlobalHotkey(int modifier, Keys key, Form form, int MessId, string cmd, string app)
        {
            this.modifier = modifier;
            this.key = (int)key;
            this.hWnd = form.Handle;
            this.command = cmd;
            this.app = app == null ? "" : app;

            id = this.GetHashCode();
        }

        public bool Register()
        {
            return RegisterHotKey(hWnd, id, modifier, key);
        }

        public bool Unregiser()
        {
            return UnregisterHotKey(hWnd, id);
        }

        public override int GetHashCode()
        {
            return modifier ^ key ^ hWnd.ToInt32();
        }

        public int ID()
        {
            return id;
        }

        public string Action()
        {
            return command;
        }

        public string App()
        {
            return app;
        }


        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
