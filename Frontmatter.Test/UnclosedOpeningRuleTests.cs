// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that a document opening with a <c>---</c> rule that never closes is treated as having no frontmatter.
/// </summary>
[TestClass]
public class UnclosedOpeningRuleTests
{
	private static readonly string Nl = Environment.NewLine;

	private static readonly string Document = $"---{Nl}{Nl}# Heading{Nl}{Nl}Text{Nl}";

	[TestMethod]
	public void HasFrontmatter_OpeningRuleNeverCloses_IsFalse()
	{
		Assert.IsFalse(Frontmatter.HasFrontmatter($"---{Nl}{Nl}# Heading{Nl}"));
		Assert.IsFalse(Frontmatter.HasFrontmatter(Document));
	}

	[TestMethod]
	public void ExtractFrontmatter_OpeningRuleNeverCloses_IsNull()
	{
		Assert.IsNull(Frontmatter.ExtractFrontmatter(Document));
	}

	[TestMethod]
	public void AddFrontmatter_OpeningRuleNeverCloses_WritesAHeaderInFrontOfTheRule()
	{
		string result = Frontmatter.AddFrontmatter(Document, new Dictionary<string, object> { ["title"] = "T" });

		Assert.AreEqual($"---{Nl}title: T{Nl}---{Nl}---{Nl}{Nl}# Heading{Nl}{Nl}Text{Nl}", result);

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(frontmatter);
		Assert.HasCount(1, frontmatter);
		Assert.AreEqual("T", frontmatter["title"]);
		Assert.AreEqual($"---{Nl}{Nl}# Heading{Nl}{Nl}Text", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void RemoveFrontmatter_OpeningRuleNeverCloses_LeavesTheDocumentAlone()
	{
		Assert.AreEqual(Document, Frontmatter.RemoveFrontmatter(Document));
	}
}
