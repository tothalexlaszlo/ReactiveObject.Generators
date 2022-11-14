using Microsoft.CodeAnalysis;

namespace ReactiveObject.Generators.Models;

public readonly struct PropertyToGenerate
{
    public readonly string FieldName { get; }
    public readonly string Name { get; }
    public readonly string Accessibility { get; }
    public readonly string Type { get; }

    public PropertyToGenerate(string fieldName, string name, string accessibility, string type)
    {
        FieldName = fieldName;
        Name = name;
        Accessibility = accessibility;
        Type = type;
    }
}
