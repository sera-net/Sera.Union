using System;

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
