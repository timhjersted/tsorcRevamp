using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class ShieldTome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Shield Tome");
            Tooltip.SetDefault("A lost legendary tome\n" +
                                "Casts Shield on the player, raising defense by 62 for 30 seconds\n" +
                                "Does not stack with Fog, Barrier or Wall spells");
        }

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetDefaults() {
            item.stack = 1;
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.Yellow;
            item.magic = true;
            item.noMelee = true;
            item.mana = 150;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.useAnimation = 20;
            item.value = 5000000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 6);
            recipe.AddIngredient(mod.GetItem("RedTitanite"));
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            player.AddBuff(ModContent.BuffType<Buffs.Shield>(), 1800, false);
            return true;
        }
        public override bool CanUseItem(Player player) {
            if (!LegacyMode)
            { //in revamp mode
                if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
                {
                    return false;
                }
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Wall>())) {
                return false;
            }
            else {
                return true;
            }
        }
    }
}