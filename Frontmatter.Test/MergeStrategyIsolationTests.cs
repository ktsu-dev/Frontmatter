// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #130: the canonical name a key merges to depends on the strategy and on
/// the other keys in the same document, so one call must not decide it for a later one.
/// </summary>
/// <remarks>
/// These tests deliberately clear nothing between calls. The bug lived in process-wide state, and a
/// test that resets that state cannot see it.
/// </remarks>
[TestClass]
public class MergeStrategyIsolationTests
{
	[TestMethod]
	public void ConservativeAfterAggressive_KeepsKeyThatOnlyAggressiveMerges()
	{
		static Dictionary<string, object> Document() => new()
		{
			["title"] = "T",
			["custom_title"] = "C",
		};

		Dictionary<string, object> aggressive = PropertyMerger.MergeSimilarProperties(Document(), FrontmatterMergeStrategy.Aggressive);
		Assert.IsFalse(aggressive.ContainsKey("custom_title"), "Aggressive should merge custom_title into title");

		Dictionary<string, object> conservative = PropertyMerger.MergeSimilarProperties(Document(), FrontmatterMergeStrategy.Conservative);

		Assert.AreEqual("T", conservative["title"]);
		Assert.IsTrue(conservative.ContainsKey("custom_title"), "Conservative only applies predefined mappings, so custom_title must survive");
		Assert.AreEqual("C", conservative["custom_title"]);
	}

	[TestMethod]
	public void AggressiveAfterConservative_StillMergesKeys()
	{
		static Dictionary<string, object> Document() => new()
		{
			["summary"] = "S",
			["page_summary"] = "P",
		};

		Dictionary<string, object> conservative = PropertyMerger.MergeSimilarProperties(Document(), FrontmatterMergeStrategy.Conservative);
		Assert.HasCount(2, conservative);

		Dictionary<string, object> aggressive = PropertyMerger.MergeSimilarProperties(Document(), FrontmatterMergeStrategy.Aggressive);

		Assert.HasCount(1, aggressive);
	}

	[TestMethod]
	public void KeyAloneInALaterDocument_IsNotRenamedByAnEarlierDocument()
	{
		PropertyMerger.MergeSimilarProperties(new Dictionary<string, object>
		{
			["blurb"] = "B",
			["page_blurb"] = "P",
		}, FrontmatterMergeStrategy.Aggressive);

		Dictionary<string, object> later = PropertyMerger.MergeSimilarProperties(new Dictionary<string, object>
		{
			["page_blurb"] = "Only",
		}, FrontmatterMergeStrategy.Aggressive);

		Assert.IsTrue(later.ContainsKey("page_blurb"), "With no other key to merge with, page_blurb keeps its name");
		Assert.AreEqual("Only", later["page_blurb"]);
	}

	[TestMethod]
	public void CombineFrontmatter_ConservativeAfterAggressive_KeepsCustomTitle()
	{
		const string first = "---\ntitle: T\ncustom_title: C\n---\nBody1\n";
		const string second = "---\ntitle: T\ncustom_title: C\n---\nBody2\n";

		Frontmatter.CombineFrontmatter(first, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Aggressive);
		string result = Frontmatter.CombineFrontmatter(second, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Conservative);

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("C", frontmatter["custom_title"]);
	}
}
