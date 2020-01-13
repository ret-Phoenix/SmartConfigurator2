using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Hotkeys;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace HotkeyWin
{

    public partial class Form1 : Form
    {
        private List<Hotkeys.GlobalHotkey> CurrentShortKeys;
        private List<string> SupportedIDENames;

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        WinEventDelegate dele = null;
        public Form1()
        {
            InitializeComponent();

            SupportedIDEList();
            CurrentShortKeys = new List<GlobalHotkey>();

            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void SupportedIDEList()
        {
            SupportedIDENames = new List<string>();

            var files = Directory.GetFiles(@"core/ide", "*.os");

            foreach (var fileName in files)
            {
                string IDEName = Path.GetFileNameWithoutExtension(fileName);
                SupportedIDENames.Add(IDEName);
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            string currentAppName = ModuleName();
            if (SupportedIDENames.Contains(currentAppName))
            {
                RegisterShortKeys();
            }
            else
            {
                UnregisterShortKeys();
            }
        }


        private void HandleHotkey()
        {
            WriteLine("Hotkey pressed!");
        }

        private void RunScriptAction(int id)
        {
            foreach (var CurSK in CurrentShortKeys)
            {
                if (CurSK.ID() == id)
                {
                    ScriptCore.GetInstance().RunAction(CurSK.Action());
                    break;
                }
            }
        }

        private string ModuleName()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint procId = 0;
            GetWindowThreadProcessId(hWnd, out procId);
            if (procId != 0)
            {
                //FIXME: Временная заплатка
                try
                {
                    var proc = Process.GetProcessById((int)procId);
                    return proc.MainModule.ModuleName;
                }
                catch (Exception)
                {
                    return "";
                }
            }

            return "";
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if ((m.Msg == Hotkeys.Constants.WM_HOTKEY_MSG_ID))
            {
                const int KEYEVENTF_KEYUP = 0x2;

                keybd_event(0x10, 0, KEYEVENTF_KEYUP, 0); // ShiftKey
                keybd_event(0x11, 0, KEYEVENTF_KEYUP, 0); // ControlKey
                keybd_event(0x12, 0, KEYEVENTF_KEYUP, 0); // Menu 
                keybd_event(0x5c, 0, KEYEVENTF_KEYUP, 0); // RWin
                keybd_event(0x5B, 0, KEYEVENTF_KEYUP, 0); // LWin

                RunScriptAction(m.WParam.ToInt32());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnRefresh_Click(sender, e);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterShortKeys();
        }

        private void WriteLine(string text)
        {
            Console.WriteLine(text);

            StreamWriter myfile = new StreamWriter("smartconf.log", true);
            myfile.WriteLine("SmartConfigurator2: " + text);
            myfile.Close();
        }

        private void UnregisterShortKeys()
        {
            string currentAppName = ModuleName();

            foreach (Hotkeys.GlobalHotkey CurSK in CurrentShortKeys)
            {
                CurSK.Unregiser();
            }
        }

        private void RegisterShortKeys()
        {
            string currentAppName = ModuleName().ToLower();

            foreach (var CurSK in CurrentShortKeys)
            {
                if (CurSK.App().Contains(currentAppName))
                {
                    CurSK.Register();
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ScriptCore.GetInstance().RunAction(@"hotkeys::add");
            btnRefresh_Click(null, null);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (listViewHotkeys.SelectedItems.Count == 0)
            {
                return;
            }

            ScriptCore.GetInstance().RunAction(@"hotkeys::edit::" + listViewHotkeys.SelectedItems[0].Tag);
            btnRefresh_Click(null, null);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listViewHotkeys.SelectedItems.Count == 0)
            {
                return;
            }

            ScriptCore.GetInstance().RunAction(@"hotkeys::delete::" + listViewHotkeys.SelectedItems[0].Tag);
            btnRefresh_Click(null, null);
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            if (listViewHotkeys.SelectedItems.Count == 0)
            {
                return;
            }

            ScriptCore.GetInstance().RunAction(@"hotkeys::moveUp::" + listViewHotkeys.SelectedItems[0].Tag);
            btnRefresh_Click(null, null);
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            if (listViewHotkeys.SelectedItems.Count == 0)
            {
                return;
            }

            ScriptCore.GetInstance().RunAction(@"hotkeys::moveDown::" + listViewHotkeys.SelectedItems[0].Tag);
            btnRefresh_Click(null, null);
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            CurrentShortKeys.Clear();

            string strHotkeys = "";
            FileStream sss = File.Open(@"settings/hotkeys.json", FileMode.Open, FileAccess.Read);
            if (sss != null)
            {
                var reader = new StreamReader(sss, System.Text.Encoding.GetEncoding("UTF-8"));
                strHotkeys = reader.ReadToEnd();
                sss.Close();

            }

            var shortKeys = JsonConvert.DeserializeObject<List<SmartConfigurator.ShortKey>>(strHotkeys);

            listViewHotkeys.BeginUpdate();
            listViewHotkeys.Items.Clear();
            foreach (var sk in shortKeys)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = sk.Command;
                lvi.Tag = sk.Id;
                lvi.SubItems.Add(ShortkeyPresentation(sk));
                lvi.SubItems.Add(sk.App);

                listViewHotkeys.Items.Add(lvi);

                int modiffs = 0;
                if (sk.Ctrl) { modiffs += Constants.CTRL; }
                if (sk.Alt) { modiffs += Constants.ALT; }
                if (sk.Shift) { modiffs += Constants.SHIFT; }
                if (sk.Win) { modiffs += Constants.WIN; }

                var NewSK = new Hotkeys.GlobalHotkey(modiffs,
                    ((Keys)TypeDescriptor.GetConverter(typeof(Keys)).ConvertFromString(sk.Key)),
                    this,
                    listViewHotkeys.Items.Count,
                    sk.Command,
                    sk.App);

                CurrentShortKeys.Add(NewSK);

            }
            listViewHotkeys.EndUpdate();
        }

        private string ShortkeyPresentation(SmartConfigurator.ShortKey sk)
        {
            string result = "";

            if (sk.Ctrl) { result += "Ctrl +"; }
            if (sk.Alt) { result += "Alt +"; }
            if (sk.Shift) { result += "Shift +"; }
            if (sk.Win) { result += "Win +"; }
            result += ((Keys)TypeDescriptor.GetConverter(typeof(Keys)).ConvertFromString(sk.Key)).ToString();

            return result;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }


        private void menuItemShowSettings_Click(object sender, EventArgs e)
        {
            this.Show();
            this.BringToFront();
            this.WindowState = FormWindowState.Normal;
        }

        private void menuItemCloseApp_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void trayMenu_Click(object sender, EventArgs e)
        {
            this.Show();
            this.BringToFront();
            this.WindowState = FormWindowState.Normal;
        }
    }
}
