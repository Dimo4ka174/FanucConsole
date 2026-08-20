# Architecture

Three layers, kept thin. FOCAS-specific things stay in `Interop/` and
don't leak anywhere else.

```
Program.cs
    │
    ▼
Services/FanucDataService
    │
    ├──► Interop/IFocasNative   ──► Fwlib32.dll / libfwlib32.so
    │
    └──► DB/FanucRepository     ──► PostgreSQL
```

## Interop

`IFocasNative` is one interface, one method per native call. The two
implementations look almost identical — the only real difference is the
`DllImport` attributes:

| Class                 | Library         | Calling convention |
| --------------------- | --------------- | ------------------ |
| `FocasNativeWindows`  | `Fwlib32.dll`   | `Winapi` (stdcall) |
| `FocasNativeLinux`    | `libfwlib32.so` | `Cdecl`            |

`FocasNativeFactory.Create()` picks one via `RuntimeInformation`.
`cnc_startupprocess` is called once on Linux before anything else; on
Windows the DLL self-initializes so we skip it.

FOCAS structs live in `FocasStructures.cs`. They're never used outside
`Interop/` — `FanucDataService` maps them to `MachineSnapshot`
immediately.

## Service

`FanucDataService.RunTestAsync` runs a bounded loop (10 iterations per
machine). Every FOCAS return code goes through `Ok(...)`, which is the
only place `EW_HANDLE (-8)` is handled: it resets `_handle` to 0, and
the outer loop decides whether to reconnect. Everything else that isn't
`0` is logged once with a readable description and the loop moves on.

The `_handle` field is process-wide state, which is fine here because
machines are polled sequentially. If this ever moves to parallel
polling, the handle has to become a per-machine object.

## Database

`FanucRepository` uses Npgsql directly. The schema is five fixed tables
and the operations are simple inserts — an ORM would just add a
dependency.

`EnsureDatabaseExistsAsync` connects to the `postgres` maintenance
database, checks `pg_database`, and creates `fanuc_data` if it's
missing. `EnsureCreatedAsync` runs `CREATE TABLE IF NOT EXISTS` for all
five tables on every startup.

**Known gap:** `SaveSnapshotAsync` runs five separate inserts without an
explicit transaction. A crash between them leaves a row in `snapshots`
without its children. Should be `BeginTransactionAsync` — it just
hasn't mattered on a test bench.