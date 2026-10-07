// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #189: a document with a header and no body must not gain a blank line after the
/// closing delimiter, and removing its header must leave an empty document.
/// </summary>
[TestClass]
public class HeaderOnlyDocumentTests
{
	private static string Lines(string newLine, params string[] lines) => string.Join(newLine, lines) + newLine;

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void AddFrontmatter_HeaderOnlyDocument_EndsAtTheClosingDelimiter(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---");

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["k"] = "v" });

		Assert.AreEqual(Lines(nl, "---", "title: A", "k: v", "---"), result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void ReplaceFrontmatter_HeaderOnlyDocument_EndsAtTheClosingDelimiter(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---");

		string result = Frontmatter.ReplaceFrontmatter(input, new Dictionary<string, object> { ["k"] = "v" });

		Assert.AreEqual(Lines(nl, "---", "k: v", "---"), result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void RemoveFrontmatter_HeaderOnlyDocument_ReturnsAnEmptyDocument(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---");

		Assert.AreEqual(string.Empty, Frontmatter.RemoveFrontmatter(input));
	}

	[TestMethod]
	public void AddFrontmatter_EmptyInput_EndsAtTheClosingDelimiter()
	{
		string nl = Environment.NewLine;

		string result = Frontmatter.AddFrontmatter(string.Empty, new Dictionary<string, object> { ["k"] = "v" });

		Assert.AreEqual(Lines(nl, "---", "k: v", "---"), result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_EmptyInput_EndsAtTheClosingDelimiter()
	{
		string nl = Environment.NewLine;

		string result = Frontmatter.ReplaceFrontmatter(string.Empty, new Dictionary<string, object> { ["k"] = "v" });

		Assert.AreEqual(Lines(nl, "---", "k: v", "---"), result);
	}

	[TestMethod]
	public void RemoveFrontmatter_EmptyInput_ReturnsAnEmptyDocument() =>
		Assert.AreEqual(string.Empty, Frontmatter.RemoveFrontmatter(string.Empty));

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	[DataRow("\r", DisplayName = "CR")]
	public void AddAndReplaceFrontmatter_RunAgainOnTheirOwnOutput_ReturnItUnchanged(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---");
		Dictionary<string, object> properties = new() { ["title"] = "A" };

		string added = Frontmatter.AddFrontmatter(input, properties);
		string replaced = Frontmatter.ReplaceFrontmatter(input, properties);

		Assert.AreEqual(added, Frontmatter.AddFrontmatter(added, properties));
		Assert.AreEqual(replaced, Frontmatter.ReplaceFrontmatter(replaced, properties));
	}
}
