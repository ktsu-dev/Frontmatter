// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class YamlSerializerTests
{
	[TestMethod]
	public void TryParseYamlObject_WithValidYaml_ReturnsTrue()
	{
		// Arrange
		string yamlContent = "title: Test Title\nauthor: Test Author";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsTrue(result, "TryParseYamlObject should return true for valid YAML");
		Assert.IsNotNull(parsedObject);
		Assert.HasCount(2, parsedObject);
		Assert.AreEqual("Test Title", parsedObject["title"]);
		Assert.AreEqual("Test Author", parsedObject["author"]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithInvalidYaml_ReturnsFalse()
	{
		// Arrange
		string yamlContent = "title: Test Title\nauthor: \n  invalidindent\n  : broken: syntax";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsFalse(result, "TryParseYamlObject should return false for invalid YAML");
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithEmptyString_ReturnsFalse()
	{
		// Arrange
		string yamlContent = "";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsFalse(result, "TryParseYamlObject should return false for empty string");
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithWhitespaceOnly_ReturnsFalse()
	{
		// Arrange
		string yamlContent = "   \n  \t  ";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsFalse(result, "TryParseYamlObject should return false for whitespace-only input");
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithNonDictionaryYaml_ReturnsFalse()
	{
		// Arrange - a YAML array instead of a dictionary
		string yamlContent = "- item1\n- item2\n- item3";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsFalse(result, "TryParseYamlObject should return false for non-dictionary YAML");
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithSpecialCharacters_ParsesCorrectly()
	{
		// Arrange
		string yamlContent = "title: \"Title with: colon\"\nauthor: 'Name with ''quotes'''\nsymbols: \"$%^&*()\"";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsTrue(result, "TryParseYamlObject should return true for YAML with special characters");
		Assert.IsNotNull(parsedObject);
		Assert.HasCount(3, parsedObject);
		Assert.AreEqual("Title with: colon", parsedObject["title"]);
		Assert.AreEqual("Name with 'quotes'", parsedObject["author"]);
		Assert.AreEqual("$%^&*()", parsedObject["symbols"]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithMultilineStrings_ParsesCorrectly()
	{
		// Arrange
		string yamlContent = "title: Test Title\ndescription: |\n  This is a multiline\n  description that spans\n  multiple lines.";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsTrue(result, "TryParseYamlObject should return true for YAML with multiline strings");
		Assert.IsNotNull(parsedObject);
		Assert.HasCount(2, parsedObject);

		string description = parsedObject["description"].ToString()!;
		Assert.Contains("This is a multiline", description, "Result should contain 'This is a multiline'");
		Assert.Contains("description that spans", description, "Result should contain 'description that spans'");
		Assert.Contains("multiple lines.", description, "Result should contain 'multiple lines.'");
	}

	[TestMethod]
	public void TryParseYamlObject_WithFoldedMultilineStrings_ParsesCorrectly()
	{
		// Arrange
		string yamlContent = "title: Test Title\ndescription: >\n  This is a folded multiline\n  description that should\n  be joined with spaces.";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? parsedObject);

		// Assert
		Assert.IsTrue(result, "TryParseYamlObject should return true for YAML with folded multiline strings");
		Assert.IsNotNull(parsedObject);
		Assert.HasCount(2, parsedObject);

		string description = parsedObject["description"].ToString()!;
		Assert.Contains("This is a folded multiline description that should be joined with spaces", description, "Result should contain 'This is a folded multiline description that should be joined with spaces'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithSimpleDictionary_SerializesCorrectly()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" }
		};

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.Contains("title: Test Title", result, "Result should contain 'title: Test Title'");
		Assert.Contains("author: Test Author", result, "Result should contain 'author: Test Author'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithEmptyDictionary_ReturnsEmptyString()
	{
		// Arrange
		Dictionary<string, object> dictionary = [];

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(string.IsNullOrWhiteSpace(result) || result == "{}" || result == "{}", "Result should be empty, whitespace, or '{}'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithPropertyContainingColon_EscapesCorrectly()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Test: Title with colon" }
		};

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		// The colon should be properly escaped in the output
		Assert.Contains("title:", result, "Result should contain 'title:'");
		Assert.Contains("Test: Title with colon", result, "Result should contain 'Test: Title with colon'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithNestedCollections_SerializesCorrectly()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Test Title" },
			{ "tags", new List<string> { "tag1", "tag2" } },
			{ "metadata", new Dictionary<string, object> {
				{ "created", new DateTime(2023, 1, 1) },
				{ "visibility", "public" }
			}}
		};

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.Contains("title: Test Title", result, "Result should contain 'title: Test Title'");
		Assert.Contains("tags:", result, "Result should contain 'tags:'");
		Assert.Contains("- tag1", result, "Result should contain '- tag1'");
		Assert.Contains("- tag2", result, "Result should contain '- tag2'");
		Assert.Contains("metadata:", result, "Result should contain 'metadata:'");
		Assert.Contains("created:", result, "Result should contain 'created:'");
		Assert.Contains("visibility: public", result, "Result should contain 'visibility: public'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithComplexNestedStructures_SerializesCorrectly()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Complex Document" },
			{ "sections", new List<object> {
				new Dictionary<string, object> {
					{ "name", "Section 1" },
					{ "content", "Content for section 1" },
					{ "subsections", new List<object> {
						new Dictionary<string, object> {
							{ "name", "Subsection 1.1" },
							{ "content", "Content for subsection 1.1" }
						},
						new Dictionary<string, object> {
							{ "name", "Subsection 1.2" },
							{ "content", "Content for subsection 1.2" }
						}
					}}
				},
				new Dictionary<string, object> {
					{ "name", "Section 2" },
					{ "content", "Content for section 2" }
				}
			}}
		};

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.Contains("title: Complex Document", result, "Result should contain 'title: Complex Document'");
		Assert.Contains("sections:", result, "Result should contain 'sections:'");
		Assert.Contains("- name: Section 1", result, "Result should contain '- name: Section 1'");
		Assert.Contains("content: Content for section 1", result, "Result should contain 'content: Content for section 1'");
		Assert.Contains("subsections:", result, "Result should contain 'subsections:'");
		Assert.Contains("- name: Subsection 1.1", result, "Result should contain '- name: Subsection 1.1'");
		Assert.Contains("content: Content for subsection 1.1", result, "Result should contain 'content: Content for subsection 1.1'");
		Assert.Contains("- name: Subsection 1.2", result, "Result should contain '- name: Subsection 1.2'");
		Assert.Contains("- name: Section 2", result, "Result should contain '- name: Section 2'");
		Assert.Contains("content: Content for section 2", result, "Result should contain 'content: Content for section 2'");
	}

	[TestMethod]
	public void SerializeYamlObject_WithMultilineString_FormatsCorrectly()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Test Title" },
			{ "description", "This is a\nmultiline\ndescription." }
		};

		// Act
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		// The multiline string should be properly serialized with the pipe character
		Assert.Contains("description:", result, "Result should contain 'description:'");
		Assert.Contains("This is a", result, "Result should contain 'This is a'");
		Assert.Contains("multiline", result, "Result should contain 'multiline'");
		Assert.Contains("description.", result, "Result should contain 'description.'");
	}

	[TestMethod]
	public void SerializeAndParse_RoundTrip_PreservesData()
	{
		// Arrange
		Dictionary<string, object> originalDictionary = new()
		{
			{ "title", "Test Title" },
			{ "tags", new List<string> { "tag1", "tag2" } },
			{ "count", 42 },
			{ "enabled", true }
		};

		// Act
		string serialized = YamlSerializer.SerializeYamlObject(originalDictionary);
		bool parseResult = YamlSerializer.TryParseYamlObject(serialized, out Dictionary<string, object>? parsedDictionary);

		// Assert
		Assert.IsTrue(parseResult, "TryParseYamlObject should return true for round-trip parsing");
		Assert.IsNotNull(parsedDictionary);
		Assert.HasCount(originalDictionary.Count, parsedDictionary);
		Assert.AreEqual(originalDictionary["title"], parsedDictionary["title"]);

		// YAML deserializer returns numbers as strings, so convert before comparison
		Assert.AreEqual(originalDictionary["count"].ToString(), parsedDictionary["count"]?.ToString());

		// YAML deserializer might return "true" instead of True, so compare as strings
		string? originalEnabledString = originalDictionary["enabled"]?.ToString()?.ToLowerInvariant();
		string? parsedEnabledString = parsedDictionary["enabled"]?.ToString()?.ToLowerInvariant();
		Assert.AreEqual(originalEnabledString, parsedEnabledString);

		// Check the list
		List<string>? originalTags = originalDictionary["tags"] as List<string>;
		System.Collections.IList? parsedTags = parsedDictionary["tags"] as System.Collections.IList;
		Assert.IsNotNull(originalTags);
		Assert.IsNotNull(parsedTags);
		Assert.HasCount(originalTags.Count, parsedTags);
		Assert.AreEqual(originalTags[0], parsedTags[0]);
		Assert.AreEqual(originalTags[1], parsedTags[1]);
	}
	private static readonly string[] value = ["item1", "item2"];

	[TestMethod]
	public void RoundTrip_WithComplexTypes_PreservesStructure()
	{
		// Arrange
		Dictionary<string, object> originalDictionary = new()
		{
			{ "title", "Complex Document" },
			{ "nestedObject", new Dictionary<string, object> {
				{ "key1", "value1" },
				{ "key2", 42 },
				{ "key3", true },
				{ "nestedArray", value }
			}}
		};

		// Act
		string serialized = YamlSerializer.SerializeYamlObject(originalDictionary);
		bool parseResult = YamlSerializer.TryParseYamlObject(serialized, out Dictionary<string, object>? parsedDictionary);

		// Assert
		Assert.IsTrue(parseResult, "TryParseYamlObject should return true for complex types round-trip");
		Assert.IsNotNull(parsedDictionary);
		Assert.HasCount(2, parsedDictionary);
		Assert.AreEqual("Complex Document", parsedDictionary["title"]);

		// Check nested object
		Dictionary<string, object>? nestedObject = parsedDictionary["nestedObject"] as Dictionary<string, object>;
		Assert.IsNotNull(nestedObject);
		Assert.HasCount(4, nestedObject);
		Assert.AreEqual("value1", nestedObject["key1"]);

		// Check nested array
		System.Collections.IList? nestedArray = nestedObject["nestedArray"] as System.Collections.IList;
		Assert.IsNotNull(nestedArray);
		Assert.HasCount(2, nestedArray);
		Assert.AreEqual("item1", nestedArray[0]);
		Assert.AreEqual("item2", nestedArray[1]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithCachedValues_ReturnsCachedResult()
	{
		// Arrange
		string yamlContent = "title: Test Title\nauthor: Test Author";

		// Act - First parse should add to cache
		bool firstParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? firstResult);
		Assert.IsTrue(firstParseResult, "First parse should succeed");
		Assert.IsNotNull(firstResult, "First parse result should not be null");

		// Modify the first result
		firstResult["marker"] = "modified";

		// Act - Second parse should retrieve from cache but return a new copy
		bool secondParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? secondResult);

		// Assert
		Assert.IsTrue(secondParseResult, "Second parse should succeed");
		Assert.IsNotNull(secondResult);
		Assert.IsFalse(secondResult.ContainsKey("marker"), "Second result should be a clean copy without modifications");
		Assert.AreEqual("Test Title", secondResult["title"]);
		Assert.AreEqual("Test Author", secondResult["author"]);

		// Verify deep cloning of nested structures
		yamlContent = "title: Test Title\nmetadata:\n  tags:\n    - tag1\n    - tag2";

		// First parse with nested structure
		bool nestedParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? nestedResult);
		Assert.IsTrue(nestedParseResult, "Nested parse should succeed");
		Assert.IsNotNull(nestedResult);

		// Modify nested structure
		Dictionary<string, object>? metadata = nestedResult["metadata"] as Dictionary<string, object>;
		Assert.IsNotNull(metadata);
		List<object>? tags = metadata["tags"] as List<object>;
		Assert.IsNotNull(tags);
		tags.Add("modified");

		// Get another copy from cache
		bool secondNestedParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out Dictionary<string, object>? secondNestedResult);
		Assert.IsTrue(secondNestedParseResult, "Second nested parse should succeed");
		Assert.IsNotNull(secondNestedResult);

		// Verify the second copy is clean
		metadata = secondNestedResult["metadata"] as Dictionary<string, object>;
		Assert.IsNotNull(metadata);
		tags = metadata["tags"] as List<object>;
		Assert.IsNotNull(tags);
		Assert.HasCount(2, tags, "Second result should have original number of tags");
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);
	}

	[TestMethod]
	public void SerializeYamlObject_WithCachedValues_ReturnsCachedResult()
	{
		// Arrange
		Dictionary<string, object> dictionary = new()
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" }
		};

		// Act - First serialization should add to cache
		string firstResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Modify the dictionary (should not affect cached result)
		dictionary["title"] = "Modified Title";

		// Act - Second serialization should retrieve from cache
		string secondResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert - Cache should not be used because the dictionary content has changed
		Assert.Contains("Modified Title", secondResult, "Result should contain 'Modified Title'");
		Assert.IsFalse(secondResult.Equals(firstResult), "Second result should not equal first result after modification");
	}
}
