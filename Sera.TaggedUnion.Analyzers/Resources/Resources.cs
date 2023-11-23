using System.Resources;
using Microsoft.CodeAnalysis;

namespace Sera.TaggedUnion.Analyzers.Resources;

public static class Strings
{
    private static ResourceManager? resourceManager;
    public static ResourceManager ResourceManager => resourceManager ??= new ResourceManager(typeof(Strings));

    public static LocalizableResourceString Get(string name) =>
        new(name, ResourceManager, typeof(Strings));

    public static LocalizableResourceString Get(string name, params string[] formatArgs) =>
        new(name, ResourceManager, typeof(Strings), formatArgs);
}
