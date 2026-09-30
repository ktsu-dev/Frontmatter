## v1.4.0 (minor)

Changes since v1.3.0:

- Assert the boolean round trip with IsFalse and Contains in scalar type tests ([@Claude](https://github.com/Claude))
- Use TryGetValue instead of ContainsKey plus indexer in scalar round-trip test ([@Claude](https://github.com/Claude))
- [minor] Keep scalar types and nulls through a frontmatter round trip ([@Claude](https://github.com/Claude))
- Keep a lone list property's key when merging properties [patch] ([@Claude](https://github.com/Claude))
- Keep the first body line's indentation when rewriting a document [patch] ([@Claude](https://github.com/Claude))
- Move CI onto the shared ci-shared.yml pipeline ([@Claude](https://github.com/Claude))
- [patch] Keep every frontmatter block when adding or combining properties ([@Claude](https://github.com/Claude))
- Merge keys that normalize alike under one name instead of swapping them [patch] ([@matt-edmondson](https://github.com/matt-edmondson))
- Use TryGetValue instead of ContainsKey plus indexer in collision test ([@matt-edmondson](https://github.com/matt-edmondson))
- Use TryGetValue instead of ContainsKey plus indexer in merge-isolation tests ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] Keep both values when two keys standardize to the same name ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] Stop the merge-strategy cache letting one call decide a key for every later call ([@matt-edmondson](https://github.com/matt-edmondson))
- Merge remote-tracking branch 'origin/main' into fix/rule-under-header-stays-in-body ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] Treat a frontmatter block with no YAML content as unreadable instead of throwing ([@Claude](https://github.com/Claude))
- [patch] Keep a thematic break directly under the header in the body ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] Keep repeated items within a list when merging properties ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] Recognise an opening delimiter with trailing whitespace or a leading BOM ([@matt-edmondson](https://github.com/matt-edmondson))

