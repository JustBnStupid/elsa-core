using System.Collections.Concurrent;
using System.Reflection;

namespace Elsa;

public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, string> SimpleAssemblyQualifiedTypeNameCache = new();

    /// <summary>
    /// Gets the assembly-qualified name of the type, without any version info etc.
    /// E.g. "System.String, System.Private.CoreLib"
    /// </summary>
    public static string GetSimpleAssemblyQualifiedName(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        return SimpleAssemblyQualifiedTypeNameCache.GetOrAdd(type, GetSimpleAssemblyQualifiedNameInternal);
    }

    private static string GetSimpleAssemblyQualifiedNameInternal(Type type) => $"{type.FullName}, {Assembly.GetAssembly(type)!.GetName().Name}";
}