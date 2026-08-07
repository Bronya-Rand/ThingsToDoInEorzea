using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Models
{
    public enum FateMatchMode : uint
    {
        AnyFate = 0,
        AnyInRegion = 1,
        SpecificFate = 2
    }
    public static class FateMatchModeExtensions
    {
        public static string GetName(this FateMatchMode matchMode, LocalizationService? loc = null)
        {
            var l = loc ?? new LocalizationService();
            return matchMode switch
            {
                FateMatchMode.AnyFate => l.T("ui.fate_mode.any"),
                FateMatchMode.AnyInRegion => l.T("ui.fate_mode.region"),
                FateMatchMode.SpecificFate => l.T("ui.fate_mode.specific"),
                _ => "Unknown"
            };
        }
    }
}
