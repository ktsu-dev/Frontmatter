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
	[DataRow("# draft")]
	[DataRow("~")]
	[DataRow("null")]
	public void AddFrontmatter_EmptyYamlBlock_LeavesTheDocumentUnchanged(string yaml)
	{
		// A block with text in it that yields no properties is left alone, as for any block that cannot be read
		string input = Document(yaml);

		Assert.AreEqual(input, Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "A" }));
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
