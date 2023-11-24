using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sera.TaggedUnion.Analyzers.Resources;
using Sera.TaggedUnion.Analyzers.Utilities;

namespace Sera.TaggedUnion.Analyzers.Generators.Templates;

public enum UnionCaseTypeKind
{
    None,
    Unmanaged,
    Class
}

public record struct UnionCase(string Name, string Tag, string Type, UnionCaseTypeKind Kind, bool IsGeneric);

public record struct UnionAttr(string TagsName, bool ExternalTags, string ExternalTagsName, string? TagsUnderlying)
{
    public static UnionAttr FromData(AttributeData data, List<Diagnostic> diagnostics)
    {
        var a = new UnionAttr("Tags", false, "{0}Tags", null);
        foreach (var kv in data.NamedArguments)
        {
            switch (kv.Key)
            {
                case "TagsName":
                    a.TagsName = (string)kv.Value.Value!;
                    break;
                case "ExternalTags":
                    a.ExternalTags = (bool)kv.Value.Value!;
                    break;
                case "ExternalTagsName":
                    a.ExternalTagsName = (string)kv.Value.Value!;
                    break;
                case "TagsUnderlying":
                    a.TagsUnderlying = kv.Value.Value?.ToString();
                    if (a.TagsUnderlying is not ("sbyte" or "byte" or "short" or "ushort" or "int" or "uint" or "long"
                        or "ulong"))
                    {
                        a.TagsUnderlying = null;
                        var desc = Utils.MakeError(Strings.Get("Generator.Union.Error.Underlying"));
                        var syntax = (AttributeSyntax)data.ApplicationSyntaxReference!.GetSyntax();
                        try
                        {
                            var expr = syntax.ArgumentList!.Arguments
                                .Where(a => a.NameEquals?.Name.ToString() == "TagsUnderlying")
                                .Select(a => a.Expression)
                                .First();
                            diagnostics.Add(Diagnostic.Create(desc, expr.GetLocation()));
                        }
                        catch
                        {
                            diagnostics.Add(Diagnostic.Create(desc, syntax.GetLocation()));
                        }
                    }
                    break;
            }
        }
        return a;
    }
}

public class TemplateStructUnion
(GenBase GenBase, string Name, UnionAttr Attr, bool ReadOnly, List<UnionCase> Cases,
    bool AnyGeneric) : ATemplate(GenBase)
{
    public const string AggressiveInlining =
        "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";

    public const string CompilerGenerated = "[global::System.Runtime.CompilerServices.CompilerGenerated]";

    private List<StringBuilder> ExTypes = new();

    private string HashName = "";

    protected override void DoGen()
    {
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(MemoryMarshal.AsBytes(FullName.AsSpan()).ToArray());
            HashName = string.Join("", bytes.Select(b => $"{b:X2}"));
            HashName = $"{Name}_{HashName}";
        }

        var impl_name = $"__impl_";
        var tags_name = Attr.ExternalTags ? string.Format(Attr.ExternalTagsName, Name) : Attr.TagsName;

        if (Attr.ExternalTags)
        {
            GenTags(tags_name, "");
            sb.AppendLine();
        }

        sb.AppendLine(GenBase.Target.Code);
        sb.AppendLine($"    : global::Sera.TaggedUnion.ITaggedUnion");
        sb.AppendLine($"    , global::System.IEquatable<{TypeName}>");
        sb.AppendLine($"    , global::System.IComparable<{TypeName}>");
        sb.AppendLine($"#if NET7_0_OR_GREATER");
        sb.AppendLine($"    , global::System.Numerics.IEqualityOperators<{TypeName}, {TypeName}, bool>");
        sb.AppendLine($"    , global::System.Numerics.IComparisonOperators<{TypeName}, {TypeName}, bool>");
        sb.AppendLine($"#endif");
        sb.AppendLine("{");

        sb.AppendLine(ReadOnly ? $"    private readonly {impl_name} _impl;" : $"    private {impl_name} _impl;");
        sb.AppendLine($"    private {Name}({impl_name} _impl) {{ this._impl = _impl; }}");

        sb.AppendLine();
        sb.AppendLine($"    public readonly {tags_name} Tag");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        {AggressiveInlining}");
        sb.AppendLine($"        get => this._impl._tag;");
        sb.AppendLine($"    }}");

        if (!Attr.ExternalTags)
        {
            sb.AppendLine();
            GenTags(tags_name);
        }

        sb.AppendLine();
        GenImpl(impl_name, tags_name);

        if (Cases.Count > 0)
        {
            sb.AppendLine();
            GenMake(impl_name, tags_name);

            sb.AppendLine();
            GenIs(tags_name);

            sb.AppendLine();
            GenGetter();
        }
        else
        {
            sb.AppendLine();
            GenMakeEmpty();
        }

        sb.AppendLine();
        GenEquatable(tags_name);

        sb.AppendLine();
        GenComparable(tags_name);

        sb.AppendLine();
        GenToStr(tags_name);

        sb.AppendLine("}");

        foreach (var ex in ExTypes)
        {
            sb.AppendLine();
            sb.Append(ex);
        }
    }

    Dictionary<string, int>? class_types;
    Dictionary<string, int>? unmanaged_types;
    Dictionary<string, int>? other_types;

    private void GenTags(string name, string spaces = "    ")
    {
        var type = Attr.TagsUnderlying;
        if (type == null)
        {
            if (Cases.Count < byte.MaxValue) type = "byte";
            else if (Cases.Count < short.MaxValue) type = "short";
            else type = "int";
        }
        sb.AppendLine($"{spaces}public enum {name} : {type}");
        sb.AppendLine($"{spaces}{{");
        foreach (var @case in Cases)
        {
            sb.AppendLine($"{spaces}    {@case.Name} = {@case.Tag},");
        }
        sb.AppendLine($"{spaces}}}");
    }

    private void GenImpl(string name, string tags_name)
    {
        sb.AppendLine($"    {CompilerGenerated}");
        sb.AppendLine($"    private struct {name}");
        sb.AppendLine($"    {{");

        var dict = Cases
            .Where(a => a.Type != "void")
            .GroupBy(a => a.Kind)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var __class_ = AnyGeneric ? $"ℍⅈⅆⅇ__{HashName}__class_" : "__class_";
        var __unmanaged_ = AnyGeneric ? $"ℍⅈⅆⅇ__{HashName}__unmanaged_" : "__unmanaged_";
        
        if (dict.TryGetValue(UnionCaseTypeKind.Class, out _))
        {
            sb.AppendLine($"        public {__class_} _class_;");
        }
        if (dict.TryGetValue(UnionCaseTypeKind.Unmanaged, out _))
        {
            sb.AppendLine($"        public {__unmanaged_} _unmanaged_;");
        }
        if (dict.TryGetValue(UnionCaseTypeKind.None, out var other_cases))
        {
            var types = other_cases.AsParallel().AsOrdered()
                .Select(a => a.Type)
                .Distinct()
                .ToArray();
            other_types = types
                .Select((a, b) => (a, b))
                .ToDictionary(a => a.a, a => a.b);
            foreach (var (type, i) in types.Select((a, b) => (a, b)))
            {
                sb.AppendLine($"        public {type} _{i};");
            }
        }

        sb.AppendLine($"        public readonly {tags_name} _tag;");

        if (dict.TryGetValue(UnionCaseTypeKind.Class, out var class_cases))
        {
            var ex_sb = AnyGeneric ? new StringBuilder() : sb;
            var space = AnyGeneric ? "" : "        ";
            var types = class_cases.AsParallel().AsOrdered()
                .Select(a => a.IsGeneric ? "object?" : a.Type)
                .Distinct()
                .ToList();
            class_types = types
                .Select((a, b) => (a, b))
                .ToDictionary(a => a.a, a => a.b);
            if (!AnyGeneric) ex_sb.AppendLine();
            ex_sb.AppendLine($"{space}{CompilerGenerated}");
            ex_sb.AppendLine(
                $"{space}[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
            ex_sb.AppendLine($"{space}internal struct {__class_}");
            ex_sb.AppendLine($"{space}{{");
            foreach (var (type, i) in types.Select((a, b) => (a, b)))
            {
                ex_sb.AppendLine($"{space}    [global::System.Runtime.InteropServices.FieldOffset(0)]");
                ex_sb.AppendLine($"{space}    public {type} _{i};");
            }
            ex_sb.AppendLine($"{space}}}");
            if (AnyGeneric) ExTypes.Add(ex_sb);
        }

        if (dict.TryGetValue(UnionCaseTypeKind.Unmanaged, out var unmanaged_cases))
        {
            var ex_sb = AnyGeneric ? new StringBuilder() : sb;
            var space = AnyGeneric ? "" : "        ";
            var types = unmanaged_cases.AsParallel().AsOrdered()
                .Select(a => a.Type)
                .Where(t => t != "void")
                .Distinct()
                .ToArray();
            unmanaged_types = types
                .Select((a, b) => (a, b))
                .ToDictionary(a => a.a, a => a.b);
            if (!AnyGeneric) ex_sb.AppendLine();
            ex_sb.AppendLine($"{space}{CompilerGenerated}");
            ex_sb.AppendLine(
                $"{space}[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
            ex_sb.AppendLine($"{space}internal struct {__unmanaged_}");
            ex_sb.AppendLine($"{space}{{");
            foreach (var (type, i) in types.Select((a, b) => (a, b)))
            {
                ex_sb.AppendLine($"{space}    [global::System.Runtime.InteropServices.FieldOffset(0)]");
                ex_sb.AppendLine($"{space}    public {type} _{i};");
            }
            ex_sb.AppendLine($"{space}}}");
            if (AnyGeneric) ExTypes.Add(ex_sb);
        }

        #region ctor

        sb.AppendLine();
        sb.AppendLine($"        public {name}({tags_name} _tag)");
        sb.AppendLine($"        {{");
        if (class_types != null)
            sb.AppendLine($"            this._class_ = default;");
        if (unmanaged_types != null)
            sb.AppendLine(
                $"            global::System.Runtime.CompilerServices.Unsafe.SkipInit(out this._unmanaged_);");
        if (other_types != null)
        {
            foreach (var i in other_types.Values)
            {
                sb.AppendLine($"            this._{i} = default!;");
            }
        }
        sb.AppendLine($"            this._tag = _tag;");
        sb.AppendLine($"        }}");

        #endregion

        sb.AppendLine($"    }}");
    }

    private void GenMakeEmpty()
    {
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine($"    public static {TypeName} Make() => default;");
    }

    private void GenMake(string impl_name, string tags_name)
    {
        foreach (var @case in Cases)
        {
            sb.AppendLine($"    {AggressiveInlining}");
            sb.Append($"    public static {TypeName} Make{@case.Name}(");
            if (@case.Type != "void")
            {
                sb.Append(@case.Type);
                sb.Append(" value");
            }
            sb.AppendLine($")");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var _impl = new {impl_name}({tags_name}.{@case.Name});");
            if (@case.Type != "void")
            {
                if (@case.Kind == UnionCaseTypeKind.None)
                {
                    var index = other_types![@case.Type];
                    sb.AppendLine($"        _impl._{index} = value;");
                }
                else if (@case.Kind == UnionCaseTypeKind.Class)
                {
                    if (@case.IsGeneric)
                    {
                        var index = class_types!["object?"];
                        sb.AppendLine($"        _impl._class_._{index} = value;");
                    }
                    else
                    {
                        var index = class_types![@case.Type];
                        sb.AppendLine($"        _impl._class_._{index} = value;");
                    }
                }
                else if (@case.Kind == UnionCaseTypeKind.Unmanaged)
                {
                    var index = unmanaged_types![@case.Type];
                    sb.AppendLine($"        _impl._unmanaged_._{index} = value;");
                }
            }
            sb.AppendLine($"        return new {TypeName}(_impl);");
            sb.AppendLine($"    }}");
        }
    }

    private void GenIs(string tags_name)
    {
        foreach (var @case in Cases)
        {
            sb.AppendLine($"    public readonly bool Is{@case.Name}");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        {AggressiveInlining}");
            sb.AppendLine($"        get => this._impl._tag == {tags_name}.{@case.Name};");
            sb.AppendLine($"    }}");
        }
    }

    private void GenGetter()
    {
        foreach (var @case in Cases)
        {
            if (@case.Type == "void") continue;
            sb.AppendLine($"    public {@case.Type} {@case.Name}");
            sb.AppendLine($"    {{");

            void GenField(bool get)
            {
                if (@case.Kind == UnionCaseTypeKind.None)
                {
                    var index = other_types![@case.Type];
                    sb.Append($"this._impl._{index}");
                    if (!get) sb.Append($" = ");
                    else sb.Append("!");
                }
                else if (@case.Kind == UnionCaseTypeKind.Class)
                {
                    if (@case.IsGeneric)
                    {
                        if (get) sb.Append($"({@case.Type})");
                        var index = class_types!["object?"];
                        sb.Append($"this._impl._class_._{index}");
                        if (!get) sb.Append($" = ({@case.Type})");
                        else sb.Append("!");
                    }
                    else
                    {
                        var index = class_types![@case.Type];
                        sb.Append($"this._impl._class_._{index}");
                        if (!get) sb.Append($" = ");
                        else sb.Append("!");
                    }
                }
                else if (@case.Kind == UnionCaseTypeKind.Unmanaged)
                {
                    var index = unmanaged_types![@case.Type];
                    sb.Append($"this._impl._unmanaged_._{index}");
                    if (!get) sb.Append($" = ");
                    else sb.Append("!");
                }
            }

            #region getter

            sb.AppendLine($"        {AggressiveInlining}");
            sb.Append($"        ");
            if (!ReadOnly) sb.Append($"readonly ");
            sb.Append($"get => !this.Is{@case.Name} ? default! : ");
            GenField(true);
            sb.AppendLine($";");

            #endregion

            if (!ReadOnly)
            {
                #region setter

                sb.AppendLine($"        {AggressiveInlining}");
                sb.Append($"        set {{ if (this.Is{@case.Name}) {{ ");
                GenField(false);
                sb.AppendLine($"value; }} }}");

                #endregion
            }

            sb.AppendLine($"    }}");
        }
    }

    private void GenEquatable(string tags_name)
    {
        #region Equals

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public readonly bool Equals({TypeName} other) => this.Tag != other.Tag ? false : this.Tag switch");
        sb.AppendLine($"    {{");
        foreach (var @case in Cases)
        {
            if (@case.Type == "void") continue;
            sb.AppendLine(
                $"        {tags_name}.{@case.Name} => global::System.Collections.Generic.EqualityComparer<{@case.Type}>.Default.Equals(this.{@case.Name}, other.{@case.Name}),");
        }
        sb.AppendLine($"        _ => true,");
        sb.AppendLine($"    }};");

        #endregion

        sb.AppendLine();

        #region HashCode

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine($"    public readonly override int GetHashCode() => this.Tag switch");
        sb.AppendLine($"    {{");
        foreach (var @case in Cases)
        {
            if (@case.Type == "void") continue;
            sb.AppendLine(
                $"        {tags_name}.{@case.Name} => global::System.HashCode.Combine(this.Tag, this.{@case.Name}),");
        }
        sb.AppendLine($"        _ => global::System.HashCode.Combine(this.Tag),");
        sb.AppendLine($"    }};");

        #endregion

        sb.AppendLine();

        #region other

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public readonly override bool Equals(object? obj) => obj is {TypeName} other && Equals(other);");

        sb.AppendLine();

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine($"    public static bool operator ==({TypeName} left, {TypeName} right) => Equals(left, right);");
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public static bool operator !=({TypeName} left, {TypeName} right) => !Equals(left, right);");

        #endregion
    }

    private void GenComparable(string tags_name)
    {
        #region CompareTo

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public readonly int CompareTo({TypeName} other) => this.Tag != other.Tag ? Comparer<{tags_name}>.Default.Compare(this.Tag, other.Tag) : this.Tag switch");
        sb.AppendLine($"    {{");
        foreach (var @case in Cases)
        {
            if (@case.Type == "void") continue;
            sb.AppendLine(
                $"        {tags_name}.{@case.Name} => global::System.Collections.Generic.Comparer<{@case.Type}>.Default.Compare(this.{@case.Name}, other.{@case.Name}),");
        }
        sb.AppendLine($"        _ => 0,");
        sb.AppendLine($"    }};");

        #endregion

        sb.AppendLine();

        #region other

        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public static bool operator <({TypeName} left, {TypeName} right) => left.CompareTo(right) < 0;");
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public static bool operator >({TypeName} left, {TypeName} right) => left.CompareTo(right) > 0;");
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public static bool operator <=({TypeName} left, {TypeName} right) => left.CompareTo(right) <= 0;");
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine(
            $"    public static bool operator >=({TypeName} left, {TypeName} right) => left.CompareTo(right) >= 0;");

        #endregion
    }

    private void GenToStr(string tags_name)
    {
        sb.AppendLine($"    {AggressiveInlining}");
        sb.AppendLine($"    public readonly override string ToString() => this.Tag switch");
        sb.AppendLine($"    {{");
        foreach (var @case in Cases)
        {
            sb.Append(
                $"        {tags_name}.{@case.Name} => $\"{{nameof({TypeName})}}.{{nameof({tags_name}.{@case.Name})}}");
            if (@case.Type != "void") sb.Append($" {{{{ {{this.{@case.Name}}} }}}}");
            sb.AppendLine($"\",");
        }
        sb.AppendLine($"        _ => nameof({TypeName}),");
        sb.AppendLine($"    }};");
    }
}
