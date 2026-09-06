using System;
using System.Windows.Forms;

namespace VQZD.PluginPacker
{
    internal static class Program
    {
        /// <summary>命令行模式：PluginPacker.exe &lt;dll&gt; &lt;out.vqzcmod&gt; [--id id] [--name name] ...</summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length >= 2 && !args[0].StartsWith("-"))
            {
                return CliRun.Run(args);
            }

#if NET48
            if (Environment.OSVersion.Version.Major >= 6)
            {
                try { SetProcessDPIAware(); } catch { }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#else
            ApplicationConfiguration.Initialize();
#endif
            Application.Run(new MainForm());
            return 0;
        }

#if NET48
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
#endif
    }
}
