# FanucFocasConsole

> A console test harness for FANUC CNC controllers via the **FOCAS**
> Ethernet API, with optional PostgreSQL logging.

[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-blue)]()
[![Architecture](https://img.shields.io/badge/arch-x86%20%7C%20x64-orange)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Latest release](https://img.shields.io/github/v/release/Dimo4ka174/FanucConsole)](https://github.com/Dimo4ka174/FanucConsole/releases)

---

## Overview

Connects to one or more FANUC CNC controllers over TCP port 8193, reads
a fixed set of status registers, and — if PostgreSQL is reachable —
writes a structured snapshot of each poll to the database.

I wrote this as a standalone test bench before porting the FOCAS interop
layer into a larger monitoring system. It is published as a reference
for anyone who has to wire the FOCAS C API into .NET: the SDK is barely
documented, and working C# wrappers are hard to find.

---

## Features

- One `IFocasNative` interface with two implementations
  (`FocasNativeWindows`, `FocasNativeLinux`), picked at runtime by
  `FocasNativeFactory`.
- Sequential polling of multiple machines.
- Twelve status registers per poll: run state, alarms + alarm texts,
  operation mode, active program (number and name), spindle and feed
  rates, spindle and servo loads.
- Optional PostgreSQL persistence: five tables, one snapshot per poll.
- Serilog (console + rolling file).
- Docker Compose for app + PostgreSQL.
- `make` targets for common tasks on Windows and Unix.

---

## Requirements

| Component         | Version    | Notes                                              |
| ----------------- | ---------- | -------------------------------------------------- |
| .NET SDK          | 10.0       | Target: `net10.0`                                  |
| FANUC FOCAS       | —          | `Fwlib32.dll`, `fwlibe1.dll`, `libfwlib32.so` — see [NativeLibs/README.md](NativeLibs/README.md) |
| PostgreSQL        | 16         | Optional                                           |
| Docker (optional) | any recent | For the containerized workflow                     |

On Windows the process is built as **x86** — `Fwlib32.dll` is 32-bit.
On Linux the SDK ships an x64 `.so` and no override is applied.

---

## Quick start

### 1. Get the FANUC libraries

See [`NativeLibs/README.md`](NativeLibs/README.md). Put the files into
`NativeLibs/` without committing them.

### 2. Local run (Windows)

```cmd
dotnet run
```

You'll be prompted for an IP. Either enter one (e.g. `192.0.2.10`) or
several comma-separated addresses.

### 3. Run against PostgreSQL in Docker

```cmd
make win 192.0.2.10
```

Starts PostgreSQL on `localhost:5432`, publishes the app as
`win-x86` self-contained, and runs it against the given IP.

### 4. Run fully in Docker (Linux)

```cmd
make linux
docker logs -f fanuc_app
```

The Linux FOCAS library is baked into the image.

---

## Architecture

```
Program.cs → Services/FanucDataService → Interop/IFocasNative → Fwlib32.dll / .so
                                       ↘ DB/FanucRepository    → PostgreSQL
```

All FOCAS-specific knowledge (calling convention, struct layout, error
codes) lives in `Interop/`. `FanucDataService` runs the poll loop and
maps native structs to `MachineSnapshot`. `FanucRepository` writes
snapshots via Npgsql — no ORM, the schema is fixed and small.

See [`docs/architecture.md`](docs/architecture.md) for the details.

---

## FOCAS interop notes

Two things bite everyone who does this.

**Calling convention is different per platform.** Windows uses `stdcall`
(`CallingConvention.Winapi`); Linux uses `cdecl`. Mixing them up doesn't
give you an error — it gives you corrupted arguments and a crash:

```csharp
// Windows
[DllImport("Fwlib32.dll", CallingConvention = CallingConvention.Winapi)]
private static extern short cnc_allclibhndl3(...);

// Linux
[DllImport("fwlib32", CallingConvention = CallingConvention.Cdecl)]
private static extern short cnc_allclibhndl3(...);
```

**Structs must mirror the C header exactly**, including padding and
union fields:

```csharp
// Fixed-size char array in C → ByValTStr in C#
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct ODBEXEPRG
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
    public string name;
    public int o_num;
}

// C union → Explicit layout + FieldOffset
[StructLayout(LayoutKind.Explicit)]
public struct IODBPSD
{
    [FieldOffset(0)] public short datano;
    [FieldOffset(2)] public short type;
    [FieldOffset(4)] public int ldata;
    [FieldOffset(4)] public short idata;
    [FieldOffset(4)] public byte cdata;
}
```

Full struct list in [`Interop/FocasStructures.cs`](Interop/FocasStructures.cs).

### Error codes

`cnc_*` returns a `short`; `0` is success. `FocasError.Describe()` maps
the ones I actually ran into:

| Code  | Constant    | Meaning                                    |
| ----- | ----------- | ------------------------------------------ |
| `2`   | `EW_FUNC`   | Option not enabled on this CNC             |
| `6`   | `EW_NODATA` | No data                                    |
| `-8`  | `EW_HANDLE` | Session closed by the CNC / bad handle     |
| `-16` | `EW_SOCKET` | Socket error                               |

See [`docs/focas-errors.md`](docs/focas-errors.md) for what actually
happened on the test bench.

---

## Database schema

Five tables, everything linked to `snapshots.id`:

| Table          | Contents                                              |
| -------------- | ----------------------------------------------------- |
| `snapshots`    | One row per poll: machine IP + timestamp              |
| `status`       | Run state, mode, program number/name                  |
| `loads`        | Spindle / servo loads, spindle / feed rates           |
| `working_time` | Power-on time, total parts, cutting time (optional)   |
| `alarms`       | One row per active alarm                              |

The tables are created on first run if the DB is reachable. If it's not,
the app logs a warning and keeps polling without persistence — FOCAS
still works.

---

## Configuration

Order of precedence:

1. Command-line arguments (IPs, space-separated).
2. Environment variables:
   - `MACHINE_IPS` — comma-separated list
   - `MACHINE_IP` — single IP
   - `MACHINE_PORT` — default `8193`
   - `MACHINE_TIMEOUT` — connection timeout in seconds, default `10`
3. `.env` in the working directory (same names).
4. Interactive prompt.

Connection string comes from `appsettings.json` →
`ConnectionStrings:DefaultConnection`, or from the environment variable
`ConnectionStrings__DefaultConnection`.

---

## Make targets

`make.bat` on Windows, `Makefile` on Linux/macOS:

| Target                    | Action                                     |
| ------------------------- | ------------------------------------------ |
| `make build`              | Build the Docker image                     |
| `make linux`              | App + PostgreSQL via `docker compose`      |
| `make win [ip ...]`       | PostgreSQL in Docker + Windows exe         |
| `make win-nodb [ip ...]`  | Windows exe, no Docker / PostgreSQL        |
| `make save-images`        | Save Docker images to `.tar` (offline)     |
| `make load-images`        | Load Docker images from `.tar`             |
| `make backup`             | `pg_dump` the database                     |
| `make db-info`            | Write a DB report to `db_report.txt`       |
| `make down`               | Stop containers                            |
| `make clean`              | Stop containers and remove volumes         |

---

## Layout

```
.
├── .github/workflows/   CI
├── DB/                  Npgsql repository
├── DTO/                 MachineSnapshot
├── Interop/             FOCAS native interface + implementations
├── NativeLibs/          Placeholder for the FANUC DLLs (see its README)
├── Services/            Poll loop
├── docs/                Architecture, FOCAS notes, native libs setup
├── Program.cs
├── FanucFocasConsole.csproj
├── Dockerfile
├── docker-compose.yaml
├── make.bat / Makefile
└── LICENSE
```

---

## Known limitations

- **x86 on Windows.** `Fwlib32.dll` is 32-bit. A 64-bit process can't
  load it. If you need one, get `Fwlib64.dll` from FANUC and change both
  the `.csproj` and the `DllImport` names.
- **`EW_HANDLE (-8)` after a few iterations.** On the machines I tested,
  the CNC drops the FOCAS session after ~5–6 seconds. The service
  reconnects once per cycle, then gives up on that machine. See
  [`docs/focas-errors.md`](docs/focas-errors.md).
- **Some registers aren't available on all models.** `cnc_rdparam(6750)`,
  `cnc_rdparam(6712)` and `cnc_rdproctime` returned `EW_FUNC (2)` /
  `EW_NODATA (6)` on all three machines, so those calls were removed
  from the loop.
- **No unit tests.** Behaviour against a real CNC can't be mocked
  faithfully; this is a manual bench.

---

## License

MIT — see [LICENSE](LICENSE).

The FANUC FOCAS libraries themselves aren't covered by this license.
They're FANUC's property and have to come through official channels.

---

## Author

**Dmitry Kharchenko** — C#/.NET developer, industrial automation and
interop with legacy equipment.

- GitHub: [@Dimo4ka174](https://github.com/Dimo4ka174)
- Email: `Dmitry.Kharchenko.Dev@yandex.ru`

> Personal project. Doesn't contain code, schematics, or data from any
> employer.