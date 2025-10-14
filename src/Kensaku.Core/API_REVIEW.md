# Kensaku.Core API Surface Review

## Overview
This review examines the public API surface of Kensaku.Core to identify potential improvements for usability, consistency, and maintainability.

## Recommendations

### 1. Consider Making Tables.fs Types Internal
**Priority: Medium**

The `Tables.fs` namespace contains types that appear to be internal database table representations (CLIMutable types with database field names). These are implementation details that should not be exposed to library consumers.

**Current state:**
- All types in `Kensaku.Core.Tables` are public
- These types are used internally for Dapper queries but returned in the public API in some places (e.g., `Tables.CharacterDictionaryReference` in `GetKanjiQueryResult`)

**Recommendation:**
- Mark all types in `Tables.fs` as `internal`
- Ensure public API only exposes domain types from `Domain.fs`
- For types like `CharacterDictionaryReference` that appear in public API, either:
  - Move them to `Domain.fs` as proper domain types, OR
  - Create public domain equivalents and map internally

**Impact:** Breaking change - but appropriate for a pre-release version

---

### 2. Standardize Async Function Naming
**Priority: Low**

Most async functions follow the `Async` suffix convention, which is good F# practice.

**Current state:**
- `getKanjiAsync` ✓
- `getWordsAsync` ✓
- `getRadicalsAsync` ✓
- `getWordLiteralsAsync` ✓
- `getKanjiLiteralsAsync` ✓
- `getRadicalLiteralsAsync` ✓
- `getRadicalNamesAsync` ✓

**Recommendation:**
- Continue this pattern for any future additions
- This is already well-established in the codebase

---

### 3. Consider Result Types for Query Operations
**Priority: Low**

Current query functions return sequences directly without any error context. Database exceptions will propagate as exceptions.

**Current behavior:**
```fsharp
let getKanjiAsync (query: GetKanjiQuery) (ctx: KensakuConnection) : Task<seq<GetKanjiQueryResult>>
```

**Recommendation:**
Consider using `Result` types for operations that might fail:
```fsharp
let getKanjiAsync (query: GetKanjiQuery) (ctx: KensakuConnection) : Task<Result<seq<GetKanjiQueryResult>, string>>
```

**Rationale:**
- Provides explicit error handling
- Makes it clear when operations can fail
- Common pattern in F# libraries
- Forces consumers to handle errors explicitly

**Caveat:** This would be a breaking change, so consider for v1.0 if not already in the design. For now, document the exception behavior in XML comments.

---

### 4. Utilities Module Design
**Priority: Medium**

The `Utilities` module is marked `[<AutoOpen>]`, which auto-imports `rune` and `String.getRunes` into any namespace that opens `Kensaku.Core`.

**Current state:**
```fsharp
[<AutoOpen>]
module Utilities =
    let inline rune input = Rune.GetRuneAt(string input, 0)
    module String =
        let getRunes (input: string) = [ for rune in input.EnumerateRunes() -> rune ]
```

**Concerns:**
- `AutoOpen` can cause name collisions in consumer code
- `rune` is a very generic name that could conflict with user code
- The `sql` function is marked internal but in an AutoOpen module (this is fine)

**Recommendation:**
- Remove `[<AutoOpen>]` attribute
- Users should explicitly open `Kensaku.Core.Utilities` if they want these helpers
- OR rename to more specific names like `Rune.fromInput` and keep AutoOpen
- Document this decision in the code

**Alternative:** If keeping AutoOpen, rename `rune` to `kensakuRune` or similar to reduce collision risk

---

### 5. CharacterCode Union Type Design
**Priority: Low**

The `CharacterCode` union type has a `.Value` member that returns the underlying string.

**Current design:**
```fsharp
type CharacterCode =
    | SkipCode of string
    | ShDescCode of string
    | FourCornerCode of string
    | DeRooCode of string
    member this.Value = ...
```

**Recommendation:**
This is well-designed. The union provides type safety while the `.Value` member provides convenience. No changes needed.

---

### 6. Connection Lifecycle
**Priority: Medium**

`KensakuConnection` inherits from `SqliteConnection` which is `IDisposable`.

**Current usage pattern:**
```fsharp
use ctx = new KensakuConnection("Data Source=kensaku.db")
ctx.OpenAsync() |> Async.AwaitTask |> Async.RunSynchronously
```

**Concerns:**
- Connection must be explicitly opened by the user
- Users need to understand IDisposable semantics
- Easy to forget to open the connection

**Recommendation:**
- Document connection lifecycle clearly (already done in README ✓)
- Consider adding factory methods that handle opening:
  ```fsharp
  static member CreateAndOpenAsync(connectionString: string) : Task<KensakuConnection>
  ```
- This would provide a cleaner API for the common case while still allowing manual control when needed

---

### 7. Query Result Types Structure
**Priority: Low**

Result types use anonymous records for nested structured data:

**Example:**
```fsharp
type GetKanjiQueryResult = {
    ...
    CharacterReadings: {|
        Kunyomi: string list
        Onyomi: string list
    |}
    KeyRadicals: {|
        Kangxi: KeyRadicalValue
        Nelson: KeyRadicalValue option
    |}
    CodePoints: {|
        Ucs: string
        Jis208: string option
        Jis212: string option
        Jis213: string option
    |}
    ...
}
```

**Pros:**
- Concise
- Clear structure
- Good for return-only data
- Type-safe

**Cons:**
- Anonymous records are not as discoverable in tooling
- Cannot add doc comments to individual fields
- Cannot be used as parameters easily
- Not extensible if you need to add behavior

**Recommendation:**
- Current approach is acceptable for query results that are read-only
- Consider named record types if:
  - These structures need to be reused elsewhere
  - You want to add XML documentation to fields
  - You want to add behavior/methods to the nested structures
- For now, document the nested structure in the parent type's XML comments

---

### 8. Public vs Private Function Boundaries
**Priority: Low**

The codebase has clear separation between public API functions and private helper functions.

**Current pattern:**
```fsharp
let private getKanjiIdsAsync ...
let private getKanjiByIdsAsync ...
let getKanjiAsync (query: GetKanjiQuery) (ctx: KensakuConnection) = ...
```

**Recommendation:**
- This is well done - no changes needed
- Continue this pattern for maintainability
- Public API is clean and focused on user-facing operations

---

### 9. Module Organization
**Priority: Low**

The library is organized into clear modules:
- `Domain.fs` - Domain types
- `Tables.fs` - Database table types
- `TypeHandlers.fs` - Dapper type handlers (internal)
- `KensakuConnection.fs` - Connection class
- `Kanji.fs` - Kanji query functions
- `Words.fs` - Word query functions
- `Radicals.fs` - Radical query functions
- `Utilities.fs` - Helper functions

**Recommendation:**
- Good separation of concerns
- Consider whether `Tables.fs` should remain public (see recommendation #1)
- Consider whether `TypeHandlers.fs` types should be in a separate namespace since they're internal implementation

---

### 10. Consistency Between Query Functions
**Priority: Low**

The three main query modules (Kanji, Words, Radicals) follow similar patterns:

**Kanji module:**
- `getKanjiAsync` - query by criteria
- `getKanjiLiteralsAsync` - query by exact characters
- `getRadicalNamesAsync` - utility function

**Words module:**
- `getWordsAsync` - query by criteria
- `getWordLiteralsAsync` - query by exact word
- `getWordForms` - utility function

**Radicals module:**
- `getRadicalsAsync` - query by criteria
- `getRadicalLiteralsAsync` - query by exact radicals

**Observation:**
- Consistent naming pattern across modules ✓
- Similar structure (criteria query + literal query) ✓
- Each module has domain-specific utility functions

**Recommendation:**
- No changes needed - consistency is good
- If adding new query modules, follow this pattern

---

## Summary

**Critical Issues:** None

**High Priority:**
- None

**Medium Priority:**
1. Make `Tables.fs` types internal and ensure public API only uses Domain types
2. Review AutoOpen usage in Utilities module (consider removing to avoid name collisions)
3. Consider connection factory methods for better API ergonomics

**Low Priority:**
1. Consider Result types for future versions (would be breaking change)
2. Document current error handling approach (exceptions propagate)
3. Consider named record types for nested structures if extensibility is needed

## Overall Assessment

The API surface is **well-designed** with:
- ✓ Clear naming conventions
- ✓ Consistent async patterns
- ✓ Good type safety with discriminated unions
- ✓ Appropriate documentation (after this PR)
- ✓ Clean public/private boundaries
- ✓ Logical module organization
- ✓ Consistency across similar operations

The main concerns are about implementation details (Tables types) leaking into the public API and the AutoOpen module potentially causing naming conflicts. These are appropriate to address in a pre-release version.

Most other recommendations are for consideration in future versions rather than immediate changes needed for a v0.1 release.
