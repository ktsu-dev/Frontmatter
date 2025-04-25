namespace ktsu.Frontmatter.Test;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the PropertyMerger class with isolation between tests.
/// </summary>
[TestClass]
public class PropertyMergerIsolationTests
{
	private static readonly string[] tags1 = ["tag1", "tag2"];
	private static readonly string[] tags2 = ["tag2", "tag3"];

	[TestInitialize]
	public void ClearPropertyMergerCache()
	{
		// Clear the static cache between tests using reflection
		var cacheField = typeof(PropertyMerger).GetField("PropertyMergeCache",
			BindingFlags.NonPublic | BindingFlags.Static);

		if (cacheField != null)
		{
			var cache = cacheField.GetValue(null) as ConcurrentDictionary<string, string>;
			cache?.Clear();
		}

		Console.WriteLine("Cache cleared");
	}

	[TestMethod]
	public void MergeSimilarProperties_AggressiveStrategy_WithCleanCache()
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

		// Log actual count and keys for diagnostic purposes
		Console.WriteLine($"Result count: {result.Count}");
		Console.WriteLine($"Keys: {string.Join(", ", result.Keys)}");

		// These assertions should pass regardless of test order or cache state
		Assert.IsTrue(result.ContainsKey("date"));
		Assert.IsTrue(result.ContainsKey("tags"));
		Assert.AreEqual(DateTime.Parse("2024-01-01"), result["date"]);
	}

	[TestMethod]
	public void MergeSimilarProperties_WithArrayValues_WithCleanCache()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "tags", tags1 },
			{ "keywords", tags2 }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Conservative);

		// Log actual count and keys for diagnostic purposes
		Console.WriteLine($"Result count: {result.Count}");
		Console.WriteLine($"Keys: {string.Join(", ", result.Keys)}");

		// These assertions should pass regardless of test order or cache state
		Assert.IsTrue(result.ContainsKey("tags") || result.ContainsKey("keywords"));
	}
}
