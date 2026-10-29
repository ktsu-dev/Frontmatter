// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #218: a YAML 1.1 merge key (<c>&lt;&lt;: *anchor</c>) is resolved into the merged
/// keys, as Jekyll, PyYAML and js-yaml read it, rather than returned as a literal <c>&lt;&lt;</c> property.
/// </summary>
[TestClass]
public class YamlMergeKeyTests
{
	[TestMethod]
	public void ExtractFrontmatter_TopLevelMergeKey_ReturnsTheMergedKeys()
	{
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter("---\ndefaults: &d\n  layout: post\n  draft: true\n<<: *d\ntitle: Hello\n---\nBody\n");

		Assert.IsNotNull(result);
		Assert.IsFalse(result.ContainsKey("<<"));
		Assert.AreEqual("post", result["layout"]);
		Assert.AreEqual(true, result["draft"]);
		Assert.AreEqual("Hello", result["title"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_NestedMergeKey_MergesIntoTheNestedMapping()
	{
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter("---\nbase: &b\n  a: 1\nderived:\n  <<: *b\n  c: 2\n---\nBody\n");

		Assert.IsNotNull(result);
		Dictionary<string, object> derived = (Dictionary<string, object>)result["derived"];
		Assert.HasCount(2, derived);
		Assert.AreEqual(1, Convert.ToInt32(derived["a"], System.Globalization.CultureInfo.InvariantCulture));
		Assert.AreEqual(2, Convert.ToInt32(derived["c"], System.Globalization.CultureInfo.InvariantCulture));
	}

	[TestMethod]
	[DataRow("---\nlayout: page\n<<: {layout: post, draft: true}\n---\nBody\n")]
	[DataRow("---\n<<: {layout: post, draft: true}\nlayout: page\n---\nBody\n")]
	public void ExtractFrontmatter_ExplicitKeyAndMergedKey_ExplicitKeyWins(string input)
	{
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(result);
		Assert.AreEqual("page", result["layout"]);
		Assert.AreEqual(true, result["draft"]);
	}

	[TestMethod]
	public void AddFrontmatter_HeaderWithMergeKey_InsertsIntoTheHeaderAsWritten()
	{
		const string input = "---\ndefaults: &d\n  layout: post\n<<: *d\ntitle: Hello\n---\nBody\n";

		string result = Frontmatter.AddFrontmatter(input, new() { ["author"] = "Ann" });

		Assert.AreEqual("---\ndefaults: &d\n  layout: post\n<<: *d\ntitle: Hello\nauthor: Ann\n---\nBody\n", result);
	}

	[TestMethod]
	public void TryParseYamlObject_SequenceOfMergedMappings_EarlierMappingWins()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("a: &a {x: 1, y: 1}\nb: &b {y: 2, z: 2}\nc:\n  <<: [*a, *b]\n", out Dictionary<string, object>? result));

		Dictionary<string, object> c = (Dictionary<string, object>)result["c"];
		Assert.AreEqual("x,y,z", string.Join(",", c.Keys));
		Assert.AreEqual("1", c["y"].ToString());
	}

	[TestMethod]
	public void TryParseYamlObject_MergeKeyWithAScalarValue_IsKeptAsAKey()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("<<: text\ntitle: Hello\n", out Dictionary<string, object>? result));

		Assert.AreEqual("text", result["<<"]);
	}

	[TestMethod]
	public void TryParseYamlObject_MappingMergedIntoItself_ReturnsFalse() =>
		Assert.IsFalse(YamlSerializer.TryParseYamlObject("a: &x {b: 1, c: {<<: *x}}", out _));
}
