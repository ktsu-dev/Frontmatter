// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that rewriting a document keeps the indentation of the body's first content line, so a body that
/// opens with an indented code block is not turned into a prose paragraph.
/// </summary>
[TestClass]
public class BodyIndentationTests
{
	private static readonly string Nl = Environment.NewLine;

	private static readonly string IndentedBody = $"    int x = 1;{Nl}    int y = 2;{Nl}{Nl}Text";

	private static string Document() => $"---{Nl}title: T{Nl}---{Nl}{IndentedBody}{Nl}";

	[TestMethod]
	public void ExtractBody_IndentedCodeBlockFirst_KeepsIndentation() =>
		Assert.AreEqual(IndentedBody, Frontmatter.ExtractBody(Document()));

	[TestMethod]
	public void RemoveFrontmatter_IndentedCodeBlockFirst_KeepsIndentation() =>
		Assert.AreEqual($"{IndentedBody}{Nl}", Frontmatter.RemoveFrontmatter(Document()));

	[TestMethod]
	public void ReplaceFrontmatter_IndentedCodeBlockFirst_KeepsIndentation()
	{
		string result = Frontmatter.ReplaceFrontmatter(Document(), new Dictionary<string, object> { ["title"] = "U" });

		Assert.AreEqual(IndentedBody, Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_ExistingFrontmatterAndIndentedCodeBlockFirst_KeepsIndentation()
	{
		string result = Frontmatter.AddFrontmatter(Document(), new Dictionary<string, object> { ["date"] = 2024 });

		Assert.AreEqual(IndentedBody, Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_NoFrontmatterAndIndentedCodeBlockFirst_KeepsIndentation()
	{
		string result = Frontmatter.AddFrontmatter($"    code{Nl}{Nl}Text{Nl}", new Dictionary<string, object> { ["title"] = "U" });

		Assert.AreEqual($"    code{Nl}{Nl}Text", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void ExtractBody_TabIndentedFirstLineAfterBlankLines_DropsOnlyTheBlankLines()
	{
		string input = $"---{Nl}title: T{Nl}---{Nl}{Nl}  {Nl}\tindented{Nl}{Nl}";

		Assert.AreEqual("\tindented", Frontmatter.ExtractBody(input));
	}

	[TestMethod]
	public void ExtractBody_WhitespaceOnlyBody_ReturnsEmpty() =>
		Assert.AreEqual(string.Empty, Frontmatter.ExtractBody($"---{Nl}title: T{Nl}---{Nl}  {Nl}\t{Nl}"));
}
