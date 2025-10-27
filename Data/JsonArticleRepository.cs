using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RhinoERPBridge.Models;

namespace RhinoERPBridge.Data
{
    public interface IArticleRepository
    {
        IReadOnlyList<Article> GetAll();
        IReadOnlyList<Article> Search(string term);
    }

    public class JsonArticleRepository : IArticleRepository
    {
        private readonly Lazy<IReadOnlyList<Article>> _articles;

        public JsonArticleRepository()
        {
            _articles = new Lazy<IReadOnlyList<Article>>(LoadFromEmbeddedResource);
        }

        public IReadOnlyList<Article> GetAll() => _articles.Value;

        public IReadOnlyList<Article> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return GetAll();
            term = term.Trim();
            return GetAll().Where(a =>
                ContainsIgnoreCase(a.Name, term) ||
                ContainsIgnoreCase(a.Sku, term) ||
                ContainsIgnoreCase(a.Description, term) ||
                ContainsIgnoreCase(a.Category, term)
            ).ToList();
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value)) return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<Article> LoadFromEmbeddedResource()
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("EmbeddedResources.test-articles.json", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                throw new FileNotFoundException("Embedded test-articles.json not found. Ensure it is EmbeddedResource in csproj.");

            using (var stream = asm.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                var items = JsonConvert.DeserializeObject<List<Article>>(json) ?? new List<Article>();
                return items;
            }
        }
    }
}


