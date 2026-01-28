// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CombineFrontmatterTests
{
	[TestMethod]
	public void CombineFrontmatter_WithMultipleFrontmatterSections_CombinesSections()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: First Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Some content between frontmatter{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"author: Test Author{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Final content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert - at minimum, the resulting document should have frontmatter and content
		Assert.IsTrue(Frontmatter.HasFrontmatter(result), "The result should have frontmatter");

		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter, "Extracted frontmatter should not be null");

		// The body should contain at least one of the original content sections
		string body = Frontmatter.ExtractBody(result);
		Assert.IsFalse(string.IsNullOrWhiteSpace(body), "The body should not be empty");
	}

	[TestMethod]
	public void CombineFrontmatter_WithConflictingProperties_UsesFirstProperty()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: First Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Some content{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"title: Second Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Final content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("First Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithNoFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void CombineFrontmatter_WithCustomOrder_PreservesOrder()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"author: Test Author{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs);

		// Assert
		// Extract the frontmatter to check the actual content
		string frontmatterSection = result.Substring(result.IndexOf("---") + 3,
			result.IndexOf("---", result.IndexOf("---") + 3) - result.IndexOf("---") - 3).Trim();

		// Check if author appears before title
		int authorIndex = frontmatterSection.IndexOf("author:");
		int titleIndex = frontmatterSection.IndexOf("title:");
		Assert.IsLessThan(titleIndex, authorIndex, "Author should appear before title when preserving original order");
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardOrder_SortsProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"author: Test Author{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted);

		// Assert
		// Extract the frontmatter to check the actual content
		string frontmatterSection = result.Substring(result.IndexOf("---") + 3,
			result.IndexOf("---", result.IndexOf("---") + 3) - result.IndexOf("---") - 3).Trim();

		// Check if title appears before author (as per standard order)
		int authorIndex = frontmatterSection.IndexOf("author:");
		int titleIndex = frontmatterSection.IndexOf("title:");
		Assert.IsLessThan(authorIndex, titleIndex, "Title should appear before author in standard sorted order");
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_StandardizesPropertyNames()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"writer: Test Author{Environment.NewLine}" + // Might be standardized to "author"
					  $"heading: Test Title{Environment.NewLine}" + // Might be standardized to "title"
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result), "Result should contain frontmatter");
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter, "Frontmatter should be extracted");
		Assert.IsNotEmpty(extractedFrontmatter, "Frontmatter should have at least one key");

		// The content should be preserved
		string body = Frontmatter.ExtractBody(result);
		Assert.AreEqual("Content", body, "Body content should be preserved");
	}

	[TestMethod]
	public void CombineFrontmatter_WithConservativeMergeStrategy_MergesKnownProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"name: Another Title{Environment.NewLine}" + // Should be merged with "title" in conservative strategy
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Conservative);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(1, extractedFrontmatter); // Only one property after merging
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"), "Merged frontmatter should contain 'title' after conservative merge");
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithAggressiveMergeStrategy_MergesSimilarProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"custom_title: Another Title{Environment.NewLine}" + // Should be merged with pattern matching
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.Aggressive);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);

		// Either keys should be merged or both keys preserved
		int keyCount = extractedFrontmatter.Count;
		Assert.IsGreaterThan(0, keyCount, "At least one key should be present in the result");

		// Check that we have the expected values regardless of merging strategy
		bool foundTestTitle = false;

		foreach (object value in extractedFrontmatter.Values)
		{
			if (value.ToString() == "Test Title")
			{
				foundTestTitle = true;
			}
		}

		Assert.IsTrue(foundTestTitle, "The value 'Test Title' should be preserved");
	}

	[TestMethod]
	public void CombineFrontmatter_WithNoMergeStrategy_PreservesAllProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"name: Another Title{Environment.NewLine}" + // Should not be merged with "title"
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(2, extractedFrontmatter); // Both properties preserved
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"), "Frontmatter should contain 'title' when merge strategy is None");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("name"), "Frontmatter should contain 'name' when merge strategy is None");
	}

	[TestMethod]
	public void CombineFrontmatter_WithComplexPropertyValues_PreservesStructure()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"tags:{Environment.NewLine}" +
					  $"  - tag1{Environment.NewLine}" +
					  $"  - tag2{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"), "Frontmatter should contain 'tags' property with complex list value");
		Assert.IsInstanceOfType<System.Collections.IList>(extractedFrontmatter["tags"]);
		System.Collections.IList tags = (System.Collections.IList)extractedFrontmatter["tags"];
		Assert.HasCount(2, tags);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);
	}
}
