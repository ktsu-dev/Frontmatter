// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for YAML aliases. An alias inside its own anchor used to overflow the stack, which
/// killed the process, and nested aliases used to expand exponentially.
/// </summary>
[TestClass]
public class YamlAliasTests
{
	// Six levels of ten aliases each expand to a million values.
	private static readonly string AliasBomb = string.Join("\n",
		"a: &a [x, x, x, x, x, x, x, x, x, x]",
		"b: &b [*a, *a, *a, *a, *a, *a, *a, *a, *a, *a]",
		"c: &c [*b, *b, *b, *b, *b, *b, *b, *b, *b, *b]",
		"d: &d [*c, *c, *c, *c, *c, *c, *c, *c, *c, *c]",
		"e: &e [*d, *d, *d, *d, *d, *d, *d, *d, *d, *d]",
		"f: &f [*e, *e, *e, *e, *e, *e, *e, *e, *e, *e]");

	[TestMethod]
	public void TryParseYamlObject_SequenceContainsItsOwnAlias_ReturnsFalse()
	{
		Assert.IsFalse(YamlSerializer.TryParseYamlObject("a: &x [1, *x]", out Dictionary<string, object>? result));
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TryParseYamlObject_MappingContainsItsOwnAlias_ReturnsFalse()
	{
		Assert.IsFalse(YamlSerializer.TryParseYamlObject("a: &x {b: *x}", out Dictionary<string, object>? result));
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TryParseYamlObject_NestedAliasesExpandPastTheLimit_ReturnsFalse() =>
		Assert.IsFalse(YamlSerializer.TryParseYamlObject(AliasBomb, out _));

	[TestMethod]
	public void TryParseYamlObject_AliasUsedTwiceWithoutACycle_ExpandsBothUses()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("a: &x [p, q]\nb: *x\nc: [*x, *x]", out Dictionary<string, object>? result));

		CollectionAssert.AreEqual(new object[] { "p", "q" }, ((List<object>)result["a"]).ToArray());
		CollectionAssert.AreEqual(new object[] { "p", "q" }, ((List<object>)result["b"]).ToArray());
		Assert.HasCount(2, (List<object>)result["c"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_SelfReferencingMapping_ReturnsNull() =>
		Assert.IsNull(Frontmatter.ExtractFrontmatter("---\na: &x {b: *x}\n---\nbody\n"));

	[TestMethod]
	public void CombineFrontmatter_SelfReferencingSequence_ReturnsTheDocumentUnchanged()
	{
		const string input = "---\na: &x [1, *x]\n---\nbody\n";

		Assert.AreEqual(input, Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None));
	}

	[TestMethod]
	public void CombineFrontmatter_AliasBomb_ReturnsTheDocumentUnchanged()
	{
		string input = $"---\n{AliasBomb}\n---\nbody\n";

		Assert.AreEqual(input, Frontmatter.CombineFrontmatter(input));
	}
}
