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

	[TestMethod]
	public void ExtractBody_RuleUnderHeaderAroundNonYamlText_KeepsTheWholeBody()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}---{Nl}Important paragraph{Nl}{Nl}---{Nl}More{Nl}";

		Assert.AreEqual($"---{Nl}Important paragraph{Nl}{Nl}---{Nl}More", Frontmatter.ExtractBody(input));
		Assert.AreEqual($"---{Nl}Important paragraph{Nl}{Nl}---{Nl}More{Nl}", Frontmatter.RemoveFrontmatter(input));
	}

	[TestMethod]
	public void AddFrontmatter_RuleUnderHeaderAroundNonYamlText_KeepsTheWholeBody()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}---{Nl}Important paragraph{Nl}{Nl}---{Nl}More{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });

		Assert.AreEqual($"---{Nl}title: A{Nl}author: B{Nl}---{Nl}---{Nl}Important paragraph{Nl}{Nl}---{Nl}More{Nl}", result);
	}

	[TestMethod]
	public void CombineFrontmatter_RuleUnderHeaderAroundYamlLikeText_DoesNotMergeItIntoTheHeader()
	{
		string input = $"---{Nl}title: My Post{Nl}---{Nl}---{Nl}Note: read this first{Nl}{Nl}---{Nl}{Nl}# Heading{Nl}";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void ExtractBody_EmptyBlockUnderHeader_KeepsTheRulesInTheBody()
	{
		string input = $"---{Nl}title: A{Nl}---{Nl}---{Nl}---{Nl}Body{Nl}";

		Assert.AreEqual($"---{Nl}---{Nl}Body", Frontmatter.ExtractBody(input));
	}

	[TestMethod]
	[DataRow("--- ", DisplayName = "Trailing space")]
	[DataRow("---\t", DisplayName = "Trailing tab")]
	[DataRow("\uFEFF---", DisplayName = "Byte order mark")]
	public void HasFrontmatter_OpeningDelimiterHasTrailingWhitespaceOrBom_RecognisesTheHeader(string opening)
	{
		string input = $"{opening}{Nl}title: A{Nl}---{Nl}Body{Nl}";

		Assert.IsTrue(Frontmatter.HasFrontmatter(input));
	}

	[TestMethod]
	[DataRow("--- ", DisplayName = "Trailing space")]
	[DataRow("---\t", DisplayName = "Trailing tab")]
	[DataRow("\uFEFF---", DisplayName = "Byte order mark")]
	public void ExtractFrontmatter_OpeningDelimiterHasTrailingWhitespaceOrBom_ReadsTheHeader(string opening)
	{
		string input = $"{opening}{Nl}title: A{Nl}---{Nl}Body{Nl}";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual("Body", Frontmatter.ExtractBody(input));
	}

	[TestMethod]
	[DataRow("--- ", DisplayName = "Trailing space")]
	[DataRow("---\t", DisplayName = "Trailing tab")]
	[DataRow("\uFEFF---", DisplayName = "Byte order mark")]
	public void AddFrontmatter_OpeningDelimiterHasTrailingWhitespaceOrBom_MergesIntoTheExistingHeader(string opening)
	{
		string input = $"{opening}{Nl}title: A{Nl}---{Nl}Body{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["author"] = "B" });

		Assert.AreEqual($"---{Nl}title: A{Nl}author: B{Nl}---{Nl}Body{Nl}", result);
	}

	[TestMethod]
	public void AddFrontmatter_BomPrefixedDocumentWithoutHeader_LeavesNoBomInTheBody()
	{
		string input = $"﻿# Heading{Nl}{Nl}text{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "T" });

		Assert.AreEqual($"---{Nl}title: T{Nl}---{Nl}# Heading{Nl}{Nl}text{Nl}", result);
		Assert.AreEqual($"# Heading{Nl}{Nl}text", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_BomPrefixedDocumentOpeningWithBlankLines_LeavesNoBomInTheBody()
	{
		string input = $"﻿{Nl}{Nl}# Heading{Nl}";

		string result = Frontmatter.AddFrontmatter(input, new Dictionary<string, object> { ["title"] = "T" });

		Assert.AreEqual($"---{Nl}title: T{Nl}---{Nl}# Heading{Nl}", result);
	}

	[TestMethod]
	public void HasFrontmatter_DelimiterWithoutLineEnding_IsNotFrontmatter()
	{
		Assert.IsFalse(Frontmatter.HasFrontmatter("--- "));
		Assert.IsFalse(Frontmatter.HasFrontmatter("\uFEFF"));
		Assert.IsFalse(Frontmatter.HasFrontmatter(string.Empty));
	}

	private static readonly string PandocDocument =
		$"---{Nl}title: A{Nl}...{Nl}# Heading{Nl}{Nl}Important paragraph.{Nl}{Nl}---{Nl}{Nl}More text{Nl}";

	[TestMethod]
	public void ExtractFrontmatter_BlockClosedByDocumentEndMarker_ReadsTheBlock()
	{
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(PandocDocument);

		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
	}

	[TestMethod]
	public void ExtractBody_BlockClosedByDocumentEndMarker_KeepsTheBodyBeforeALaterRule()
	{
		string body = Frontmatter.ExtractBody(PandocDocument);

		Assert.AreEqual($"# Heading{Nl}{Nl}Important paragraph.{Nl}{Nl}---{Nl}{Nl}More text", body);
	}

	[TestMethod]
	public void RemoveFrontmatter_BlockClosedByDocumentEndMarker_KeepsTheBodyBeforeALaterRule()
	{
		string result = Frontmatter.RemoveFrontmatter(PandocDocument);

		Assert.AreEqual($"# Heading{Nl}{Nl}Important paragraph.{Nl}{Nl}---{Nl}{Nl}More text{Nl}", result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_BlockClosedByDocumentEndMarker_ReplacesOnlyTheBlock()
	{
		string result = Frontmatter.ReplaceFrontmatter(PandocDocument, new() { { "title", "B" } });

		Assert.AreEqual($"---{Nl}title: B{Nl}---{Nl}# Heading{Nl}{Nl}Important paragraph.{Nl}{Nl}---{Nl}{Nl}More text{Nl}", result);
	}

	[TestMethod]
	public void ExtractFrontmatter_BlockClosedByDocumentEndMarkerWithNoLaterRule_ReadsTheBlock()
	{
		string input = $"---{Nl}title: A{Nl}...   {Nl}Body{Nl}";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual("Body", Frontmatter.ExtractBody(input));
	}

	[TestMethod]
	public void ExtractFrontmatter_DocumentEndMarkerOnTheFirstLine_IsNotAnOpener()
	{
		string input = $"...{Nl}title: A{Nl}---{Nl}Body{Nl}";

		Assert.IsFalse(Frontmatter.HasFrontmatter(input));
		Assert.IsNull(Frontmatter.ExtractFrontmatter(input));
		Assert.AreEqual(input, Frontmatter.RemoveFrontmatter(input));
	}

	[TestMethod]
	public void ExtractFrontmatter_IndentedDocumentEndMarkerInsideAValue_DoesNotCloseTheBlock()
	{
		string input = $"---{Nl}notes: |{Nl}  ...{Nl}title: A{Nl}---{Nl}Body{Nl}";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(input);

		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("A", frontmatter["title"]);
		Assert.AreEqual("Body", Frontmatter.ExtractBody(input));
	}
}
