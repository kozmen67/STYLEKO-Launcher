using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Styleko.Launcher
{
    internal static class NoticeLoader
    {
        private static readonly Regex LineRegex = new Regex(
            @"<div\b(?<attrs>[^>]*)\bdata-launcher-line\b(?<attrs2>[^>]*)>(?<text>.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex IconRegex = new Regex(@"data-icon\s*=\s*[""'](?<icon>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        internal static List<string> Load(string root)
        {
            string serverNotice = Path.GetFullPath(Path.Combine(root, "..", "Server", "launcher_notice.html"));
            string localNotice = Path.Combine(root, "launcher_notice.html");
            string file = File.Exists(serverNotice) ? serverNotice : localNotice;

            var result = new List<string>();
            if (!File.Exists(file))
                return result;

            string html = File.ReadAllText(file);
            foreach (Match match in LineRegex.Matches(html))
            {
                string attrs = match.Groups["attrs"].Value + " " + match.Groups["attrs2"].Value;
                Match iconMatch = IconRegex.Match(attrs);
                string icon = iconMatch.Success ? iconMatch.Groups["icon"].Value : string.Empty;
                string text = WebUtility.HtmlDecode(TagRegex.Replace(match.Groups["text"].Value, string.Empty)).Trim();
                if (text.Length == 0)
                    continue;

                result.Add(Prefix(icon) + " " + text);
                if (result.Count == 5)
                    break;
            }

            return result;
        }

        private static string Prefix(string icon)
        {
            switch ((icon ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "star": return "[*]";
                case "event": return "[E]";
                case "gift": return "[+]";
                case "shield": return "[S]";
                case "discord": return "[D]";
                case "warning": return "[!]";
                default: return "[-]";
            }
        }
    }
}
