# Kensaku.Schema

⚠️ **This library is an implementation detail and is considered unstable.**

This library contains internal database schema types used across different parts of the Kensaku project. It is only exposed for sharing between `Kensaku.Core` and `Kensaku.CreateDatabase`.

## Purpose

The types in this library represent the database table structures and are used internally for:
- Database operations in `Kensaku.Core`
- Database creation and population in `Kensaku.CreateDatabase`

## Stability Warning

**This library is NOT intended for public consumption.** The API may change at any time without notice or version bumping. It exists solely to avoid duplication between internal Kensaku components.

If you're looking to use Kensaku in your project, please use:
- **[Kensaku.Core](../Kensaku.Core)** - For querying the dictionary database
- **[Kensaku.DataSources](../Kensaku.DataSources)** - For parsing source dictionary files

## Types

All types in this library are CLIMutable record types that map directly to database tables. They include fields with database-specific concerns (like foreign key IDs) and are not suitable for use in application code.
