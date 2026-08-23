using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems;

public class WeaponPrefixEdits : ModSystem
{
    public override void Load()
    {
        On_Item.TryGetPrefixStatMultipliersForItem += CustomWeaponPrefixStats; //applies weapon prefix stats and correctly alters tooltips
    }
        private static bool CustomWeaponPrefixStats(On_Item.orig_TryGetPrefixStatMultipliersForItem orig, Item self, int rolledPrefix, out float dmg, out float kb, out float spd, out float size, out float shtspd, out float mcst, out int crt)
        {
			dmg = 1f;
			kb = 1f;
			spd = 1f;
			size = 1f;
			shtspd = 1f;
			mcst = 1f;
			crt = 0;
			switch (rolledPrefix)
			{
                #region Universal modifiers
                case PrefixID.Keen:
                    crt = 3;
                    break;
                case PrefixID.Superior:
                    dmg = 1.1f;
                    crt = 3;
                    kb = 1.1f;
                    break;
                case PrefixID.Forceful:
                    kb = 1.2f;
                    break;
                case PrefixID.Broken:
                    dmg = 0.7f;
                    crt = 50;
                    kb = 0.8f;
                    break;
                case PrefixID.Damaged:
                    dmg = 0.85f;
                    break;
                case PrefixID.Shoddy:
                    kb = 0.85f;
                    dmg = 0.9f;
                    break;
                case PrefixID.Hurtful:
                    dmg = 1.1f;
                    break;
                case PrefixID.Strong:
                    kb = 1.25f;
                    break;
                case PrefixID.Unpleasant:
                    kb = 1.1f;
                    dmg = 1.05f;
                    break;
                case PrefixID.Weak:
                    kb = 0.8f;
                    break;
                case PrefixID.Ruthless:
                    kb = 0.8f;
                    dmg = 1.2f;
                    break;
                case PrefixID.Godly:
                    kb = 1.12f;
                    dmg = 1.12f;
                    crt = 5;
                    break;
                case PrefixID.Demonic:
                    dmg = 1.15f;
                    crt = 5;
                    break;
                case PrefixID.Zealous:
                    crt = 8;
                    break;
                #endregion

                #region Common modifiers
                case PrefixID.Quick:
                    spd = 0.9f;
                    break;
                case PrefixID.Deadly2:
                    dmg = 1.1f;
                    spd = 0.9f;
                    break;
                case PrefixID.Agile:
                    spd = 0.9f;
                    crt = 6;
                    break;
                case PrefixID.Nimble:
                    spd = 0.95f;
                    break;
                case PrefixID.Murderous:
                    crt = 10;
                    spd = 0.94f;
                    dmg = 1.05f;
                    break;
                case PrefixID.Slow:
                    spd = 1.15f;
                    kb = 1.25f;
                    break;
                case PrefixID.Sluggish:
                    spd = 1.2f;
                    kb = 1.35f;
                    break;
                case PrefixID.Lazy:
                    spd = 1.08f;
                    break;
                case PrefixID.Annoying:
                    dmg = 0.8f;
                    spd = 1.15f;
                    break;
                case PrefixID.Nasty:
                    kb = 0.7f;
                    spd = 0.95f;
                    dmg = 1.15f;
                    crt = 4;
                    break;
                #endregion

                #region Melee modifiers
                case PrefixID.Large:
                    size = 1.12f;
                    break;
                case PrefixID.Massive:
                    size = 1.33f; //default 1.18
                    spd = 1.15f;
                    dmg = 1.1f;
                    kb = 1.1f;
                    break;
                case PrefixID.Dangerous:
                    dmg = 1.05f;
                    crt = 4;
                    size = 1.1f;
                    break;
                case PrefixID.Savage:
                    dmg = 1.1f;
                    size = 1.2f;
                    kb = 1.1f;
                    break;
                case PrefixID.Sharp:
                    dmg = 1.15f;
                    break;
                case PrefixID.Pointy:
                    dmg = 1.1f;
                    crt = 5;
                    break;
                case PrefixID.Tiny:
                    size = 0.8f;
                    spd = 0.75f;
                    break;
                case PrefixID.Terrible:
                    kb = 0.85f;
                    dmg = 0.85f;
                    size = 0.87f;
                    break;
                case PrefixID.Small:
                    size = 0.9f;
                    spd = 0.92f;
                    break;
                case PrefixID.Dull:
                    dmg = 0.85f;
                    break;
                case PrefixID.Unhappy:
                    spd = 1.1f;
                    kb = 0.9f;
                    size = 0.9f;
                    break;
                case PrefixID.Bulky:
                    kb = 1.25f;
                    dmg = 1.1f;
                    size = 1.25f;
                    spd = 1.15f;
                    break;
                case PrefixID.Shameful:
                    kb = 0.8f;
                    dmg = 0.9f;
                    size = 1.42f;
                    break;
                case PrefixID.Heavy:
                    kb = 1.5f;
                    spd = 1.15f;
                    break;
                case PrefixID.Light:
                    kb = 0.5f;
                    spd = 0.8f;
                    break;
                case PrefixID.Legendary:
                    kb = 1.1f;
                    dmg = 1.1f;
                    crt = 4;
                    spd = 0.9f;
                    size = 1.1f;
                    break;
                case PrefixID.Legendary2: //can only apply to the Terrarian yoyo
                    kb = 1.17f;
                    dmg = 1.17f;
                    crt = 8;
                    break;
                #endregion

                #region Ranged Modifiers
                case PrefixID.Sighted:
                    dmg = 1.15f;
                    crt = 9;
                    spd = 1.1f;
                    break;
                case PrefixID.Rapid:
                    spd = 0.85f;
                    shtspd = 1.1f;
                    break;
                case PrefixID.Hasty:
                    spd = 0.8f;
                    shtspd = 1.2f;
                    crt = -5;
                    break;
                case PrefixID.Intimidating:
                    dmg = 1.1f;
                    kb = 1.15f;
                    shtspd = 1.5f;
                    break;
                case PrefixID.Deadly:
                    kb = 1.25f;
                    shtspd = 1.25f;
                    dmg = 1.05f;
                    spd = 0.9f;
                    crt = 3;
                    break;
                case PrefixID.Staunch:
                    kb = 1.5f;
                    dmg = 1.1f;
                    break;
                case PrefixID.Awful:
                    kb = 0.9f;
                    shtspd = 0.9f;
                    dmg = 0.85f;
                    break;
                case PrefixID.Lethargic:
                    spd = 1.15f;
                    shtspd = 0.9f;
                    break;
                case PrefixID.Awkward:
                    spd = 1.1f;
                    kb = 1.7f;
                    break;
                case PrefixID.Powerful:
                    spd = 1.1f;
                    dmg = 1.15f;
                    crt = 15;
                    break;
                case PrefixID.Frenzying:
                    spd = 0.72f;
                    dmg = 0.85f;
                    crt = -8;
                    break;
                case PrefixID.Unreal:
                    kb = 1.1f;
                    dmg = 1.1f;
                    crt = 5;
                    spd = 0.9f;
                    shtspd = 1.1f;
                    break;
                #endregion

                #region Magic modifiers
                case PrefixID.Mystic:
                    mcst = 0.8f;
                    dmg = 1.1f;
                    crt = 6;
                    break;
                case PrefixID.Adept:
                    mcst = 0.85f;
                    break;
                case PrefixID.Masterful:
                    mcst = 0.85f;
                    dmg = 1.15f;
                    kb = 1.05f;
                    break;
                case PrefixID.Inept:
                    mcst = 1.1f;
                    break;
                case PrefixID.Ignorant:
                    mcst = 1.2f;
                    dmg = 0.9f;
                    break;
                case PrefixID.Deranged:
                    kb = 0.9f;
                    dmg = 0.9f;
                    crt = 20;
                    break;
                case PrefixID.Intense:
                    mcst = 1.2f;
                    dmg = 1.15f;
                    spd = 0.9f;
                    break;
                case PrefixID.Taboo:
                    mcst = 1.1f;
                    kb = 1.1f;
                    spd = 0.9f;
                    break;
                case PrefixID.Celestial:
                    mcst = 0.9f;
                    kb = 1.1f;
                    spd = 1.1f;
                    dmg = 1.2f;
                    break;
                case PrefixID.Furious:
                    mcst = 1.4f;
                    dmg = 1.3f;
                    kb = 1.15f;
                    break;
                case PrefixID.Manic:
                    mcst = 1.2f;
                    dmg = 0.9f;
                    spd = 0.65f;
                    break;
                case PrefixID.Mythical:
                    kb = 1.1f;
                    dmg = 1.1f;
                    crt = 4;
                    spd = 0.9f;
                    mcst = 0.9f;
                    break;
                #endregion
			default:
			{
				ModPrefix modPrefix = PrefixLoader.GetPrefix(rolledPrefix);
				if (modPrefix != null)
				{
					if (!modPrefix.AllStatChangesHaveEffectOn(self))
					{
						return false;
					}
					modPrefix.SetStats(ref dmg, ref kb, ref spd, ref size, ref shtspd, ref mcst, ref crt);
				}
				break;
			}
			}
			if (dmg != 1f && Math.Round((float)self.damage * dmg) == (double)self.damage)
			{
				return false;
			}
			if (spd != 1f && Math.Round((float)self.useAnimation * spd) == (double)self.useAnimation)
			{
				return false;
			}
			if (mcst != 1f && Math.Round((float)self.mana * mcst) == (double)self.mana)
			{
				return false;
			}
			if (kb != 1f && self.knockBack == 0f)
			{
				return false;
			}
			return true;
        }

}