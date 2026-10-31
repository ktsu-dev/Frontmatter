// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #229: only a plain <c>&lt;&lt;</c> is the YAML 1.1 merge key. A single- or
/// double-quoted <c>"&lt;&lt;"</c> is an ordinary string key, as PyYAML and js-yaml read it, and is not
/// flattened into its parent.
/// </summary>
[TestClass]
public class QuotedMergeKeyTests
{
	[TestMethod]
	[DataRow("---\n\"<<\": {a: 1}\nb: 2\n---\nBody\n")]
	[DataRow("---\n'<<': {a: 1}\nb: 2\n---\nBody\n")]
	public void ExtractFrontmatter_QuotedMergeKey_IsALiteralKey(string input)
	{
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(result);
		Assert.AreEqual("<<,b", string.Join(",", result.Keys));
		Dictionary<string, object> literal = (Dictionary<string, object>)result["<<"];
		Assert.AreEqual("1", literal["a"].ToString());
	}

	[TestMethod]
	public void ExtractFrontmatter_QuotedMergeKeyAndSameKeyAsItsContent_KeepsBoth()
	{
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter("---\n\"<<\": {a: 1}\na: 5\n---\nBody\n");

		Assert.IsNotNull(result);
		Assert.AreEqual("<<,a", string.Join(",", result.Keys));
		Assert.AreEqual("5", result["a"].ToString());
	}

	[TestMethod]
	public void TryParseYamlObject_NestedQuotedMergeKey_IsALiteralKey()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("base: &b {a: 1}\nderived:\n  \"<<\": *b\n  c: 2\n", out Dictionary<string, object>? result));

		Dictionary<string, object> derived = (Dictionary<string, object>)result["derived"];
		Assert.AreEqual("<<,c", string.Join(",", derived.Keys));
	}

	[TestMethod]
	public void TryParseYamlObject_QuotedMergeKeyAsAValue_ReadsBackAsTheString()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("k: \"<<\"\nlist: ['<<', x]\n", out Dictionary<string, object>? result));

		Assert.AreEqual("<<", result["k"]);
		CollectionAssert.AreEqual(new object[] { "<<", "x" }, (System.Collections.ICollection)result["list"]);
	}

	[TestMethod]
	public void TryParseYamlObject_PlainMergeKeyBesideQuotedOne_StillMerges()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("d: &d {layout: post}\nx:\n  <<: *d\n  \"<<\": literal\n", out Dictionary<string, object>? result));

		Dictionary<string, object> x = (Dictionary<string, object>)result["x"];
		Assert.AreEqual("post", x["layout"]);
		Assert.AreEqual("literal", x["<<"]);
	}
}
