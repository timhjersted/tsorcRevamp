using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using System;

namespace tsorcRevamp.Projectiles.Melee
{
    class MLGSCrescent : ModProjectile
    {
        public static int MaxTicksBeforeSpeedIncrease => 10;
        public int ticksBeforeSpeedIncrease;
        public override void SetStaticDefaults()
        {

        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.penetrate = 5;
            Projectile.friendly = true;
            Projectile.scale = 1.5f;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 70; // Expires a bit more than a screen width away from the player
            Projectile.extraUpdates = 1;
            ticksBeforeSpeedIncrease = 0;
        }

        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Color color;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.velocity.X > 0)
            {
                spriteEffects = SpriteEffects.FlipVertically;
            }
            if (Main.dayTime)
            {
                color = lightColor;
            }
            else
            {
                color = Color.White;
            }

            if (Projectile.ai[0] > 8)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, texture.Bounds, color, Projectile.rotation, texture.Size() / 2f, Projectile.scale, spriteEffects, 0);
            }

            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Projectile.DamageType = MoonlightGreatsword.GetDamageType(Main.player[Projectile.owner]);

            if (!Main.dayTime)
                modifiers.SourceDamage.Base *= 1f + (MoonlightGreatsword.NightDamageIncrease / 100f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, 0, 0, 70, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        // Don't spawn directly on player
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.position += Projectile.velocity / 4f;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            ticksBeforeSpeedIncrease++;
            if (ticksBeforeSpeedIncrease == MaxTicksBeforeSpeedIncrease)
            {
                Projectile.velocity *= 1.2f;
                ticksBeforeSpeedIncrease = 0;
            }

            if (Projectile.ai[0] > 8)
            {

                Lighting.AddLight(Projectile.position, 0.0452f, 0.21f, 0.1f);

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default, 1f);
                    Main.dust[dust].noGravity = true;

                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 110, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
            }

        }
    }
}
