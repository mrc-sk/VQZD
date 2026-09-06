using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VQZD.PluginPacker
{
    /// <summary>
    /// PluginPacker 主窗体：拖入 DLL → 自动读取信息 → 填写清单 → 打包 .vqzcmod。
    /// </summary>
    public sealed class MainForm : Form
    {
        private TextBox _txtDll;
        private Button _btnBrowseDll;
        private Label _lblAsmInfo;
        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtVersion;
        private TextBox _txtAuthor;
        private TextBox _txtDesc;
        private ComboBox _cmbType;
        private ComboBox _cmbClass;
        private TextBox _txtMethod;
        private TextBox _txtPermissions;
        private TextBox _txtOut;
        private Button _btnBrowseOut;
        private Button _btnPack;
        private Label _lblLog;
        private string _dllPath = "";

        public MainForm()
        {
            Text = "VQZ 插件打包工具 (PluginPacker)";
            ClientSize = new Size(640, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 245, 250);
            Font = new Font("微软雅黑", 9F);
            AllowDrop = true;
            DragEnter += (_, e) => { if (e.Data!.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += OnDllDropped;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var y = 14;
            var lblW = 90;
            var fieldW = 420;

            // DLL 选择
            Controls.Add(MakeLabel("插件 DLL：", 14, y + 4));
            _txtDll = new TextBox { Location = new Point(14 + lblW, y), Width = fieldW - 80, ReadOnly = true };
            _txtDll.TextChanged += (_, _) => OnDllChanged(_txtDll.Text);
            _btnBrowseDll = new Button { Text = "浏览…", Location = new Point(14 + lblW + fieldW - 76, y - 1), Size = new Size(70, 26) };
            _btnBrowseDll.Click += (_, _) => BrowseDll();
            Controls.Add(_txtDll); Controls.Add(_btnBrowseDll);
            y += 34;

            _lblAsmInfo = new Label
            {
                Location = new Point(14 + lblW, y), AutoSize = true,
                ForeColor = Color.DimGray, Text = "拖入 DLL 或点击浏览（支持命令行：PluginPacker <dll> <out.vqzcmod> [--id ..] [--name ..] [--type ..] [--class ..] [--method ..] [--permission a,b]）"
            };
            Controls.Add(_lblAsmInfo);
            y += 30;

            // 表单字段
            _txtId = MakeField(y, "插件 ID：", "com.vqzd.plugin.myplugin"); y += 30;
            _txtName = MakeField(y, "插件名称：", "我的插件"); y += 30;
            _txtVersion = MakeField(y, "版本：", "1.0.0"); y += 30;
            _txtAuthor = MakeField(y, "作者：", ""); y += 30;
            _txtDesc = MakeField(y, "描述：", ""); y += 30;

            // 类型
            Controls.Add(MakeLabel("插件类型：", 14, y + 4));
            _cmbType = new ComboBox { Location = new Point(14 + lblW, y), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType.Items.AddRange(new object[] { "ui（界面扩展）", "data（数据处理）", "network（网络协议）", "system（系统底层-高风险）", "other（其他）" });
            _cmbType.SelectedIndex = 4;
            Controls.Add(_cmbType);
            y += 30;

            // 入口类（可下拉，可手输）
            Controls.Add(MakeLabel("入口类：", 14, y + 4));
            _cmbClass = new ComboBox { Location = new Point(14 + lblW, y), Width = fieldW };
            _cmbClass.Text = "";
            Controls.Add(_cmbClass);
            y += 30;

            _txtMethod = MakeField(y, "入口方法：", "（留空自动 Run/OnLoad/Execute）"); y += 30;
            _txtPermissions = MakeField(y, "权限声明：", "多个用英文逗号分隔，如 filesystem,network"); y += 30;

            // 输出
            Controls.Add(MakeLabel("输出文件：", 14, y + 4));
            _txtOut = new TextBox { Location = new Point(14 + lblW, y), Width = fieldW - 80 };
            _btnBrowseOut = new Button { Text = "浏览…", Location = new Point(14 + lblW + fieldW - 76, y - 1), Size = new Size(70, 26) };
            _btnBrowseOut.Click += (_, _) => BrowseOut();
            Controls.Add(_txtOut); Controls.Add(_btnBrowseOut);
            y += 40;

            // 打包按钮
            _btnPack = new Button
            {
                Text = "打 包 .vqzcmod", Location = new Point(14 + lblW, y),
                Size = new Size(180, 40), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnPack.FlatAppearance.BorderSize = 0;
            _btnPack.Click += (_, _) => Pack();
            Controls.Add(_btnPack);
            y += 52;

            // 日志
            Controls.Add(MakeLabel("打包记录：", 14, y));
            y += 22;
            _lblLog = new Label
            {
                Location = new Point(14, y), Size = new Size(ClientSize.Width - 28, ClientSize.Height - y - 14),
                ForeColor = Color.FromArgb(0, 120, 60), AutoSize = false,
                Text = "就绪。"
            };
            Controls.Add(_lblLog);
        }

        private Label MakeLabel(string text, int x, int y) =>
            new() { Text = text, Location = new Point(x, y), AutoSize = true };

        private TextBox MakeField(int y, string caption, string hint)
        {
            Controls.Add(MakeLabel(caption, 14, y + 4));
            var tb = new TextBox { Location = new Point(104, y), Width = 420 };
            tb.TextChanged += (_, _) => tb.Tag = tb.Text; // 记录手输
            Controls.Add(tb);
            return tb;
        }

        private void BrowseDll()
        {
            using var ofd = new OpenFileDialog { Filter = "程序集 (*.dll)|*.dll" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDll.Text = ofd.FileName;
                OnDllChanged(ofd.FileName);
            }
        }

        private void BrowseOut()
        {
            using var sfd = new SaveFileDialog { Filter = "VQZ 插件 (*.vqzcmod)|*.vqzcmod", FileName = SuggestOutName() };
            if (sfd.ShowDialog(this) == DialogResult.OK) _txtOut.Text = sfd.FileName;
        }

        private string SuggestOutName()
        {
            var baseName = string.IsNullOrWhiteSpace(_txtName.Text) ? "plugin" : _txtName.Text.Trim();
            var ver = string.IsNullOrWhiteSpace(_txtVersion.Text) ? "" : "_" + _txtVersion.Text.Trim();
            return Path.Combine(
                string.IsNullOrWhiteSpace(_txtDll.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) : Path.GetDirectoryName(_txtDll.Text) ?? "",
                baseName + ver + ".vqzcmod");
        }

        private void OnDllDropped(object? sender, DragEventArgs e)
        {
            if (e.Data!.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    _txtDll.Text = files[0];
                    OnDllChanged(files[0]);
                }
            }
        }

        private void OnDllChanged(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            _dllPath = path;
            var info = AsmInfo.Read(path);
            if (info.Name != null && string.IsNullOrWhiteSpace(_txtName.Text)) _txtName.Text = info.Name;
            if (info.Version != null && string.IsNullOrWhiteSpace(_txtVersion.Text)) _txtVersion.Text = info.Version;
            if (info.Company != null && string.IsNullOrWhiteSpace(_txtAuthor.Text)) _txtAuthor.Text = info.Company;            if (info.Description != null && string.IsNullOrWhiteSpace(_txtDesc.Text)) _txtDesc.Text = info.Description;
            if (string.IsNullOrWhiteSpace(_txtId.Text)) _txtId.Text = "com.vqzd.plugin." + info.Name?.ToLowerInvariant().Replace(" ", "");

            _lblAsmInfo.Text = $"已读取：{info.Name} v{info.Version}";
            if (info.MainType != null)
            {
                _cmbClass.Text = info.MainType;
                _lblAsmInfo.Text += $"（建议入口类 {info.MainType}）";
            }

            // 填充入口类候选
            var types = AsmInfo.ListTypes(path);
            _cmbClass.Items.Clear();
            foreach (var t in types) _cmbClass.Items.Add(t);
            if (info.MainType != null) _cmbClass.Text = info.MainType;

            // 默认输出路径
            if (string.IsNullOrWhiteSpace(_txtOut.Text)) _txtOut.Text = SuggestOutName();
        }

        private void Pack()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_dllPath) || !File.Exists(_dllPath))
                {
                    Log("错误：请先选择插件 DLL。", true); return;
                }
                var typeRaw = (_cmbType.SelectedItem as string ?? "other").Split('（')[0].Trim().ToLowerInvariant();
                var manifest = new PluginManifest
                {
                    Id = _txtId.Text.Trim(),
                    Name = _txtName.Text.Trim(),
                    Version = _txtVersion.Text.Trim(),
                    Author = _txtAuthor.Text.Trim(),
                    Description = _txtDesc.Text.Trim(),
                    Type = typeRaw,
                    EntryAssembly = Path.GetFileName(_dllPath),
                    EntryClass = _cmbClass.Text.Trim(),
                    EntryMethod = _txtMethod.Text.Trim(),
                    Permissions = _txtPermissions.Text
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim()).Where(p => p.Length > 0).ToList()
                };

                if (string.IsNullOrWhiteSpace(manifest.Id)) { Log("错误：插件 ID 不能为空。", true); return; }
                if (string.IsNullOrWhiteSpace(manifest.Name)) { Log("错误：插件名称不能为空。", true); return; }
                if (string.IsNullOrWhiteSpace(manifest.EntryClass)) { Log("错误：入口类不能为空。", true); return; }
                if (string.IsNullOrWhiteSpace(manifest.EntryAssembly)) { Log("错误：入口程序集名为空。", true); return; }

                var outPath = _txtOut.Text.Trim();
                if (string.IsNullOrWhiteSpace(outPath)) { Log("错误：请选择输出路径。", true); return; }
                if (!outPath.EndsWith(".vqzcmod", StringComparison.OrdinalIgnoreCase))
                    outPath += ".vqzcmod";

                var entries = new List<(string, string)> { (_dllPath, Path.GetFileName(_dllPath)) };
                ModPacker.Pack(outPath, manifest, entries);
                Log($"打包成功：{Path.GetFullPath(outPath)}");
                Log($"  插件：{manifest.Name} v{manifest.Version} | ID：{manifest.Id} | 入口：{manifest.EntryClass}.{manifest.EntryMethod} | 类型：{manifest.Type}");
                if (manifest.Permissions.Count > 0) Log($"  权限声明：{string.Join(", ", manifest.Permissions)}");

                // 尝试打开所在目录
                try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + Path.GetFullPath(outPath) + "\""); }
                catch { }
            }
            catch (Exception ex)
            {
                Log("打包失败：" + ex.Message, true);
            }
        }

        private void Log(string msg, bool isError = false)
        {
            _lblLog.ForeColor = isError ? Color.FromArgb(200, 40, 40) : Color.FromArgb(0, 120, 60);
            _lblLog.Text = msg;
        }
    }
}
