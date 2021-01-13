using System;
using System.Collections.Generic;
using System.Linq;

namespace AkyuiUnity.Loader
{
    public static class AkyuiDependencyResolver
    {
        public static IAkyuiLoader[] Resolve(IAkyuiLoader[] loaders)
        {
            var dependencies = new Dictionary<string, string[]>();
            var nameToLoader = loaders.ToDictionary(x => x.LayoutInfo.Name, x => x);
            var unImported = loaders.Select(x => x.LayoutInfo.Name).ToList();
            var sortedLoaders = new List<IAkyuiLoader>();
            foreach (var loader in loaders)
            {
                var reference = new List<string>();
                foreach (var e in loader.LayoutInfo.Elements)
                {
                    if (e is PrefabElement prefabElement)
                    {
                        reference.Add(prefabElement.Reference);
                    }
                }
                dependencies[loader.LayoutInfo.Name] = reference.Distinct().ToArray();
            }

            while (unImported.Count > 0)
            {
                var import = dependencies
                    .Where(x => unImported.Contains(x.Key))
                    .Where(x => x.Value.Count(y => unImported.Contains(y)) == 0)
                    .ToArray();

                foreach (var i in import)
                {
                    unImported.Remove(i.Key);
                    sortedLoaders.Add(nameToLoader[i.Key]);
                }

                if (!import.Any())
                {
                    throw new Exception($"AkyuiDependencyResolver error");
                }
            }

            return sortedLoaders.ToArray();
        }
    }
}