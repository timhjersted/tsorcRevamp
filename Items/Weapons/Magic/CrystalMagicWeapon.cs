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
                                "\nLasts 20 seconds, 60 second cooldown");

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

            player.AddBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>(), 1200); //20s
            player.AddBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>(), modPlayer.ManaCloak ? 1800 : 3600);

            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicImbueCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>()) || player.HasBuff(BuffID.WeaponImbueFire))
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