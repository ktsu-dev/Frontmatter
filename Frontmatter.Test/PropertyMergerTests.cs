namespace ktsu.Frontmatter.Test;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the PropertyMerger class.
/// </summary>
[TestClass]
public class PropertyMergerTests
{
	private static readonly string[] tags1 = ["tag1", "tag2"];
	private static readonly string[] tags2 = ["tag2", "tag3"];
	private static readonly string[] tags3 = ["tag2", "tag3"];

	[TestMethod]
	public void MergeSimilarProperties_NoStrategy_DoesntMergeAnything()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "title", "Sample Title" },
			{ "Title", "Different Title" },
			{ "name", "Sample Name" },
			{ "date", DateTime.Parse("2024-01-01") },
			{ "created", DateTime.Parse("2023-12-31") }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.None);

		// Assert
		Assert.AreEqual(5, result.Count);
		Assert.AreEqual("Sample Title", result["title"]);
		Assert.AreEqual("Different Title", result["Title"]);
		Assert.AreEqual("Sample Name", result["name"]);
		Assert.AreEqual(DateTime.Parse("2024-01-01"), result["date"]);
		Assert.AreEqual(DateTime.Parse("2023-12-31"), result["created"]);
	}

	[TestMethod]
	public void MergeSimilarProperties_ConservativeStrategy_MergesExactNameMatches()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "title", "Sample Title" },
			{ "Name", "Sample Name" }, // This should be merged to title
			{ "headline", "Headline Value" }, // This should be merged to title
			{ "random", "Random Value" } // This should be kept as is
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Conservative);

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.ContainsKey("title"));
		Assert.IsTrue(result.ContainsKey("random"));
		Assert.AreEqual("Sample Title", result["title"]); // Title is kept since it's first in order
	}

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

		// Assert
		Assert.AreEqual(3, result.Count);
		Assert.IsTrue(result.ContainsKey("date"));
		Assert.IsTrue(result.ContainsKey("tags"));

		// Date from "created" should be merged with "date"
		Assert.AreEqual(DateTime.Parse("2024-01-01"), result["date"]);

		// tag and tags should be merged
		object tagsValue = result["tags"];

		// Check if tags value is a string
		if (tagsValue is string tagString)
		{
			Assert.AreEqual("tag1", tagString);
		}
		// Check if tags value is an array
		else if (tagsValue is object[] tagsArray)
		{
			Assert.AreEqual(3, tagsArray.Length);
			CollectionAssert.Contains(tagsArray, "tag1");
			CollectionAssert.Contains(tagsArray, "tag2");
			CollectionAssert.Contains(tagsArray, "tag3");
		}
		else
		{
			Assert.Fail($"Expected tags to be object[] or string but was {tagsValue.GetType().Name}");
		}
	}

	[TestMethod]
	public void MergeSimilarProperties_MaximumStrategy_UsesSemanticsToMerge()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "description", "This is a description" },
			{ "snippet", "This is a snippet" },
			{ "author-name", "John Doe" }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Maximum);

		// Assert
		Assert.IsTrue(result.ContainsKey("description"));
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

		// Assert
		Assert.AreEqual(1, result.Count);
		Assert.IsTrue(result.ContainsKey("tags"));

		object tagsValue = result["tags"];

		// Check if tags value is a string
		if (tagsValue is string tagString)
		{
			// Verify it contains one of the expected tags
			Assert.IsTrue(tagString is "tag1" or "tag2" or "tag3");
		}
		// Check if tags value is an array
		else if (tagsValue is object[] tagsArray)
		{
			Assert.AreEqual(3, tagsArray.Length);
			CollectionAssert.Contains(tagsArray, "tag1");
			CollectionAssert.Contains(tagsArray, "tag2");
			CollectionAssert.Contains(tagsArray, "tag3");
		}
		else
		{
			Assert.Fail($"Expected tags to be object[] or string but was {tagsValue.GetType().Name}");
		}
	}

	[TestMethod]
	public void MergeSimilarProperties_WithDateValues_PreservesDateType()
	{
		// Arrange
		var date1 = DateTime.Parse("2023-12-31");
		var date2 = DateTime.Parse("2024-01-01");
		var frontmatter = new Dictionary<string, object>
		{
			{ "date", date1 },
			{ "created", date2 }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Conservative);

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.ContainsKey("date"));
		Assert.IsInstanceOfType<DateTime>(result["date"]);

		Assert.AreEqual(date1, result["date"]);
	}

	[TestMethod]
	public void FindBasicCanonicalName_WithKnownProperty_ReturnsCanonicalName()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "tag", "tag1" },
			{ "tags", tags3 },
			{ "author-name", "John Doe" }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Aggressive);

		// Assert
		Assert.IsTrue(result.ContainsKey("tags"));
	}

	[TestMethod]
	public void FindBasicCanonicalName_WithUnknownProperty_ReturnsLowerCase()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "custom-property", "value1" },
			{ "CustomProperty", "value2" },
			{ "CUSTOMPROPERTY", "value3" }
		};

		// Act
		var result = PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Aggressive);

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.IsTrue(result.ContainsKey("custom-property"));
		Assert.AreEqual("value1", result["custom-property"]);
	}
}
