using Client;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using x;
using static AotForms.ESP;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace AotForms
{


    public partial class Form1 : Form
    {
        private bool isStreamModeActive = false;
        public static bool Streaming;
        IntPtr mainHandle;
        public Form1(IntPtr handle)
        {
            InitializeComponent();
            mainHandle = handle;
        }

        #region P/Invoke và Hằng số cho Stream Mode & Hotkey

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName); //short => IntPtr sorry my mistake


        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity); //short => bool

        enum WDA
        {
            WDA_NONE = 0x00000000,
            WDA_MONITOR = 0x00000001,
            WDA_EXCLUDEFROMCAPTURE = 0x00000011,
        }

        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]

        public static extern uint SetWindowDisPlayAffinity(IntPtr hWnd, uint dwReserved);

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 1;

        #endregion
        private const string FileUrl = "http://103.157.205.156:5050/esp";
        private async void loginBtn_Click(object sender, EventArgs e)
        {

        }

        ImFontPtr smallFont;
        ImFontPtr bigFont;
        private async void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            KillProcess("HD-Adb");
            await Task.Delay(2000);
            KillProcess("HD-Player");
            await Task.Delay(1000);
            Environment.Exit(0);
        }
        public void KillProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }
        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
        static IntPtr FindRenderWindow(IntPtr parent)
        {
            IntPtr renderWindow = IntPtr.Zero;
            WinAPI.EnumChildWindows(parent, (hWnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                WinAPI.GetWindowText(hWnd, sb, sb.Capacity);
                string windowName = sb.ToString();
                if (!string.IsNullOrEmpty(windowName))
                {
                    if (windowName != "HD-Player")
                    {
                        renderWindow = hWnd;
                    }
                }
                return true;
            }, IntPtr.Zero);

            return renderWindow;
        }

        static IntPtr FindRenderWindow1(IntPtr parent)
        {
            IntPtr renderWindow = IntPtr.Zero;
            WinAPI.EnumChildWindows(parent, (hWnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                WinAPI.GetWindowText(hWnd, sb, sb.Capacity);
                string windowName = sb.ToString();
                if (!string.IsNullOrEmpty(windowName))
                {
                    if (windowName == "BlueStacks Android PluginAndroid")
                        renderWindow = hWnd;

                }
                return true;
            }, IntPtr.Zero);

            return renderWindow;
        }
        private async void FormAh_Load(object sender, EventArgs e)
        {


        }

        private void connectbtn_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            string downloadFolder = @"C:\Extracted";
            string zipFileName = "ESPWeapon.zip";

            string zipFilePath = Path.Combine(downloadFolder, zipFileName);
            string extractFolderName = Path.GetFileNameWithoutExtension(zipFilePath);
            string extractPath = Path.Combine(downloadFolder, extractFolderName);
            try
            {
                if (Directory.Exists(extractPath))
                {
                    Console.WriteLine($"Thư mục '{extractFolderName}' đã tồn tại. Không cần làm gì thêm.");
                }
                else
                {
                    Directory.CreateDirectory(downloadFolder);

                    if (System.IO.File.Exists(zipFilePath))
                    {
                        Console.WriteLine($"File '{zipFileName}' đã tồn tại. Bỏ qua bước tải về.");
                    }
                    else
                    {
                        await DownloadFileAsync(FileUrl, zipFilePath);
                    }

                    ExtractZipFile(zipFilePath, extractPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nĐã có lỗi xảy ra: {ex.Message}");
            }


            ImGui.CreateContext();
            ImGuiIOPtr io = ImGui.GetIO();

            byte[] verdanaFontData;
            using (Stream fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Client.Fonts.verdana.ttf"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fontStream.CopyTo(ms);
                    verdanaFontData = ms.ToArray();
                }
            }

            byte[] interFontData;
            using (Stream fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Client.Fonts.Inter.SemiBold.ttf"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fontStream.CopyTo(ms);
                    interFontData = ms.ToArray();
                }
            }

            unsafe
            {
                fixed (byte* verdanaPtr = verdanaFontData)
                fixed (byte* interPtr = interFontData)
                {
                    FontManager.VerdanaSmall = io.Fonts.AddFontFromMemoryTTF(new IntPtr(verdanaPtr), verdanaFontData.Length, 13f);
                    FontManager.VerdanaNormal = io.Fonts.AddFontFromMemoryTTF(new IntPtr(verdanaPtr), verdanaFontData.Length, 15f);
                    FontManager.VerdanaBig = io.Fonts.AddFontFromMemoryTTF(new IntPtr(verdanaPtr), verdanaFontData.Length, 16f);

                    FontManager.InterSmall = io.Fonts.AddFontFromMemoryTTF(new IntPtr(interPtr), interFontData.Length, 13f);
                    FontManager.InterNormal = io.Fonts.AddFontFromMemoryTTF(new IntPtr(interPtr), interFontData.Length, 15f);
                    FontManager.InterBig = io.Fonts.AddFontFromMemoryTTF(new IntPtr(interPtr), interFontData.Length, 16f);
                }
            }

            io.Fonts.Build();
        }
        private static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using (var httpClient = new HttpClient())
            {
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await System.IO.File.WriteAllBytesAsync(destinationPath, fileBytes);
            }
        }

        private static void ExtractZipFile(string zipPath, string destinationPath)
        {
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            ZipFile.ExtractToDirectory(zipPath, destinationPath);
        }

        private async void guna2Button1_Click_1(object sender, EventArgs e)
        {
            var esp = new ESP();
            await esp.Start();

            new Thread(Data.Work) { IsBackground = true }.Start();
            new Thread(AimbotZexHybrid.Work) { IsBackground = true }.Start();
            new Thread(PullPlayer.Work) { IsBackground = true }.Start();
            new Thread(Teleport.Work) { IsBackground = true }.Start();
            new Thread(TeleportV3.Work) { IsBackground = true }.Start();
            new Thread(FixRenderUI.Work) { IsBackground = true }.Start();

            this.Hide();
        }
        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void guna2Button2_Click(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("HD-Player");

            if (processes.Length != 1)
            {
                MessageBox.Show($"Không tìm thấy {processes}");
                return;
            }

            var process = processes[0];
            var mainModulePath = Path.GetDirectoryName(process.MainModule.FileName);
            var adbPath = Path.Combine(mainModulePath, "HD-Adb.exe");

            if (!System.IO.File.Exists(adbPath))
            {
                MessageBox.Show($"Khởi động lại {processes}");
                return;
            }

            string adbPort = guna2TextBox3.Text;

            if (string.IsNullOrEmpty(adbPort))
            {
                MessageBox.Show("Port ADB không được để trống.");
                return;
            }

            var adb = new Adb(adbPath, adbPort);
            await adb.Kill();

            var started = await adb.Start();
            if (!started)
            {
                MessageBox.Show("Có lỗi ở ADB");
                Application.Exit();
                return;
            }

            var moduleAddr = await adb.FindModule("com.dts.freefireth", "libil2cpp.so");
            Offsets.Il2Cpp = (uint)moduleAddr;

            Core.Handle = FindRenderWindow(mainHandle);


            //this.guna2Button2.Text = "Connect ADB: " + moduleAddr.ToString("X");
            //this.guna2Button1.Enabled = true;

            var esp = new ESP();
            await esp.Start();

            new Thread(Data.Work) { IsBackground = true }.Start();
            new Thread(PullPlayer.Work) { IsBackground = true }.Start();
            new Thread(TeleportV2.Work) { IsBackground = true }.Start();
            new Thread(FOV.Work) { IsBackground = true }.Start();
            new Thread(AimbotV2.Work) { IsBackground = true }.Start();
            new Thread(AimbotNew.Work) { IsBackground = true }.Start();
            new Thread(AimbotMouseZex.Work) { IsBackground = true }.Start();


            RegisterHotKey(this.Handle, HOTKEY_ID, 0, (uint)Keys.F8);

            this.Hide();
        }
        private string adbPort = "5555";
        private async void guna2Button3_Click(object sender, EventArgs e)
        {

        }
        static int GetProcessWithLeastMemoryUsage(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                throw new Exception("No processes found with the name: " + processName);
            }
            var leastMemoryProcess = processes.OrderBy(p => p.WorkingSet64).First();
            return leastMemoryProcess.Id;
        }
        private void guna2Separator1_Click(object sender, EventArgs e)
        {

        }

        private async void guna2Button3_Click_1(object sender, EventArgs e)
        {
            guna2Button3.Enabled = false;
            string userKey = guna2TextBox1.Text;

            if (string.IsNullOrWhiteSpace(userKey))
            {
                MessageBox.Show("Vui lòng nhập key.");
                guna2Button3.Enabled = true;
                return;
            }

            AuthResponse result = await AuthHandler.ValidateKeyAsync(userKey);
            if (result.success)
            {

                AuthHandler.ExpirationDate = result.expires;
                this.Size = new Size(283, 160);

            }
            else
            {
                MessageBox.Show("Key không hợp lệ hoặc đã hết hạn. Vui lòng kiểm tra lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                guna2Button3.Enabled = true;
            }
        }

        private void guna2TextBox3_TextChanged(object sender, EventArgs e)
        {
            string newPort = guna2TextBox3.Text;
            if (int.TryParse(newPort, out int portNumber))
            {

                if (portNumber >= 0 && portNumber <= 65535)
                {
                    adbPort = newPort;
                }
                else
                {
                    MessageBox.Show("Cổng ADB phải nằm trong khoảng từ 0 đến 65535.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng nhập một số hợp lệ cho cổng ADB.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2CustomCheckBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleStreamMode();
            }
            base.WndProc(ref m);
        }
        private void ToggleStreamMode()
        {
            isStreamModeActive = !isStreamModeActive;

            string overlay = "Overlay";
            IntPtr OverlayHwnd = FindWindow(null, overlay);

            if (isStreamModeActive)
            {
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);
                SetWindowDisplayAffinity(OverlayHwnd, (uint)WDA.WDA_EXCLUDEFROMCAPTURE);
            }
            else
            {
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TOOLWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                SetWindowDisplayAffinity(this.Handle, 0);
                SetWindowDisplayAffinity(OverlayHwnd, (uint)WDA.WDA_NONE);
            }
        }
    }
}
