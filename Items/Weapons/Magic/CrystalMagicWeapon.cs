using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class CrystalMagicWeapon : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal Magic Weapon");
            Tooltip.SetDefault("Imbues melee weapons with crystalline magic,\n" +
                                "allowing for incredible magic damage scaling" +
                                "\nAdds 100% of bonus magic damage" +
                                "\nPlus 1 damage for every 20 max mana over 100" +
                                "\nLasts 1 minute, 2 minute cooldown" +
                                "\nNot compatible with other weapon imbues");

        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.Cyan;
            item.magic = true;
            item.noMelee = true;
            item.mana = 240;
            item.UseSound = SoundID.Item82;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useTime = 34;
            item.useAnimation = 34;
            item.value = 30000;

        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("GreatMagicWeapon"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 50000);
            recipe.AddIngredient(mod.GetItem("GuardianSoul")); //lol idek how early you can get these but will do for now
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            player.AddBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>(), 3600); //60s

            if (!modPlayer.DarkmoonCloak)
            {
                player.AddBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>(), 7200);
            }

            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>()) 
                || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>())
                || player.meleeEnchant == 1
                || player.meleeEnchant == 2
                || player.meleeEnchant == 3
                || player.meleeEnchant == 4
                || player.meleeEnchant == 5
                || player.meleeEnchant == 6
                || player.meleeEnchant == 7
                || player.meleeEnchant == 8)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}