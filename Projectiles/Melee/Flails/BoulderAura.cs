using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Flails
{

    public class BerserkerNightmareAura : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hide = true;
        }

        public override void AI()
        {
            int ballID = (int)Projectile.ai[0];
            Projectile ball = Main.projectile[ballID];

            if (!ball.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ball.Center;

            // Dust ring
            for (int i = 0; i < 32; i++)
            {
                float angle = MathHelper.TwoPi * i / 32f;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 60f;
                Dust d = Dust.NewDustPerfect(pos, 180, Vector2.Zero, 100, Color.Cyan, 1.25f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 2 * 60);
        }
    }

    public class HeavensTearAura : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 154;
            Projectile.height = 154;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hide = true;
        }

        public override void AI()
        {
            int ballID = (int)Projectile.ai[0];
            Projectile ball = Main.projectile[ballID];

            if (!ball.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ball.Center;

            // Dust ring
            for (int i = 0; i < 38; i++)
            {
                float angle = MathHelper.TwoPi / 36 * i;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 78f;
                Dust d = Dust.NewDustPerfect(pos, DustID.WaterCandle, Vector2.Zero, 100, Color.Blue, 1.5f);
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.MageNPCs.Contains(target.type) && tsorcRevamp.GhostNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.4f;
            }
        }
    }

    public class SunderingLightAura : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 5;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hide = true;
        }

        public override void AI()
        {
            int ballID = (int)Projectile.ai[0];
            Projectile ball = Main.projectile[ballID];

            if (!ball.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ball.Center;

            // Dust ring
            for (int i = 0; i < 48; i++)
            {
                float angle = MathHelper.TwoPi / 48 * i;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 115f;
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, Color.Yellow, 1.5f);
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.MageNPCs.Contains(target.type) && tsorcRevamp.GhostNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.5f;
            }
        }
    }
}