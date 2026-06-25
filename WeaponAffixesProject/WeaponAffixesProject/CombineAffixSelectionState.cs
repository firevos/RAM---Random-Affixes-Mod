using System.Collections.Generic;

namespace WeaponAffixesProject
{
    internal static class CombineAffixSelectionState
    {
        private const int MaxSlots = 10;
        private static readonly Dictionary<XUiC_CombineWindowGroup, bool[]> SelectedFromItemB = new Dictionary<XUiC_CombineWindowGroup, bool[]>();
        private static readonly Dictionary<XUiC_CombineWindowGroup, bool[]> HasExplicitSelection = new Dictionary<XUiC_CombineWindowGroup, bool[]>();

        internal static bool IsSelected(XUiC_CombineWindowGroup group, int slot, bool itemB)
        {
            if (group == null || slot < 0 || slot >= MaxSlots)
                return false;

            return GetArray(HasExplicitSelection, group)[slot] && GetArray(SelectedFromItemB, group)[slot] == itemB;
        }

        internal static void Toggle(XUiC_CombineWindowGroup group, int slot, bool itemB)
        {
            if (group == null || slot < 0 || slot >= MaxSlots)
                return;

            bool[] explicitSelection = GetArray(HasExplicitSelection, group);
            bool[] selectedFromB = GetArray(SelectedFromItemB, group);

            if (explicitSelection[slot] && selectedFromB[slot] == itemB)
            {
                explicitSelection[slot] = false;
                selectedFromB[slot] = false;
                return;
            }

            explicitSelection[slot] = true;
            selectedFromB[slot] = itemB;
        }

        internal static void ClearIfSelected(XUiC_CombineWindowGroup group, int slot, bool itemB)
        {
            if (group == null || slot < 0 || slot >= MaxSlots)
                return;

            bool[] explicitSelection = GetArray(HasExplicitSelection, group);
            bool[] selectedFromB = GetArray(SelectedFromItemB, group);
            if (!explicitSelection[slot] || selectedFromB[slot] != itemB)
                return;

            explicitSelection[slot] = false;
            selectedFromB[slot] = false;
        }

        internal static void Clear(XUiC_CombineWindowGroup group)
        {
            if (group == null)
                return;

            HasExplicitSelection.Remove(group);
            SelectedFromItemB.Remove(group);
        }

        internal static ItemValue[] BuildResultAffixes(XUiC_CombineWindowGroup group, ItemValue itemA, ItemValue itemB)
        {
            int maxAffixes = AffixUtils.GetConfiguredMaxAffixes();
            ItemValue[] result = new ItemValue[maxAffixes];
            bool[] explicitSelection = GetArray(HasExplicitSelection, group);
            bool[] selectedFromB = GetArray(SelectedFromItemB, group);

            for (int i = 0; i < maxAffixes; i++)
            {
                ItemValue[] sourceMods = explicitSelection[i] && selectedFromB[i] ? itemB?.CosmeticMods : itemA?.CosmeticMods;
                result[i] = CloneAffixAt(sourceMods, i);
            }

            return result;
        }

        internal static int CountSelectedAffixesFromItemB(XUiC_CombineWindowGroup group, ItemValue itemB)
        {
            if (group == null || itemB?.CosmeticMods == null)
                return 0;

            int maxAffixes = AffixUtils.GetConfiguredMaxAffixes();
            bool[] explicitSelection = GetArray(HasExplicitSelection, group);
            bool[] selectedFromB = GetArray(SelectedFromItemB, group);
            int count = 0;

            for (int i = 0; i < maxAffixes; i++)
            {
                if (!explicitSelection[i] || !selectedFromB[i])
                    continue;
                if (i >= itemB.CosmeticMods.Length || itemB.CosmeticMods[i] == null || itemB.CosmeticMods[i].IsEmpty())
                    continue;

                count++;
            }

            return count;
        }

        private static ItemValue CloneAffixAt(ItemValue[] mods, int slot)
        {
            if (mods == null || slot < 0 || slot >= mods.Length || mods[slot] == null || mods[slot].IsEmpty())
                return ItemValue.None.Clone();

            return mods[slot].Clone();
        }

        private static bool[] GetArray(Dictionary<XUiC_CombineWindowGroup, bool[]> states, XUiC_CombineWindowGroup group)
        {
            if (!states.TryGetValue(group, out bool[] state))
            {
                state = new bool[MaxSlots];
                states[group] = state;
            }

            return state;
        }
    }
}
