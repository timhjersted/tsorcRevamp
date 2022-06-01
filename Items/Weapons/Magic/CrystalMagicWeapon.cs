using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class CrystalMagicWeapon : ModItem
    {
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
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 240;
            Item.UseSound = SoundID.Item82;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.value = PriceByRarity.Cyan_9;

        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("GreatMagicWeapon").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 50000);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type); //lol idek how early you can get these but will do for now
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
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