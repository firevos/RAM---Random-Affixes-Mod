using HarmonyLib;
using System;

namespace RandomPlusProject
{
    [HarmonyPatch(typeof(ItemValue), nameof(ItemValue.GetValue))]
    public class ItemValueGetValue
    {
        static void Postfix(ItemValue __instance, EntityAlive _entity, PassiveEffects _passiveEffect, FastTags<TagGroup.Global> _tags)
        {
            Log.Out($"ItemValue: {__instance.ItemClass.localizedName}");

            foreach (var effectGroup in __instance.ItemClass.Effects.EffectGroups)
            {
                foreach (var effect in effectGroup.PassiveEffects)
                {
                    Log.Out(effect.Modifier.ToString());
                    Log.Out(effect.Type.ToString());
                    foreach (var value in effect.Values)
                        Log.Out(value.ToString());
                }
            }
        }
    }
}
