using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class GreatMagicWeapon : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Magic Weapon");
            Tooltip.SetDefault("Imbues melee weapons with powerful magic,\n" +
                                "allowing for greater magic damage scaling" +
                                "\nLasts 25 seconds, 60 second cooldown");

        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.magic = true;
            item.noMelee = true;
            item.mana = 120;
            item.UseSound = SoundID.Item82;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useTime = 34;
            item.useAnimation = 34;
            item.value = 100000;

        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("MagicWeapon"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddIngredient(ItemID.SoulofNight, 3);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>(), 1500); //25s
            player.AddBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>(), modPlayer.ManaCloak ? 1800 : 3600);

            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>()) || player.HasBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>()) || player.HasBuff(BuffID.WeaponImbueFire))
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