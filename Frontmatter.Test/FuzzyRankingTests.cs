// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that candidate property names are chosen by similarity rather than by iteration order.
/// </summary>
/// <remarks>
/// Containment admits a candidate; it does not rank one. Before these matchers scored their
/// candidates, the winner was whichever plausible name <see cref="StandardOrder.PropertyNames"/>
/// happened to list first, so a more specific name lost to a shorter one that merely appeared
/// earlier.
/// </remarks>
[TestClass]
public class FuzzyRankingTests
{
	private static Dictionary<string, object> Standardize(string key, string value)
	{
		string input = $"---{Environment.NewLine}" +
					   $"{key}: {value}{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		Dictionary<string, object>? extracted = Frontmatter.ExtractFrontmatter(result);

		Assert.IsNotNull(extracted);

		return extracted;
	}

	[TestMethod]
	public void StandardizePropertyNames_CandidateSharesAPrefixWithAnEarlierProperty_PrefersTheCloserMatch()
	{
		// "subtitles_track" contains both "title" and "subtitle". "title" is listed first in
		// StandardOrder.PropertyNames, so an unranked match returns it.
		Dictionary<string, object> extracted = Standardize("subtitles_track", "en.vtt");

		Assert.IsTrue(
			extracted.ContainsKey("subtitle"),
			$"Expected 'subtitles_track' to standardize to 'subtitle'; got '{string.Join(", ", extracted.Keys)}'.");
		Assert.AreEqual("en.vtt", extracted["subtitle"]);
	}

	[TestMethod]
	public void StandardizePropertyNames_CandidateContainsACompoundProperty_PrefersTheMoreSpecificMatch()
	{
		// "review_status_flag" contains both "status" and "review_status". "status" is listed
		// first, so an unranked match returns the less specific of the two.
		Dictionary<string, object> extracted = Standardize("review_status_flag", "approved");

		Assert.IsTrue(
			extracted.ContainsKey("review_status"),
			$"Expected 'review_status_flag' to standardize to 'review_status'; got '{string.Join(", ", extracted.Keys)}'.");
		Assert.AreEqual("approved", extracted["review_status"]);
	}

	[TestMethod]
	public void Score_NeitherNameIsASubsequenceOfTheOther_ReportsNoScore()
	{
		Assert.AreEqual(int.MinValue, FuzzyRanking.Score("review_notes", "notes_reviewed"));
	}

	[TestMethod]
	public void Score_IsSymmetric_SoAShorterCandidateStillScores()
	{
		// The callers' containment gates admit a candidate whether it contains the key or is
		// contained by it, so a one-directional score would leave half of them unranked.
		Assert.AreEqual(
			FuzzyRanking.Score("subtitle", "subtitles_track"),
			FuzzyRanking.Score("subtitles_track", "subtitle"));
	}

	[TestMethod]
	public void Score_RanksTheCloserOfTwoContainedNamesHigher() =>
		Assert.IsGreaterThan(
			FuzzyRanking.Score("subtitles_track", "title"),
			FuzzyRanking.Score("subtitles_track", "subtitle"));
}
