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
        public override void SetDefaults() {
            Item.stack = 1;
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 150;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(Mod.GetItem("WhiteTitanite"), 6);
            recipe.AddIngredient(Mod.GetItem("RedTitanite"));
            recipe.AddIngredient(Mod.GetItem("CursedSoul"), 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool? UseItem(Player player) {
            player.AddBuff(ModContent.BuffType<Buffs.Shield>(), 1800, false);
            return true;
        }
        public override bool CanUseItem(Player player) {
            if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
            {
                return false;
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