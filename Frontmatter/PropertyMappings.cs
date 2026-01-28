// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter;

using System.Collections.Generic;

/// <summary>
/// Provides standard mappings for frontmatter property names.
/// This class contains predefined mappings for common frontmatter properties,
/// organized by categories such as Title, Author, Date, etc.
/// </summary>
internal static class PropertyMappings
{
	/// <summary>
	/// Maps various title-related property names to the canonical "title" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Title = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "title", "title" },
			{ "name", "title" },
			{ "heading", "title" },
			{ "subject", "title" },
			{ "post-title", "title" },
			{ "pagetitle", "title" },
			{ "page-title", "title" },
			{ "headline", "title" }
		};

	/// <summary>
	/// Maps various author-related property names to the canonical "author" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Author = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "author", "author" },
			{ "authors", "author" },
			{ "creator", "author" },
			{ "contributor", "author" },
			{ "contributors", "author" },
			{ "by", "author" },
			{ "written-by", "author" },
			{ "writer", "author" }
		};

	/// <summary>
	/// Maps various date-related property names to the canonical "date" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Date = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "date", "date" },
			{ "created", "date" },
			{ "creation_date", "date" },
			{ "creation-date", "date" },
			{ "creationdate", "date" },
			{ "published", "date" },
			{ "publish_date", "date" },
			{ "publish-date", "date" },
			{ "publishdate", "date" },
			{ "post-date", "date" },
			{ "posting-date", "date" },
			{ "pubdate", "date" }
		};

	/// <summary>
	/// Maps various tag-related property names to the canonical "tags" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "tags", "tags" },
			{ "tag", "tags" },
			{ "keywords", "tags" },
			{ "keyword", "tags" },
			{ "topics", "tags" },
			{ "topic", "tags" }
		};

	/// <summary>
	/// Maps various category-related property names to the canonical "categories" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "categories", "categories" },
			{ "category", "categories" },
			{ "section", "categories" },
			{ "sections", "categories" },
			{ "group", "categories" },
			{ "groups", "categories" }
		};

	/// <summary>
	/// Maps various description-related property names to the canonical "description" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Description = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "description", "description" },
			{ "summary", "description" },
			{ "abstract", "description" },
			{ "excerpt", "description" },
			{ "desc", "description" },
			{ "overview", "description" },
			{ "snippet", "description" }
		};

	/// <summary>
	/// Maps various modification date-related property names to the canonical "modified" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Modified = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "modified", "modified" },
			{ "last_modified", "modified" },
			{ "last-modified", "modified" },
			{ "lastmodified", "modified" },
			{ "updated", "modified" },
			{ "update_date", "modified" },
			{ "update-date", "modified" },
			{ "updatedate", "modified" },
			{ "revision-date", "modified" },
			{ "last-update", "modified" }
		};

	/// <summary>
	/// Maps various layout-related property names to the canonical "layout" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Layout = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "layout", "layout" },
			{ "template", "layout" },
			{ "page-layout", "layout" },
			{ "type", "layout" },
			{ "page-type", "layout" }
		};

	/// <summary>
	/// Maps various URL-related property names to the canonical "permalink" name.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Permalink = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "permalink", "permalink" },
			{ "url", "permalink" },
			{ "link", "permalink" },
			{ "slug", "permalink" },
			{ "path", "permalink" }
		};

	/// <summary>
	/// Combined dictionary of all known property mappings.
	/// This dictionary is created by merging all category-specific mappings.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> All = CreateAndValidateAllMappings();

	/// <summary>
	/// Gets all available canonical property names.
	/// </summary>
	public static HashSet<string> CanonicalNames { get; } = new HashSet<string>(All.Values, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Creates and validates the combined dictionary of all property mappings.
	/// Throws an InvalidOperationException if any property name is mapped to different canonical names across categories.
	/// </summary>
	private static Dictionary<string, string> CreateAndValidateAllMappings()
	{
		Dictionary<string, (string Value, string Source)> allMappings = new(StringComparer.OrdinalIgnoreCase);
		(string Name, IReadOnlyDictionary<string, string> Dict)[] categoryMappings =
		[
			(Name: nameof(Title), Dict: Title),
			(Name: nameof(Author), Dict: Author),
			(Name: nameof(Date), Dict: Date),
			(Name: nameof(Tags), Dict: Tags),
			(Name: nameof(Categories), Dict: Categories),
			(Name: nameof(Description), Dict: Description),
			(Name: nameof(Modified), Dict: Modified),
			(Name: nameof(Layout), Dict: Layout),
			(Name: nameof(Permalink), Dict: Permalink)
		];

		foreach ((string categoryName, IReadOnlyDictionary<string, string> mappings) in categoryMappings)
		{
			foreach (KeyValuePair<string, string> mapping in mappings)
			{
				if (allMappings.TryGetValue(mapping.Key, out (string Value, string Source) existing) && existing.Value != mapping.Value)
				{
					throw new InvalidOperationException(
						$"Property '{mapping.Key}' is mapped to '{mapping.Value}' in {categoryName} " +
						$"but was already mapped to '{existing.Value}' in {existing.Source}");
				}

				allMappings[mapping.Key] = (mapping.Value, categoryName);
			}
		}

		Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, (string Value, string Source)> kvp in allMappings)
		{
			result[kvp.Key] = kvp.Value.Value;
		}

		return result;
	}
}
