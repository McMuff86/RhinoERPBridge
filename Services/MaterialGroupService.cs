using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace RhinoERPBridge.Services
{
    public static class MaterialGroupService
    {
        private static IReadOnlyDictionary<string, string> _map;

        public static string TryResolve(string code)
        {
            if (_map == null)
                _map = LoadMap();
            if (string.IsNullOrWhiteSpace(code)) return code;
            return _map.TryGetValue(code.Trim(), out var name) ? name : code;
        }

        private static IReadOnlyDictionary<string, string> LoadMap()
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("EmbeddedResources.material-groups.json", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                return new Dictionary<string, string>();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                var items = JsonConvert.DeserializeObject<List<MaterialGroupDto>>(json) ?? new List<MaterialGroupDto>();
                return items.Where(i => !string.IsNullOrWhiteSpace(i.code))
                            .GroupBy(i => i.code.Trim())
                            .ToDictionary(g => g.Key, g => g.Last().name ?? g.Key, StringComparer.OrdinalIgnoreCase);
            }
        }

        private class MaterialGroupDto
        {
            public string code { get; set; }
            public string name { get; set; }
        }
    }
}


