// See https://aka.ms/new-console-template for more information
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

if (args.Length != 1)
{
	Console.Error.WriteLine("Usage: dotnet run -- <path-to-csharp-file>");
	return 1;
}

var filePath = Path.GetFullPath(args[0]);
if (!File.Exists(filePath))
{
	Console.Error.WriteLine($"File not found: {filePath}");
	return 1;
}

var originalText = File.ReadAllText(filePath);
var syntaxTree = CSharpSyntaxTree.ParseText(originalText);
var root = syntaxTree.GetCompilationUnitRoot();

var replacements = CollectReplacements(root);

if (replacements.Length == 0)
{
	Console.WriteLine("No block bodies required replacement.");
	return 0;
}

var updated = ApplyReplacements(originalText, replacements);
File.WriteAllText(filePath, updated);
Console.WriteLine($"Updated {replacements.Length} block bodies in {filePath}.");
return 0;

static ImmutableArray<TextReplacement> CollectReplacements(CompilationUnitSyntax root)
{
	var builder = ImmutableArray.CreateBuilder<TextReplacement>();
	foreach (var block in root.DescendantNodes().OfType<BlockSyntax>())
	{
		if (!ShouldRewrite(block))
		{
			continue;
		}

		var interiorStart = block.OpenBraceToken.Span.End;
		var interiorEnd = block.CloseBraceToken.SpanStart;
		if (interiorEnd <= interiorStart)
		{
			continue;
		}

		var replacementText = "/* body removed for skeleton view. */";

		builder.Add(new TextReplacement(interiorStart, interiorEnd, replacementText));
	}

	return builder.ToImmutable();
}

static string ApplyReplacements(string source, ImmutableArray<TextReplacement> replacements)
{
	var ordered = replacements
		.OrderByDescending(r => r.Start)
		.ToArray();

	var updated = new System.Text.StringBuilder(source);
	foreach (var replacement in ordered)
	{
		updated.Remove(replacement.Start, replacement.Length);
		updated.Insert(replacement.Start, replacement.Text);
	}

	return updated.ToString();
}

static bool ShouldRewrite(BlockSyntax block)
{
	return block.Parent switch
	{
		MethodDeclarationSyntax => true,
		ConstructorDeclarationSyntax => true,
		DestructorDeclarationSyntax => true,
		AccessorDeclarationSyntax => true,
		OperatorDeclarationSyntax => true,
		ConversionOperatorDeclarationSyntax => true,
		LocalFunctionStatementSyntax => true,
		_ => false,
	};
}

readonly record struct TextReplacement(int Start, int End, string Text)
{
	public int Length => End - Start;
}
