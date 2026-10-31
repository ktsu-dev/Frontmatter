// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #210: a body that opens with a <c>---</c> … <c>---</c> passage, set off from the
/// header by a blank line, must stay in the body when the header is rewritten, rather than being written
/// directly under the header and read back as another frontmatter block.
/// </summary>
[TestClass]
public class BodyOpeningRuleTests
{
	private static string Lines(string newLine, params string[] lines) => string.Join(newLine, lines) + newLine;

	private static string WithHeader(string nl) =>
		Lines(nl, "---", "title: My post", "---", string.Empty, "---", "Note: this post is archived.", "---", string.Empty, "Hello world.");

	private static string WithoutHeader(string nl) =>
		Lines(nl, string.Empty, "---", "Note: this post is archived.", "---", string.Empty, "Hello world.");

	private static Dictionary<string, object> Tags() => new() { ["tags"] = new List<object> { "a" } };

	private static Dictionary<string, object> NewTitle() => new() { ["title"] = "New" };

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyOpensWithARule_KeepsTheBody(string nl)
	{
		string input = WithHeader(nl);

		string result = Frontmatter.AddFrontmatter(input, Tags());

		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
		Dictionary<string, object>? properties = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(properties);
		Assert.IsFalse(properties.ContainsKey("Note"));
		Assert.IsTrue(properties.ContainsKey("tags"));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_NoHeaderAndBodyOpensWithARule_KeepsTheBody(string nl)
	{
		string input = WithoutHeader(nl);

		string result = Frontmatter.AddFrontmatter(input, Tags());

		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
		Dictionary<string, object>? properties = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(properties);
		Assert.IsFalse(properties.ContainsKey("Note"));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void ReplaceFrontmatter_BodyOpensWithARule_KeepsTheBody(string nl)
	{
		string input = WithHeader(nl);

		string result = Frontmatter.ReplaceFrontmatter(input, NewTitle());

		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
		Dictionary<string, object>? properties = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(properties);
		Assert.IsFalse(properties.ContainsKey("Note"));
		Assert.AreEqual("New", properties["title"]);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void ReplaceFrontmatter_NoHeaderAndBodyOpensWithARule_KeepsTheBody(string nl)
	{
		string input = WithoutHeader(nl);

		string result = Frontmatter.ReplaceFrontmatter(input, NewTitle());

		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void RemoveFrontmatter_BodyOpensWithARule_LeavesNoHeader(string nl)
	{
		string input = WithHeader(nl);

		string result = Frontmatter.RemoveFrontmatter(input);

		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.AreEqual(WithoutHeader(nl), result);
		Assert.AreEqual(result, Frontmatter.RemoveFrontmatter(result));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void ReplaceFrontmatter_WithNoProperties_BodyOpensWithARule_LeavesNoHeader(string nl)
	{
		string result = Frontmatter.ReplaceFrontmatter(WithHeader(nl), []);

		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.AreEqual(WithoutHeader(nl), result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void RemoveFrontmatter_BodyOpensWithANonYamlRulePassage_KeepsTheWholeBody(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", string.Empty, "---", string.Empty, "Intro paragraph.", string.Empty, "---", string.Empty, "More text.");

		string result = Frontmatter.RemoveFrontmatter(input);

		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.AreEqual(Frontmatter.ExtractBody(input), Frontmatter.ExtractBody(result));
		Dictionary<string, object> added = Frontmatter.ExtractFrontmatter(Frontmatter.AddFrontmatter(result, NewTitle())) ?? [];
		Assert.AreEqual("New", added["title"]);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void RemoveFrontmatter_BodyWithoutALeadingRule_HasNoLeadingBlankLine(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", string.Empty, "Hello", string.Empty, "---", "after a rule");

		Assert.AreEqual(Lines(nl, "Hello", string.Empty, "---", "after a rule"), Frontmatter.RemoveFrontmatter(input));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyOpensWithARule_WritesOneBlankLineBeforeIt(string nl)
	{
		string result = Frontmatter.AddFrontmatter(WithHeader(nl), Tags());

		Assert.AreEqual(
			Lines(nl, "---", "title: My post", "tags:", "- a", "---", string.Empty, "---", "Note: this post is archived.", "---", string.Empty, "Hello world."),
			result);
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyOpensWithARule_IsIdempotent(string nl)
	{
		string once = Frontmatter.AddFrontmatter(WithHeader(nl), Tags());

		Assert.AreEqual(once, Frontmatter.AddFrontmatter(once, Tags()));
	}

	[TestMethod]
	[DataRow("\r\n", DisplayName = "CRLF")]
	[DataRow("\n", DisplayName = "LF")]
	public void AddFrontmatter_BodyWithoutALeadingRule_IsWrittenDirectlyUnderTheHeader(string nl)
	{
		string input = Lines(nl, "---", "title: A", "---", string.Empty, "Hello", string.Empty, "---", "after a rule");

		string result = Frontmatter.AddFrontmatter(input, Tags());

		Assert.AreEqual(Lines(nl, "---", "title: A", "tags:", "- a", "---", "Hello", string.Empty, "---", "after a rule"), result);
	}
}
