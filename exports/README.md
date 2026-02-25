# Google Sheets Import Files

Generated from `zzRandomAffixesMod/Config/item_modifiers.xml`, `zzRandomAffixesMod/Config/buffs.xml`, and `zzRandomAffixesMod/Config/Localization.txt`.

## Files
- `affixes_weapon.csv` — weapon/tool-oriented affix families with installable tags and rarity 1–6 effect text.
- `affixes_armor.csv` — armor-oriented affix families with installable tags and rarity 1–6 effect text.
- `armor_set_bonuses.csv` — armor full set bonus rows by set and quality (1–6), including localized text and XML effect details.

## Suggested Google Sheets workflow
1. Create a new Google Sheet.
2. Import `affixes_weapon.csv` into tab `Weapon Affixes`.
3. Import `affixes_armor.csv` into tab `Armor Affixes`.
4. Import `armor_set_bonuses.csv` into tab `Armor Set Bonuses`.

When importing each file, choose **Insert new sheet(s)** so each CSV becomes its own tab.
