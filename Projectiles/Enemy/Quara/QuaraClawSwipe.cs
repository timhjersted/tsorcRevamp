using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///The Pincer Leap's claw swipe: a short-lived melee arc in front of the crab that sweeps top-to-bottom,
    ///drawing a white crescent "swoosh" of dust for effect. A hit applies a brief Doused coating. ai[0] =
    ///facing (1/-1), ai[1] = owner NPC index (it rides the crab's front). Purely a hitbox + dust VFX; a
    ///sprite-based slash can be layered in later.
    ///</summary>
    class QuaraClawSwipe : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int Life = 16;

        int Facing => Projectile.ai[0] >= 0f ? 1 : -1;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 88;
            Projectile.height = 76;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            int owner = (int)Projectile.ai[1];
            if (owner < 0 || owner >= Main.maxNPCs || !Main.npc[owner].active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 pivot = Main.npc[owner].Center;
            Projectile.Center = pivot + new Vector2(Facing * 34f, -6f);

            float t = 1f - Projectile.timeLeft / (float)Life; //0 -> 1 over its life
            float ang = MathHelper.Lerp(-0.95f, 0.95f, t) * Facing; //sweep from up to down

            //White crescent swoosh
            for (int i = 0; i < 6; i++)
            {
                float r = 28f + i * 9f;
                Vector2 p = pivot + new Vector2(Facing * r, 0f).RotatedBy(ang);
                int d = Dust.NewDust(p, 2, 2, DustID.SilverFlame, 0f, 0f, 0, Color.White, 1.1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = new Vector2(Facing * 0.6f, 0f).RotatedBy(ang);
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.3f, 0.3f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Doused>(), 3 * 60);
        }
    }
}
