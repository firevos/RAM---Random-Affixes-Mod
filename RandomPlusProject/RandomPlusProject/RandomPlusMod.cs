using HarmonyLib;

namespace RandomPlusMod
{
    public class RandomPlus : IModApi
    {
        public void InitMod(Mod __mod)
        {
            Log.Out("[RandomPlusMod] Initializing...");
            var harmony = new Harmony("com.example.randomplus");
            harmony.PatchAll();
        }
    }
}
