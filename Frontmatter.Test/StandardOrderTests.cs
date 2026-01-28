// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
		Assert.IsNotEmpty(propertyNames, "PropertyNames array should not be empty");
	}

	[TestMethod]
	public void PropertyNames_ContainsStandardProperties()
	{
		// Act
		string[] propertyNames = StandardOrder.PropertyNames;

		// Assert
		// Check that essential frontmatter properties are included
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "title"), "PropertyNames should contain 'title'");
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "author"), "PropertyNames should contain 'author'");
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "date"), "PropertyNames should contain 'date'");
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "tags"), "PropertyNames should contain 'tags'");
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "categories"), "PropertyNames should contain 'categories'");
		Assert.IsTrue(Array.Exists(propertyNames, name => name == "description"), "PropertyNames should contain 'description'");
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
		Assert.IsLessThan(10, titleIndex, "Title should appear within the first 10 properties");

		// Author should appear after title but before most other properties
		Assert.IsGreaterThan(titleIndex, authorIndex, "Author should appear after title in standard order");

		// Date should be in a logical position
		Assert.IsGreaterThan(0, dateIndex, "Date should not be the first property");
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
		Assert.IsLessThan(10, Math.Abs(tagsIndex - categoriesIndex), "Tags and categories should be grouped within 10 positions of each other");

		// Tags and topics should be near each other if topics exists
		if (topicsIndex >= 0)
		{
			Assert.IsLessThan(10, Math.Abs(tagsIndex - topicsIndex), "Tags and topics should be grouped within 10 positions of each other");
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
		Assert.IsLessThan(authorIndex, titleIndex, "Title should appear before author in sorted order");

		// Title should appear before description
		Assert.IsLessThan(descriptionIndex, titleIndex, "Title should appear before description in sorted order");

		// Description should appear before author
		Assert.IsLessThan(authorIndex, descriptionIndex, "Description should appear before author in sorted order");

		// Tags should appear after author
		Assert.IsLessThan(tagsIndex, authorIndex, "Author should appear before tags in sorted order");
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
		Assert.IsLessThan(customFieldIndex, authorIndex, "Author should appear before custom_field when preserving original order");
		Assert.IsLessThan(titleIndex, customFieldIndex, "Custom_field should appear before title when preserving original order");
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
		Assert.IsLessThan(authorIndex, titleIndex, "Title should appear before author in sorted order");

		// Custom properties should appear after standard properties
		Assert.IsLessThan(customFieldIndex, authorIndex, "Standard properties should appear before custom properties in sorted order");
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
		Assert.IsLessThan(createdIndex, dateIndex, "Date should appear before created in standard order");
		Assert.IsLessThan(updatedIndex, createdIndex, "Created should appear before updated in standard order");
	}

	[TestMethod]
	public void CombineFrontmatter_WithSortedOrderAndMergeStrategy_AppliesBothCorrectly()
	{
		// Arrange - use Standard naming to get property name standardization
		string input = $"---{Environment.NewLine}" +
					   $"writer: Test Author{Environment.NewLine}" + // Should be merged to "author"
					   $"headline: Test Title{Environment.NewLine}" + // Should be merged to "title"
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act - use Standard naming to get property name standardization
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

		// Extract the frontmatter to verify property names and order
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);

		// Properties should be merged and standardized
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"), "Merged frontmatter should contain 'title'");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"), "Merged frontmatter should contain 'author'");

		// Extract sections from the result for position comparison
		int titleIndex = result.IndexOf("title:");
		int authorIndex = result.IndexOf("author:");

		// Properties should be ordered according to standard order - title before author
		Assert.IsLessThan(authorIndex, titleIndex, "Title should appear before author in sorted order");
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
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);

		// Verify standard property names were used
		Assert.IsTrue(extractedFrontmatter.ContainsKey("title"), "Standardized frontmatter should contain 'title'");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("author"), "Standardized frontmatter should contain 'author'");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"), "Standardized frontmatter should contain 'tags'");

		// Check position in the raw output for order verification
		int titleIndex = result.IndexOf("title:");
		int authorIndex = result.IndexOf("author:");
		int tagsIndex = result.IndexOf("tags:");

		// Verify order is correct according to StandardOrder - title before author, author before tags
		Assert.IsLessThan(authorIndex, titleIndex, "Title should appear before author in standard order");
		Assert.IsLessThan(tagsIndex, authorIndex, "Author should appear before tags in standard order");
	}
}
