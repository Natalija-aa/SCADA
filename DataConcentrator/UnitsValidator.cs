using System.Linq;

namespace DataConcentrator
{
    // zajednicka pravila za polje Jedinice (Units) na AI/AO tagovima
    // koristi ih i rucni unos (AddWindow) i uvoz iz JSON-a (ConfigurationService)
    public static class UnitsValidator
    {
        public const int MaxLength = 20;

        // prazno je dozvoljeno - Jedinice nisu obavezno polje
        public static readonly string[] AllowedUnits =
        {
            "", "bar", "°C", "l/s", "%", "kPa", "Pa", "m3/h", "V", "A", "Hz", "rpm", "m", "kg"
        };

        public static bool IsValid(string units)
        {
            if (units == null) return true;
            if (units.Length > MaxLength) return false;
            return AllowedUnits.Contains(units);
        }
    }
}
