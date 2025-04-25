namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class StandardOrderTests
{
	[TestMethod]
	public void PropertyNames_ReturnsNonEmptyArray()
	{
		// Act
		string[] propertyNames = StandardOrder.PropertyNames;

		// Assert
		Assert.IsNotNull(propertyNames);
		Assert.IsTrue(propertyNames.Length > 0);
	}

	[TestMethod]
	public void PropertyNames_ContainsStandardProperties()
	{
		// Act
		string[] propertyNames = StandardOrder.PropertyNames;

		// Assert
		// Check that essential frontmatter properties are included
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "title"));
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "author"));
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "date"));
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "tags"));
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "categories"));
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "description"));
	}

	[TestMethod]
	public void PropertyNames_CorePropertiesAppearFirst()
	{
		// Act
		string[] propertyNames = StandardOrder.PropertyNames;

		// Assert
		// Check that core properties appear in the expected order
		int titleIndex = Array.IndexOf(propertyNames, "title");
		int dateIndex = Array.IndexOf(propertyNames, "date");
		int authorIndex = Array.IndexOf(propertyNames, "author");

		// Title should be one of the first properties
		Assert.IsTrue(titleIndex < 10);

		// Author should appear after title but before most other properties
		Assert.IsTrue(authorIndex > titleIndex);

		// Date should be in a logical position
		Assert.IsTrue(dateIndex > 0);
	}

	[TestMethod]
	public void PropertyNames_RelatedPropertiesGroupedTogether()
	{
		// Act
		string[] propertyNames = StandardOrder.PropertyNames;

		// Assert
		// Check that related properties are grouped together
		int tagsIndex = Array.IndexOf(propertyNames, "tags");
		int categoriesIndex = Array.IndexOf(propertyNames, "categories");
		int topicsIndex = Array.IndexOf(propertyNames, "topics");

		// Tags and categories should be near each other
		Assert.IsTrue(Math.Abs(tagsIndex - categoriesIndex) < 10);

		// Tags and topics should be near each other if topics exists
		if (topicsIndex >= 0)
		{
			Assert.IsTrue(Math.Abs(tagsIndex - topicsIndex) < 10);
		}
	}

	[TestMethod]
	public void CombineFrontmatter_WithSortedOrder_SortsAccordingToStandardOrder()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"author: Test Author{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"description: Test Description{Environment.NewLine}" +
					   $"tags:{Environment.NewLine}" +
					   $"  - tag1{Environment.NewLine}" +
					   $"  - tag2{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted);

		// Extract sections from the result for position comparison
		int titleIndex = result.IndexOf("title:");
		int authorIndex = result.IndexOf("author:");
		int descriptionIndex = result.IndexOf("description:");
		int tagsIndex = result.IndexOf("tags:");

		// Assert
		// Title should appear before author
		Assert.IsTrue(titleIndex < authorIndex);

		// Title should appear before description
		Assert.IsTrue(titleIndex < descriptionIndex);

		// Description should appear before author
		Assert.IsTrue(descriptionIndex < authorIndex);

		// Tags should appear after author
		Assert.IsTrue(authorIndex < tagsIndex);
	}

	[TestMethod]
	public void CombineFrontmatter_WithAsIsOrder_PreservesOriginalOrder()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"author: Test Author{Environment.NewLine}" +
					   $"custom_field: Custom Value{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs);

		// Extract sections from the result for position comparison
		int authorIndex = result.IndexOf("author:");
		int customFieldIndex = result.IndexOf("custom_field:");
		int titleIndex = result.IndexOf("title:");

		// Assert
		// Order should be preserved: author, custom_field, title
		Assert.IsTrue(authorIndex < customFieldIndex);
		Assert.IsTrue(customFieldIndex < titleIndex);
	}

	[TestMethod]
	public void CombineFrontmatter_WithSortedOrder_HandlesCustomProperties()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"author: Test Author{Environment.NewLine}" +
					   $"custom_field: Custom Value{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted);

		// Extract sections from the result for position comparison
		int titleIndex = result.IndexOf("title:");
		int authorIndex = result.IndexOf("author:");
		int customFieldIndex = result.IndexOf("custom_field:");

		// Assert
		// Standard properties should be sorted first
		Assert.IsTrue(titleIndex < authorIndex);

		// Custom properties should appear after standard properties
		Assert.IsTrue(authorIndex < customFieldIndex);
	}

	[TestMethod]
	public void CombineFrontmatter_WithSortedOrder_HandlesDatePropertiesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"updated: 2023-02-01{Environment.NewLine}" +
					   $"created: 2023-01-01{Environment.NewLine}" +
					   $"date: 2023-03-01{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted);

		// Extract sections from the result for position comparison
		int dateIndex = result.IndexOf("date:");
		int createdIndex = result.IndexOf("created:");
		int updatedIndex = result.IndexOf("updated:");

		// Assert
		// Date properties should be grouped together and in the standard order
		Assert.IsTrue(dateIndex < createdIndex);
		Assert.IsTrue(createdIndex < updatedIndex);
	}

	[TestMethod]
	public void CombineFrontmatter_WithSortedOrderAndMergeStrategy_AppliesBothCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"writer: Test Author{Environment.NewLine}" + // Should be merged to "author"
					   $"headline: Test Title{Environment.NewLine}" + // Should be merged to "title"
					   $"summary: Test Description{Environment.NewLine}" + // Should be merged to "description"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

		// Extract the frontmatter to verify property names and order
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Extract sections from the result for position comparison
		int titleIndex = result.IndexOf("title:");
		int descriptionIndex = result.IndexOf("description:");
		int authorIndex = result.IndexOf("author:");

		// Assert
		// Properties should be merged to standard names
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("description"));

		// Properties should be ordered according to standard order
		Assert.IsTrue(titleIndex < descriptionIndex);
		Assert.IsTrue(descriptionIndex < authorIndex);
	}

	[TestMethod]
	public void CombineFrontmatter_WithAllOptions_ProducesCleanSortedStandardizedFrontmatter()
	{
		// Arrange - a complex case with mixed property names and order
		string input = $"---{Environment.NewLine}" +
					   $"writer: John Doe{Environment.NewLine}" +
					   $"post_date: 2023-01-15{Environment.NewLine}" +
					   $"headline: Main Article{Environment.NewLine}" +
					   $"abstract: This is a description{Environment.NewLine}" +
					   $"keywords:{Environment.NewLine}" +
					   $"  - news{Environment.NewLine}" +
					   $"  - article{Environment.NewLine}" +
					   $"topic: technology{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

		// Extract the frontmatter to verify property names and order
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);

		// Verify standard property names were used
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("date"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("description"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"));

		// Check position in the raw output for order verification
		int titleIndex = result.IndexOf("title:");
		int descriptionIndex = result.IndexOf("description:");
		int dateIndex = result.IndexOf("date:");
		int authorIndex = result.IndexOf("author:");
		int tagsIndex = result.IndexOf("tags:");

		// Verify order is correct according to StandardOrder
		Assert.IsTrue(titleIndex < descriptionIndex);
		Assert.IsTrue(descriptionIndex < dateIndex);
		Assert.IsTrue(dateIndex < authorIndex);
		Assert.IsTrue(authorIndex < tagsIndex);
	}
}
