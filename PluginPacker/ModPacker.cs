using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VQZD.PluginPacker
{
    /// <summary>
    /// 插件清单模型（与 .vqzcmod 容器内 config.json 的 schema 一致）。
    /// 字段说明见协议文档：docs/vqzcmod-protocol.md。
    /// </summary>
    public sealed class PluginManifest
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "1.0.0";
        [JsonPropertyName("author")] public string Author { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "other";
        [JsonPropertyName("entryAssembly")] public string EntryAssembly { get; set; } = "";
        [JsonPropertyName("entryClass")] public string EntryClass { get; set; } = "";
        [JsonPropertyName("entryMethod")] public string EntryMethod { get; set; } = "";
        [JsonPropertyName("permissions")] public List<string> Permissions { get; set; } = new List<string>();
        [JsonPropertyName("minAppVersion")] public string MinAppVersion { get; set; } = "";

        public string ToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>从 JSON 解析；失败返回 null。</summary>
        public static PluginManifest? Parse(string json)
        {
            try { return JsonSerializer.Deserialize<PluginManifest>(json); }
            catch { return null; }
        }
    }

    /// <summary>
    /// .vqzcmod 打包器：
    ///   容器 = ZIP，根目录含 config.json 与入口 DLL（可附带资源文件）。
    /// </summary>
    public static class ModPacker
    {
        public const string ConfigFileName = "config.json";

        /// <summary>
        /// 打包。entries 为 (源文件路径, 包内文件名) 列表。
        /// </summary>
        public static void Pack(string outputPath, PluginManifest manifest, IReadOnlyList<(string srcPath, string entryName)> entries)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(manifest.Id)) throw new InvalidOperationException("缺少插件 ID。");
            if (string.IsNullOrWhiteSpace(manifest.EntryAssembly)) throw new InvalidOperationException("缺少入口程序集名。");

            // 校验入口文件确实在 entries 中
            bool hasEntry = entries.Any(e =>
                e.entryName.Equals(manifest.EntryAssembly, StringComparison.OrdinalIgnoreCase));
            if (!hasEntry) throw new InvalidOperationException("入口程序集不在待打包文件列表中： " + manifest.EntryAssembly);

            var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(outputPath)) File.Delete(outputPath);

            using (var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                // 1) config.json
                var cfgEntry = zip.CreateEntry(ConfigFileName, CompressionLevel.Optimal);
                using (var w = new StreamWriter(cfgEntry.Open(), new UTF8Encoding(false)))
                {
                    w.Write(manifest.ToJson());
                }

                // 2) 各文件
                foreach (var (src, name) in entries)
                {
                    if (!File.Exists(src))
                        throw new FileNotFoundException("文件不存在：" + src, src);
                    var nameSafe = Path.GetFileName(name.Replace('\\', Path.DirectorySeparatorChar));
                    var e = zip.CreateEntry(nameSafe, CompressionLevel.Optimal);
                    using (var srcStream = File.OpenRead(src))
                    using (var dstStream = e.Open())
                    {
                        srcStream.CopyTo(dstStream);
                    }
                }
            }
        }

        /// <summary>读取 .vqzcmod 包内的 config.json（不落盘）。</summary>
        public static PluginManifest? Peek(string modPath)
        {
            if (!File.Exists(modPath)) return null;
            try
            {
                using var zip = ZipFile.OpenRead(modPath);
                var entry = zip.Entries.FirstOrDefault(e =>
                    e.FullName.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith("/" + ConfigFileName, StringComparison.OrdinalIgnoreCase));
                if (entry == null) return null;
                using var sr = new StreamReader(entry.Open());
                return PluginManifest.Parse(sr.ReadToEnd());
            }
            catch { return null; }
        }
    }
}
