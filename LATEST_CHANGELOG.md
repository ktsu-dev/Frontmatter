## v1.4.6 (patch)

Changes since v1.4.5:

- Keep the header as written when AddFrontmatter adds no new key [patch] (Claude)
- Write the body after an inserted key as a rebuilt header writes it (Claude)
- Merge main into fix/170-add-keeps-header-text (Claude)
- Drop a null check the parse already rules out, and use AreSequenceEqual (Claude)
- Filter the added keys with Where in TryInsertIntoHeader (Claude)
- Insert added keys into the header instead of rebuilding it [patch] (Claude)
- Keep the blank line that sets a body rule apart from the header [patch] (Claude)
- Report a block with a complex key as unreadable [patch] (Claude)
- Fold IsEmptyYamlDocument's three catches into one filtered catch (Claude)
- Treat a header with no keys as empty so AddFrontmatter fills it [patch] (Claude)
- Double-quote strings holding a carriage return or NEL so they round-trip [patch] (Claude)

