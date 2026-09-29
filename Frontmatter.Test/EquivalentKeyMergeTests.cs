// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for keys that normalize to the same name, such as <c>related</c> and
/// <c>page_related</c>. Each key used to be mapped to the other key's name, so the two were
/// swapped rather than merged.
/// </summary>
[TestClass]
public class EquivalentKeyMergeTests
{
	private static readonly string[] ListA = ["a"];
	private static readonly string[] ListB = ["b"];

	[TestMethod]
	[DataRow(FrontmatterMergeStrategy.Aggressive, false)]
	[DataRow(FrontmatterMergeStrategy.Aggressive, true)]
	[DataRow(FrontmatterMergeStrategy.Maximum, false)]
	[DataRow(FrontmatterMergeStrategy.Maximum, true)]
	public void EquivalentListKeys_MergeUnderOneNameWithBothValues(FrontmatterMergeStrategy strategy, bool prefixedFirst)
	{
		Dictionary<string, object> frontmatter = prefixedFirst
			? new() { ["page_related"] = ListB, ["related"] = ListA }
			: new() { ["related"] = ListA, ["page_related"] = ListB };

		Dictionary<string, object> result = PropertyMerger.MergeSimilarProperties(frontmatter, strategy);

		Assert.HasCount(1, result);
		Assert.IsTrue(result.TryGetValue("related", out object? related), "The unprefixed key should name the merged property");
		CollectionAssert.AreEquivalent(new object[] { "a", "b" }, ((IEnumerable<object>)related).ToArray());
	}

	[TestMethod]
	[DataRow(FrontmatterMergeStrategy.Aggressive, false)]
	[DataRow(FrontmatterMergeStrategy.Aggressive, true)]
	[DataRow(FrontmatterMergeStrategy.Maximum, false)]
	[DataRow(FrontmatterMergeStrategy.Maximum, true)]
	public void EquivalentScalarKeys_MergeIntoOneProperty(FrontmatterMergeStrategy strategy, bool prefixedFirst)
	{
		Dictionary<string, object> frontmatter = prefixedFirst
			? new() { ["page_related"] = "y", ["related"] = "x" }
			: new() { ["related"] = "x", ["page_related"] = "y" };

		Dictionary<string, object> result = PropertyMerger.MergeSimilarProperties(frontmatter, strategy);

		Assert.HasCount(1, result, "Two scalars that normalize alike should merge into one property");
	}

	[TestMethod]
	public void CombineFrontmatter_Aggressive_MergesRelatedListsInsteadOfSwappingThem()
	{
		const string input = "---\nrelated:\n- a\npage_related:\n- b\n---\nBody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Aggressive);

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.IsTrue(frontmatter.TryGetValue("related", out object? related), "The merged list should be written under related");
		CollectionAssert.AreEquivalent(new object[] { "a", "b" }, ((IEnumerable<object>)related).ToArray());
	}
}
