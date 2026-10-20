// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that a frontmatter block whose YAML has no content (a comment only, <c>~</c> or <c>null</c>)
/// is treated as an empty header rather than throwing.
/// </summary>
[TestClass]
public class EmptyYamlBlockTests
{
	private static readonly string Nl = Environment.NewLine;

	private static string Document(string yaml) => $"---{Nl}{yaml}{Nl}---{Nl}Body text{Nl}";

	[TestMethod]
	[DataRow("# nothing here yet")]
	[DataRow("~")]
	[DataRow("null")]
	public void ExtractBody_EmptyYamlBlock_ReturnsBody(string yaml) =>
		Assert.AreEqual("Body text", Frontmatter.ExtractBody(Document(yaml)));

	[TestMethod]
	[DataRow("# draft")]
	[DataRow("~")]
	[DataRow("null")]
	public void ExtractFrontmatter_EmptyYamlBlock_ReturnsNull(string yaml) =>
		Assert.IsNull(Frontmatter.ExtractFrontmatter(Document(yaml)));

	[TestMethod]
	[DataRow("# draft")]
	[DataRow("~")]
	[DataRow("null")]
	public void RemoveFrontmatter_EmptyYamlBlock_ReturnsBody(string yaml) =>
		Assert.AreEqual($"Body text{Nl}", Frontmatter.RemoveFrontmatter(Document(yaml)));

	[TestMethod]
	[DataRow("# draft")]
	[DataRow("~")]
	[DataRow("null")]
	public void CombineFrontmatter_EmptyYamlBlock_DoesNotThrow(string yaml)
	{
		string result = Frontmatter.CombineFrontmatter(Document(yaml));

		Assert.Contains("Body text", result);
	}

	[TestMethod]
	[DataRow("# TODO: fill in")]
	[DataRow("~")]
	[DataRow("null")]
	[DataRow("{}")]
	public void AddFrontmatter_EmptyYamlBlock_AddsTheProperties(string yaml)
	{
		// A header that parses but holds no keys is empty, not unreadable, so the new properties fill it
		string result = Frontmatter.AddFrontmatter(Document(yaml), new Dictionary<string, object> { ["title"] = "Hello" });

		Assert.AreEqual($"---{Nl}title: Hello{Nl}---{Nl}Body text{Nl}", result);
	}

	[TestMethod]
	[DataRow("# c")]
	[DataRow("~")]
	[DataRow("{}")]
	public void CombineFrontmatter_EmptyBlockStackedOnARealOne_CombinesThem(string yaml)
	{
		string input = $"---{Nl}{yaml}{Nl}---{Nl}---{Nl}title: a{Nl}---{Nl}Body text{Nl}";

		Assert.AreEqual($"---{Nl}title: a{Nl}---{Nl}Body text{Nl}", Frontmatter.CombineFrontmatter(input));
	}

	[TestMethod]
	[DataRow("key: [unclosed")]
	[DataRow("just a scalar")]
	[DataRow("- a list")]
	public void AddFrontmatter_UnreadableBlock_LeavesTheDocumentUnchanged(string yaml)
	{
		// A block that fails to parse, or whose root is not a mapping, is still left alone rather than overwritten
		string input = Document(yaml);

		Assert.AreEqual(input, Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "Hello" }));
	}

	[TestMethod]
	[DataRow("# draft")]
	[DataRow("~")]
	[DataRow("null")]
	public void TryParseYamlObject_EmptyYamlDocument_ReturnsFalse(string yaml)
	{
		Assert.IsFalse(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? result));
		Assert.IsNull(result);
	}
}
