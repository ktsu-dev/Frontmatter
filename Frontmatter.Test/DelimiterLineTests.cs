// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that frontmatter delimiters are recognised only as whole lines at the top of a document.
/// </summary>
[TestClass]
public class DelimiterLineTests
{
	private static readonly string Nl = Environment.NewLine;

	[TestMethod]
	public void ExtractFrontmatter_BodyHasHorizontalRulesAroundKeyValueLine_LeavesItInTheBody()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}Intro{Nl}{Nl}---{Nl}{Nl}note: hi{Nl}{Nl}---{Nl}{Nl}More{Nl}";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);
		string body = Frontmatter.ExtractBody(input);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual($"Intro{Nl}{Nl}---{Nl}{Nl}note: hi{Nl}{Nl}---{Nl}{Nl}More", body);
	}

	[TestMethod]
	public void CombineFrontmatter_BodyHasHorizontalRulesAroundKeyValueLine_DoesNotMergeItIntoTheHeader()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}Intro{Nl}{Nl}---{Nl}{Nl}note: hi{Nl}{Nl}---{Nl}{Nl}More{Nl}";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual($"---{Nl}title: A{Nl}---{Nl}Intro{Nl}{Nl}---{Nl}{Nl}note: hi{Nl}{Nl}---{Nl}{Nl}More{Nl}", result);
	}

	[TestMethod]
	public void ExtractFrontmatter_ValueContainsDelimiterMidLine_KeepsTheWholeHeader()
	{
		string input = $"---{Nl}text: foo---{Nl}title: A{Nl}---{Nl}Body{Nl}";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);
		string body = Frontmatter.ExtractBody(input);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(2, frontmatter);
		Assert.AreEqual("foo---", frontmatter["text"]);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual("Body", body);
	}

	[TestMethod]
	public void CombineFrontmatter_TwoConsecutiveBlocks_CombinesThemWithoutLeavingTheSecondInTheBody()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}---{Nl}author: B{Nl}---{Nl}Body{Nl}";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual($"---{Nl}title: A{Nl}author: B{Nl}---{Nl}Body{Nl}", result);
	}

	[TestMethod]
	public void ExtractFrontmatter_ClosingDelimiterEndsTheDocument_ReadsTheFrontmatter()
	{
		string input = $"---{Nl}title: A{Nl}---";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
	}

	[TestMethod]
	public void AddFrontmatter_ClosingDelimiterEndsTheDocument_KeepsTheExistingProperties()
	{
		string input = $"---{Nl}title: A{Nl}---";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(2, frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual("B", frontmatter["author"]);
	}

	[TestMethod]
	public void AddFrontmatter_ExistingFrontmatterIsUnreadable_ReturnsTheDocumentUnchanged()
	{
		string input = $"---{Nl}title: [unclosed{Nl}---{Nl}Body{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });

		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_ExistingFrontmatterIsEmpty_AddsTheProperties()
	{
		string input = $"---{Nl}---{Nl}Body{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });

		Assert.AreEqual($"---{Nl}author: B{Nl}---{Nl}Body{Nl}", result);
	}
}
