using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Tools
{
    public class GreatMagicWeapon : ModItem
    {
        public static int Duration = 60;
        public static int Cooldown = 120;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(tsorcGlobalItem.BonusDamage2, Duration, Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.rare = ItemRarityID.LightRed;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 120;
            Item.UseSound = SoundID.Item82;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.value = 100000;
        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<MagicWeapon>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddIngredient(ItemID.SoulofNight, 3);
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>(), Duration * 60);

            if (!modPlayer.DarkmoonCloak)
            {
                player.AddBuff(ModContent.BuffType<MagicImbueCooldown>(), Cooldown * 60);
            }

            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<MagicImbueCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>())
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