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
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

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
		string yamlContent = "title: Test Title\nauthor: \n  invalidindent\n  : broken: syntax";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
	}

	[TestMethod]
	public void TryParseYamlObject_WithEmptyString_ReturnsFalse()
	{
		// Arrange
		string yamlContent = "";

		// Act
		bool result = YamlSerializer.TryParseYamlObject(yamlContent, out var parsedObject);

		// Assert
		Assert.IsFalse(result);
		Assert.IsNull(parsedObject);
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
		string result = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert
		Assert.IsTrue(result.Contains("title: Test Title"));
		Assert.IsTrue(result.Contains("author: Test Author"));
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
		string result = YamlSerializer.SerializeYamlObject(dictionary);

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
		string serialized = YamlSerializer.SerializeYamlObject(originalDictionary);
		bool parseResult = YamlSerializer.TryParseYamlObject(serialized, out var parsedDictionary);

		// Assert
		Assert.IsTrue(parseResult);
		Assert.IsNotNull(parsedDictionary);
		Assert.AreEqual(originalDictionary.Count, parsedDictionary.Count);
		Assert.AreEqual(originalDictionary["title"], parsedDictionary["title"]);

		// YAML deserializer returns numbers as strings, so convert before comparison
		Assert.AreEqual(originalDictionary["count"].ToString(), parsedDictionary["count"]?.ToString());

		// YAML deserializer might return "true" instead of True, so compare as strings
		string? originalEnabledString = originalDictionary["enabled"]?.ToString()?.ToLowerInvariant();
		string? parsedEnabledString = parsedDictionary["enabled"]?.ToString()?.ToLowerInvariant();
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

	[TestMethod]
	public void TryParseYamlObject_WithCachedValues_ReturnsCachedResult()
	{
		// Arrange
		string yamlContent = "title: Test Title\nauthor: Test Author";

		// Act - First parse should add to cache
		bool firstParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var firstResult);
		Assert.IsTrue(firstParseResult, "First parse should succeed");
		Assert.IsNotNull(firstResult, "First parse result should not be null");

		// Modify the first result to verify we get the same instance back
		firstResult["marker"] = "modified";

		// Act - Second parse should retrieve from cache
		bool secondParseResult = YamlSerializer.TryParseYamlObject(yamlContent, out var secondResult);

		// Assert
		Assert.IsTrue(secondParseResult);
		Assert.IsNotNull(secondResult);
		Assert.IsTrue(secondResult.ContainsKey("marker"));
		Assert.AreEqual("modified", secondResult["marker"]);
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
		string firstResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Modify the dictionary (should not affect cached result)
		dictionary["title"] = "Modified Title";

		// Act - Second serialization should retrieve from cache
		string secondResult = YamlSerializer.SerializeYamlObject(dictionary);

		// Assert - Cache should not be used because the dictionary content has changed
		Assert.IsTrue(secondResult.Contains("Modified Title"));
		Assert.IsFalse(secondResult.Equals(firstResult));
	}
}
