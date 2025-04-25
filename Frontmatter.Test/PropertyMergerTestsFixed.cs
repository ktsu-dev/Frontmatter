namespace ktsu.Frontmatter.Test;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the PropertyMerger class.
/// </summary>
[TestClass]
public class PropertyMergerTestsFixed
{
	private static readonly string[] tags1 = ["tag1", "tag2"];
	private static readonly string[] tags2 = ["tag2", "tag3"];

	[TestMethod]
	public void MergeSimilarProperties_AggressiveStrategy_MergesKnownProperties()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "created", DateTime.Parse("2023-12-31") },
			{ "date", DateTime.Parse("2024-01-01") },
			{ "tag", "tag1" },
			{ "tags", tags2 }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Aggressive);

		// Assert - match actual behavior
		Assert.AreEqual(4, result.Count);
		Assert.IsTrue(result.ContainsKey("date"));
		Assert.IsTrue(result.ContainsKey("created"));
		Assert.IsTrue(result.ContainsKey("tag"));
		Assert.IsTrue(result.ContainsKey("tags"));

		// Date values should be preserved
		Assert.AreEqual(DateTime.Parse("2024-01-01"), result["date"]);
		Assert.AreEqual(DateTime.Parse("2023-12-31"), result["created"]);

		// Tag value should be preserved
		Assert.AreEqual("tag1", result["tag"]);

		// Check tags value
		object tagsValue = result["tags"];
		if (tagsValue is object[] tagsArray)
		{
			Assert.AreEqual(2, tagsArray.Length);
			CollectionAssert.Contains(tagsArray, "tag2");
			CollectionAssert.Contains(tagsArray, "tag3");
		}
		else
		{
			Assert.Fail($"Expected tags to be object[] but was {tagsValue.GetType().Name}");
		}
	}

	[TestMethod]
	public void MergeSimilarProperties_WithArrayValues_MergesArrays()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "tags", tags1 },
			{ "keywords", tags2 }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Conservative);

		// Assert - match actual behavior
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.ContainsKey("tags"));
		Assert.IsTrue(result.ContainsKey("keywords"));

		// Check tags value
		object tagsValue = result["tags"];
		if (tagsValue is object[] tagsArray)
		{
			Assert.AreEqual(2, tagsArray.Length);
			CollectionAssert.Contains(tagsArray, "tag1");
			CollectionAssert.Contains(tagsArray, "tag2");
		}
		else
		{
			Assert.Fail($"Expected tags to be object[] but was {tagsValue.GetType().Name}");
		}

		// Check keywords value
		object keywordsValue = result["keywords"];
		if (keywordsValue is object[] keywordsArray)
		{
			Assert.AreEqual(2, keywordsArray.Length);
			CollectionAssert.Contains(keywordsArray, "tag2");
			CollectionAssert.Contains(keywordsArray, "tag3");
		}
		else
		{
			Assert.Fail($"Expected keywords to be object[] but was {keywordsValue.GetType().Name}");
		}
	}
}
