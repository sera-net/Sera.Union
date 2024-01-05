using System;
using Sera.TaggedUnion.Misc;

namespace Sera.TaggedUnion;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false)]
public sealed class UnionAttribute : Attribute
{
    /// <summary>Tags name</summary>
    public string TagsName { get; set; } = "Tags";
    /// <summary>Whether to put Tags outside Union</summary>
    public bool ExternalTags { get; set; } = false;
    /// <summary>Naming format for external Tags, position 0 is the union name</summary>
    public string ExternalTagsName { get; set; } = "{0}Tags";
    /// <summary>The underlying type of the Tags enum, the smallest required type is used by default</summary>
    public Type? TagsUnderlying { get; set; }
}

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class UnionTemplateAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class UnionTagAttribute(object tag) : Attribute
{
    public object Tag { get; } = tag;
}

/// <summary>
/// Manually specify symbol semantics
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class, Inherited = false)]
public sealed class UnionSymbolAttribute : Attribute
{
    /// <summary>
    /// True if the type is unmanaged according to language rules. False if managed or if the language
    /// has no concept of unmanaged types.
    /// </summary>
    public MayBool IsUnmanagedType { get; set; }
    /// <summary>
    /// True if this type is known to be a reference type. It is never the case that
    /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
    /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
    /// </summary>
    public MayBool IsReferenceType { get; set; }
    /// <summary>
    /// True if this type is known to be a value type. It is never the case that
    /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
    /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
    /// </summary>
    public MayBool IsValueType { get; set; }
}