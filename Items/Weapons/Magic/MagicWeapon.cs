using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class MagicWeapon : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Weapon");
            Tooltip.SetDefault("Imbues melee weapons with magic,\n" +
                                "allowing for weak magic damage scaling" +
                                "\nAdds 50% of bonus magic damage" +
                                "\nPlus 1 damage for every 60 max mana over 100" +
                                "\nLasts 1 minute, 2 minute cooldown" +
                                "\nNot compatible with other weapon imbues");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 60;
            Item.UseSound = SoundID.Item82;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.value = 15000;

        }

        public override void AddRecipes() //recipe/progression subject to change
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.ManaCrystal, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            player.AddBuff(ModContent.BuffType<Buffs.MagicWeapon>(), 3600); //60s

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