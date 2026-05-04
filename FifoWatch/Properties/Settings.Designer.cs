namespace FifoWatch.Properties
{
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get { return defaultInstance; }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string LastIpAddress
        {
            get { return ((string)(this["LastIpAddress"])); }
            set { this["LastIpAddress"] = value; }
        }
    }
}
