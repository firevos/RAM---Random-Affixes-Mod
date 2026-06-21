using HarmonyLib;
using WeaponAffixesProject;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        public void InitMod(Mod __mod)
        {
            Log.Out("[WeaponBuffMod] Initializing...");
            CustomSandboxSettings.Register();
            var harmony = new Harmony("com.example.weaponbuff");
            harmony.PatchAll();
        }
    }
}