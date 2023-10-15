using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class FlaskItems : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.FlaskofPoison
                || item.type == ItemID.FlaskofIchor
                )
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", Flasks.IchorFlaskDMG)));
            }
            if (item.type == ItemID.FlaskofGold)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", Flasks.GoldFlaskDMG)));
            }
            if (item.type == ItemID.FlaskofParty)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", Flasks.ConfettiFlaskDMG)));
            }
            if (item.type == ItemID.FlaskofFire)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", Flasks.FireFlaskDMG)));
            }
            if (item.type == ItemID.FlaskofFire)
            {
                tooltips.Insert(4, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.FireDamage")));
            }
            if (item.type == ItemID.FlaskofCursedFlames)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", Flasks.CursedFlaskDMG)));
            }
            if (item.type == ItemID.FlaskofVenom)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", Flasks.VenomFlaskDMGCrit, Buffs.Flasks.VenomFlaskDMGCrit)));
            }
            if (item.type == ItemID.FlaskofNanites)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", LangUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", Flasks.NanitesFlaskDMGCrit, Buffs.Flasks.NanitesFlaskDMGCrit)));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = Recipe.Create(ItemID.FlaskofFire, 1);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Deathweed, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 4);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();

        }
    }
}
