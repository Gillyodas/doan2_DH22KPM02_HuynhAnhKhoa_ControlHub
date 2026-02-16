namespace ControlHub.SharedKernel.Common.Logs
{
    public class CommonLogs
    {
        // Log cho l?i thi?u c?u hình (Admin c?n fix ngay)
        public static readonly LogCode System_ConfigMissing =
            new("System.ConfigMissing", "Master Key is missing in AppSettings configuration");

        // Log cho l?i nh?p sai Master Key (Có th? là t?n công)
        public static readonly LogCode Auth_InvalidMasterKey =
            new("Auth.InvalidMasterKey", "Invalid Master Key provided during registration attempt");

        public static readonly LogCode System_InvalidConfiguration =
            new("System.InvalidConfiguration", "Invalid system configuration value");

        public static readonly LogCode System_ConfigFallback =
            new("System.ConfigFallback", "Configuration invalid or missing, using default fallback value");
    }
}
