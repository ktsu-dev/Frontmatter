namespace ktsu.Frontmatter.Test;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
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
					   $"tags: [broken, array, syntax{Environment.NewLine}" + // Missing closing bracket
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act - Should not throw exception
		string result = Frontmatter.CombineFrontmatter(input);

		// Assert
		Assert.IsNotNull(result);
		// The malformed section should be skipped
		Assert.IsFalse(Frontmatter.HasFrontmatter(result) && Frontmatter.ExtractFrontmatter(result)!.ContainsKey("tags"));
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.IsTrue(extractedFrontmatter.ContainsKey("nested_lists"));

		// Verify the nested list structure is preserved
		var nestedLists = extractedFrontmatter["nested_lists"] as System.Collections.IList;
		Assert.IsNotNull(nestedLists);
		Assert.AreEqual(2, nestedLists.Count);

		// Check the first nested item
		var firstCategory = nestedLists[0] as Dictionary<object, object>;
		Assert.IsNotNull(firstCategory);
		Assert.AreEqual("Category1", firstCategory["category"]);

		// Check items in the first category
		var firstCategoryItems = firstCategory["items"] as System.Collections.IList;
		Assert.IsNotNull(firstCategoryItems);
		Assert.AreEqual(2, firstCategoryItems.Count);
		Assert.AreEqual("Item1", firstCategoryItems[0]);
		Assert.AreEqual("Item2", firstCategoryItems[1]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithExcessiveFrontmatterSections_ThrowsException()
	{
		// Arrange
		var inputBuilder = new StringBuilder();

		// Create a string with more than 100 frontmatter sections
		for (int i = 0; i < 101; i++)
		{
			inputBuilder.AppendLine("---");
			inputBuilder.AppendLine($"section: {i}");
			inputBuilder.AppendLine("---");
			inputBuilder.AppendLine($"Content section {i}");
		}

		string input = inputBuilder.ToString();

		// Act & Assert
		Assert.ThrowsException<InvalidOperationException>(() => Frontmatter.CombineFrontmatter(input));
	}

	[TestMethod]
	public void CombineFrontmatter_WithLargeFrontmatter_ProcessesEfficiently()
	{
		// Arrange
		var inputBuilder = new StringBuilder();
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
		var stopwatch = Stopwatch.StartNew();
		string result = Frontmatter.CombineFrontmatter(input);
		stopwatch.Stop();

		// Assert
		Assert.IsNotNull(result);
		// Performance check - should process quickly (adjust threshold as needed)
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms");

		// Verify all properties were preserved
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(1000, extractedFrontmatter.Count);
	}

	[TestMethod]
	public void CombineFrontmatter_WithLargeContent_ProcessesEfficiently()
	{
		// Arrange
		var inputBuilder = new StringBuilder();
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
		var stopwatch = Stopwatch.StartNew();
		string result = Frontmatter.CombineFrontmatter(input);
		stopwatch.Stop();

		// Assert
		Assert.IsNotNull(result);
		// Performance check - should process quickly (adjust threshold as needed)
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms");

		// Verify frontmatter was preserved
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(1, extractedFrontmatter.Count);
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithCaching_ReturnsCachedResultForIdenticalInput()
	{
		// Arrange
		string input = $"---{Environment.NewLine}title: Test Title{Environment.NewLine}---{Environment.NewLine}Content";

		// Act - First call
		var firstCallStopwatch = Stopwatch.StartNew();
		string firstResult = Frontmatter.CombineFrontmatter(input);
		firstCallStopwatch.Stop();

		// Act - Second call (should use cache)
		var secondCallStopwatch = Stopwatch.StartNew();
		string secondResult = Frontmatter.CombineFrontmatter(input);
		secondCallStopwatch.Stop();

		// Assert
		Assert.AreEqual(firstResult, secondResult);
		// Second call should be significantly faster due to caching
		Assert.IsTrue(secondCallStopwatch.ElapsedMilliseconds < firstCallStopwatch.ElapsedMilliseconds,
			$"Second call ({secondCallStopwatch.ElapsedMilliseconds}ms) should be faster than first call ({firstCallStopwatch.ElapsedMilliseconds}ms)");
	}

	[TestMethod]
	public void CombineFrontmatter_WithStrangePropertyNames_HandlesCorrectly()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"property with spaces: Value with spaces{Environment.NewLine}" +
					   $"property-with-hyphens: Value-with-hyphens{Environment.NewLine}" +
					   $"property_with_underscores: Value_with_underscores{Environment.NewLine}" +
					   $"123numeric: Numeric value{Environment.NewLine}" +
					   $"!@#special: Special characters{Environment.NewLine}" +
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(5, extractedFrontmatter.Count);

		// Verify all the strange property names were preserved
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property with spaces"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property-with-hyphens"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("property_with_underscores"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("123numeric"));
		Assert.IsTrue(extractedFrontmatter.ContainsKey("!@#special"));
	}

	[TestMethod]
	public void CombineFrontmatter_WithDuplicateProperties_KeepsFirstValue()
	{
		// Arrange
		string input = $"---{Environment.NewLine}" +
					   $"title: First Title{Environment.NewLine}" +
					   $"title: Second Title{Environment.NewLine}" + // Duplicate property
					   $"---{Environment.NewLine}" +
					   $"Content";

		// Act
		string result = Frontmatter.CombineFrontmatter(input);
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(1, extractedFrontmatter.Count);
		Assert.AreEqual("First Title", extractedFrontmatter["title"]);
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(input);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(3, extractedFrontmatter.Count);

		// Verify escaped characters were handled correctly
		Assert.IsTrue(extractedFrontmatter["title"].ToString()!.Contains("\"quotes\""));
		Assert.IsTrue(extractedFrontmatter["description"].ToString()!.Contains("'single quotes'"));
		Assert.IsTrue(extractedFrontmatter["content"].ToString()!.Contains("Line1"));
		Assert.IsTrue(extractedFrontmatter["content"].ToString()!.Contains("Line2"));
		Assert.IsTrue(extractedFrontmatter["content"].ToString()!.Contains("Line3"));
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(4, extractedFrontmatter.Count);
		Assert.AreEqual("测试标题", extractedFrontmatter["标题"]);
		Assert.AreEqual("张三", extractedFrontmatter["作者"]);
		Assert.AreEqual("тестовое описание", extractedFrontmatter["описание"]);

		var tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.AreEqual(3, tags.Count);
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(3, extractedFrontmatter.Count);
		Assert.AreEqual("Test Title", extractedFrontmatter["title"]);
		Assert.AreEqual("Test Description", extractedFrontmatter["description"]);

		var tags = extractedFrontmatter["tags"] as System.Collections.IList;
		Assert.IsNotNull(tags);
		Assert.AreEqual(2, tags.Count);
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
		var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);

		// Assert
		Assert.IsNotNull(extractedFrontmatter);
		Assert.AreEqual(1, extractedFrontmatter.Count);
		Assert.IsTrue(extractedFrontmatter.ContainsKey(longPropertyName));
		Assert.AreEqual(longPropertyValue, extractedFrontmatter[longPropertyName]);
	}

	[TestMethod]
	public void CombineFrontmatter_WithConcurrentAccess_HandlesCorrectly()
	{
		// Arrange
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
				string result = Frontmatter.CombineFrontmatter(inputs[i]);
				var frontmatter = Frontmatter.ExtractFrontmatter(result);
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
		Assert.AreEqual(0, exceptions.Count, "No exceptions should occur during parallel processing");
		Assert.AreEqual(inputs.Length, results.Count, "All inputs should be processed");

		// Verify each result
		foreach (var kvp in results)
		{
			var frontmatter = kvp.Value;
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
		var stopwatch = Stopwatch.StartNew();

		foreach (string input in inputs)
		{
			string result = Frontmatter.CombineFrontmatter(input);
			Assert.IsNotNull(result);
			var extractedFrontmatter = Frontmatter.ExtractFrontmatter(result);
			Assert.IsNotNull(extractedFrontmatter);
			Assert.AreEqual(1, extractedFrontmatter.Count);
		}

		stopwatch.Stop();

		// Performance assertion - processing should complete within reasonable time
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000,
			$"Processing large data took too long: {stopwatch.ElapsedMilliseconds}ms");
	}
}
