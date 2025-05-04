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
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void HasFrontmatter_WithoutFrontmatter_ReturnsFalse()
	{
		// Arrange
		var input = "Just content without frontmatter";

		// Act
		var result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void HasFrontmatter_WithEmptyContent_ReturnsFalse()
	{
		// Arrange
		var input = string.Empty;

		// Act
		var result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void HasFrontmatter_WithContentStartingWithHyphen_ReturnsFalse()
	{
		// Arrange
		var input = $"--{Environment.NewLine}This is not frontmatter{Environment.NewLine}";

		// Act
		var result = Frontmatter.HasFrontmatter(input);

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithValidFrontmatter_ReturnsFrontmatterDictionary()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Test", result["title"]);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithMultilineValues_ParsesCorrectly()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}description: |{Environment.NewLine}  This is a multiline{Environment.NewLine}  description for testing{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("Test", result["title"]);
		Assert.IsTrue(result["description"].ToString()!.Contains("multiline"));
		Assert.IsTrue(result["description"].ToString()!.Contains("description for testing"));
	}

	[TestMethod]
	public void ExtractFrontmatter_WithEmptyFrontmatter_ReturnsEmptyDictionary()
	{
		// Arrange
		var input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithoutFrontmatter_ReturnsNull()
	{
		// Arrange
		var input = "Just content without frontmatter";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithComplexTypes_ParsesCorrectly()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}tags:{Environment.NewLine}- tag1{Environment.NewLine}- tag2{Environment.NewLine}nested:{Environment.NewLine}  key1: value1{Environment.NewLine}  key2: value2{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.Count);
		Assert.AreEqual("Test", result["title"]);

		// Check tags list
		var tags = result["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.AreEqual(2, tags.Count);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);

		// Check nested dictionary
		var nested = result["nested"] as Dictionary<object, object>;
		Assert.IsNotNull(nested);
		Assert.AreEqual(2, nested.Count);
		Assert.AreEqual("value1", nested["key1"]);
		Assert.AreEqual("value2", nested["key2"]);
	}

	[TestMethod]
	public void ExtractBody_WithFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual("Content", result);
	}

	[TestMethod]
	public void ExtractBody_WithEmptyFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		var input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual("Content", result);
	}

	[TestMethod]
	public void ExtractBody_WithMultilineFrontmatter_ReturnsBodyOnly()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}description: |{Environment.NewLine}  This is a multiline{Environment.NewLine}  description for testing{Environment.NewLine}---{Environment.NewLine}Content with multiple lines{Environment.NewLine}Second line{Environment.NewLine}Third line";

		// Act
		var result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual($"Content with multiple lines{Environment.NewLine}Second line{Environment.NewLine}Third line", result);
	}

	[TestMethod]
	public void ExtractBody_WithoutFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		var input = "Just content without frontmatter";

		// Act
		var result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void ExtractBody_WithEmptyContent_ReturnsEmptyString()
	{
		// Arrange
		var input = string.Empty;

		// Act
		var result = Frontmatter.ExtractBody(input);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithoutFrontmatter_AddsFrontmatter()
	{
		// Arrange
		var input = "Content without frontmatter";
		var frontmatter = new Dictionary<string, object> { { "title", "Added Title" } };

		// Act
		var result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result));
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Added Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content without frontmatter", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void AddFrontmatter_WithNullFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		var input = "Content without frontmatter";

		// Act
		var result = Frontmatter.AddFrontmatter(input, null!);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_WithEmptyFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		var input = "Content without frontmatter";
		var frontmatter = new Dictionary<string, object>();

		// Act
		var result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void AddFrontmatter_ToContentWithExistingFrontmatter_CombinesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var frontmatter = new Dictionary<string, object> { { "author", "Test Author" } };

		// Act
		var result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Author", extractedFrontmatter["author"]);
	}

	[TestMethod]
	public void AddFrontmatter_WithOverlappingKeys_UsesOriginalValue()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var frontmatter = new Dictionary<string, object> { { "title", "New Title" } };

		// Act
		var result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
	}
	private static readonly string[] valueArr2 = ["tag1", "tag2"];

	[TestMethod]
	public void AddFrontmatter_WithComplexValues_SerializesCorrectly()
	{
		// Arrange
		var input = "Content without frontmatter";
		var frontmatter = new Dictionary<string, object> {
			{ "title", "Complex Title" },
			{ "tags", valueArr2 },
			{ "nested", new Dictionary<string, object> {
				{ "key1", "value1" },
				{ "key2", 42 }
			}}
		};

		// Act
		var result = Frontmatter.AddFrontmatter(input, frontmatter);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("Complex Title", extractedFrontmatter["title"]);

		// Verify tags array was serialized and deserialized correctly
		var tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.AreEqual(2, tags.Count);
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);

		// Verify nested dictionary was serialized and deserialized correctly
		var nested = extractedFrontmatter["nested"] as Dictionary<object, object>;
		Assert.IsNotNull(nested);
		Assert.AreEqual(2, nested.Count);
		Assert.AreEqual("value1", nested["key1"]);
		Assert.AreEqual("42", nested["key2"].ToString());
	}

	[TestMethod]
	public void RemoveFrontmatter_WithFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.IsTrue(result.StartsWith("Content"));
	}

	[TestMethod]
	public void RemoveFrontmatter_WithEmptyFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.IsTrue(result.StartsWith("Content"));
	}

	[TestMethod]
	public void RemoveFrontmatter_WithoutFrontmatter_ReturnsOriginalContent()
	{
		// Arrange
		var input = "Just content without frontmatter";

		// Act
		var result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void RemoveFrontmatter_WithEmptyContent_ReturnsEmptyString()
	{
		// Arrange
		var input = string.Empty;

		// Act
		var result = Frontmatter.RemoveFrontmatter(input);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithExistingFrontmatter_ReplacesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var replacement = new Dictionary<string, object> { { "title", "New Title" } };

		// Act
		var result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("New Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithoutExistingFrontmatter_AddsFrontmatter()
	{
		// Arrange
		var input = "Content without frontmatter";
		var replacement = new Dictionary<string, object> { { "title", "New Title" } };

		// Act
		var result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual("New Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Content without frontmatter", Frontmatter.ExtractBody(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithNullFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";

		// Act
		var result = Frontmatter.ReplaceFrontmatter(input, null!);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.AreEqual("Content", result.TrimEnd());
	}

	[TestMethod]
	public void ReplaceFrontmatter_WithEmptyFrontmatter_RemovesFrontmatter()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Original Title{Environment.NewLine}---{Environment.NewLine}Content";
		var replacement = new Dictionary<string, object>();

		// Act
		var result = Frontmatter.ReplaceFrontmatter(input, replacement);

		// Assert
		Assert.IsFalse(Frontmatter.HasFrontmatter(result));
		Assert.AreEqual("Content", result.TrimEnd());
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
		var result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("author: Test Author"));
		Assert.IsTrue(result.Contains("date: "));
	}
	private static readonly string[] valueArr3 = ["tag1", "tag2", "tag3"];

	[TestMethod]
	public void SerializeFrontmatter_WithComplexNestedTypes_SerializesCorrectly()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>
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
		var result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("tags:"));
		Assert.IsTrue(result.Contains("- tag1"));
		Assert.IsTrue(result.Contains("- tag2"));
		Assert.IsTrue(result.Contains("- tag3"));
		Assert.IsTrue(result.Contains("metadata:"));
		Assert.IsTrue(result.Contains("created:"));
		Assert.IsTrue(result.Contains("updated:"));
		Assert.IsTrue(result.Contains("status: published"));
	}

	[TestMethod]
	public void SerializeFrontmatter_WithEmptyDictionary_ReturnsEmptyString()
	{
		// Arrange
		var frontmatter = new Dictionary<string, object>();

		// Act
		var result = Frontmatter.SerializeFrontmatter(frontmatter);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void SerializeFrontmatter_WithNullDictionary_ReturnsEmptyString()
	{
		// Act
		var result = Frontmatter.SerializeFrontmatter(null!);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void CombineFrontmatterRoundTrip_PreservesAllData()
	{
		// Arrange
		var input = $"---{Environment.NewLine}" +
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

		// Act
		var result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.IsTrue(Frontmatter.HasFrontmatter(result));

		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);

		// Check original values are preserved
		Assert.AreEqual("Original Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Original Author", extractedFrontmatter["author"]);

		// Check that tags and keywords are both present
		Assert.IsTrue(extractedFrontmatter.ContainsKey("tags"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("keywords"));

		// Check content is correctly preserved
		var body = Frontmatter.ExtractBody(result);
		Assert.IsTrue(body.Contains("Original content"));
		Assert.IsTrue(body.Contains("More content"));
	}

	[TestMethod]
	public void CacheConsistency_MultipleCalls_ReturnsSameResult()
	{
		// Arrange
		var input = $"---{Environment.NewLine}title: Test{Environment.NewLine}---{Environment.NewLine}Content";

		// Act - First call should process and cache
		var firstResult = Frontmatter.CombineFrontmatter(input);

		// Act - Second call should use cache
		var secondResult = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.AreEqual(firstResult, secondResult);
	}
}
