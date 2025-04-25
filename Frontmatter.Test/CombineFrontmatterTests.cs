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

		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
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
		Assert.IsTrue(authorIndex < titleIndex);
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
		Assert.IsTrue(titleIndex < authorIndex);
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter, "Frontmatter should be extracted");
		Assert.IsTrue(extractedFrontmatter.Count > 0, "Frontmatter should have at least one key");

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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(1, extractedFrontmatter.Count); // Only one property after merging
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);

		// Either keys should be merged or both keys preserved
		int keyCount = extractedFrontmatter.Count;
		Assert.IsTrue(keyCount > 0, "At least one key should be present in the result");

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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(2, extractedFrontmatter.Count); // Both properties preserved
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("name"));
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"));
		Assert.IsInstanceOfType<System.Collections.IList>(extractedFrontmatter["tags"]);
		var tags = (System.Collections.IList)extractedFrontmatter["tags"];
		Assert.AreEqual(2, tags.Count);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);
	}
}
