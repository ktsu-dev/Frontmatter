namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

using ktsu.Extensions;

/// <summary>
/// Provides methods for processing and manipulating YAML frontmatter in markdown files.
/// </summary>
public static class Frontmatter
{
	/// <summary>
	/// The delimiter that marks the beginning and end of a frontmatter section.
	/// </summary>
	private const string FrontmatterDelimiter = "---";

	/// <summary>
	/// Cache for processed frontmatter to avoid repeated processing of identical content
	/// </summary>
	private static readonly ConcurrentDictionary<uint, string> ProcessedFrontmatterCache = new();

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input) =>
		CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode) =>
		CombineFrontmatter(input, propertyNamingMode, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <param name="orderMode">The ordering mode for frontmatter properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode, FrontmatterOrder orderMode) =>
		CombineFrontmatter(input, propertyNamingMode, orderMode, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <param name="orderMode">The ordering mode for frontmatter properties.</param>
	/// <param name="mergeStrategy">The strategy for merging similar properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode, FrontmatterOrder orderMode, FrontmatterMergeStrategy mergeStrategy)
	{
		// Generate a unique cache key based on the content and options
		uint optionsHash = (uint)propertyNamingMode | ((uint)orderMode << 8) | ((uint)mergeStrategy << 16);
		uint cacheKey = HashUtil.CreateCacheKey(input, optionsHash);

		// Try to get from cache first
		if (ProcessedFrontmatterCache.TryGetValue(cacheKey, out string? cachedResult))
		{
			return cachedResult;
		}

		var frontmatterObjects = ExtractFrontmatterObjects(input, out string body);

		if (frontmatterObjects.Count == 0)
		{
			// Cache the original content since no processing was needed
			ProcessedFrontmatterCache.TryAdd(cacheKey, input);
			return input;
		}

		var combinedFrontmatterObject = frontmatterObjects.First();
		foreach (var frontmatterObject in frontmatterObjects.Skip(1))
		{
			combinedFrontmatterObject = CombineFrontmatterObjects(combinedFrontmatterObject, frontmatterObject);
		}

		// Apply property merging if enabled
		if (mergeStrategy != FrontmatterMergeStrategy.None)
		{
			combinedFrontmatterObject = PropertyMerger.MergeSimilarProperties(combinedFrontmatterObject, mergeStrategy);
		}

		// Standardize property names using fuzzy matching if enabled
		if (propertyNamingMode == FrontmatterNaming.Standard)
		{
			combinedFrontmatterObject = NameStandardizer.StandardizePropertyNames(combinedFrontmatterObject);
		}

		// Sort properties according to standard conventions if enabled
		if (orderMode == FrontmatterOrder.Sorted)
		{
			combinedFrontmatterObject = SortFrontmatterProperties(combinedFrontmatterObject);
		}

		string combinedFrontmatter = YamlSerializer.SerializeYamlObject(combinedFrontmatterObject).Trim();

		string nl = Environment.NewLine;
		string result = $"{FrontmatterDelimiter}{nl}{combinedFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body}";

		// Cache the processed result
		ProcessedFrontmatterCache.TryAdd(cacheKey, result);

		return result;
	}

	/// <summary>
	/// Extracts frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A dictionary containing the frontmatter properties, or null if no frontmatter is found.</returns>
	public static Dictionary<string, object>? ExtractFrontmatter(string input)
	{
		if (!HasFrontmatter(input))
		{
			return null;
		}

		var frontmatterObjects = ExtractFrontmatterObjects(input, out _);
		return frontmatterObjects.Count > 0 ? frontmatterObjects.First() : null;
	}

	/// <summary>
	/// Checks if a markdown document contains frontmatter.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>True if the document contains frontmatter, false otherwise.</returns>
	public static bool HasFrontmatter(string input) => !string.IsNullOrEmpty(input) && input.StartsWithOrdinal(FrontmatterDelimiter + Environment.NewLine);

	/// <summary>
	/// Adds frontmatter to a markdown document that doesn't already have it.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The frontmatter properties to add.</param>
	/// <returns>A string containing the markdown document with added frontmatter.</returns>
	public static string AddFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		if (frontmatter == null || frontmatter.Count == 0)
		{
			return input;
		}

		if (HasFrontmatter(input))
		{
			// Document already has frontmatter, use CombineFrontmatter instead
			var existing = ExtractFrontmatter(input);
			var combined = CombineFrontmatterObjects(existing ?? [], frontmatter);
			return ReplaceFrontmatter(input, combined);
		}

		string yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		string nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{input.Trim()}{nl}";
	}

	/// <summary>
	/// Replaces existing frontmatter in a markdown document with new frontmatter.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The new frontmatter properties.</param>
	/// <returns>A string containing the markdown document with replaced frontmatter.</returns>
	public static string ReplaceFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		if (frontmatter == null || frontmatter.Count == 0)
		{
			return RemoveFrontmatter(input);
		}

		ExtractFrontmatterObjects(input, out string body);
		string yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		string nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body.Trim()}{nl}";
	}

	/// <summary>
	/// Removes frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing the markdown document with frontmatter removed.</returns>
	public static string RemoveFrontmatter(string input)
	{
		if (!HasFrontmatter(input))
		{
			return input;
		}

		ExtractFrontmatterObjects(input, out string body);
		return body.Trim() + Environment.NewLine;
	}

	/// <summary>
	/// Extracts the document body (content after frontmatter) from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing only the document body without frontmatter.</returns>
	public static string ExtractBody(string input)
	{
		ExtractFrontmatterObjects(input, out string body);
		return body.Trim();
	}

	/// <summary>
	/// Serializes a dictionary to YAML frontmatter format.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary to serialize.</param>
	/// <returns>A string containing the serialized YAML frontmatter.</returns>
	public static string SerializeFrontmatter(Dictionary<string, object> frontmatter) => frontmatter == null || frontmatter.Count == 0 ? string.Empty : YamlSerializer.SerializeYamlObject(frontmatter).Trim();

	/// <summary>
	/// Sorts frontmatter properties according to standard conventions.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary to sort.</param>
	/// <returns>A new dictionary with properties in the standard order.</returns>
	private static Dictionary<string, object> SortFrontmatterProperties(Dictionary<string, object> frontmatter)
	{
		// Create a new dictionary to hold the sorted properties
		Dictionary<string, object> sortedFrontmatter = [];

		// First add properties in the standard order (if they exist)
		foreach (string key in StandardOrder.PropertyNames)
		{
			if (frontmatter.TryGetValue(key, out object? value))
			{
				sortedFrontmatter[key] = value;
			}
		}

		// Then add any remaining properties that weren't in the standard order
		foreach (var property in frontmatter)
		{
			if (!sortedFrontmatter.ContainsKey(property.Key))
			{
				sortedFrontmatter[property.Key] = property.Value;
			}
		}

		return sortedFrontmatter;
	}

	/// <summary>
	/// Extracts all frontmatter objects from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="body">Output parameter that will contain the markdown body without frontmatter.</param>
	/// <returns>A collection of dictionaries representing each frontmatter section.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there are too many frontmatter sections in the document.</exception>
	private static Collection<Dictionary<string, object>> ExtractFrontmatterObjects(string input, out string body)
	{
		Collection<Dictionary<string, object>> frontmatterSections = [];

		// Create a working copy of the input to avoid modifying the output parameter during processing
		string workingContent = input;
		body = input;  // Default value if no frontmatter is found

		string delimeterAndNewLine = FrontmatterDelimiter + Environment.NewLine;

		if (string.IsNullOrEmpty(workingContent) || !workingContent.StartsWithOrdinal(delimeterAndNewLine))
		{
			return frontmatterSections;
		}

		int processCount = 0;
		const int maxFrontmatterSections = 100; // Reasonable limit to prevent excessive processing

		// Count the frontmatter sections first to validate against the maximum limit
		string tempContent = workingContent;
		int delimiterCount = 0;
		int lastIndex = 0;

		while ((lastIndex = tempContent.IndexOf(FrontmatterDelimiter, lastIndex)) != -1)
		{
			delimiterCount++;
			lastIndex += FrontmatterDelimiter.Length;
		}

		// Each frontmatter section requires 2 delimiters, so divide by 2
		int potentialSections = delimiterCount / 2;

		if (potentialSections > maxFrontmatterSections)
		{
			throw new InvalidOperationException($"Document contains more than {maxFrontmatterSections} frontmatter sections. This may indicate a parsing error or malformed document.");
		}

		while (workingContent.StartsWithOrdinal(delimeterAndNewLine))
		{
			processCount++;

			if (processCount > maxFrontmatterSections)
			{
				throw new InvalidOperationException($"Document contains more than {maxFrontmatterSections} frontmatter sections. This may indicate a parsing error or malformed document.");
			}

			string[] splitSections = workingContent.Split(FrontmatterDelimiter, 3, StringSplitOptions.RemoveEmptyEntries);
			if (splitSections.Length < 2)
			{
				// Not enough sections found, so exit the loop
				break;
			}

			string frontmatter = splitSections[0].Trim();
			workingContent = splitSections.Length > 1 ? splitSections[1].Trim() : string.Empty;

			// Special case: If the frontmatter is empty, add an empty dictionary but continue processing
			if (string.IsNullOrWhiteSpace(frontmatter))
			{
				frontmatterSections.Add([]);
				continue;
			}

			if (YamlSerializer.TryParseYamlObject(frontmatter, out var result))
			{
				frontmatterSections.Add(result);
			}
			else
			{
				// Invalid YAML, so return empty collection and keep original content
				frontmatterSections.Clear();
				body = input;
				return frontmatterSections;
			}
		}

		// Only set the final body output parameter after all processing is complete
		if (frontmatterSections.Count > 0)
		{
			body = workingContent;
		}

		return frontmatterSections;
	}

	/// <summary>
	/// Combines two frontmatter dictionaries into a single dictionary.
	/// </summary>
	/// <param name="a">The first frontmatter dictionary.</param>
	/// <param name="b">The second frontmatter dictionary.</param>
	/// <returns>A new dictionary containing the combined frontmatter.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there is a conflict between frontmatter values.</exception>
	private static Dictionary<string, object> CombineFrontmatterObjects(IDictionary<string, object> a, IDictionary<string, object> b)
	{
		Dictionary<string, object> combinedFrontmatterObject = [];

		HashSet<string> combinedKeys = [.. a.Keys];
		combinedKeys.AddMany(b.Keys);

		foreach (string key in combinedKeys)
		{
			bool isAKey = a.ContainsKey(key);
			bool isBKey = b.ContainsKey(key);

			if (isAKey && !isBKey)
			{
				combinedFrontmatterObject[key] = a[key];
				continue;
			}

			if (!isAKey && isBKey)
			{
				combinedFrontmatterObject[key] = b[key];
				continue;
			}

			object aValue = a[key];
			object bValue = b[key];

			if (aValue.GetType() != bValue.GetType())
			{
				combinedFrontmatterObject[key] = aValue; // Keep the first value
				continue;
			}

			if (aValue is IDictionary<string, object> aDict && bValue is IDictionary<string, object> bDict)
			{
				combinedFrontmatterObject[key] = CombineFrontmatterObjects(aDict, bDict);
			}
			else if (aValue is ICollection<object> aList && bValue is ICollection<object> bList)
			{
				// Use List instead of HashSet to preserve order
				List<object> combinedList = [.. aList];

				// Add items from bList that aren't already in combinedList
				foreach (object item in bList)
				{
					if (!combinedList.Contains(item))
					{
						combinedList.Add(item);
					}
				}

				combinedFrontmatterObject[key] = combinedList;
			}
			else
			{
				combinedFrontmatterObject[key] = aValue; // Keep the first value
			}
		}

		return combinedFrontmatterObject;
	}
}
