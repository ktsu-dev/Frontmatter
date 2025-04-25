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
		int dateIndex = Array.IndexOf(propertyNames, "date");
		int createdIndex = Array.IndexOf(propertyNames, "created");
		int updatedIndex = Array.IndexOf(propertyNames, "updated");
		int modifiedIndex = Array.IndexOf(propertyNames, "modified");

		// Date-related properties should be near each other
		Assert.IsTrue(Math.Abs(dateIndex - createdIndex) < 10);
		Assert.IsTrue(Math.Abs(dateIndex - updatedIndex) < 10);
		Assert.IsTrue(Math.Abs(dateIndex - modifiedIndex) < 10);

		// Check tags and categories
		int tagsIndex = Array.IndexOf(propertyNames, "tags");
		int categoriesIndex = Array.IndexOf(propertyNames, "categories");

		// Tags and categories should be near each other
		Assert.IsTrue(Math.Abs(tagsIndex - categoriesIndex) < 10);
	}

	[TestMethod]
	public void SortFrontmatterProperties_WithMixedProperties_SortsCorrectly()
	{
		// This test indirectly tests the SortFrontmatterProperties method through the CombineFrontmatter method

		// Arrange
		string input = $"---{Environment.NewLine}" +
					  $"layout: post{Environment.NewLine}" +
					  $"author: Test Author{Environment.NewLine}" +
					  $"title: Test Title{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.Sorted);

		// Assert
		string frontmatterSection = result.Substring(result.IndexOf("---") + 3,
			result.IndexOf("---", result.IndexOf("---") + 3) - result.IndexOf("---") - 3).Trim();

		// In standard order, title should appear before author, and both should appear before layout
		int titleIndex = frontmatterSection.IndexOf("title:");
		int authorIndex = frontmatterSection.IndexOf("author:");
		int layoutIndex = frontmatterSection.IndexOf("layout:");

		Assert.IsTrue(titleIndex < authorIndex);
		Assert.IsTrue(authorIndex < layoutIndex);
	}
}
