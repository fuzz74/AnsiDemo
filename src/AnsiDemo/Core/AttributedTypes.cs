using System.Reflection;
using System.Runtime.CompilerServices;

namespace AnsiDemo.Core;

/// <summary>
/// Finds the classes carrying <typeparamref name="TAttribute"/> in every loaded assembly whose simple name starts with
/// "AnsiDemo". Each assembly is scanned once; assemblies loaded later are picked up by the next lookup.
/// </summary>
internal sealed class AttributedTypes<TAttribute>(Func<TAttribute, string> nameOf, string kind) where TAttribute : Attribute
{
    private const string AssemblyPrefix = "AnsiDemo";

    private readonly ConditionalWeakTable<Assembly, (Type Type, TAttribute Attribute)[]> _byAssembly = [];

    public IReadOnlyList<string> Names
    {
        get
        {
            string[] names = [.. Index().Keys];
            Array.Sort(names, StringComparer.Ordinal);
            return names;
        }
    }

    public (Type Type, TAttribute Attribute) Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Index().TryGetValue(name, out var entry)
            ? entry
            : throw new ArgumentException($"Unknown {kind} '{name}'.", nameof(name));
    }

    /// <summary>A new instance of the class registered as <paramref name="name"/>, made with its public parameterless constructor.</summary>
    public TInstance Create<TInstance>(string name) where TInstance : class
    {
        Type type = Find(name).Type;
        if (!typeof(TInstance).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"The {kind} '{name}' ({type.FullName}) must be a concrete {typeof(TInstance).Name} with a public parameterless constructor.");
        }
        return (TInstance)Activator.CreateInstance(type)!;
    }

    private Dictionary<string, (Type Type, TAttribute Attribute)> Index()
    {
        var index = new Dictionary<string, (Type Type, TAttribute Attribute)>(StringComparer.Ordinal);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name?.StartsWith(AssemblyPrefix, StringComparison.Ordinal) != true)
            {
                continue;
            }
            foreach (var entry in _byAssembly.GetValue(assembly, Scan))
            {
                string name = nameOf(entry.Attribute);
                if (!index.TryAdd(name, entry))
                {
                    throw new InvalidOperationException(
                        $"The {kind} name '{name}' is used by both {index[name].Type.FullName} and {entry.Type.FullName}.");
                }
            }
        }
        return index;
    }

    private static (Type Type, TAttribute Attribute)[] Scan(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
        }

        var found = new List<(Type Type, TAttribute Attribute)>();
        foreach (Type? type in types)
        {
            if (type is { IsClass: true } && type.GetCustomAttribute<TAttribute>(inherit: false) is { } attribute)
            {
                found.Add((type, attribute));
            }
        }
        return [.. found];
    }
}
