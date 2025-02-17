
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Herta.Utils.RouteCacheMatcher
{
    public static class RouteCacheMatcher
    {
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>();

        // 将路由模板转换为正则表达式
        public static Regex GetRouteRegex(string routeTemplate)
        {
            return RegexCache.GetOrAdd(routeTemplate, CreateRegexFromTemplate);
        }

        private static Regex CreateRegexFromTemplate(string template)
        {
            // 防止正则表达式注入攻击，将路由参数（如 {id}）转换为捕获组
            string pattern = Regex.Replace(template, @"\{([a-z0-9_]+)\}", "(?<$1>[^/]+)");
            return new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase); // 添加锚点确保完整匹配
        }

        // 检查请求路径是否匹配路由模板
        public static bool IsPathMatch(string requestPath, string routeTemplate, out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>();
            var regex = GetRouteRegex(routeTemplate);
            var match = regex.Match(requestPath);

            if (match.Success)
            {
                foreach (var groupName in regex.GetGroupNames())
                {
                    if (int.TryParse(groupName, out int groupIndex) == false)
                    {
                        parameters[groupName] = match.Groups[groupName].Value;
                    }
                }
                return true;
            }
            return false;
        }
    }
}