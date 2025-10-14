# Kensaku.Schema

⚠️ **This library is an implementation detail and is considered unstable.**

This library contains internal database schema types used across different parts of the Kensaku project. It is only exposed for sharing between `Kensaku.Core` and `Kensaku.CreateDatabase`.

## Purpose

The types in this library represent the database table structures and are used internally for:
- Database operations in `Kensaku.Core`
- Database creation and population in `Kensaku.CreateDatabase`

## Stability Warning

**This library is NOT intended for public consumption.** It exists solely to avoid duplication between internal Kensaku components.

If you're looking to use Kensaku in your project, please use:
- **[Kensaku.Core](../Kensaku.Core)** - For querying the dictionary database
- **[Kensaku.DataSources](../Kensaku.DataSources)** - For parsing source dictionary files
