// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using YamlDotNet.Serialization;

/// <summary>
/// Regression tests for #137: a round trip must keep what each scalar means. Quoted strings that look
/// like other types stay strings, plain scalars keep their types, and nulls stay null.
/// </summary>
[TestClass]
public class ScalarTypeRoundTripTests
{
	private static readonly string Nl = Environment.NewLine;

	private static readonly IDeserializer TypedReader = new DeserializerBuilder()
		.WithAttemptingUnquotedStringTypeDeserialization()
		.Build();

	private static Dictionary<object, object?> ReadHeader(string document)
	{
		string[] parts = document.Split(["---"], 3, StringSplitOptions.None);
		return TypedReader.Deserialize<Dictionary<object, object?>>(parts[1]);
	}

	[TestMethod]
	[DataRow(FrontmatterMergeStrategy.None)]
	[DataRow(FrontmatterMergeStrategy.Conservative)]
	public void CombineFrontmatter_QuotedLookalikesNullsAndBooleans_KeepTheirTypes(FrontmatterMergeStrategy strategy)
	{
		string input = $"---{Nl}title: \"true\"{Nl}version: \"1.0\"{Nl}zip: \"01234\"{Nl}null_str: \"null\"{Nl}empty:{Nl}draft: false{Nl}count: 3{Nl}---{Nl}body{Nl}";

		string result = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, strategy);
		Dictionary<object, object?> header = ReadHeader(result);

		Assert.AreEqual("true", header["title"], result);
		Assert.AreEqual("1.0", header["version"], result);
		Assert.AreEqual("01234", header["zip"], result);
		Assert.AreEqual("null", header["null_str"], result);
		Assert.IsTrue(header.TryGetValue("empty", out object? emptyValue), result);
		Assert.IsNull(emptyValue, result);
		Assert.IsInstanceOfType<bool>(header["draft"], result);
		Assert.IsFalse((bool)header["draft"]!, result);
		Assert.AreEqual(3L, Convert.ToInt64(header["count"], System.Globalization.CultureInfo.InvariantCulture), result);
		Assert.Contains($"draft: false{Nl}", result);
	}

	[TestMethod]
	public void AddFrontmatter_StringsThatLookLikeOtherTypes_AreWrittenQuoted()
	{
		string result = Frontmatter.AddFrontmatter("body", new Dictionary<string, object> { ["title"] = "true", ["id"] = "007" });
		Dictionary<object, object?> header = ReadHeader(result);

		Assert.AreEqual("true", header["title"], result);
		Assert.AreEqual("007", header["id"], result);
	}

	[TestMethod]
	public void ExtractFrontmatter_PlainAndQuotedScalars_KeepDistinctTypes()
	{
		Dictionary<string, object>? frontmatter = Frontmatter.ExtractFrontmatter($"---{Nl}draft: false{Nl}label: \"false\"{Nl}---{Nl}body{Nl}");

		Assert.IsNotNull(frontmatter);
		Assert.IsInstanceOfType<bool>(frontmatter["draft"]);
		Assert.IsFalse((bool)frontmatter["draft"]);
		Assert.AreEqual("false", frontmatter["label"]);
	}
}
