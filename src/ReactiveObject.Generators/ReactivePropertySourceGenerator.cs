using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using ReactiveObject.Generators.Models;

namespace ReactiveObject.Generators;

[Generator]
public class ReactivePropertySourceGenerator : IIncrementalGenerator
{
    private const string ReactivePropertyAttribute = "ReactiveProperty";
    private const string ReactivePropertyAttributeFullName = $"{nameof(ReactiveObject)}.{nameof(Generators)}.{nameof(Generators.ReactivePropertyAttribute)}";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{nameof(ReactivePropertyAttribute)}.g.cs", SourceText.From(Constants.Attribute, Encoding.UTF8)));

        // Do a simple filter for classes
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => IsSyntaxTargetForGeneration(syntaxNode), // select class with attributes
                transform: static (generatorSyntaxContext, _) => GetSemanticTargetForGeneration(generatorSyntaxContext)) // select the class with the [ReactiveProperty] attribute
            .Where(static m => m is not null)!; // filter out attributed class that we don't care about

        // Combine the selected classes with the `Compilation`
        var compilationAndProperties = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and classes
        context.RegisterSourceOutput(compilationAndProperties, static (sourceProductionContext, source) => Execute(source.Left, source.Right, sourceProductionContext));
    }

    /// <summary>
    /// Filters syntax to only classes which have one or more attributes.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax syntax && syntax.Members.Count > 0;

    /// <summary>
    /// Filters syntax to only classes which have the [ReactiveProperty] attribute.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(in GeneratorSyntaxContext context)
    {
        /// We know the node is a <see cref="ClassDeclarationSyntax"/> thanks to <see cref="IsSyntaxTargetForGeneration(SyntaxNode)"/>.
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var member in classDeclarationSyntax.Members)
        {
            // Member is not field.
            if (member is not FieldDeclarationSyntax fieldDeclarationSyntax)
            {
                continue;
            }

            // Loop through all the attributes on the field.
            foreach (var attributeListSyntax in fieldDeclarationSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not ISymbol attributeSymbol)
                    {
                        // Weird, we couldn't get the symbol, ignore it.
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // Is the attribute the [ReactiveProperty] attribute?
                    if (fullName.Equals(ReactivePropertyAttributeFullName, StringComparison.Ordinal))
                    {
                        return classDeclarationSyntax;
                    }
                }
            }
        }

        // We didn't find the attribute we were looking for.
        return null;
    }

    private static void Execute(Compilation compilation, in ImmutableArray<ClassDeclarationSyntax?> fields, in SourceProductionContext context)
    {
        if (fields.IsDefaultOrEmpty)
        {
            // Nothing to do yet.
            return;
        }

        // Stop if we're asked to.
        context.CancellationToken.ThrowIfCancellationRequested();

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        var distinctFields = fields.Distinct();

        // Convert each ClassDeclarationSyntax to a ClassToGenerate.
        var classesToGenerate = GetTypesToGenerate(compilation, distinctFields, context.CancellationToken);

        /// If there were errors in the <see cref="ClassDeclarationSyntax"/>, we won't create a
        /// <see cref="ClassToGenerate"/> for it, so make sure we have something to generate.
        foreach (var classToGenerate in classesToGenerate)
        {
            // Generate the source code and add it to the output.
            string result = SourceGenerationHelper.GenerateExtensionClass(classToGenerate);
            context.AddSource($"{classToGenerate.Name}{ReactivePropertyAttribute}.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private static List<ClassToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax?> classes, CancellationToken cancellationToken)
    {
        var classesToGenerate = new List<ClassToGenerate>();

        // Get the semantic representation of our marker attribute.
        var ownAttribute = compilation.GetTypeByMetadataName(ReactivePropertyAttributeFullName);
        if (ownAttribute is null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classesToGenerate;
        }

        foreach (var classDeclarationSyntax in classes.Where(x => x is not null).Select(x => x!))
        {
            // Stop if we're asked to.
            cancellationToken.ThrowIfCancellationRequested();

            // Get the semantic representation of the class syntax.
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken: cancellationToken) is not INamedTypeSymbol classSymbol)
            {
                // Something went wrong, bail out.
                continue;
            }

            // Get the full type name of the class.
            var fullyQualifiedName = classSymbol.ToString();
            var name = classSymbol.Name;
            var nameSpace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToString();
            var propertiesToGenerates = new List<PropertyToGenerate>();

            // Loop through all of the members on the class in order to find the fields
            foreach (var member in classSymbol.GetMembers())
            {
                if (member is not IFieldSymbol field)
                {
                    continue;
                }

                // Loop through all of the attributes on the field until we find the [ReactiveProperty] attribute
                foreach (var fieldAttributeData in field.GetAttributes())
                {
                    if (!ownAttribute.Equals(fieldAttributeData.AttributeClass, SymbolEqualityComparer.Default))
                    {
                        // This isn't the [ReactiveProperty] attribute
                        continue;
                    }

                    var propertyToGenerate = new PropertyToGenerate(
                        fieldName: field.Name,
                        name: ConvertFieldNameToCamelCase(field.Name.AsSpan()),
                        accessibility: classSymbol.DeclaredAccessibility.ToString().ToLower(),
                        type: field.Type.ToString());

                    propertiesToGenerates.Add(propertyToGenerate);
                }
            }

            classesToGenerate.Add(new ClassToGenerate(
                name: name,
                @namespace: nameSpace,
                fullyQualifiedName: fullyQualifiedName,
                accessibility: classSymbol.DeclaredAccessibility.ToString().ToLower(),
                propertiesToGenerate: propertiesToGenerates.ToArray()));
        }

        return classesToGenerate;
    }

    private static string ConvertFieldNameToCamelCase(ReadOnlySpan<char> fieldName)
    {
        // Remove underscore ('_').
        if (fieldName[0] == '_')
        {
            fieldName = fieldName.Slice(1);
        }

        // Shift the character to upper.
        if (fieldName[0] is >= 'a' and <= 'z')
        {
            // This can be optimized on .NET6+ with string.Create(); instead of Span<char>.
            Span<char> buffer = stackalloc char[fieldName.Length];
            fieldName.CopyTo(buffer);

            const int distanceBetweenLowAndUpCharactersInAscii = 32;
            buffer[0] = (char)(fieldName[0] - distanceBetweenLowAndUpCharactersInAscii);

            return buffer.ToString();
        }

        return fieldName.ToString();
    }
}
