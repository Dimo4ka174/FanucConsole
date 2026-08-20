namespace FanucFocasConsole.Interop
{
    public static class FocasError
    {
        public static string Describe(short code) => code switch
        {
            0 => "EW_OK (success)",
            1 => "EW_FUNC (function not permitted)",
            2 => "EW_FUNC (function not permitted)",
            3 => "EW_FUNC (function not permitted)",
            -1 => "EW_BUSY (busy)",
            -2 => "EW_PARAM (invalid parameter)",
            -3 => "EW_BUFFER (buffer overflow)",
            -4 => "EW_BUSY (busy)",
            -5 => "EW_DATA (invalid data)",
            -6 => "EW_NODATA (no data)",
            -7 => "EW_PROT (write protected)",
            -8 => "EW_HANDLE (connection lost / invalid handle)",
            -9 => "EW_LENGTH (length error)",
            -10 => "EW_MODE (mode error)",
            -16 => "EW_SOCKET (socket communication error)",
            _ => "unknown error"
        };
    }
}
