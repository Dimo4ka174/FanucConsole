using System.Runtime.InteropServices;

namespace FanucFocasConsole.Interop
{
    // ---------- ODBST (cnc_statinfo) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBST
    {
        public short hdck;
        public short tmmode;
        public short aut;
        public short run;
        public short motion;
        public short mstb;
        public short emergency;
        public short alarm;
        public short edit;
    }

    // ---------- ODBPRO (cnc_rdprgnum) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBPRO
    {
        public short dummy1;
        public short dummy2;
        public short data;    // current program number
        public short mdata;   // main program number
    }

    // ---------- ODBEXEPRG (cnc_exeprgname) ----------
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ODBEXEPRG
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
        public string name;
        public int o_num;    // long in C (4 bytes) -> int
    }

    // ---------- ODBACT (cnc_acts / cnc_actf) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBACT
    {
        public short dummy1;
        public short dummy2;
        public int data;    // long in C -> int (4 bytes)
    }

    // ---------- ODBSPN (cnc_rdspload) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBSPN
    {
        public short datano;
        public short type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]  // MAX_SPINDLE is usually 8
        public short[] data;
    }

    // ---------- IODBPSD (cnc_rdparam) ----------
    [StructLayout(LayoutKind.Explicit)]
    public struct IODBPSD
    {
        [FieldOffset(0)] public short datano;
        [FieldOffset(2)] public short type;
        [FieldOffset(4)] public int ldata;    // 4-byte long
        [FieldOffset(4)] public short idata;
        [FieldOffset(4)] public byte cdata;
    }

    // ---------- ODBPTIME (cnc_rdproctime) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBPTIME
    {
        public short num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ODBPTIMEDATA[] data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ODBPTIMEDATA
    {
        public int prg_no;
        public short hour;
        public byte minute;
        public byte second;
    }

    // ---------- ODBSVLOAD (cnc_rdsvmeter) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBSVLOAD
    {
        public int data;      // long -> int
        public short dec;
        public short unit;
        public byte name;
        public byte suff1;
        public byte suff2;
        public byte reserve;
    }

    // ---------- ODBSPLOAD (cnc_rdspmeter) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ODBSPLOAD
    {
        public int spload_data;   // long -> int
        public short spload_dec;
        public short spload_unit;
        public byte spload_name;
        public byte spload_suff1;
        public byte spload_suff2;
        public byte spload_reserve;
        public int spspeed_data;   // long -> int
        public short spspeed_dec;
        public short spspeed_unit;
        public byte spspeed_name;
        public byte spspeed_suff1;
        public byte spspeed_suff2;
        public byte spspeed_reserve;
    }

    // ---------- ALMINFO2 (cnc_rdalminfo2) ----------
    [StructLayout(LayoutKind.Sequential)]
    public struct ALMINFO2
    {
        public short axis1;
        public short alm_no1;
        public short axis2;
        public short alm_no2;
        public short axis3;
        public short alm_no3;
        public short axis4;
        public short alm_no4;
        public short axis5;
        public short alm_no5;
        public short data_end;
    }

    // ---------- ODBALMMSG2 (cnc_rdalmmsg2) ----------
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ODBALMMSG2
    {
        public int alm_no;    // long -> int
        public short type;
        public short axis;
        public short dummy;
        public short msg_len;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string alm_msg;
    }
}
