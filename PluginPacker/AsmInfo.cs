using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VQZD.PluginPacker
{
    /// <summary>
    /// 从插件 DLL 读取基本信息（名称/版本/公司/描述），并枚举公开类型供入口选择。
    /// </summary>
    internal static class AsmInfo
    {
        public sealed class Info
        {
            public string? Name { get; set; }
            public string? Version { get; set; }
            public string? Company { get; set; }
            public string? Description { get; set; }
            public string? MainType { get; set; }
        }

        public static Info Read(string dllPath)
        {
            var result = new Info();
            try
            {
                var an = AssemblyName.GetAssemblyName(dllPath);
                result.Name = an.Name;
                result.Version = an.Version?.ToString();
            }
            catch { }

            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                var company = asm.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault()?.Company;
                var desc = asm.GetCustomAttributes<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description;
                var title = asm.GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault()?.Title;
                result.Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
                result.Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim();
                if (string.IsNullOrWhiteSpace(result.Name) && !string.IsNullOrWhiteSpace(title))
                    result.Name = title.Trim();

                // 找入口类型：优先含 Main 静态方法的类，否则第一个公开类
                Type? fallback = null;
                foreach (var t in asm.GetExportedTypes())
                {
                    if (t.IsAbstract && t.IsSealed) continue; // static class
                    if (t.Name == "Program" || t.Name == "MainForm") { result.MainType = t.FullName; break; }
                    var main = t.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                    if (main != null) { result.MainType = t.FullName; break; }
                    fallback ??= t;
                }
                result.MainType ??= fallback?.FullName;
            }
            catch (Exception ex)
            {
                // 依赖缺失时仍可打包（用户手填入口）
                System.Diagnostics.Debug.WriteLine("AsmInfo: " + ex.Message);
            }
            return result;
        }

        /// <summary>枚举公开类型全名（供入口类下拉选择）。</summary>
        public static string[] ListTypes(string dllPath)
        {
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                return asm.GetExportedTypes()
                    .Where(t => !t.IsAbstract || !t.IsSealed)
                    .Select(t => t.FullName ?? t.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .OrderBy(n => n)
                    .ToArray()!;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
