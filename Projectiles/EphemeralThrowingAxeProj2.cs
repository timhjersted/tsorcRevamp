using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.WyvernMage;
using tsorcRevamp.NPCs.Bosses.Okiku;
using tsorcRevamp.NPCs.Bosses.Okiku.FirstForm;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm;
using tsorcRevamp.NPCs.Bosses.Okiku.FinalForm;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Projectiles
{
    class EphemeralThrowingAxeProj2 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.width = 38;
            Projectile.height = 72;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.MageNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.2f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int spectreWrath = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ProjectileID.SpectreWrath,
                    Projectile.damage / 2,
                    Projectile.knockBack / 2,
                    Projectile.owner
                );

            if (tsorcRevamp.MageNPCs.Contains(target.type))
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        Vector2.Zero,
                        ProjectileID.SpectreWrath,
                        Projectile.damage / 2,
                        Projectile.knockBack / 2,
                        Projectile.owner
                    );
                }
            }
        }

        public override void AI()
        {
            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 91, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 91;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            Projectile.active = false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/EphemeralThrowingAxeProj2_Glowmask").Value;
            Vector2 origin = new Vector2(glowTexture.Width / 2f, glowTexture.Height / 2f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 3; i++) 
            {
                Vector2 offset = Projectile.velocity * -i * 0.5f; 
                float alpha = 0.4f * (1f - (i / 6f)); 
                Main.EntitySpriteDraw(
                    glowTexture,
                    drawPosition + offset, 
                    null,
                    Color.White * alpha, 
                    Projectile.rotation,
                    origin,
                    1f, 
                    SpriteEffects.None,
                    0
                );
            }

            Main.EntitySpriteDraw(
                glowTexture,
                drawPosition,
                null,
                Color.White * 0.4f, 
                Projectile.rotation,
                origin,
                1f, 
                SpriteEffects.None,
                0
            );
        }
    }
}