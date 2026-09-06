using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VQZD.PluginPacker
{
    /// <summary>命令行打包模式。</summary>
    internal static class CliRun
    {
        public static int Run(string[] args)
        {
            try
            {
                // 参数解析：PluginPacker <dll> <out.vqzcmod> [--key value ...]
                string dll = args[0];
                string output = args[1];
                if (!File.Exists(dll))
                {
                    Console.Error.WriteLine("错误：找不到 DLL：" + dll);
                    return 1;
                }
                if (!dll.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("错误：第一个参数必须是 .dll 文件。");
                    return 2;
                }
                if (!output.EndsWith(".vqzcmod", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("错误：输出文件必须以 .vqzcmod 结尾。");
                    return 2;
                }

                var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 2; i < args.Length - 1; i += 2)
                {
                    if (args[i].StartsWith("--") && i + 1 < args.Length)
                    {
                        opts[args[i].Substring(2)] = args[i + 1];
                    }
                }

                // 从程序集自动读取基本信息
                var info = AsmInfo.Read(dll);

                var manifest = new PluginManifest
                {
                    Id = Get(opts, "id", "com.vqzd.plugin." + (info.Name ?? "plugin").ToLowerInvariant()),
                    Name = Get(opts, "name", info.Name ?? Path.GetFileNameWithoutExtension(dll)),
                    Version = Get(opts, "version", info.Version ?? "1.0.0"),
                    Author = Get(opts, "author", info.Company ?? ""),
                    Description = Get(opts, "description", info.Description ?? ""),
                    Type = Get(opts, "type", "other").ToLowerInvariant(),
                    EntryAssembly = Path.GetFileName(dll),
                    EntryClass = Get(opts, "class", info.MainType ?? ""),
                    EntryMethod = Get(opts, "method", ""),
                    MinAppVersion = Get(opts, "minAppVersion", "")
                };
                if (opts.TryGetValue("permission", out var perm))
                {
                    manifest.Permissions.AddRange(perm.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim()).Where(p => p.Length > 0));
                }

                // 打包（DLL + 同目录下可选资源：config.json 由工具生成，故资源仅附加 dll 之外的）
                var entries = new List<(string, string)> { (dll, Path.GetFileName(dll)) };
                ModPacker.Pack(output, manifest, entries);

                Console.WriteLine("已生成：" + Path.GetFullPath(output));
                Console.WriteLine("  ID:      " + manifest.Id);
                Console.WriteLine("  名称:    " + manifest.Name);
                Console.WriteLine("  版本:    " + manifest.Version);
                Console.WriteLine("  入口:    " + manifest.EntryClass + "." + (string.IsNullOrEmpty(manifest.EntryMethod) ? "Run/OnLoad/Execute" : manifest.EntryMethod));
                Console.WriteLine("  类型:    " + manifest.Type);
                Console.WriteLine("  权限:    " + (manifest.Permissions.Count > 0 ? string.Join(", ", manifest.Permissions) : "（无）"));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("打包失败：" + ex.Message);
                return 1;
            }
        }

        private static string Get(Dictionary<string, string> opts, string key, string def)
            => opts.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : def;
    }
}
