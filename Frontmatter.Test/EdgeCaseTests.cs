namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text;

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
}
