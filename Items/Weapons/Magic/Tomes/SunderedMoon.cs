using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class SunderedMoon : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 130;
            Item.autoReuse = true;
            Item.knockBack = 4;
            Item.UseSound = SoundID.Item29 with { Volume = 0.4f };
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.shootSpeed = 16;
            Item.crit = 15;
            Item.mana = 35;
            Item.noMelee = true;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.SunderedMoon>();
        }

        //Make it start offset from the player
        float rotation = MathHelper.TwoPi / 12f;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                rotation += MathHelper.TwoPi / 6f;
                position += new Vector2(80, 0).RotatedBy(rotation);
            }
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.collapseDelay = 30;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                for (int i = 0; i < 4; i++)
                {
                    float randomRotation = Main.rand.NextFloat(0, MathHelper.TwoPi); 
                    Vector2 spawnPosition = player.Center + new Vector2(80, 0).RotatedBy(randomRotation);
                    Projectile.NewProjectile(source, spawnPosition, velocity, type, damage, knockback, player.whoAmI);

                    for (int j = 0; j < 10; j++) 
                    {
                        float angle = (j * MathHelper.TwoPi / 6f); 
                        Vector2 dustOffset = new Vector2(8f, 0).RotatedBy(angle); 
                        Vector2 dustPosition = spawnPosition + dustOffset;
                        Vector2 dustVelocity = dustOffset * 0.2f; 
                        Dust dust = Dust.NewDustPerfect(dustPosition, 180, dustVelocity, 100, default, 1.4f); 
                        dust.noGravity = true;
                    }
                }
            }
            return false; 
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
    }
}
