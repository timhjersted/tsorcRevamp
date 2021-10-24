using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class MagicWeapon : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Weapon");
            Tooltip.SetDefault("Imbues melee weapons with magic,\n" +
                                "allowing for weak magic damage scaling" +
                                "\nLasts 30 seconds, 60 second cooldown" +
                                "\nNot compatible with other weapon imbues");

        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.Blue;
            item.magic = true;
            item.noMelee = true;
            item.mana = 60;
            item.UseSound = SoundID.Item82;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useTime = 34;
            item.useAnimation = 34;
            item.value = 15000;

        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.ManaCrystal, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            player.AddBuff(ModContent.BuffType<Buffs.MagicWeapon>(), 1800); //30s

            if (!modPlayer.DarkmoonCloak)
            {
                player.AddBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>(), 3600);
            }

            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>()) 
                || player.HasBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>()) 
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