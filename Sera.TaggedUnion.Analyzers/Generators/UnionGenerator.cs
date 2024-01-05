using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sera.TaggedUnion.Analyzers.Generators.Templates;
using Sera.TaggedUnion.Analyzers.Resources;
using Sera.TaggedUnion.Analyzers.Utilities;

namespace Sera.TaggedUnion.Analyzers.Generators;

[Generator]
public class UnionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sources = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Sera.TaggedUnion.UnionAttribute",
                static (syntax, _) => syntax is StructDeclarationSyntax or ClassDeclarationSyntax,
                static (ctx, _) =>
                {
                    var diagnostics = new List<Diagnostic>();
                    var attr = ctx.Attributes.First();
                    var union_attr = UnionAttr.FromData(attr, diagnostics);
                    var syntax = (TypeDeclarationSyntax)ctx.TargetNode;
                    var semanticModel = ctx.SemanticModel;
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var rawFullName = symbol.ToDisplayString();
                    var nameWraps = symbol.WrapNames();
                    var nameWrap = symbol.WrapName();
                    var readOnly = symbol.IsReadOnly;
                    var isClass = syntax is ClassDeclarationSyntax;
                    return (syntax, semanticModel, union_attr, readOnly, isClass, rawFullName, nameWraps, nameWrap,
                        AlwaysEq.Create(diagnostics));
                }
            )
            .Combine(context.CompilationProvider)
            .Select(static (input, _) =>
            {
                var ((syntax, semanticModel, union_attr, readOnly, isClass, rawFullName, nameWraps, nameWrap,
                        diagnostics),
                        compilation) =
                    input;
                var nullable = compilation.Options.NullableContextOptions;
                var usings = new HashSet<string>();
                Utils.GetUsings(syntax, usings);
                var genBase = new GenBase(rawFullName, nullable, usings, nameWraps, nameWrap);
                var templates = syntax.Members
                    .Where(static t => t is InterfaceDeclarationSyntax)
                    .Cast<InterfaceDeclarationSyntax>()
                    .Select(t =>
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(t);
                        return (t, symbol);
                    })
                    .Where(i =>
                    {
                        var (_, symbol) = i;
                        return symbol!.GetAttributes().Any(a =>
                            a.AttributeClass?.ToDisplayString() == "Sera.TaggedUnion.UnionTemplateAttribute");
                    })
                    .ToArray();
                if (templates.Length > 1)
                {
                    var desc = Utils.MakeWarning(Strings.Get("Generator.Union.Error.MultiTemplate"));
                    foreach (var t in templates)
                    {
                        diagnostics.Value.Add(Diagnostic.Create(desc, t.t.Identifier.GetLocation()));
                    }
                }
                var cases = new List<UnionCase>();
                var any_generic = false;
                foreach (var (template, _) in templates)
                {
                    foreach (var (member, i) in template.Members.Select((a, b) => (a, b)))
                    {
                        if (member is MethodDeclarationSyntax mds)
                        {
                            var case_name = mds.Identifier.ToString();
                            var tag = $"{i + 1}";
                            var ret_type = mds.ReturnType.ToString();
                            var kind = UnionCaseTypeKind.None;
                            var member_symbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(mds)!;
                            var tag_attr = member_symbol.GetAttributes().FirstOrDefault(a =>
                                a.AttributeClass?.ToDisplayString() == "Sera.TaggedUnion.UnionTagAttribute");
                            if (tag_attr != null)
                            {
                                tag = tag_attr.ConstructorArguments.First().Value?.ToString() ?? tag;
                            }
                            var ret_type_symbol = member_symbol.ReturnType;
                            var is_generic = ret_type_symbol.IsNotInstGenericType();
                            if (is_generic) any_generic = true;
                            if (ret_type_symbol.IsUnmanagedType) kind = UnionCaseTypeKind.Unmanaged;
                            else if (ret_type_symbol.IsReferenceType) kind = UnionCaseTypeKind.Class;
                            if (is_generic)
                            {
                                if (!ret_type_symbol.IsReferenceType) kind = UnionCaseTypeKind.None;
                            }
                            var symbol_attr = member_symbol.GetAttributes().FirstOrDefault(a =>
                                a.AttributeClass?.ToDisplayString() == "Sera.TaggedUnion.UnionSymbolAttribute");
                            if (symbol_attr == null)
                            {
                                symbol_attr = ret_type_symbol.GetAttributes().FirstOrDefault(a =>
                                    a.AttributeClass?.ToDisplayString() == "Sera.TaggedUnion.UnionSymbolAttribute");
                            }
                            if (symbol_attr != null)
                            {
                                var args = symbol_attr.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
                                var symbol_attr_IsUnmanagedType = MayBool.None;
                                var symbol_attr_IsReferenceType = MayBool.None;
                                if (args.TryGetValue("IsUnmanagedType", out var _symbol_attr_IsUnmanagedType))
                                    symbol_attr_IsUnmanagedType = (MayBool)(byte)_symbol_attr_IsUnmanagedType.Value!;
                                else if (args.TryGetValue("IsReferenceType", out var _symbol_attr_IsReferenceType))
                                    symbol_attr_IsReferenceType = (MayBool)(byte)_symbol_attr_IsReferenceType.Value!;
                                if (symbol_attr_IsUnmanagedType is MayBool.True) kind = UnionCaseTypeKind.Unmanaged;
                                else if (symbol_attr_IsReferenceType is MayBool.True) kind = UnionCaseTypeKind.Class;
                                if (is_generic)
                                {
                                    if (symbol_attr_IsReferenceType is MayBool.False) kind = UnionCaseTypeKind.None;
                                }
                                if (symbol_attr_IsUnmanagedType is MayBool.False && kind is UnionCaseTypeKind.Unmanaged)
                                    kind = UnionCaseTypeKind.None;
                                if (symbol_attr_IsReferenceType is MayBool.False && kind is UnionCaseTypeKind.Class)
                                    kind = UnionCaseTypeKind.None;
                            }
                            if (symbol_attr == null)
                            {
                                if (ret_type_symbol is not ITypeParameterSymbol &&
                                    SymbolEqualityComparer.Default.Equals(
                                        ret_type_symbol.OriginalDefinition.ContainingAssembly,
                                        compilation.Assembly))
                                {
                                    var desc = Utils.MakeInfo(
                                        Strings.Get("Generator.Union.Info.PossiblyInvalidSymbol"));
                                    diagnostics.Value.Add(Diagnostic.Create(desc, member.GetLocation()));
                                }
                            }
                            cases.Add(new UnionCase(case_name, tag, ret_type, kind, is_generic));
                        }
                        else
                        {
                            var desc = Utils.MakeWarning(Strings.Get("Generator.Union.Error.IllegalTemplateMember"));
                            if (member is BaseTypeDeclarationSyntax bts)
                            {
                                diagnostics.Value.Add(Diagnostic.Create(desc, bts.Identifier.GetLocation()));
                            }
                            else
                            {
                                diagnostics.Value.Add(Diagnostic.Create(desc, member.GetLocation()));
                            }
                        }
                    }
                }
                var name = syntax.Identifier.ToString();
                return (name, union_attr, readOnly, isClass, cases, any_generic, genBase, diagnostics);
            });

        context.RegisterSourceOutput(sources, static (ctx, input) =>
        {
            var (name, union_attr, readOnly, isClass, cases, any_generic, genBase, diagnostics) = input;
            if (diagnostics.Value.Count > 0)
            {
                foreach (var diagnostic in diagnostics.Value)
                {
                    ctx.ReportDiagnostic(diagnostic);
                }
            }
            var code = new TemplateStructUnion(genBase, name, union_attr, readOnly, isClass, cases, any_generic).Gen();
            var sourceText = SourceText.From(code, Encoding.UTF8);
            var rawSourceFileName = genBase.FileFullName;
            var sourceFileName = $"{rawSourceFileName}.union.g.cs";
            ctx.AddSource(sourceFileName, sourceText);
        });
    }
}
