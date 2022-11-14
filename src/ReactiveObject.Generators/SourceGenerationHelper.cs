using System.Text;
using ReactiveObject.Generators.Extensions;
using ReactiveObject.Generators.Models;

namespace ReactiveObject.Generators;

public static class SourceGenerationHelper
{
    public static string GenerateExtensionClass(in ClassToGenerate classToGenerate)
    {
        var accessibility = classToGenerate.Accessibility.ToString();
        var isStatic = false;

        var stringBuilder = new StringBuilder();
        stringBuilder
            .Append(Constants.Header)
            .AppendLine()
            .AppendNamespaceOpening(classToGenerate.Namespace)
            .AppendClassOpening(accessibility, isStatic, classToGenerate.Name);

        foreach (var propertyToGenerate in classToGenerate.PropertiesToGenerate)
        {
            stringBuilder.AppendProperty(propertyToGenerate);
        }

        stringBuilder
            .AppendClassEnding()
            .AppendNamespaceEnding(classToGenerate.Namespace);

        return stringBuilder.ToString();
    }
}
