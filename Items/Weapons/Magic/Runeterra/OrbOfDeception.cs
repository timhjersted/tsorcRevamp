using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Melee;
using System.Collections.Generic;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfDeception : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Deception");
            Tooltip.SetDefault("Throws a magical Orb which will return to you after a certain distance" +
                "\nCasts homing flames while the Orb is sent out which restore half their mana cost on-hit" +
                "\nThe Orb deals more damage on the way back" +
                "\nGathers stacks of Essence Thief on Orb hits and enemy kills, doubled on crits" +
                "\nUpon reaching 9 stacks, the next Orb cast will consume all stacks, heal you and deal double damage" +
                "\nHeal scales based on maximum mana and magic damage");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 20;
            Item.mana = 20;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 20f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfDeceptionOrb>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().OrbExists)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionFlame>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionOrb>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief >= 9)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionOrbFilled>();
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowOrb);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
