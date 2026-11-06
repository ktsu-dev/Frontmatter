// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #241: <see cref="JsonElement"/> and <see cref="JsonNode"/> values, which
/// <c>System.Text.Json</c> produces when it deserializes into <c>Dictionary&lt;string, object&gt;</c>, are
/// written as the values they hold rather than as mappings of their properties.
/// </summary>
[TestClass]
public class JsonValueTests
{
	private const string Json = "{\"title\":\"Hello\",\"draft\":true,\"weight\":3,\"tags\":[\"a\",\"b\"]}";

	private static Dictionary<string, object> FromJson(string json) =>
		JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

	[TestMethod]
	public void AddFrontmatter_JsonElementValues_AreWrittenAsTheirValues()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", FromJson(Json));

		Assert.AreEqual("---\ntitle: Hello\ndraft: true\nweight: 3\ntags:\n- a\n- b\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_JsonElementValues_ReadBackAsPlainValues()
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter(Frontmatter.AddFrontmatter("Body\n", FromJson(Json)))!;

		Assert.AreEqual("Hello", header["title"]);
		Assert.IsTrue((bool)header["draft"]);
		Assert.AreEqual(3, Convert.ToInt32(header["weight"], System.Globalization.CultureInfo.InvariantCulture));
		CollectionAssert.AreEqual(new object[] { "a", "b" }, ((IEnumerable<object>)header["tags"]).ToArray());
	}

	[TestMethod]
	public void AddFrontmatter_JsonElementIntoAnExistingHeader_InsertsTheValues()
	{
		string result = Frontmatter.AddFrontmatter("---\nauthor: Me\n---\nBody\n", FromJson("{\"title\":\"Hello\",\"weight\":3}"));

		Assert.AreEqual("---\nauthor: Me\ntitle: Hello\nweight: 3\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_NestedJsonElement_IsWrittenAsMappingsAndSequences()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", FromJson("{\"meta\":{\"a\":1,\"list\":[{\"b\":\"x\"}]}}"));

		Assert.AreEqual("---\nmeta:\n  a: 1\n  list:\n  - b: x\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_JsonNull_IsWrittenAsANull()
	{
		// System.Text.Json gives a top-level null as a C# null, so build the JsonElement null directly.
		JsonElement jsonNull = JsonDocument.Parse("null").RootElement;
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter(Frontmatter.AddFrontmatter("Body\n", new() { ["title"] = "T", ["summary"] = jsonNull }))!;

		Assert.IsTrue(header.ContainsKey("summary"));
		Assert.IsNull(header["summary"]);
	}

	[TestMethod]
	public void AddFrontmatter_JsonStringThatYaml11ReadsAsABoolean_IsStillQuoted()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", FromJson("{\"draft\":\"no\"}"));

		Assert.AreEqual("---\ndraft: \"no\"\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void SerializeFrontmatter_JsonNode_IsWrittenAsTheValueItHolds()
	{
		string result = Frontmatter.SerializeFrontmatter(new() { ["data"] = JsonNode.Parse("{\"a\":1,\"b\":[true,\"s\"]}")! });

		Assert.AreEqual("data:\n  a: 1\n  b:\n  - true\n  - s", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void SerializeFrontmatter_JsonValueNode_IsWrittenAsAScalar()
	{
		string result = Frontmatter.SerializeFrontmatter(new() { ["title"] = JsonValue.Create("Hello")!, ["rating"] = JsonValue.Create(2.5)! });

		Assert.AreEqual("title: Hello\nrating: 2.5", result.ReplaceLineEndings("\n"));
	}
}
