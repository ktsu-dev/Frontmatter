// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #170: adding keys to a document that already has a single header keeps the
/// header's comments and formatting as written, and only inserts the new keys.
/// </summary>
[TestClass]
public class AddKeepsHeaderTextTests
{
	private static string Lines(string newLine, params string[] lines) => string.Join(newLine, lines) + newLine;

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void AddFrontmatter_HeaderWithComments_KeepsThemAndAddsTheKey(string nl)
	{
		string input = Lines(nl, "---", "# Draft: do not publish until legal signs off", "title: Launch # working title", "tags: [a, b]", "---", "Body");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual(
			Lines(nl, "---", "# Draft: do not publish until legal signs off", "title: Launch # working title", "tags: [a, b]", "author: Jane", "---", "Body"),
			result);
	}

	[TestMethod]
	public void AddFrontmatter_SomeKeysAlreadyPresent_InsertsOnlyTheNewOnesAndKeepsTheExistingValues()
	{
		string input = "---\ntitle: Launch # keep me\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "Other", ["author"] = "Jane" });

		Assert.AreEqual("---\ntitle: Launch # keep me\nauthor: Jane\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_ListValue_IsInsertedAsYaml()
	{
		string input = "---\n# note\ntitle: A\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["tags"] = new List<object> { "x", "y" } });

		Assert.AreEqual("---\n# note\ntitle: A\ntags:\n- x\n- y\n---\nBody\n", result);
		Dictionary<string, object>? properties = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(properties);
		Assert.AreEqual("A", properties["title"]);
		Assert.AreSequenceEqual(new List<object> { "x", "y" }, (List<object>)properties["tags"]);
	}

	[TestMethod]
	public void AddFrontmatter_BlockScalarBeforeTheLastKey_KeepsItAsWritten()
	{
		string input = "---\ndescription: |\n  line one\n  line two\ntitle: A\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual("---\ndescription: |\n  line one\n  line two\ntitle: A\nauthor: Jane\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_OpeningDelimiterHasABomAndTrailingSpace_KeepsTheCommentsAndNormalizesTheDelimiter()
	{
		string input = "\uFEFF--- \ntitle: A # c\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual("---\ntitle: A # c\nauthor: Jane\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_FlowMappingHeader_IsRebuiltSoTheResultStillParses()
	{
		string input = "---\n{title: A}\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Dictionary<string, object>? properties = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(properties);
		Assert.AreEqual("A", properties["title"]);
		Assert.AreEqual("Jane", properties["author"]);
		Assert.AreEqual("Body", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_DocumentEndMarkerClosesTheHeader_KeepsIt()
	{
		string input = "---\ntitle: A # c\n...\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual("---\ntitle: A # c\nauthor: Jane\n...\nBody\n", result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyIsWrittenAsARebuiltHeaderWritesIt(string nl)
	{
		string input = Lines(nl, "---", "title: A # c", "---", string.Empty, "Hello", "  ");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual(Lines(nl, "---", "title: A # c", "author: Jane", "---", "Hello"), result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyOpensWithARule_KeepsTheCommentsAndTheBlankLineBeforeTheRule(string nl)
	{
		string input = Lines(nl, "---", "title: A # c", "---", string.Empty, "---", "Note: archived", "---", string.Empty, "Hello");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "Jane" });

		Assert.AreEqual(
			Lines(nl, "---", "title: A # c", "author: Jane", "---", string.Empty, "---", "Note: archived", "---", string.Empty, "Hello"),
			result);
		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_StackedBlocks_AreStillFoldedIntoOne()
	{
		string input = "---\ntitle: A\n---\n---\nauthor: B\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["date"] = "2026-01-01" });

		Assert.AreEqual("---\ntitle: A\nauthor: B\ndate: 2026-01-01\n---\nBody\n", result);
	}
}
