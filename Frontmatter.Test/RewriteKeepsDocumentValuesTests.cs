// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #233: rewriting a header writes the values it already held as they were read from
/// the document, so YAML 1.1 quoting, meant for caller values, doesn't turn <c>published: no</c> into the
/// truthy string <c>"no"</c>.
/// </summary>
[TestClass]
public class RewriteKeepsDocumentValuesTests
{
	private const string Document = "---\ntitle: Draft post\npublished: no\nlast_seen: 12:30\n---\n# Body\n";

	[TestMethod]
	public void ReplaceFrontmatter_AfterChangingAnotherKey_KeepsExistingPlainScalars()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		frontmatter["title"] = "Final post";

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Final post\npublished: no\nlast_seen: 12:30\n---\n# Body\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_CallerChangesAValueToAYaml11Lookalike_StillQuotesIt()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		frontmatter["title"] = "no";
		frontmatter["status"] = "on";

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: \"no\"\npublished: no\nlast_seen: 12:30\nstatus: \"on\"\n---\n# Body\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_DocumentWithoutHeader_StillQuotesCallerValues()
	{
		string result = Frontmatter.ReplaceFrontmatter("# Body\n", new Dictionary<string, object> { ["published"] = "no" });

		Assert.AreEqual("---\npublished: \"no\"\n---\n# Body\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_StackedHeader_KeepsExistingPlainScalars()
	{
		string input = "---\ntitle: T\npublished: no\n---\n---\nauthor: A\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["tags"] = "x" });

		Assert.AreEqual("---\ntitle: T\npublished: no\nauthor: A\ntags: x\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_StackedHeader_StillQuotesAnAddedYaml11Lookalike()
	{
		string input = "---\ntitle: T\npublished: no\n---\n---\nauthor: A\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["draft"] = "yes" });

		Assert.AreEqual("---\ntitle: T\npublished: no\nauthor: A\ndraft: \"yes\"\n---\nBody\n", result);
	}
}
