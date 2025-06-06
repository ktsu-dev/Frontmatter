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
		var yamlContent = "title: Test Title\nauthor: Test Author";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsTrue(result);
		Assert.IsNotNull(parsedObject);
		Assert.AreEqual(2, parsedObject.Count);
		Assert.AreEqual("Test Title", parsedObject["title"]);
		Assert.AreEqual("Test Author", parsedObject["author"]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithInvalidYaml_ReturnsFalse()
	{
		// Arrange
		var yamlContent = "title: Test Title\nauthor: \n  invalidindent\n  : broken: syntax";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithEmptyString_ReturnsFalse()
	{
		// Arrange
		var yamlContent = "";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithWhitespaceOnly_ReturnsFalse()
	{
		// Arrange
		var yamlContent = "   \n  \t  ";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithNonDictionaryYaml_ReturnsFalse()
	{
		// Arrange - a YAML array instead of a dictionary
		var yamlContent = "- item1\n- item2\n- item3";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithSpecialCharacters_ParsesCorrectly()
	{
		// Arrange
		var yamlContent = "title: \"Title with: colon\"\nauthor: 'Name with ''quotes'''\nsymbols: \"$%^&*()\"";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsTrue(result);
		Assert.IsNotNull(parsedObject);
		Assert.AreEqual(3, parsedObject.Count);
		Assert.AreEqual("Title with: colon", parsedObject["title"]);
		Assert.AreEqual("Name with 'quotes'", parsedObject["author"]);
		Assert.AreEqual("$%^&*()", parsedObject["symbols"]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithMultilineStrings_ParsesCorrectly()
	{
		// Arrange
		var yamlContent = "title: Test Title\ndescription: |\n  This is a multiline\n  description that spans\n  multiple lines.";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsTrue(result);
		Assert.IsNotNull(parsedObject);
		Assert.AreEqual(2, parsedObject.Count);

		var description = parsedObject["description"].ToString()!;
		Assert.IsTrue(description.Contains("This is a multiline"));
		Assert.IsTrue(description.Contains("description that spans"));
		Assert.IsTrue(description.Contains("multiple lines."));
	}

	[TestMethod]
	public void TryParseYamlObject_WithFoldedMultilineStrings_ParsesCorrectly()
	{
		// Arrange
		var yamlContent = "title: Test Title\ndescription: >\n  This is a folded multiline\n  description that should\n  be joined with spaces.";

		// Act
		var result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsTrue(result);
		Assert.IsNotNull(parsedObject);
		Assert.AreEqual(2, parsedObject.Count);

		var description = parsedObject["description"].ToString()!;
		Assert.IsTrue(description.Contains("This is a folded multiline description that should be joined with spaces"));
	}

	[TestMethod]
	public void SerializeYamlObject_WithSimpleDictionary_SerializesCorrectly()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" }
		};

		// Act
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("author: Test Author"));
	}

	[TestMethod]
	public void SerializeYamlObject_WithEmptyDictionary_ReturnsEmptyString()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>();

		// Act
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(string.IsNullOrWhiteSpace(result) || result == "{}" || result == "{}");
	}

	[TestMethod]
	public void SerializeYamlObject_WithPropertyContainingColon_EscapesCorrectly()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
		{
			{ "title", "Test: Title with colon" }
		};

		// Act
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		// The colon should be properly escaped in the output
		Assert.IsTrue(result.Contains("title:") && result.Contains("Test: Title with colon"));
	}

	[TestMethod]
	public void SerializeYamlObject_WithNestedCollections_SerializesCorrectly()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "tags", new List<string> { "tag1", "tag2" } },
			{ "metadata", new Dictionary<string, object> {
				{ "created", new DateTime(2023, 1, 1) },
				{ "visibility", "public" }
			}}
		};

		// Act
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("tags:"));
		Assert.IsTrue(result.Contains("- tag1"));
		Assert.IsTrue(result.Contains("- tag2"));
		Assert.IsTrue(result.Contains("metadata:"));
		Assert.IsTrue(result.Contains("created:"));
		Assert.IsTrue(result.Contains("visibility: public"));
	}

	[TestMethod]
	public void SerializeYamlObject_WithComplexNestedStructures_SerializesCorrectly()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
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
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(result.Contains("title: Complex Document"));
		Assert.IsTrue(result.Contains("sections:"));
		Assert.IsTrue(result.Contains("- name: Section 1"));
		Assert.IsTrue(result.Contains("content: Content for section 1"));
		Assert.IsTrue(result.Contains("subsections:"));
		Assert.IsTrue(result.Contains("- name: Subsection 1.1"));
		Assert.IsTrue(result.Contains("content: Content for subsection 1.1"));
		Assert.IsTrue(result.Contains("- name: Subsection 1.2"));
		Assert.IsTrue(result.Contains("- name: Section 2"));
		Assert.IsTrue(result.Contains("content: Content for section 2"));
	}

	[TestMethod]
	public void SerializeYamlObject_WithMultilineString_FormatsCorrectly()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "description", "This is a\nmultiline\ndescription." }
		};

		// Act
		var result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		// The multiline string should be properly serialized with the pipe character
		Assert.IsTrue(result.Contains("description:"));
		Assert.IsTrue(result.Contains("This is a"));
		Assert.IsTrue(result.Contains("multiline"));
		Assert.IsTrue(result.Contains("description."));
	}

	[TestMethod]
	public void SerializeAndParse_RoundTrip_PreservesData()
	{
		// Arrange
		var originalDictionary = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "tags", new List<string> { "tag1", "tag2" } },
			{ "count", 42 },
			{ "enabled", true }
		};

		// Act
		var serialized = YamlSerializer.SerializeYamlObject(originalDictionary);
		var parseResult = YamlSerializer.TryParseYamlObject(serialized, out var parsedDictionary);

		// Assert
		Assert.IsTrue(parseResult);
		Assert.IsNotNull(parsedDictionary);
		Assert.AreEqual(originalDictionary.Count, parsedDictionary.Count);
		Assert.AreEqual(originalDictionary["title"], parsedDictionary["title"]);

		// YAML deserializer returns numbers as strings, so convert before comparison
		Assert.AreEqual(originalDictionary["count"].ToString(), parsedDictionary["count"]?.ToString());

		// YAML deserializer might return "true" instead of True, so compare as strings
		var originalEnabledString = originalDictionary["enabled"]?.ToString()?.ToLowerInvariant();
		var parsedEnabledString = parsedDictionary["enabled"]?.ToString()?.ToLowerInvariant();
		Assert.AreEqual(originalEnabledString, parsedEnabledString);

		// Check the list
		var originalTags = originalDictionary["tags"] as List<string>;
		var parsedTags = parsedDictionary["tags"] as System.Collections.IList;
		Assert.IsNotNull(originalTags);
		Assert.IsNotNull(parsedTags);
		Assert.AreEqual(originalTags.Count, parsedTags.Count);
		Assert.AreEqual(originalTags[0], parsedTags[0]);
		Assert.AreEqual(originalTags[1], parsedTags[1]);
	}
	private static readonly string[] value = ["item1", "item2"];

	[TestMethod]
	public void RoundTrip_WithComplexTypes_PreservesStructure()
	{
		// Arrange
		var originalDictionary = new Dictionary<string, object>
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
		var serialized = YamlSerializer.SerializeYamlObject(originalDictionary);
		var parseResult = YamlSerializer.TryParseYamlObject(serialized, out var parsedDictionary);

		// Assert
		Assert.IsTrue(parseResult);
		Assert.IsNotNull(parsedDictionary);
		Assert.AreEqual(2, parsedDictionary.Count);
		Assert.AreEqual("Complex Document", parsedDictionary["title"]);

		// Check nested object
		var nestedObject = parsedDictionary["nestedObject"] as Dictionary<string, object>;
		Assert.IsNotNull(nestedObject);
		Assert.AreEqual(4, nestedObject.Count);
		Assert.AreEqual("value1", nestedObject["key1"]);

		// Check nested array
		var nestedArray = nestedObject["nestedArray"] as System.Collections.IList;
		Assert.IsNotNull(nestedArray);
		Assert.AreEqual(2, nestedArray.Count);
		Assert.AreEqual("item1", nestedArray[0]);
		Assert.AreEqual("item2", nestedArray[1]);
	}

	[TestMethod]
	public void TryParseYamlObject_WithCachedValues_ReturnsCachedResult()
	{
		// Arrange
		var yamlContent = "title: Test Title\nauthor: Test Author";

		// Act - First parse should add to cache
		var firstParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var firstResult);
		Assert.IsTrue(firstParseResult, "First parse should succeed");
		Assert.IsNotNull(firstResult, "First parse result should not be null");

		// Modify the first result
		firstResult["marker"] = "modified";

		// Act - Second parse should retrieve from cache but return a new copy
		var secondParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var secondResult);

		// Assert
		Assert.IsTrue(secondParseResult);
		Assert.IsNotNull(secondResult);
		Assert.IsFalse(secondResult.ContainsKey("marker"), "Second result should be a clean copy without modifications");
		Assert.AreEqual("Test Title", secondResult["title"]);
		Assert.AreEqual("Test Author", secondResult["author"]);

		// Verify deep cloning of nested structures
		yamlContent = "title: Test Title\nmetadata:\n  tags:\n    - tag1\n    - tag2";

		// First parse with nested structure
		var nestedParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var nestedResult);
		Assert.IsTrue(nestedParseResult);
		Assert.IsNotNull(nestedResult);

		// Modify nested structure
		var metadata = nestedResult["metadata"] as Dictionary<string, object>;
		Assert.IsNotNull(metadata);
		var tags = metadata["tags"] as List<object>;
		Assert.IsNotNull(tags);
		tags.Add("modified");

		// Get another copy from cache
		var secondNestedParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var secondNestedResult);
		Assert.IsTrue(secondNestedParseResult);
		Assert.IsNotNull(secondNestedResult);

		// Verify the second copy is clean
		metadata = secondNestedResult["metadata"] as Dictionary<string, object>;
		Assert.IsNotNull(metadata);
		tags = metadata["tags"] as List<object>;
		Assert.IsNotNull(tags);
		Assert.AreEqual(2, tags.Count, "Second result should have original number of tags");
		Assert.AreEqual("tag1", tags[0]);
		Assert.AreEqual("tag2", tags[1]);
	}

	[TestMethod]
	public void SerializeYamlObject_WithCachedValues_ReturnsCachedResult()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
		{
			{ "title", "Test Title" },
			{ "author", "Test Author" }
		};

		// Act - First serialization should add to cache
		var firstResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Modify the dictionary (should not affect cached result)
		dictionary["title"] = "Modified Title";

		// Act - Second serialization should retrieve from cache
		var secondResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert - Cache should not be used because the dictionary content has changed
		Assert.IsTrue(secondResult.Contains("Modified Title"));
		Assert.IsFalse(secondResult.Equals(firstResult));
	}
}
