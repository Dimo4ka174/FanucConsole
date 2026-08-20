# FOCAS error codes

`cnc_*` returns a `short`. Zero is success.

| Code  | Meaning                                            |
| ----- | -------------------------------------------------- |
| `2`   | `EW_FUNC` — option not enabled on this CNC         |
| `6`   | `EW_NODATA` — no data                              |
| `-8`  | `EW_HANDLE` — session closed / invalid handle      |
| `-16` | `EW_SOCKET` — socket error                         |

The full list is in `fwlib32.h`.

## What I actually saw

Three FANUC CNCs, all over Ethernet. Below is what worked and what
didn't on those particular machines; other models will behave
differently.

**Worked:** `cnc_statinfo`, `cnc_alarm2`, `cnc_rdalminfo2`,
`cnc_rdalmmsg2`, `cnc_rdopmode`, `cnc_rdprgnum`, `cnc_exeprgname`,
`cnc_acts`, `cnc_actf`, `cnc_rdspload`, `cnc_rdsvmeter`.

**Didn't:**

- `cnc_rdparam(6750)` → `2` (power-on time option not enabled)
- `cnc_rdparam(6712)` → `2` (total-parts counter not enabled)
- `cnc_rdproctime` → `6` (no cutting-time journal)

All three were removed from the polling loop.

## `EW_HANDLE (-8)`

Every machine dropped the FOCAS session roughly 5–6 seconds after the
first successful read. After that, everything returns `-8`.

I didn't chase it to ground. Candidates:

- The CNC has an idle-session timeout configured (`#14880`–`#14884` in
  the FOCAS manual).
- The controller has a limit on concurrent FOCAS clients and something
  else was holding a slot.
- The session needs a keep-alive call on a short timer.

Current workaround: reconnect once per polling cycle, give up if the
reconnect fails. A production client should probably send a cheap
keep-alive (`cnc_statinfo`) every 1–2 seconds instead of relying on
reconnects.

## `cnc_rdspload` units

Raw values came back anywhere from `60` to `782`, depending on the
machine and what it was doing. These are not percentages. What they map
to depends on the CNC model and the `ODBSPN.type` field. The value is
stored in the DB as-is.