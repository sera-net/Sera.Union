﻿using System.Collections.Generic;
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
public class StructTemplateUnionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sources = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Sera.TaggedUnion.UnionAttribute",
                static (syntax, _) => syntax is StructDeclarationSyntax,
                static (ctx, _) =>
                {
                    var diagnostics = new List<Diagnostic>();
                    var attr = ctx.Attributes.First();
                    var union_attr = UnionAttr.FromData(attr, diagnostics);
                    var syntax = (StructDeclarationSyntax)ctx.TargetNode;
                    var semanticModel = ctx.SemanticModel;
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var rawFullName = symbol.ToDisplayString();
                    var nameWraps = symbol.WrapNames();
                    var nameWrap = symbol.WrapName();
                    var readOnly = symbol.IsReadOnly;
                    return (syntax, semanticModel, union_attr, readOnly, rawFullName, nameWraps, nameWrap,
                        AlwaysEq.Create(diagnostics));
                }
            )
            .Combine(context.CompilationProvider)
            .Select(static (input, _) =>
            {
                var ((syntax, semanticModel, union_attr, readOnly, rawFullName, nameWraps, nameWrap, diagnostics),
                        compilation) =
                    input;
                var nullable = compilation.Options.NullableContextOptions;
                var usings = new HashSet<string>();
                Utils.GetUsings(syntax, usings);
                var genBase = new GenBase(rawFullName, nullable, usings, nameWraps, nameWrap);
                var templates = syntax.Members
                    .Where(static t => t is ClassDeclarationSyntax or RecordDeclarationSyntax)
                    .Cast<TypeDeclarationSyntax>()
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
                foreach (var (template, template_symbol) in templates)
                {
                    if (template is RecordDeclarationSyntax)
                    {
                        var desc = Utils.MakeWarning(Strings.Get("Generator.Union.Error.Record"));
                        diagnostics.Value.Add(Diagnostic.Create(desc, template.Identifier.GetLocation()));
                        continue;
                    }
                    if (!template_symbol!.IsAbstract)
                    {
                        var desc = Utils.MakeWarning(Strings.Get("Generator.Union.Error.MustAbstract"));
                        diagnostics.Value.Add(Diagnostic.Create(desc, template.Identifier.GetLocation()));
                    }
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
                            if (ret_type_symbol.IsUnmanagedType) kind = UnionCaseTypeKind.Unmanaged;
                            else if (ret_type_symbol.IsReferenceType) kind = UnionCaseTypeKind.Class;
                            cases.Add(new UnionCase(case_name, tag, ret_type, kind));
                        }
                        else
                        {
                            var desc = Utils.MakeWarning(Strings.Get(" Generator.Union.Error.IllegalTemplateMember"));
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
                return (name, union_attr, readOnly, cases, genBase, diagnostics);
            });

        context.RegisterSourceOutput(sources, static (ctx, input) =>
        {
            var (name, union_attr, readOnly, cases, genBase, diagnostics) = input;
            if (diagnostics.Value.Count > 0)
            {
                foreach (var diagnostic in diagnostics.Value)
                {
                    ctx.ReportDiagnostic(diagnostic);
                }
            }
            var code = new TemplateStructUnion(genBase, name, union_attr, readOnly, cases).Gen();
            var sourceText = SourceText.From(code, Encoding.UTF8);
            var rawSourceFileName = genBase.FileFullName;
            var sourceFileName = $"{rawSourceFileName}.union.g.cs";
            ctx.AddSource(sourceFileName, sourceText);
        });
    }
}