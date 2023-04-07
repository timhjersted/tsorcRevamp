using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Melee;
using System.Collections.Generic;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Projectiles.Ranged.Runeterra;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfFlame : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Flame");
            Tooltip.SetDefault("Throws a fiery Orb which will return to you after a certain distance" +
                "\nCasts homing flames while the Orb is sent out which restore half their mana cost on-hit" +
                "\nThe Orb deals more damage on the way back" +
                "\nGathers stacks of Essence Thief on Orb hits and enemy kills, doubled on crits" +
                "\nUpon reaching 9 stacks, the next Orb cast will consume all stacks, heal you and deal double damage" +
                "\nHeal scales based on maximum mana and magic damage" +
                "\nRight click to cast a fireball which sunders enemies, increasing their vulnerability to magic damage");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 60;
            Item.mana = 40;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 20f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfFlameOrb>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().OrbExists)
            {
                type = ModContent.ProjectileType<OrbOfFlameFlame>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfFlameOrb>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief >= 9)
            {
                type = ModContent.ProjectileType<OrbOfFlameOrbFilled>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrbOfFlameFireball>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<OrbOfFlameFireballCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<OrbOfFlameFireballCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfDeception>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
