using WeaponBuffMod.HarmonyPatches;

namespace WeaponAffixesProject
{
    internal static class CustomSandboxSettings
    {
        internal const string MaxAffixes = "MaxAffixes";
        internal const string AffixAbundance = "AffixAbundance";
        internal const string KillsToUpgrade = "KillsToUpgrade";
        internal const string ToggleKillcounter = "ToggleKillcounter";
        internal const string AffixRarity = "AffixRarity";
        internal const string TokenLootAbundance = "TokenLootAbundance";

        internal static void Register()
        {
            if (SandboxOptions.SandboxOptionManager.HasInstance)
            {
                RamSandboxOptions.EnsureRegistered(SandboxOptions.SandboxOptionManager.Current);
            }
        }

        internal static int GetInt(string name, int defaultValue)
        {
            Register();

            switch (name)
            {
                case MaxAffixes:
                    return RamSandboxOptions.GetMaxAffixesValue();
                case AffixAbundance:
                    return RamSandboxOptions.GetAffixAbundanceValue();
                case KillsToUpgrade:
                    return RamSandboxOptions.GetKillsToUpgradeValue();
                case ToggleKillcounter:
                    return RamSandboxOptions.GetToggleKillcounterValue();
                case AffixRarity:
                    return RamSandboxOptions.GetAffixRarityValue();
                case TokenLootAbundance:
                    return RamSandboxOptions.GetTokenLootAbundanceValue();
                default:
                    return defaultValue;
            }
        }
    }
}
