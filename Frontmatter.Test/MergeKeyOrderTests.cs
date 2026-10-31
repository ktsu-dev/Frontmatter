// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #224: merging emits keys in input order, so <see cref="FrontmatterOrder.AsIs"/>
/// keeps the document's order and a second <see cref="Frontmatter.CombineFrontmatter(string, FrontmatterNaming, FrontmatterOrder, FrontmatterMergeStrategy)"/>
/// run does not reorder what the first one wrote.
/// </summary>
[TestClass]
public class MergeKeyOrderTests
{
	private static readonly string[] AliasAndStandardKeys =
	[
		"title", "heading", "summary", "excerpt", "description", "layout", "template", "creator", "author",
		"ts", "date", "tags", "keywords", "categories", "category", "permalink", "slug", "custom", "notes",
	];

	private static readonly FrontmatterMergeStrategy[] Strategies =
	[
		FrontmatterMergeStrategy.None,
		FrontmatterMergeStrategy.Conservative,
		FrontmatterMergeStrategy.Aggressive,
		FrontmatterMergeStrategy.Maximum,
	];

	private static readonly FrontmatterOrder[] Orders = [FrontmatterOrder.AsIs, FrontmatterOrder.Sorted];

	private static string[] KeysOf(string document) => [.. Frontmatter.ExtractFrontmatter(document)!.Keys];

	[TestMethod]
	public void AsIs_KeysThatAreNotMerged_KeepTheirPositions()
	{
		const string input = "---\nsummary: Short\nlayout: post\nexcerpt: [a, b]\n---\nBody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Conservative);

		string[] expected = ["summary", "layout", "excerpt"];
		CollectionAssert.AreEqual(expected, KeysOf(result));
	}

	[TestMethod]
	public void MergeSimilarProperties_AMergedGroup_TakesThePositionOfItsFirstMember()
	{
		Dictionary<string, object> frontmatter = new()
		{
			["notes"] = "n",
			["tags"] = new List<object> { "a" },
			["layout"] = "post",
			["keywords"] = new List<object> { "b" },
		};

		Dictionary<string, object> merged = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Conservative);

		string[] expectedKeys = ["notes", "tags", "layout"];
		object[] expectedTags = ["a", "b"];
		CollectionAssert.AreEqual(expectedKeys, merged.Keys.ToArray());
		CollectionAssert.AreEqual(expectedTags, (object[])merged["tags"]);
	}

	[TestMethod]
	public void Sorted_SecondRun_LeavesTheOrderAsTheFirstRunWroteIt()
	{
		const string input = "---\ncreator: [v1]\nts: v2\nexcerpt: [v0]\nsummary: v0\n---\nBody\n";

		string once = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);
		string twice = Frontmatter.CombineFrontmatter(once, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

		Assert.AreEqual(once, twice);
	}

	[TestMethod]
	public void SeededDocuments_AsIsOrder_KeepsTheRelativeOrderOfEverySurvivingKey()
	{
		Random random = new(224);
		for (int run = 0; run < 300; run++)
		{
			string input = RandomDocument(random, out string[] inputKeys);
			foreach (FrontmatterMergeStrategy strategy in Strategies)
			{
				string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, strategy);

				string[] surviving = [.. KeysOf(result).Where(inputKeys.Contains)];
				string[] expected = [.. inputKeys.Where(surviving.Contains)];
				CollectionAssert.AreEqual(expected, surviving, $"{strategy} reordered keys of:\n{input}");
			}
		}
	}

	[TestMethod]
	public void SeededDocuments_AsIsNaming_SecondRunChangesNothing()
	{
		Random random = new(2240);
		for (int run = 0; run < 300; run++)
		{
			string input = RandomDocument(random, out _);
			foreach (FrontmatterMergeStrategy strategy in Strategies)
			{
				foreach (FrontmatterOrder order in Orders)
				{
					string once = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, order, strategy);
					string twice = Frontmatter.CombineFrontmatter(once, FrontmatterNaming.AsIs, order, strategy);

					Assert.AreEqual(once, twice, $"{strategy}/{order} is not idempotent for:\n{input}");
				}
			}
		}
	}

	private static string RandomDocument(Random random, out string[] keys)
	{
		keys = [.. AliasAndStandardKeys.OrderBy(_ => random.Next()).Take(random.Next(2, 7))];
		StringBuilder builder = new("---\n");
		foreach (string key in keys)
		{
			int value = random.Next(100);
			builder.Append(key).Append(": ").Append(random.Next(2) == 0 ? $"v{value}" : $"[v{value}]").Append('\n');
		}

		return builder.Append("---\nBody\n").ToString();
	}
}
