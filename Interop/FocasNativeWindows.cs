using System.Runtime.InteropServices;

namespace FanucFocasConsole.Interop
{
    public class FocasNativeWindows : IFocasNative
    {
        private const string DllName = "Fwlib32.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_startupprocess(int type, string logfile);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_exitprocess();

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_allclibhndl3(string ip, ushort port, int timeout, out ushort handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_freelibhndl(ushort handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_statinfo(ushort handle, out ODBST stat);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_alarm2(ushort handle, out int alarmStatus);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdopmode(ushort handle, out short mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdprgnum(ushort handle, out ODBPRO pro);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_exeprgname(ushort handle, out ODBEXEPRG exeprg);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_acts(ushort handle, out ODBACT act);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_actf(ushort handle, out ODBACT act);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdspload(ushort handle, short spindleNo, out ODBSPN load);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdsvmeter(ushort handle, ref short axisNo, out ODBSVLOAD load);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdspmeter(ushort handle, short type, ref short spindleNo, out ODBSPLOAD meter);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdparam(ushort handle, short paramNo, short axis, short type, out IODBPSD param);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdproctime(ushort handle, out ODBPTIME ptime);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdalminfo2(ushort handle, short almType, short almCount, short dummy, out ALMINFO2 info);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        private static extern short cnc_rdalmmsg2(ushort handle, short type, ref short almNo, out ODBALMMSG2 msg);

        public short StartupProcess(int type, string logfile) => cnc_startupprocess(type, logfile);
        public short ExitProcess() => cnc_exitprocess();
        public short Connect(string ip, ushort port, int timeout, out ushort handle) => cnc_allclibhndl3(ip, port, timeout, out handle);
        public short Disconnect(ushort handle) => cnc_freelibhndl(handle);
        public short ReadStatInfo(ushort handle, out ODBST stat) => cnc_statinfo(handle, out stat);
        public short ReadAlarmStatus(ushort handle, out int alarmStatus) => cnc_alarm2(handle, out alarmStatus);
        public short ReadOpMode(ushort handle, out short mode) => cnc_rdopmode(handle, out mode);
        public short ReadProgramNumber(ushort handle, out ODBPRO pro) => cnc_rdprgnum(handle, out pro);
        public short ReadProgramName(ushort handle, out ODBEXEPRG exeprg) => cnc_exeprgname(handle, out exeprg);
        public short ReadActs(ushort handle, out ODBACT act) => cnc_acts(handle, out act);
        public short ReadActf(ushort handle, out ODBACT act) => cnc_actf(handle, out act);
        public short ReadSpindleLoad(ushort handle, short spindleNo, out ODBSPN load) => cnc_rdspload(handle, spindleNo, out load);
        public short ReadServoLoad(ushort handle, short axisNo, out ODBSVLOAD load)
        {
            short axis = axisNo;
            return cnc_rdsvmeter(handle, ref axis, out load);
        }
        public short ReadSpindleMeter(ushort handle, short type, short spindleNo, out ODBSPLOAD meter)
        {
            short spindle = spindleNo;
            return cnc_rdspmeter(handle, type, ref spindle, out meter);
        }
        public short ReadParameter(ushort handle, short paramNo, short axis, short type, out IODBPSD param) => cnc_rdparam(handle, paramNo, axis, type, out param);
        public short ReadProcTime(ushort handle, out ODBPTIME ptime) => cnc_rdproctime(handle, out ptime);
        public short ReadAlarmInfo2(ushort handle, out ALMINFO2 info) => cnc_rdalminfo2(handle, 0, 5, 0, out info);
        public short ReadAlarmMessage2(ushort handle, short axis, ref short almNo, out ODBALMMSG2 msg) => cnc_rdalmmsg2(handle, 0, ref almNo, out msg);
    }
}
