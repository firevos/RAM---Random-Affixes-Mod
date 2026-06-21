using WeaponBuffMod.HarmonyPatches;

namespace WeaponAffixesProject
{
    internal static class CustomSandboxSettings
    {
        internal const string MaxAffixes = "MaxAffixes";
        internal const string AffixAbundance = "AffixAbundance";
        internal const string KillsToUpgrade = "KillsToUpgrade";

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
                default:
                    return defaultValue;
            }
        }
    }
}
