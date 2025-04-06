@echo off
if not defined KENSAKU_DB_PATH (
    set "KENSAKU_DB_PATH=%~dp0kensaku.db"
)
"%~dp0kensaku.exe" %*
