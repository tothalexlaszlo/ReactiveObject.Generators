using System.Text;
using ReactiveObject.Generators.Models;

namespace ReactiveObject.Generators.Extensions;

internal static class StringBuilderExtensions
{
    internal static StringBuilder AppendNamespaceOpening(this StringBuilder stringBuilder, string nameSpace) => string.IsNullOrEmpty(nameSpace) ? stringBuilder : stringBuilder.Append($@"
namespace {nameSpace}
{{");

    internal static StringBuilder AppendNamespaceEnding(this StringBuilder stringBuilder, string nameSpace) => string.IsNullOrEmpty(nameSpace) ? stringBuilder : stringBuilder.Append(@"
}");

    internal static StringBuilder AppendClassOpening(this StringBuilder stringBuilder, string accesibility, bool isStatic, string name)
    {
        string staticKeyword = isStatic ? "static " : string.Empty;

        return stringBuilder.Append($@"
    {accesibility} {staticKeyword}partial class {name}
    {{");
    }

    internal static StringBuilder AppendClassEnding(this StringBuilder stringBuilder) => stringBuilder.Append(@"
    }");

    internal static StringBuilder AppendProperty(this StringBuilder stringBuilder, in PropertyToGenerate propertyToGenerate)
    {
        return stringBuilder.Append($@"
        {propertyToGenerate.Accessibility} {propertyToGenerate.Type} {propertyToGenerate.Name}
        {{
            get => {propertyToGenerate.FieldName};
            set => this.RaiseAndSetIfChanged(ref {propertyToGenerate.FieldName}, value);
        }}
        ");
    }

    internal static StringBuilder AppendMethod(this StringBuilder stringBuilder, string accesibility, bool isStatic, string returnType, string nameWithSignature)
    {
        string staticKeyword = isStatic ? "static " : string.Empty;

        return stringBuilder.Append($@"
        {accesibility} {staticKeyword}{returnType} {nameWithSignature}");
    }
}
