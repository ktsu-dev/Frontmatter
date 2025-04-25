namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NameStandardizerTests
{
	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_StandardizesPropertyNames()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"headline: Test Title{Environment.NewLine}" + // Should be standardized to "title"
					   $"writer: Test Author{Environment.NewLine}" +  // Should be standardized to "author"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(2, extractedFrontmatter.Count);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"));
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["author"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithAsIsNaming_PreservesOriginalPropertyNames()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"headline: Test Title{Environment.NewLine}" +
					   $"writer: Test Author{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(2, extractedFrontmatter.Count);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("headline"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("writer"));
		Assert.AreEqual("Test Title", extractedFrontmatter["headline"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["writer"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_HandlesMultipleVariantsOfSameProperty()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"headline: First Title{Environment.NewLine}" +  // Should be standardized to "title"
					   $"title: Second Title{Environment.NewLine}" +    // Already standard
					   $"post-title: Third Title{Environment.NewLine}" + // Should be standardized to "title"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);

		// Should merge to title, but keep the value from the first occurrence
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.AreEqual("First Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_PreservesUnknownProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"custom_property: Custom Value{Environment.NewLine}" + // Not in standard properties
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(2, extractedFrontmatter.Count);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("custom_property"));
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Custom Value", extractedFrontmatter["custom_property"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_StandardizesSimilarProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"desc: Short description{Environment.NewLine}" +      // Should be standardized to "description"
					   $"abstract: Longer description{Environment.NewLine}" + // Should be standardized to "description"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("description"));
		Assert.AreEqual("Short description", extractedFrontmatter["description"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_HandlesEmptyFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(0, extractedFrontmatter.Count);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_PreservesDataTypes()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"publish_date: 2023-01-01{Environment.NewLine}" + // Should be standardized to "date"
					   $"tags:{Environment.NewLine}" +
					   $"  - tag1{Environment.NewLine}" +
					   $"  - tag2{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);

		// Should standardize property names but preserve data types
		Assert.IsTrue(extractedFrontmatter.ContainsKey("date"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"));

		// Verify tags is still a collection
		var tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.AreEqual(2, tags.Count);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNaming_HandlesPrefixesAndSuffixes()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"post_title: Test Title{Environment.NewLine}" + // Should be standardized to "title"
					   $"author_name: Test Author{Environment.NewLine}" + // Should be standardized to "author"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"));
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["author"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithStandardNamingAndSorting_StandardizesAndSorts()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"writer: Test Author{Environment.NewLine}" +   // Should be standardized to "author" and sorted
					   $"headline: Test Title{Environment.NewLine}" +  // Should be standardized to "title" and sorted
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.Sorted);

		// Assert - verify the order in the serialized YAML by checking the raw string
		Assert.IsTrue(result.IndexOf("title:") < result.IndexOf("author:"),
			"Title should appear before author in the sorted output");
	}
}
