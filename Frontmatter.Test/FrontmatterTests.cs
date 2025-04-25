namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FrontmatterTests
{
	[TestMethod]
	public void HasFrontmatter_WithFrontmatter_ReturnsTrue()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		bool result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void HasFrontmatter_WithoutFrontmatter_ReturnsFalse()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		bool result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithValidFrontmatter_ReturnsFrontmatterDictionary()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Test", result["title"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithoutFrontmatter_ReturnsNull()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public void ExtractBody_WithFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual("Content", result);
	}

	[TestMethod]
	public void ExtractBody_WithoutFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		string result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithoutFrontmatter_AddsFrontmatter()
	{
		// Arrange
		string input = "Content without frontmatter";
		var frontmatter = new Dictionary<string, object> { { "title", "Added Title" } };

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result));
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Added Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content without frontmatter", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithExistingFrontmatter_CombinesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var frontmatter = new Dictionary<string, object> { { "author", "Test Author" } };

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["author"]);
	}

	[TestMethod]
	public void RemoveFrontmatter_WithFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.IsTrue(result.StartsWith("Content"));
	}

	[TestMethod]
	public void RemoveFrontmatter_WithoutFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		string result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithExistingFrontmatter_ReplacesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var replacement = new Dictionary<string, object> { { "title", "New Title" } };

		// Act
		string result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("New Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void SerializeFrontmatter_WithValidDictionary_ReturnsYamlString()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" },
			{ "date", new DateTime(2023, 1, 1) }
		};

		// Act
		string result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("author: Test Author"));
		Assert.IsTrue(result.Contains("date: "));
	}

	[TestMethod]
	public void SerializeFrontmatter_WithEmptyDictionary_ReturnsEmptyString()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>();

		// Act
		string result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void SerializeFrontmatter_WithNullDictionary_ReturnsEmptyString()
	{
		// Act
		string result = Frontmatter.SerializeFrontmatter(null!);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}
}
