# Native libraries

FANUC FOCAS libraries go here. They're **not** in Git — they're FANUC's
property and can't be redistributed.

Expected files:

- `Fwlib32.dll` (Windows, 32-bit)
- `fwlibe1.dll` (Windows, Ethernet transport)
- `libfwlib32-linux-x64.so.1.0.5` (Linux)
- `fwlib32.h` (header, for reference)

Get them from the FANUC support portal, from the CD that ships with the
CNC, or from your integrator. Don't download them from random sources.

Place them in this folder under the names above and rebuild. Details in
[`../docs/setup-native-libs.md`](../docs/setup-native-libs.md).