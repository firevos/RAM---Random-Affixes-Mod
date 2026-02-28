using HarmonyLib;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        public void InitMod(Mod __mod)
        {
            Log.Out("[WeaponBuffMod] Initializing...");
            var harmony = new Harmony("com.example.weaponbuff");
            harmony.PatchAll();
        }
    }
}
