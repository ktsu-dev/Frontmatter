// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter.Test;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EdgeCaseTests
{
	[TestMethod]
	public void CombineFrontmatter_WithMalformedYaml_HandlesGracefully()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"malformed:{Environment.NewLine}" +
					   $"  - not properly indented{Environment.NewLine}" +
					   $" wrong indentation{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(result), "Result should not be null or empty");
		Assert.Contains("Content", result, "Result should contain 'Content'"); // Original content should be preserved

		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		// For malformed YAML, we expect either null or an empty dictionary
		if (extractedFrontmatter != null)
		{
			Assert.IsEmpty(extractedFrontmatter);
		}
	}

	[TestMethod]
	public void CombineFrontmatter_WithNestedLists_HandlesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"nested_lists:{Environment.NewLine}" +
					   $"  - category: Category1{Environment.NewLine}" +
					   $"    items:{Environment.NewLine}" +
					   $"      - Item1{Environment.NewLine}" +
					   $"      - Item2{Environment.NewLine}" +
					   $"  - category: Category2{Environment.NewLine}" +
					   $"    items:{Environment.NewLine}" +
					   $"      - Item3{Environment.NewLine}" +
					   $"      - Item4{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("nested_lists"), "Frontmatter should contain 'nested_lists' key");

		// Verify the nested list structure is preserved
		System.Collections.IList? nestedLists = extractedFrontmatter["nested_lists"] as System.Collections.IList;
		Assert.IsNotNull(nestedLists);
		Assert.HasCount(2, nestedLists);

		// Check the first nested item - YamlDotNet returns IDictionary<object, object>
		System.Collections.IDictionary? firstCategory = nestedLists[0] as System.Collections.IDictionary;
		Assert.IsNotNull(firstCategory);
		Assert.AreEqual("Category1", firstCategory["category"]);

		// Check items in the first category
		System.Collections.IList? firstCategoryItems = firstCategory["items"] as System.Collections.IList;
		Assert.IsNotNull(firstCategoryItems);
		Assert.HasCount(2, firstCategoryItems);
		Assert.AreEqual("Item1", firstCategoryItems[0]);
		Assert.AreEqual("Item2", firstCategoryItems[1]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithExcessiveFrontmatterSections_HandlesGracefully()
	{
		// Arrange
		StringBuilder inputBuilder = new();

		// Create a string with more than 100 frontmatter sections
		for (int i = 0; i < 101; i++)
		{
			inputBuilder.AppendLine("---");
			inputBuilder.AppendLine($"section: {i}");
			inputBuilder.AppendLine("---");
			inputBuilder.AppendLine($"Content section {i}");
		}

		string input = inputBuilder.ToString();

		// Act - the implementation handles large numbers of sections gracefully
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert - should return a valid result
		Assert.IsNotNull(result);
		Assert.IsFalse(string.IsNullOrEmpty(result), "Result should not be null or empty");
	}

	[TestMethod]
	public void CombineFrontmatter_WithLargeFrontmatter_ProcessesEfficiently()
	{
		// Arrange
		StringBuilder inputBuilder = new();
		inputBuilder.AppendLine("---");

		// Add a large number of properties (1000)
		for (int i = 0; i < 1000; i++)
		{
			inputBuilder.AppendLine($"property{i}: value{i}");
		}

		inputBuilder.AppendLine("---");
		inputBuilder.AppendLine("Content");

		string input = inputBuilder.ToString();

		// Act
		Stopwatch stopwatch = Stopwatch.StartNew();
		string result = Frontmatter.CombineFrontmatter(input);
		stopwatch.Stop();

		// Assert
		Assert.IsNotNull(result);
		// Performance check - should process quickly (adjust threshold as needed)
		Assert.IsLessThan(1000L, stopwatch.ElapsedMilliseconds, $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms");

		// Verify all properties were preserved
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(1000, extractedFrontmatter);
	}

	[TestMethod]
	public void CombineFrontmatter_WithLargeContent_ProcessesEfficiently()
	{
		// Arrange
		StringBuilder inputBuilder = new();
		inputBuilder.AppendLine("---");
		inputBuilder.AppendLine("title: Test Title");
		inputBuilder.AppendLine("---");

		// Add large content (100,000 characters)
		for (int i = 0; i < 10000; i++)
		{
			inputBuilder.AppendLine($"Line {i} of content with some additional text to make it longer.");
		}

		string input = inputBuilder.ToString();

		// Act
		Stopwatch stopwatch = Stopwatch.StartNew();
		string result = Frontmatter.CombineFrontmatter(input);
		stopwatch.Stop();

		// Assert
		Assert.IsNotNull(result);
		// Performance check - should process quickly (adjust threshold as needed)
		Assert.IsLessThan(1000L, stopwatch.ElapsedMilliseconds, $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms");

		// Verify frontmatter was preserved
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(1, extractedFrontmatter);
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithCaching_ReturnsCachedResultForIdenticalInput()
	{
		// Create a unique input that won't be in the cache
		StringBuilder uniqueInputBuilder = new();
		uniqueInputBuilder.AppendLine("---");
		uniqueInputBuilder.AppendLine($"title: Test Title {Guid.NewGuid()}");
		for (int i = 0; i < 1000; i++)
		{
			uniqueInputBuilder.AppendLine($"property{i}: value{i}");
		}

		uniqueInputBuilder.AppendLine("---");
		uniqueInputBuilder.AppendLine("Content");
		string uniqueInput = uniqueInputBuilder.ToString();

		// Act - First call
		Stopwatch firstCallStopwatch = Stopwatch.StartNew();
		string firstResult = Frontmatter.CombineFrontmatter(uniqueInput);
		firstCallStopwatch.Stop();

		// Small delay to ensure we get different timestamps
		Thread.Sleep(10);

		// Act - Second call (should use cache)
		Stopwatch secondCallStopwatch = Stopwatch.StartNew();
		string secondResult = Frontmatter.CombineFrontmatter(uniqueInput);
		secondCallStopwatch.Stop();

		// Assert
		Assert.AreEqual(firstResult, secondResult);

		// Second call should be at most as fast as the first call (typically faster due to caching)
		Assert.IsLessThanOrEqualTo(firstCallStopwatch.ElapsedMilliseconds, secondCallStopwatch.ElapsedMilliseconds,
			$"Second call ({secondCallStopwatch.ElapsedMilliseconds}ms) should be at most as fast as first call ({firstCallStopwatch.ElapsedMilliseconds}ms)");
	}

	[TestMethod]
	public void CombineFrontmatter_WithStrangePropertyNames_HandlesCorrectly()
	{
		// Arrange - use quoted strings to ensure YAML parses special property names correctly
		string input = $"---{Environment.NewLine}" +
					   $"\"property with spaces\": Value with spaces{Environment.NewLine}" +
					   $"property-with-hyphens: Value-with-hyphens{Environment.NewLine}" +
					   $"property_with_underscores: Value_with_underscores{Environment.NewLine}" +
					   $"\"123numeric\": Numeric value{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act - use AsIs naming to preserve original property names
		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(4, extractedFrontmatter);

		// Verify most of the strange property names were preserved
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property with spaces"), "Frontmatter should contain 'property with spaces' key");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property-with-hyphens"), "Frontmatter should contain 'property-with-hyphens' key");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property_with_underscores"), "Frontmatter should contain 'property_with_underscores' key");
		Assert.IsTrue(extractedFrontmatter.ContainsKey("123numeric"), "Frontmatter should contain '123numeric' key");
	}

	[TestMethod]
	public void CombineFrontmatter_WithDuplicateProperties_KeepsLastValue()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: First Title{Environment.NewLine}" +
					   $"title: Second Title{Environment.NewLine}" + // Duplicate property - YAML keeps last value
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(1, extractedFrontmatter);
		// YAML parsers typically keep the last value for duplicate keys
		Assert.AreEqual("Second Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithNoEndDelimiter_HandlesGracefully()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: Test Title{Environment.NewLine}" +
					   $"Content without end delimiter";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		// Should handle invalid frontmatter gracefully
		Assert.IsNotNull(result);
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void ExtractFrontmatter_WithEscapedCharacters_ParsesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: \"Title with \\\"quotes\\\" inside\"{Environment.NewLine}" +
					   $"description: 'Description with ''single quotes'' inside'{Environment.NewLine}" +
					   $"content: \"Line1\\nLine2\\nLine3\"{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(3, extractedFrontmatter);

		// Verify escaped characters were handled correctly
		Assert.Contains("\"quotes\"", extractedFrontmatter["title"].ToString()!, "Title should contain escaped double quotes");
		Assert.Contains("'single quotes'", extractedFrontmatter["description"].ToString()!, "Description should contain escaped single quotes");
		Assert.Contains("Line1", extractedFrontmatter["content"].ToString()!, "Content should contain 'Line1'");
		Assert.Contains("Line2", extractedFrontmatter["content"].ToString()!, "Content should contain 'Line2'");
		Assert.Contains("Line3", extractedFrontmatter["content"].ToString()!, "Content should contain 'Line3'");
	}

	[TestMethod]
	public void CombineFrontmatter_WithUnicodeCharacters_HandlesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"标题: 测试标题{Environment.NewLine}" +
					   $"作者: 张三{Environment.NewLine}" +
					   $"описание: тестовое описание{Environment.NewLine}" +
					   $"tags: [测试, тест, 테스트]{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content with Unicode: 内容 содержание 콘텐츠";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(4, extractedFrontmatter);
		Assert.AreEqual("测试标题", extractedFrontmatter["标题"]);
		Assert.AreEqual("张三", extractedFrontmatter["作者"]);
		Assert.AreEqual("тестовое описание", extractedFrontmatter["описание"]);

		System.Collections.IList? tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.HasCount(3, tags);
		CollectionAssert.Contains(tags.Cast<object>().ToArray(), "测试");
		CollectionAssert.Contains(tags.Cast<object>().ToArray(), "тест");
		CollectionAssert.Contains(tags.Cast<object>().ToArray(), "테스트");
	}

	[TestMethod]
	public void CombineFrontmatter_WithMixedLineEndings_HandlesCorrectly()
	{
		// Arrange
		string input = "---\r\n" +
					   "title: Test Title\n" +
					   "description: Test Description\r" +
					   "tags:\r\n" +
					   "  - tag1\n" +
					   "  - tag2\r" +
					   "---\r\n" +
					   "Content with\nmixed\r\nline\rendings";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(3, extractedFrontmatter);
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Description", extractedFrontmatter["description"]);

		System.Collections.IList? tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.HasCount(2, tags);
		CollectionAssert.Contains(tags.Cast<object>().ToArray(), "tag1");
		CollectionAssert.Contains(tags.Cast<object>().ToArray(), "tag2");

		// Verify content is preserved with original line endings
		string body = Frontmatter.ExtractBody(result);
		Assert.AreEqual("Content with\nmixed\r\nline\rendings", body);
	}

	[TestMethod]
	public void CombineFrontmatter_WithVeryLongPropertyNames_HandlesCorrectly()
	{
		// Arrange
		string longPropertyName = new('a', 1000);
		string longPropertyValue = new('b', 1000);

		string input = $"---{Environment.NewLine}" +
					   $"{longPropertyName}: {longPropertyValue}{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.HasCount(1, extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey(longPropertyName), "Frontmatter should contain the long property name key");
		Assert.AreEqual(longPropertyValue, extractedFrontmatter[longPropertyName]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithConcurrentAccess_HandlesCorrectly()
	{
		// Arrange - use AsIs naming to preserve original property names
		string[] inputs = new string[100];
		for (int i = 0; i < inputs.Length; i++)
		{
			inputs[i] = $"---{Environment.NewLine}" +
						$"title{i}: Test Title {i}{Environment.NewLine}" +
						$"description{i}: Test Description {i}{Environment.NewLine}" +
						$"---{Environment.NewLine}" +
						$"Content {i}";
		}

		// Act
		ConcurrentBag<InvalidOperationException> exceptions = [];
		ConcurrentDictionary<int, Dictionary<string, object>> results = new();

		Parallel.For(0, inputs.Length, i =>
		{
			try
			{
				// Use AsIs naming and None merge strategy to preserve original property names
				string result = Frontmatter.CombineFrontmatter(inputs[i], FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);
				Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);
				if (frontmatter != null)
				{
					results[i] = frontmatter;
				}
			}
			catch (InvalidOperationException ex)
			{
				exceptions.Add(ex);
			}
		});

		// Assert
		Assert.IsEmpty(exceptions, "No exceptions should occur during parallel processing");
		Assert.HasCount(inputs.Length, results, "All inputs should be processed");

		// Verify each result
		foreach (KeyValuePair<int, Dictionary<string, object>> kvp in results)
		{
			Dictionary<string, object> frontmatter = kvp.Value;
			Assert.IsNotNull(frontmatter);
			Assert.IsTrue(frontmatter.ContainsKey($"title{kvp.Key}"), $"Missing title{kvp.Key}");
			Assert.IsTrue(frontmatter.ContainsKey($"description{kvp.Key}"), $"Missing description{kvp.Key}");
			Assert.AreEqual($"Test Title {kvp.Key}", frontmatter[$"title{kvp.Key}"]);
			Assert.AreEqual($"Test Description {kvp.Key}", frontmatter[$"description{kvp.Key}"]);
		}
	}

	[TestMethod]
	public void CombineFrontmatter_WithMemoryPressure_HandlesGracefully()
	{
		// Arrange
		string largeValue = new('x', 1024 * 1024); // 1MB string
		List<string> inputs = [];

		for (int i = 0; i < 10; i++) // Will create ~10MB of data
		{
			inputs.Add($"---{Environment.NewLine}" +
					  $"large_value_{i}: {largeValue}{Environment.NewLine}" +
					  $"---{Environment.NewLine}" +
					  $"Content {i}");
		}

		// Act & Assert
		Stopwatch stopwatch = Stopwatch.StartNew();

		foreach (string input in inputs)
		{
			string result = Frontmatter.CombineFrontmatter(input);
			Assert.IsNotNull(result);
			Dictionary<string, object>? extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
			Assert.IsNotNull(extractedFrontmatter);
			Assert.HasCount(1, extractedFrontmatter);
		}

		stopwatch.Stop();

		// Performance assertion - processing should complete within reasonable time
		Assert.IsLessThan(5000L, stopwatch.ElapsedMilliseconds,
			$"Processing large data took too long: {stopwatch.ElapsedMilliseconds}ms");
	}
}
