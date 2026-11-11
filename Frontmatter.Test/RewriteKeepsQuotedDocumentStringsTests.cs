// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #239: rewriting a header keeps a string the header already held quoted as a quoted
/// string, so a YAML 1.1 reader doesn't turn <c>published: 'no'</c> into <c>false</c> or <c>when: '12:30'</c>
/// into <c>750</c> after an unrelated edit. A value the header held plain stays plain, as #233 requires.
/// </summary>
[TestClass]
public class RewriteKeepsQuotedDocumentStringsTests
{
	private const string Document = "---\ntitle: Hello\npublished: 'no'\nwhen: '12:30'\ndraft: no\n---\nBody\n";

	[TestMethod]
	public void ReplaceFrontmatter_AfterChangingAnotherKey_KeepsQuotedStringsQuotedAndPlainScalarsPlain()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		frontmatter["title"] = "Changed";

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Changed\npublished: \"no\"\nwhen: \"12:30\"\ndraft: no\n---\nBody\n", result);
	}

	[TestMethod]
	[DataRow("'no'", "\"no\"")]
	[DataRow("'yes'", "\"yes\"")]
	[DataRow("'on'", "\"on\"")]
	[DataRow("'12:30'", "\"12:30\"")]
	[DataRow("\"1e3\"", "\"1e3\"")]
	[DataRow("'<<'", "\"<<\"")]
	public void ReplaceFrontmatter_QuotedYaml11Lookalike_StaysQuoted(string written, string expected)
	{
		string input = $"---\ntitle: Hello\nvalue: {written}\n---\nBody\n";
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(input)!;
		frontmatter["title"] = "Changed";

		string result = Frontmatter.ReplaceFrontmatter(input, frontmatter);

		Assert.AreEqual($"---\ntitle: Changed\nvalue: {expected}\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_QuotedStringTheCallerChanged_IsWrittenAsACallerValue()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		frontmatter["published"] = "later";

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Hello\npublished: later\nwhen: \"12:30\"\ndraft: no\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_StackedHeader_KeepsQuotedStringsQuotedAndPlainScalarsPlain()
	{
		string input = "---\ntitle: T\ndraft: 'yes'\npublished: no\n---\n---\nauthor: A\nwhen: '12:30'\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["tags"] = "x" });

		Assert.AreEqual("---\ntitle: T\ndraft: \"yes\"\npublished: no\nauthor: A\nwhen: \"12:30\"\ntags: x\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_StackedHeader_RepeatedKeyTakesItsQuotingFromTheBlockThatWins()
	{
		string input = "---\ntitle: T\npublished: no\n---\n---\npublished: 'no'\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["tags"] = "x" });

		Assert.AreEqual("---\ntitle: T\npublished: no\ntags: x\n---\nBody\n", result);
	}

	[TestMethod]
	public void CombineFrontmatter_AsIs_KeepsQuotedStringsQuotedAndPlainScalarsPlain()
	{
		string input = "---\ntitle: Hello\npublished: 'no'\n---\n---\nwhen: '12:30'\ndraft: no\n---\nBody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual("---\ntitle: Hello\npublished: \"no\"\nwhen: \"12:30\"\ndraft: no\n---\nBody\n", result);
	}

	[TestMethod]
	public void CombineFrontmatter_Defaults_KeepsQuotedStringsQuotedAndPlainScalarsPlain()
	{
		string input = "---\ntitle: Hello\npublished: 'no'\ndraft: no\n---\nBody\n";

		string result = Frontmatter.CombineFrontmatter(input);

		Assert.Contains("published: \"no\"\n", result);
		Assert.Contains("draft: no\n", result);
	}

	[TestMethod]
	public void CombineFrontmatter_RenamedKey_KeepsItsQuotedStringQuoted()
	{
		string input = "---\ntitle: Hello\ncreator: 'yes'\n---\nBody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual("---\ntitle: Hello\nauthor: \"yes\"\n---\nBody\n", result);
	}
}
