// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #147: merging must not rename a list property, since renaming is the job of
/// <see cref="FrontmatterNaming.Standard"/>, and <see cref="FrontmatterNaming.AsIs"/> turns it off.
/// </summary>
[TestClass]
public class ListKeyNamingTests
{
	private static readonly string Nl = Environment.NewLine;

	[TestMethod]
	[DataRow(FrontmatterMergeStrategy.None)]
	[DataRow(FrontmatterMergeStrategy.Conservative)]
	[DataRow(FrontmatterMergeStrategy.Aggressive)]
	[DataRow(FrontmatterMergeStrategy.Maximum)]
	public void CombineFrontmatter_AsIsNaming_LoneListPropertyKeepsItsKey(FrontmatterMergeStrategy strategy)
	{
		string input = $"---{Nl}section:{Nl}  - news{Nl}summary: hi{Nl}---{Nl}Body{Nl}";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, strategy);
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);

		Assert.IsNotNull(frontmatter);
		Assert.IsTrue(frontmatter.ContainsKey("section"), $"section was renamed: {result}");
		Assert.IsFalse(frontmatter.ContainsKey("categories"), $"section was renamed: {result}");
	}

	[TestMethod]
	[DataRow("keywords")]
	[DataRow("category")]
	public void MergeSimilarProperties_LoneListProperty_KeepsItsKey(string key)
	{
		Dictionary<string, object> source = new()
		{
			[key] = new List<object> { "a", "b" },
		};

		Dictionary<string, object> result = PropertyMerger.MergeSimilarProperties(source, FrontmatterMergeStrategy.Conservative);

		Assert.IsTrue(result.ContainsKey(key), $"{key} was renamed to {string.Join(", ", result.Keys)}");
		Assert.AreEqual(1, result.Count);
	}
}
