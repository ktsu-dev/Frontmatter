// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #242: when the caller edits a nested mapping or list, the entries inside it that the
/// header already held are still written as the document wrote them, so YAML 1.1 quoting, meant for caller
/// values, doesn't turn <c>draft: no</c> into the truthy string <c>"no"</c>.
/// </summary>
[TestClass]
public class RewriteKeepsNestedDocumentValuesTests
{
	private const string Document = "---\ntitle: Hello\nflags:\n  draft: no\n  comments: yes\ntags: [on, off]\n---\nBody\n";

	[TestMethod]
	public void ReplaceFrontmatter_AfterEditingNestedValues_KeepsTheirUntouchedSiblingsPlain()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		((Dictionary<string, object>)frontmatter["flags"])["toc"] = true;
		((List<object>)frontmatter["tags"]).Add("news");

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Hello\nflags:\n  draft: no\n  comments: yes\n  toc: true\ntags:\n- on\n- off\n- news\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_NewNestedYaml11Lookalike_IsStillQuoted()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		((Dictionary<string, object>)frontmatter["flags"])["toc"] = "no";
		((List<object>)frontmatter["tags"]).Add("yes");

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Hello\nflags:\n  draft: no\n  comments: yes\n  toc: \"no\"\ntags:\n- on\n- off\n- \"yes\"\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_CallerChangesANestedValueToAYaml11Lookalike_QuotesIt()
	{
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(Document)!;
		((Dictionary<string, object>)frontmatter["flags"])["draft"] = "off";

		string result = Frontmatter.ReplaceFrontmatter(Document, frontmatter);

		Assert.AreEqual("---\ntitle: Hello\nflags:\n  draft: \"off\"\n  comments: yes\ntags:\n- on\n- off\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_NestedStringTheDocumentQuoted_KeepsItsQuotes()
	{
		string document = "---\nflags:\n  draft: 'no'\n  comments: yes\n---\nBody\n";
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(document)!;
		((Dictionary<string, object>)frontmatter["flags"])["toc"] = true;

		string result = Frontmatter.ReplaceFrontmatter(document, frontmatter);

		Assert.AreEqual("---\nflags:\n  draft: \"no\"\n  comments: yes\n  toc: true\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_DeeplyNestedValues_KeepTheirUntouchedSiblingsPlain()
	{
		string document = "---\nsite:\n  build:\n    drafts: no\n    future: off\n  menus:\n  - name: Home\n    hidden: no\n---\nBody\n";
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(document)!;
		Dictionary<string, object> site = (Dictionary<string, object>)frontmatter["site"];
		((Dictionary<string, object>)site["build"])["ttl"] = 5;
		((Dictionary<string, object>)((List<object>)site["menus"])[0])["weight"] = 1;

		string result = Frontmatter.ReplaceFrontmatter(document, frontmatter);

		Assert.AreEqual("---\nsite:\n  build:\n    drafts: no\n    future: off\n    ttl: 5\n  menus:\n  - name: Home\n    hidden: no\n    weight: 1\n---\nBody\n", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_NestedValueReachedThroughAnAlias_IsQuotedAsACallerValue()
	{
		// The scalar under the alias was written quoted at its anchor, which the alias's path doesn't show.
		string document = "---\nbase: &b 'no'\nflags:\n  draft: *b\n---\nBody\n";
		Dictionary<string, object> frontmatter = Frontmatter.ExtractFrontmatter(document)!;
		((Dictionary<string, object>)frontmatter["flags"])["toc"] = true;

		string result = Frontmatter.ReplaceFrontmatter(document, frontmatter);

		Assert.AreEqual("---\nbase: \"no\"\nflags:\n  draft: \"no\"\n  toc: true\n---\nBody\n", result);
	}
}
