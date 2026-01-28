// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
		Assert.IsTrue(result, "Expected HasFrontmatter to return true for content with frontmatter");
	}

	[TestMethod]
	public void HasFrontmatter_WithoutFrontmatter_ReturnsFalse()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		bool result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result, "Expected HasFrontmatter to return false for content without frontmatter");
	}

	[TestMethod]
	public void HasFrontmatter_WithEmptyContent_ReturnsFalse()
	{
		// Arrange
		string input = string.Empty;

		// Act
		bool result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result, "Expected HasFrontmatter to return false for empty content");
	}

	[TestMethod]
	public void HasFrontmatter_WithContentStartingWithHyphen_ReturnsFalse()
	{
		// Arrange
		string input = $"--{Environment.NewLine}This is not frontmatter{Environment.NewLine}";

		// Act
		bool result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result, "Expected HasFrontmatter to return false for content starting with incomplete delimiter");
	}

	[TestMethod]
	public void ExtractFrontmatter_WithValidFrontmatter_ReturnsFrontmatterDictionary()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(1, result);
		Assert.AreEqual("Test", result["title"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithMultilineValues_ParsesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}description: |{Environment.NewLine}  This is a multiline{Environment.NewLine}  description for testing{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(2, result);
		Assert.AreEqual("Test", result["title"]);
		Assert.Contains("multiline", result["description"].ToString()!, "Expected description to contain 'multiline'");
		Assert.Contains("description for testing", result["description"].ToString()!, "Expected description to contain 'description for testing'");
	}

	[TestMethod]
	public void ExtractFrontmatter_WithEmptyFrontmatter_ReturnsNull()
	{
		// Arrange
		string input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		// Assert - empty frontmatter parses as null since there are no properties
		Assert.IsNull(result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithoutFrontmatter_ReturnsNull()
	{
		// Arrange
		string input = "Just content without frontmatter";

		// Act
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithComplexTypes_ParsesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}tags:{Environment.NewLine}- tag1{Environment.NewLine}- tag2{Environment.NewLine}nested:{Environment.NewLine}  key1: value1{Environment.NewLine}  key2: value2{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		Dictionary<string, object>? result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(3, result);
		Assert.AreEqual("Test", result["title"]);

		// Check tags list
		System.Collections.IList? tags = result["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.HasCount(2, tags);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);

		// Check nested dictionary - YamlDotNet returns IDictionary
		System.Collections.IDictionary? nested = result["nested"] as System.Collections.IDictionary;
		Assert.IsNotNull(nested);
		Assert.HasCount(2, nested);
		Assert.AreEqual("value1", nested["key1"]);
		Assert.AreEqual("value2", nested["key2"]);
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
	public void ExtractBody_WithEmptyFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual("Content", result);
	}

	[TestMethod]
	public void ExtractBody_WithMultilineFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}description: |{Environment.NewLine}  This is a multiline{Environment.NewLine}  description for testing{Environment.NewLine}---{Environment.NewLine}Content with multiple lines{Environment.NewLine}Second line{Environment.NewLine}Third line";

		// Act
		string result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual($"Content with multiple lines{Environment.NewLine}Second line{Environment.NewLine}Third line", result);
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
	public void ExtractBody_WithEmptyContent_ReturnsEmptyString()
	{
		// Arrange
		string input = string.Empty;

		// Act
		string result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithoutFrontmatter_AddsFrontmatter()
	{
		// Arrange
		string input = "Content without frontmatter";
		Dictionary<string, object> frontmatter = new() { { "title", "Added Title" } };

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result), "Result should have frontmatter after adding");
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Added Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content without frontmatter", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_WithNullFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		string input = "Content without frontmatter";

		// Act
		string result = Frontmatter.AddFrontmatter(input, null!);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_WithEmptyFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		string input = "Content without frontmatter";
		Dictionary<string, object> frontmatter = [];

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithExistingFrontmatter_CombinesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		Dictionary<string, object> frontmatter = new() { { "author", "Test Author" } };

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["author"]);
	}

	[TestMethod]
	public void AddFrontmatter_WithOverlappingKeys_UsesOriginalValue()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		Dictionary<string, object> frontmatter = new() { { "title", "New Title" } };

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
	}
	private static readonly string[] valueArr2 = ["tag1", "tag2"];

	[TestMethod]
	public void AddFrontmatter_WithComplexValues_SerializesCorrectly()
	{
		// Arrange
		string input = "Content without frontmatter";
		Dictionary<string, object> frontmatter = new() {
			{ "title", "Complex Title" },
			{ "tags", valueArr2 },
			{ "nested", new Dictionary<string, object> {
				{ "key1", "value1" },
				{ "key2", 42 }
			}}
		};

		// Act
		string result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Complex Title", extractedFrontmatter["title"]);

		// Verify tags array was serialized and deserialized correctly
		System.Collections.IList? tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.HasCount(2, tags);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);

		// Verify nested dictionary was serialized and deserialized correctly - YamlDotNet returns IDictionary
		System.Collections.IDictionary? nested = extractedFrontmatter["nested"] as System.Collections.IDictionary;
		Assert.IsNotNull(nested);
		Assert.HasCount(2, nested);
		Assert.AreEqual("value1", nested["key1"]);
		Assert.AreEqual("42", nested["key2"]!.ToString());
	}

	[TestMethod]
	public void RemoveFrontmatter_WithFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result), "Result should not have frontmatter after removal");
		Assert.StartsWith("Content", result, "Result should start with 'Content' after frontmatter removal");
	}

	[TestMethod]
	public void RemoveFrontmatter_WithEmptyFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result), "Result should not have frontmatter after removal");
		Assert.StartsWith("Content", result, "Result should start with 'Content' after empty frontmatter removal");
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
	public void RemoveFrontmatter_WithEmptyContent_ReturnsEmptyString()
	{
		// Arrange
		string input = string.Empty;

		// Act
		string result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithExistingFrontmatter_ReplacesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		Dictionary<string, object> replacement = new() { { "title", "New Title" } };

		// Act
		string result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("New Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithoutExistingFrontmatter_AddsFrontmatter()
	{
		// Arrange
		string input = "Content without frontmatter";
		Dictionary<string, object> replacement = new() { { "title", "New Title" } };

		// Act
		string result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("New Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content without frontmatter", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithNullFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		string result = Frontmatter.ReplaceFrontmatter(input, null!);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result), "Result should not have frontmatter after replacement with null");
		Assert.AreEqual("Content", result.TrimEnd());
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithEmptyFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		Dictionary<string, object> replacement = [];

		// Act
		string result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result), "Result should not have frontmatter after replacement with empty dictionary");
		Assert.AreEqual("Content", result.TrimEnd());
	}

	[TestMethod]
	public void SerializeFrontmatter_WithValidDictionary_ReturnsYamlString()
	{
		// Arrange
		Dictionary<string, object> frontmatter = new()
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" },
			{ "date", new DateTime(2023, 1, 1) }
		};

		// Act
		string result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.Contains("title: Test Title", result, "Serialized result should contain 'title: Test Title'");
		Assert.Contains("author: Test Author", result, "Serialized result should contain 'author: Test Author'");
		Assert.Contains("date: ", result, "Serialized result should contain 'date: '");
	}
	private static readonly string[] valueArr3 = ["tag1", "tag2", "tag3"];

	[TestMethod]
	public void SerializeFrontmatter_WithComplexNestedTypes_SerializesCorrectly()
	{
		// Arrange
		Dictionary<string, object> frontmatter = new()
		{
			{ "title", "Test Title" },
			{ "tags", valueArr3 },
			{ "metadata", new Dictionary<string, object>
				{
					{ "created", new DateTime(2023, 1, 1) },
					{ "updated", new DateTime(2023, 2, 1) },
					{ "status", "published" }
				}
			}
		};

		// Act
		string result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.Contains("title: Test Title", result, "Serialized result should contain 'title: Test Title'");
		Assert.Contains("tags:", result, "Serialized result should contain 'tags:'");
		Assert.Contains("- tag1", result, "Serialized result should contain '- tag1'");
		Assert.Contains("- tag2", result, "Serialized result should contain '- tag2'");
		Assert.Contains("- tag3", result, "Serialized result should contain '- tag3'");
		Assert.Contains("metadata:", result, "Serialized result should contain 'metadata:'");
		Assert.Contains("created:", result, "Serialized result should contain 'created:'");
		Assert.Contains("updated:", result, "Serialized result should contain 'updated:'");
		Assert.Contains("status: published", result, "Serialized result should contain 'status: published'");
	}

	[TestMethod]
	public void SerializeFrontmatter_WithEmptyDictionary_ReturnsEmptyString()
	{
		// Arrange
		Dictionary<string, object> frontmatter = [];

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

	[TestMethod]
	public void CombineFrontmatterRoundTrip_PreservesAllData()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: Original Title{Environment.NewLine}" +
					   $"author: Original Author{Environment.NewLine}" +
					   $"tags:{Environment.NewLine}" +
					   $"  - tag1{Environment.NewLine}" +
					   $"  - tag2{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Original content{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"keywords:{Environment.NewLine}" +
					   $"  - keyword1{Environment.NewLine}" +
					   $"  - keyword2{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"More content";

		// Act - default CombineFrontmatter uses Conservative merge which merges keywords into tags
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result), "Combined result should have frontmatter");

		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);

		// Check original values are preserved
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Original Author", extractedFrontmatter["author"]);

		// Check that tags is present (keywords is merged into tags with Conservative strategy)
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"), "Extracted frontmatter should contain 'tags' key");

		// Check content is correctly preserved
		string body = Frontmatter.ExtractBody(result);
		Assert.Contains("Original content", body, "Body should contain 'Original content'");
		Assert.Contains("More content", body, "Body should contain 'More content'");
	}

	[TestMethod]
	public void CacheConsistency_MultipleCalls_ReturnsSameResult()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act - First call should process and cache
		string firstResult = Frontmatter.CombineFrontmatter(input);

		// Act - Second call should use cache
		string secondResult = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.AreEqual(firstResult, secondResult);
	}
}
