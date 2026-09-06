using System;
using System.IO;

namespace VQZD.Plugin.HelloWorld
{
    /// <summary>
    /// 示例插件入口：Run 方法被主程序反射调用。
    /// 约定：入口类需公开可实例化（或静态方法）；entryMethod 留空时主程序依次尝试 Run / OnLoad / Execute。
    /// </summary>
    public sealed class HelloWorldPlugin
    {
        /// <summary>无参入口（最常用）。返回字符串会显示为主程序日志。</summary>
        public string Run()
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] 你好，我是示例插件 HelloWorld v1.0.0！";
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugin-logs");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "hello.txt"), line + Environment.NewLine);
            }
            catch { }
            return line;
        }
    }
}
