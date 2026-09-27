// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #136: when two keys standardize to the same name, standard naming must keep
/// both values instead of letting one overwrite the other.
/// </summary>
[TestClass]
public class StandardNamingCollisionTests
{
	[TestMethod]
	public void DefaultOptions_AuthorAndCreatorListOfDifferentType_KeepsBothValues()
	{
		const string input = "---\nauthor: Alice\ncreator:\n  - Bob\n---\nbody\n";

		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(Frontmatter.CombineFrontmatter(input));

		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("Alice", frontmatter["author"]);
		Assert.IsTrue(frontmatter.TryGetValue("creator", out object? creator), "creator's list must survive alongside author");
		CollectionAssert.AreEqual(new object[] { "Bob" }, (System.Collections.ICollection)creator);
	}

	[TestMethod]
	public void NoMerge_AuthorAndCreator_KeepsBothValues()
	{
		const string input = "---\nauthor: Alice\ncreator: Bob\n---\nbody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);

		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("Alice", frontmatter["author"]);
		Assert.AreEqual("Bob", frontmatter["creator"]);
	}

	[TestMethod]
	public void NoMerge_TitleCasedAndLowerCased_KeepsBothValues()
	{
		const string input = "---\nTitle: A\ntitle: B\n---\nbody\n";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter(result);

		Assert.IsNotNull(frontmatter);
		Assert.AreEqual("A", frontmatter["Title"]);
		Assert.AreEqual("B", frontmatter["title"]);
	}

	[TestMethod]
	public void StandardizePropertyNames_AliasBeforeStandardName_KeepsAliasUnchanged()
	{
		Dictionary<string, object> result = NameStandardizer.StandardizePropertyNames(new Dictionary<string, object>
		{
			["creator"] = "Bob",
			["author"] = "Alice",
		});

		Assert.HasCount(2, result);
		Assert.AreEqual("Bob", result["creator"]);
		Assert.AreEqual("Alice", result["author"]);
	}

	[TestMethod]
	public void StandardizePropertyNames_LoneAlias_IsStillRenamed()
	{
		Dictionary<string, object> result = NameStandardizer.StandardizePropertyNames(new Dictionary<string, object>
		{
			["creator"] = "Bob",
		});

		Assert.HasCount(1, result);
		Assert.AreEqual("Bob", result["author"]);
	}
}
