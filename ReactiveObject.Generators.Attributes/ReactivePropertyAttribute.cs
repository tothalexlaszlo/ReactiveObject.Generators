namespace ReactiveObject.Generators
{
    /// <summary>
    /// Add to property to indicate that extension methods should be generated for the type.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    [System.Diagnostics.Conditional("REACTIVEOBJECT_GENERATORS_USAGES")]
    public class ReactivePropertyAttribute : System.Attribute
    {
    }
}
