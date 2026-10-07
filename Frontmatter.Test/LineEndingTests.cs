// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that every writer emits the input document's line ending, whatever the host's is.
/// </summary>
[TestClass]
public class LineEndingTests
{
	private static string Lines(string newLine, params string[] lines) => string.Join(newLine, lines) + newLine;

	private static void AssertOnlyLineEnding(string newLine, string text)
	{
		string withoutLineEndings = text.Replace(newLine, string.Empty, StringComparison.Ordinal);
		Assert.DoesNotContain("\r", withoutLineEndings);
		Assert.DoesNotContain("\n", withoutLineEndings);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void CombineFrontmatter_KeepsTheInputLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", "---", "author: B", "---", "line1", "line2");

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual(Lines(nl, "---", "title: A", "author: B", "---", "line1", "line2"), result);
		AssertOnlyLineEnding(nl, result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_DocumentWithoutHeader_KeepsTheInputLineEnding(string nl)
	{
		string input = Lines(nl, "line1", "line2");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "T", ["author"] = "B" });

		Assert.AreEqual(Lines(nl, "---", "title: T", "author: B", "---", "line1", "line2"), result);
		AssertOnlyLineEnding(nl, result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_DocumentWithHeader_KeepsTheInputLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", "line1", "line2");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });

		Assert.AreEqual(Lines(nl, "---", "title: A", "author: B", "---", "line1", "line2"), result);
		AssertOnlyLineEnding(nl, result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void ReplaceFrontmatter_KeepsTheInputLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", "line1", "line2");

		string result = Frontmatter.ReplaceFrontmatter(input, new Dictionary<string, object> { ["title"] = "B", ["tags"] = new List<object> { "x", "y" } });

		Assert.StartsWith($"---{nl}title: B{nl}tags:{nl}", result);
		Assert.EndsWith($"{nl}---{nl}line1{nl}line2{nl}", result);
		AssertOnlyLineEnding(nl, result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void RemoveFrontmatter_KeepsTheInputLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", "line1", "line2");

		string result = Frontmatter.RemoveFrontmatter(input);

		Assert.AreEqual(Lines(nl, "line1", "line2"), result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void ExtractBody_DropsBlankLinesAfterTheHeader_ForEveryLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: x", "---", string.Empty, string.Empty, "Body");

		Assert.AreEqual("Body", Frontmatter.ExtractBody(input));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void RemoveFrontmatter_DropsBlankLinesAfterTheHeader_ForEveryLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: x", "---", string.Empty, string.Empty, "Body");

		Assert.AreEqual(Lines(nl, "Body"), Frontmatter.RemoveFrontmatter(input));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void ReplaceFrontmatter_DropsBlankLinesAfterTheHeader_ForEveryLineEnding(string nl)
	{
		string input = Lines(nl, "---", "title: x", "---", string.Empty, string.Empty, "Body");

		string result = Frontmatter.ReplaceFrontmatter(input, new Dictionary<string, object> { ["title"] = "y" });

		Assert.AreEqual(Lines(nl, "---", "title: y", "---", "Body"), result);
	}
}
