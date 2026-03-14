using Challenges;
using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(ChallengeObjectiveUseItem), "get_DescriptionText")]
    public static class ChallengeObjectiveUseItemDescriptionText
    {
        public static void Postfix(ChallengeObjectiveUseItem __instance, ref string __result)
        {
            var challenge = __instance.Owner?.ChallengeClass?.Name;
            switch (challenge)
            {
                case "getoneaffix":
                    __result = "Kill With Any Affix";
                    break;
                case "upgradeoneaffix":
                    __result = "Upgrade Any Affix";
                    break;
                case "getoneaffixrare":
                    __result = "Kill With Any Rare Affix";
                    break;
                case "unlockoneaffix":
                    __result = "Unlock Any Affix";
                    break;
                case "getthreeaffixes":
                    __result = "Kill With 3 Affixes";
                    break;
                case "upgradeepicaffix":
                    __result = "Upgrade to Epic Affix";
                    break;
                case "upgrademythicaffix":
                    __result = "Upgrade to Mythic Affix";
                    break;
                case "getfiveaffixes":
                    __result = "Kill With 6 Affixes";
                    break;
                case "upgradefifteenaffixes":
                    __result = "Upgrade Any Affix";
                    break;
                case "upgradefifteentimes":
                    __result = "Upgrade 1 Weapon 15 times";
                    break;
                case "upgradetenmythicaffix":
                    __result = "Upgrade to Mythic Affix";
                    break;
                case "getelevenaffixes":
                    __result = "Kill With 11 Affixes";
                    break;
                case "getfivemythicaffixes":
                    __result = "Kill with 5+ Mythic Affixes";
                    break;
                default:
                    break;
            }
        }
    }
}
