using System;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using Microsoft.Win32;

static class LinkGuard
{
    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int pid);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    const int ASFW_ANY = -1;

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0) return;
        string url = args[0];

        if (!FromOutlook()) { OpenInBrowser(url); return; }

        Application.EnableVisualStyles();
        string target = Unwrap(url);
        Confirm(url, target);
    }

    static bool FromOutlook()
    {
        try
        {
            int pid = Process.GetCurrentProcess().Id;
            using (var s = new ManagementObjectSearcher("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId=" + pid))
            foreach (ManagementObject o in s.Get())
            {
                var parent = Process.GetProcessById(Convert.ToInt32(o["ParentProcessId"]));
                string n = parent.ProcessName.ToLowerInvariant();
                return n == "outlook" || n == "olk";
            }
        }
        catch { }
        return true; // emin olamazsak sor
    }

    // Safe Links / benzeri sarmalayıcıların içindeki gerçek hedefi çıkar
    static string Unwrap(string url)
    {
        try
        {
            var u = new Uri(url);
            if (u.Host.EndsWith("safelinks.protection.outlook.com", StringComparison.OrdinalIgnoreCase))
            {
                string inner = HttpUtility.ParseQueryString(u.Query)["url"];
                if (!string.IsNullOrEmpty(inner)) return inner;
            }
        }
        catch { }
        return url;
    }

    static string Warnings(string url)
    {
        var w = new System.Collections.Generic.List<string>();
        Uri u;
        if (!Uri.TryCreate(url, UriKind.Absolute, out u)) return "⚠ Adres çözümlenemedi.";
        if (u.Scheme == "http") w.Add("⚠ Şifresiz bağlantı (https değil).");
        if (u.Host.Contains("xn--") || Regex.IsMatch(u.Host, @"[^\x00-\x7F]")) w.Add("⚠ Alan adında Latin dışı/benzer görünen karakterler var (punycode).");
        if (Regex.IsMatch(u.Host, @"^\d{1,3}(\.\d{1,3}){3}$")) w.Add("⚠ Alan adı yerine çıplak IP adresi.");
        if (!string.IsNullOrEmpty(u.UserInfo)) w.Add("⚠ Adreste '@' öncesi kullanıcı bilgisi var — gerçek site '@' sonrasıdır.");
        return string.Join(Environment.NewLine, w);
    }

    static DialogResult Confirm(string original, string target)
    {
        string host = "?";
        Uri u;
        if (Uri.TryCreate(target, UriKind.Absolute, out u)) host = u.IdnHost;

        var f = new Form
        {
            Text = "Bağlantıyı açmak istediğinize emin misiniz?",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false, MaximizeBox = false, TopMost = true,
            ClientSize = new Size(640, 330), Font = new Font("Segoe UI", 9.5f)
        };

        var lbl = new Label { Text = "Gidilecek site:", Location = new Point(16, 14), AutoSize = true };
        var hostLbl = new Label
        {
            Text = host, Location = new Point(16, 36), Size = new Size(608, 30),
            Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Color.DarkBlue
        };
        var lbl2 = new Label { Text = "Tam adres:", Location = new Point(16, 74), AutoSize = true };
        string full = target;
        if (target != original) full += Environment.NewLine + Environment.NewLine + "(Microsoft Safe Links üzerinden açılacak)";
        var box = new TextBox
        {
            Text = full, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Location = new Point(16, 94), Size = new Size(608, 110), Font = new Font("Consolas", 9.5f)
        };
        string warn = Warnings(target);
        var warnLbl = new Label
        {
            Text = warn, Location = new Point(16, 212), Size = new Size(608, 64),
            ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        if (warn.Length > 0) hostLbl.ForeColor = Color.Firebrick;

        var ok = new Button { Text = "Aç", Location = new Point(444, 286), Size = new Size(85, 30) };
        // Tarayıcıyı pencere hâlâ öndeyken başlat; pencereyi tarayıcı öne geçene kadar
        // kapatma, yoksa Windows odağı Outlook'a geri verir ve tarayıcı arkada kalır.
        ok.Click += (s, e) =>
        {
            ok.Enabled = false;
            OpenInBrowser(original);
            DateTime until = DateTime.Now.AddSeconds(3);
            var t = new Timer { Interval = 100 };
            t.Tick += (s2, e2) =>
            {
                if (GetForegroundWindow() != f.Handle || DateTime.Now > until)
                {
                    t.Stop();
                    f.DialogResult = DialogResult.OK;
                }
            };
            t.Start();
        };
        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(539, 286), Size = new Size(85, 30) };
        var copy = new Button { Text = "Adresi kopyala", Location = new Point(16, 286), Size = new Size(120, 30) };
        copy.Click += (s, e) => Clipboard.SetText(target);

        f.Controls.AddRange(new Control[] { lbl, hostLbl, lbl2, box, warnLbl, ok, cancel, copy });
        f.AcceptButton = cancel;   // Enter'a yanlışlıkla basmak açmasın
        f.CancelButton = cancel;
        f.Shown += (s, e) => { cancel.Focus(); f.Activate(); };
        return f.ShowDialog();
    }

    static void OpenInBrowser(string url)
    {
        // Chrome zaten açıksa yeni süreç URL'yi mevcut sürece devreder; ona da öne gelme izni ver
        AllowSetForegroundWindow(ASFW_ANY);
        string cmd = null;
        using (var k = Registry.ClassesRoot.OpenSubKey(@"ChromeHTML\shell\open\command"))
            if (k != null) cmd = k.GetValue("") as string;

        if (cmd != null)
        {
            var m = Regex.Match(cmd, "^\"([^\"]+)\"\\s*(.*)$");
            string exe = m.Groups[1].Value;
            string rest = m.Groups[2].Value.Replace("%1", url);
            Process.Start(exe, rest);
        }
        else
        {
            Process.Start("msedge.exe", "\"" + url + "\"");
        }
    }
}
