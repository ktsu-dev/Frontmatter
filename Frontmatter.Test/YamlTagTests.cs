// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #243: a header holding a YAML tag, such as R Markdown's <c>!r</c>, can be read, and
/// the rebuild paths leave the document alone rather than silently dropping the tag.
/// </summary>
[TestClass]
public class YamlTagTests
{
	private const string RMarkdown = "---\ntitle: \"Sales report\"\noutput: html_document\nparams:\n  date: !r Sys.Date()\n---\nBody\n";

	[TestMethod]
	public void ExtractFrontmatter_RMarkdownTag_ReadsTheHeader()
	{
		Dictionary<string, object>? header = Frontmatter.ExtractFrontmatter(RMarkdown);

		Assert.IsNotNull(header);
		Assert.AreEqual("Sales report", header["title"]);
		Assert.AreEqual("html_document", header["output"]);
	}

	[TestMethod]
	public void AddFrontmatter_RMarkdownTag_InsertsTheKeyAndKeepsTheTag()
	{
		string result = Frontmatter.AddFrontmatter(RMarkdown, new() { ["author"] = "Ana" });

		Assert.AreEqual("---\ntitle: \"Sales report\"\noutput: html_document\nparams:\n  date: !r Sys.Date()\nauthor: Ana\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void ExtractFrontmatter_SeqTag_ReadsAList()
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter("---\ntitle: T\ntags: !!seq [a, b]\n---\nBody\n")!;

		CollectionAssert.AreEqual(new object[] { "a", "b" }, ((IEnumerable<object>)header["tags"]).ToArray());
	}

	[TestMethod]
	public void ExtractFrontmatter_NullTag_ReadsANull()
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter("---\ntitle: T\nk: !!null ''\n---\nBody\n")!;

		Assert.IsTrue(header.ContainsKey("k"));
		Assert.IsNull(header["k"]);
	}

	[TestMethod]
	[DataRow("!!str 3", "3")]
	[DataRow("!!str no", "no")]
	[DataRow("! 12", "12")]
	public void ExtractFrontmatter_StringTag_ReadsAString(string written, string expected)
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter($"---\nk: {written}\n---\nBody\n")!;

		Assert.AreEqual(expected, header["k"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_IntAndBoolTags_ReadTheirValues()
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter("---\nn: !!int \"3\"\nb: !!bool true\nm: !!map {a: 1}\n---\nBody\n")!;

		Assert.AreEqual(3, Convert.ToInt32(header["n"], System.Globalization.CultureInfo.InvariantCulture));
		Assert.IsTrue((bool)header["b"]);
		Assert.IsInstanceOfType<IDictionary<string, object>>(header["m"]);
	}

	[TestMethod]
	[DataRow("!expr x + 1")]
	[DataRow("!!binary aGVsbG8=")]
	[DataRow("!!set {a, b}")]
	[DataRow("!!omap [a: 1, b: 2]")]
	[DataRow("!local value")]
	public void ExtractFrontmatter_OtherTags_ReadTheHeader(string written)
	{
		Dictionary<string, object>? header = Frontmatter.ExtractFrontmatter($"---\ntitle: T\nk: {written}\n---\nBody\n");

		Assert.IsNotNull(header);
		Assert.AreEqual("T", header["title"]);
		Assert.IsTrue(header.ContainsKey("k"));
	}

	[TestMethod]
	public void ReplaceFrontmatter_CustomTag_LeavesTheDocumentAlone()
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter(RMarkdown)!;
		header["title"] = "Other";

		Assert.AreEqual(RMarkdown, Frontmatter.ReplaceFrontmatter(RMarkdown, header));
	}

	[TestMethod]
	public void ReplaceFrontmatter_CoreTagsOnly_RewritesTheHeader()
	{
		const string doc = "---\ntitle: T\ntags: !!seq [a, b]\n---\nBody\n";

		string result = Frontmatter.ReplaceFrontmatter(doc, new() { ["title"] = "Other" });

		Assert.AreEqual("---\ntitle: Other\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void CombineFrontmatter_CustomTag_LeavesTheDocumentAlone()
	{
		const string doc = "---\ntitle: T\n---\n---\ndate: !r Sys.Date()\n---\nBody\n";

		Assert.AreEqual(doc, Frontmatter.CombineFrontmatter(doc));
	}

	[TestMethod]
	public void AddFrontmatter_CustomTagInAFlowHeader_LeavesTheDocumentAlone()
	{
		// A flow-mapping header can't take the insert path, so it would be rebuilt and lose the tag.
		const string doc = "---\n{title: T, date: !r Sys.Date()}\n---\nBody\n";

		Assert.AreEqual(doc, Frontmatter.AddFrontmatter(doc, new() { ["author"] = "Ana" }));
	}
}
