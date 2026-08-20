# FANUC FOCAS libraries

The SDK is FANUC's property and isn't shipped here. You need to get it
before the app will run.

## Files

| File                            | Platform | Notes                |
| ------------------------------- | -------- | -------------------- |
| `Fwlib32.dll`                   | Windows  | 32-bit only          |
| `fwlibe1.dll`                   | Windows  | Ethernet transport   |
| `libfwlib32-linux-x64.so.1.0.5` | Linux    | 64-bit               |
| `fwlib32.h`                     | —        | C header (reference) |

## Where to get them

- **FANUC support portal** — needs a customer account.
- **Machine supplier** — usually on a CD or USB stick that comes with
  the CNC, together with the FOCAS manual.
- **Your integrator** — if you're going through one.

## Install

Drop the files into `NativeLibs/` with the exact names from the table.
The `.csproj` copies them next to the executable at build time. After
`dotnet build`, check:

```cmd
dir bin\Debug\net10.0\Fwlib32.dll
```

On Linux the library must be visible as `libfwlib32.so`. The `.csproj`
handles the rename via `<TargetPath>`; the Dockerfile creates the
symlink.

On Linux, `cnc_startupprocess` has to be called once before any other
FOCAS call. `FanucDataService` does this automatically. On Windows the
DLL self-initializes, so the call is skipped.