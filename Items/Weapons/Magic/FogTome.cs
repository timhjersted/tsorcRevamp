using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class FogTome : ModItem 
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fog Tome");
            Tooltip.SetDefault("A lost beginner's tome\n" +
                                "Casts Fog on the player, raising defense by 8 for 10 seconds" +
                                "\nDoes not stack with Barrier, Wall or Shield spells");

        }

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetDefaults()
        {
            item.stack = 1;
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.magic = true;
            item.noMelee = true;
            item.mana = 20;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.useAnimation = 20;
            item.value = 1400;

        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("HealingElixir"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }



        public override bool UseItem(Player player) 
        {
            player.AddBuff(ModContent.BuffType<Buffs.Fog>(), LegacyMode ? 600 : 900, false);
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (!LegacyMode)
            { //in revamp mode
                if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
                {
                    return false;
                }
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Wall>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            Player player = Main.LocalPlayer;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                tooltips.Add(new TooltipLine(mod, "FogTomeTip", "[c/00ff00:Revamped Mode:] Duration increased to 15 seconds"));
            }
        }
    }
}