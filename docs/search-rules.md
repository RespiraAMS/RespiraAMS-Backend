# Search logic in RespiraAMS

In RespiraAMS, the search features/endpoints use `PostgreSQL` full text search.

The rules are:

- Accent-insensitive search (PostgreSQL does not support Vietnamese accent,
  so Vietnamese data accent search is not available.)
- Case-insensitive search
- Prefix full text search: every whitespace-separated word in `searchTerm` must
  match the beginning of a word (never the middle or end) in one of the entity's
  searched fields, and when several words are given all of them must be present
  (AND).
- Results are returned ranked by relevance, where a hit in a higher-priority
  field outranks a hit in a lower-priority one
- `SearchTerm` must contains at least 2 letters/digits/hyphens/spaces characters.
  Any characters outside of the group listed will immediately result to empty result,
  even if the data actual has them
