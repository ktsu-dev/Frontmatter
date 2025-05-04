// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode, FrontmatterOrder orderMode, FrontmatterMergeStrategy mergeStrategy)
	{
		ArgumentNullException.ThrowIfNull(input);

		// Generate a unique cache key based on the content and options
		var optionsHash = (uint)propertyNamingMode | ((uint)orderMode << 8) | ((uint)mergeStrategy << 16);
		var cacheKey = HashUtil.CreateCacheKey(input, optionsHash);

		// Try to get from cache first
		if (ProcessedFrontmatterCache.TryGetValue(cacheKey, out var cachedResult))
		{
			return cachedResult;
		}

		var frontmatterObjects = ExtractFrontmatterObjects(input, out var body);

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

		var combinedFrontmatter = YamlSerializer.SerializeYamlObject(combinedFrontmatterObject).Trim();

		var nl = Environment.NewLine;
		var result = $"{FrontmatterDelimiter}{nl}{combinedFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body}";

		// Cache the processed result
		ProcessedFrontmatterCache.TryAdd(cacheKey, result);

		return result;
	}

	/// <summary>
	/// Extracts frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A dictionary containing the frontmatter properties, or null if no frontmatter is found.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static Dictionary<string, object>? ExtractFrontmatter(string input)
	{
		ArgumentNullException.ThrowIfNull(input);

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
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static bool HasFrontmatter(string input)
	{
		ArgumentNullException.ThrowIfNull(input);
		return !string.IsNullOrEmpty(input) && input.StartsWithOrdinal(FrontmatterDelimiter + Environment.NewLine);
	}

	/// <summary>
	/// Adds frontmatter to a markdown document that doesn't already have it.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The frontmatter properties to add.</param>
	/// <returns>A string containing the markdown document with added frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string AddFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		ArgumentNullException.ThrowIfNull(input);

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

		var yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		var nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{input.Trim()}{nl}";
	}

	/// <summary>
	/// Replaces existing frontmatter in a markdown document with new frontmatter.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The new frontmatter properties.</param>
	/// <returns>A string containing the markdown document with replaced frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string ReplaceFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		ArgumentNullException.ThrowIfNull(input);

		if (frontmatter == null || frontmatter.Count == 0)
		{
			return RemoveFrontmatter(input);
		}

		ExtractFrontmatterObjects(input, out var body);
		var yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		var nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body.Trim()}{nl}";
	}

	/// <summary>
	/// Removes frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing the markdown document with frontmatter removed.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string RemoveFrontmatter(string input)
	{
		ArgumentNullException.ThrowIfNull(input);

		if (!HasFrontmatter(input))
		{
			return input;
		}

		ExtractFrontmatterObjects(input, out var body);
		return body.Trim() + Environment.NewLine;
	}

	/// <summary>
	/// Extracts the document body (content after frontmatter) from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing only the document body without frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string ExtractBody(string input)
	{
		ArgumentNullException.ThrowIfNull(input);

		ExtractFrontmatterObjects(input, out var body);
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
		foreach (var key in StandardOrder.PropertyNames)
		{
			if (frontmatter.TryGetValue(key, out var value))
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
	private static List<Dictionary<string, object>> ExtractFrontmatterObjects(string input, out string body)
	{
		var frontmatterObjects = new List<Dictionary<string, object>>();
		body = input;

		if (!HasFrontmatter(input))
		{
			return frontmatterObjects;
		}

		var sections = input.Split([FrontmatterDelimiter + Environment.NewLine], StringSplitOptions.None);
		body = string.Join(FrontmatterDelimiter + Environment.NewLine, sections.Skip(2));

		for (var i = 1; i < sections.Length; i += 2)
		{
			var section = sections[i].Trim();
			if (string.IsNullOrWhiteSpace(section))
			{
				continue;
			}

			if (YamlSerializer.TryParseYamlObject(section, out var frontmatterObject) && frontmatterObject != null)
			{
				frontmatterObjects.Add(frontmatterObject);
			}
		}

		return frontmatterObjects;
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

		// First, add all properties from dictionary a
		foreach (var kvp in a)
		{
			combinedFrontmatterObject[kvp.Key] = kvp.Value;
		}

		// Then, add properties from dictionary b that don't exist in a
		foreach (var kvp in b)
		{
			if (!combinedFrontmatterObject.ContainsKey(kvp.Key))
			{
				combinedFrontmatterObject[kvp.Key] = kvp.Value;
			}
		}

		return combinedFrontmatterObject;
	}
}
