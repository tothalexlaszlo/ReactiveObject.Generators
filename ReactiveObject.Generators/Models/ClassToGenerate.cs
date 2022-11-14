namespace ReactiveObject.Generators.Models;

public readonly struct ClassToGenerate
{
    public readonly string Name { get; }
    public readonly string Namespace { get; }
    public readonly string FullyQualifiedName { get; }
    public readonly string Accessibility { get; }
    public readonly PropertyToGenerate[] PropertiesToGenerate { get; }

    public ClassToGenerate(string name, string @namespace, string fullyQualifiedName, string accessibility, PropertyToGenerate[] propertiesToGenerate)
    {
        Name = name;
        Namespace = @namespace;
        FullyQualifiedName = fullyQualifiedName;
        Accessibility = accessibility;
        PropertiesToGenerate = propertiesToGenerate;
    }
}
