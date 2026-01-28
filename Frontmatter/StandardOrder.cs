// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Frontmatter;

/// <summary>
/// Provides a standard ordering for frontmatter properties.
/// </summary>
public static class StandardOrder
{
	/// <summary>
	/// Gets the standard property names in their recommended order.
	/// </summary>
	public static readonly string[] PropertyNames =
	[
		// Core metadata (common across most systems)
		"title",
		"subtitle",
		"description",
		"summary",
		"abstract",

		// Date properties
		"date",
		"created",
		"updated",
		"modified",
		"lastmod",
		"review_date",
		"expiry_date",
		"start_date",
		"end_date",
		"due_date",

		// Author information
		"author",
		"authors",
		"reviewers",
		"approvers",
		"contributors",
		"editor",
		"editors",
		"owner",
		"maintainer",

		// Categorization
		"categories",
		"category",
		"tags",
		"topics",
		"subject",
		"classification",
		"department",
		"team",

		// Document workflow status
		"status",
		"workflow_state",
		"stage",
		"review_status",
		"approval_status",
		"version",
		"revision",
		"maturity",
		"stability",
		"confidence",
		"validity",
		"priority",
		"importance",
		"completeness",
		"progress",

		// Publishing controls
		"draft",
		"published",
		"publish",
		"hidden",
		"unlisted",
		"featured",
		"sticky",
		"archived",
		"outdated",
		"obsolete",
		"searchable",
		"indexable",

		// Obsidian-specific organization
		"aliases",
		"cssclass",
		"type",
		"project",
		"area",
		"resource",
		"moc",
		"obsidian",
		"links",
		"backlinks",

		// Academic/research specific
		"doi",
		"citation",
		"references",
		"bibliography",
		"journal",
		"volume",
		"issue",
		"conference",
		"institution",
		"grant",
		"funding",

		// Layout related
		"layout",
		"template",
		"format",
		"style",

		// URL related
		"permalink",
		"slug",
		"url",

		// Display properties
		"comments",
		"toc",
		"table_of_contents",
		"image",
		"images",
		"thumbnail",
		"banner",
		"cover",
		"icon",
		"logo",
		"color",
		"theme",

		// Media and attachments
		"attachments",
		"audio",
		"video",
		"gallery",
		"downloads",

		// SEO properties
		"keywords",
		"description_seo",
		"og_title",
		"og_description",
		"og_image",
		"twitter_card",
		"twitter_title",
		"twitter_description",
		"twitter_image",

		// Localization
		"lang",
		"language",
		"locale",
		"region",
		"translations",

		// Navigation/ordering
		"weight",
		"menu",
		"sidebar",
		"nav_order",
		"breadcrumb",
		"parent",
		"children",
		"next",
		"prev",
		"related",

		// Customization
		"css",
		"js",
		"classes",
		"attributes",
		"styles",
		"scripts",

		// Advanced SEO/linking
		"canonical",
		"redirects",
		"redirectFrom",
		"redirectTo",

		// Analytics
		"analytics",
		"tracking",
		"metrics",

		// Rights management
		"license",
		"copyright",
		"rights",
		"permissions",
	];

	/// <summary>
	/// Compares two frontmatter property names according to the standard order.
	/// </summary>
	/// <param name="a">The first property name.</param>
	/// <param name="b">The second property name.</param>
	/// <returns>
	/// A negative value if 'a' should appear before 'b',
	/// zero if they are equal in order,
	/// a positive value if 'a' should appear after 'b'.
	/// </returns>
	public static int Compare(string a, string b)
	{
		Ensure.NotNull(a);
		Ensure.NotNull(b);

		if (a == b)
		{
			return 0;
		}

		// Get the indices from the standard property list
		int indexA = Array.IndexOf(PropertyNames, a.ToLowerInvariant());
		int indexB = Array.IndexOf(PropertyNames, b.ToLowerInvariant());

		// If both properties are not in the standard list, sort them alphabetically
		if (indexA < 0 && indexB < 0)
		{
			return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
		}

		// If only one property is in the standard list, it should come first
		if (indexA < 0)
		{
			return 1;
		}

		if (indexB < 0)
		{
			return -1;
		}

		// Both properties are in the standard list, sort by their index
		return indexA - indexB;
	}
}
