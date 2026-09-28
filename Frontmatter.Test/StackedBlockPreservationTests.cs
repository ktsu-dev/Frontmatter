// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for documents with more than one stacked frontmatter block. The rebuilt header used
/// to be built from a subset of the blocks while the body still started after all of them, so the
/// blocks left out were deleted from the document.
/// </summary>
[TestClass]
public class StackedBlockPreservationTests
{
	[TestMethod]
	public void AddFrontmatter_TwoStackedBlocks_KeepsPropertiesFromEveryBlock()
	{
		const string input = "---\ntitle: T\n---\n---\ntags: [x]\n---\nbody\n";

		string result = Frontmatter.AddFrontmatter(input, new() { ["author"] = "Me" });

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("T", frontmatter["title"]);
		Assert.AreEqual("Me", frontmatter["author"]);
		Assert.IsTrue(frontmatter.ContainsKey("tags"), "The second block's tags should survive adding a property");
		Assert.AreEqual("body", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_StackedBlocksRepeatAKey_FirstBlockWins()
	{
		const string input = "---\ntitle: First\n---\n---\ntitle: Second\n---\nbody\n";

		string result = Frontmatter.AddFrontmatter(input, new() { ["author"] = "Me" });

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("First", frontmatter["title"]);
	}

	// The first block is always read as frontmatter, so an unparseable first block followed by one that
	// parses is the case where the unreadable text sits in the header rather than the body.
	private const string UnreadableFirstBlock = "---\nkey: [unclosed\n---\n---\ntitle: T\n---\nbody\n";

	[TestMethod]
	public void AddFrontmatter_UnreadableFirstBlock_ReturnsInputUnchanged() =>
		Assert.AreEqual(UnreadableFirstBlock, Frontmatter.AddFrontmatter(UnreadableFirstBlock, new() { ["author"] = "Me" }));

	[TestMethod]
	[DataRow(FrontmatterMergeStrategy.None)]
	[DataRow(FrontmatterMergeStrategy.Conservative)]
	[DataRow(FrontmatterMergeStrategy.Maximum)]
	public void CombineFrontmatter_UnreadableFirstBlock_ReturnsInputUnchanged(FrontmatterMergeStrategy strategy)
	{
		string result = Frontmatter.CombineFrontmatter(UnreadableFirstBlock, FrontmatterNaming.Standard, FrontmatterOrder.Sorted, strategy);

		Assert.AreEqual(UnreadableFirstBlock, result);
	}

	[TestMethod]
	public void CombineFrontmatter_UnreadableSecondBlock_StaysVerbatimInTheBody()
	{
		const string input = "---\ntitle: T\n---\n---\nkey: [unclosed\n---\nbody\n";

		string result = Frontmatter.CombineFrontmatter(input);

		Assert.Contains("key: [unclosed", result);
	}
}
