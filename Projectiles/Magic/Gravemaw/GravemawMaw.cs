using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Gravemaw
{
    ///<summary>
    ///Hungering Maw: a channeled INHALE cone in front of the player (the boss's signature, in your
    ///hands). Drags small enemies toward you, damages everything in the cone rapidly, and trickles a
    ///little life back on hit (soul eating, capped). Held while the button is down; costs mana over time.
    ///No sprite — drawn as inhaling dust. velocity carries the aim unit-vector (synced from the owner).
    ///</summary>
    class GravemawMaw : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float Range = 230f;
        const float ConeHalfAngle = 0.62f; // ~35°
        const int ManaInterval = 12;
        const int ManaCost = 3;

        bool UsesRightClick => Projectile.ai[0] == 1f;
        float HealCooldown { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = (int)(Range * 2f);
            Projectile.height = (int)(Range * 2f);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => true;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            bool channeling = UsesRightClick ? player.controlUseTile : player.channel;
            if (!player.active || player.dead || !channeling || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 6;
            Projectile.Center = player.Center;

            // Owner drives the aim; store as a unit vector in velocity so clients read it.
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 aim = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                if (Vector2.Distance(aim, Projectile.velocity) > 0.02f) Projectile.netUpdate = true;
                Projectile.velocity = aim;
                player.ChangeDir(aim.X > 0f ? 1 : -1);
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;

                // Mana upkeep.
                if (Projectile.localAI[0] % ManaInterval == 0 && !player.CheckMana(ManaCost, true))
                {
                    Projectile.Kill();
                    return;
                }
            }
            Projectile.localAI[0]++;
            if (HealCooldown > 0f) HealCooldown--;

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 mouth = player.Center + dir * 18f;

            // Pull small enemies toward the mouth.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this) || npc.boss || npc.knockBackResist <= 0f) continue;
                Vector2 to = mouth - npc.Center;
                float dist = to.Length();
                if (dist > Range || dist < 12f) continue;
                if (Vector2.Dot(to.SafeNormalize(Vector2.Zero), -dir) < Math.Cos(ConeHalfAngle)) continue; // in the cone
                npc.velocity += to.SafeNormalize(Vector2.Zero) * 0.30f * npc.knockBackResist;
            }

            // Inhaling dust: motes streaming from the cone edge into the mouth.
            for (int i = 0; i < 5; i++)
            {
                float a = dir.ToRotation() + Main.rand.NextFloat(-ConeHalfAngle, ConeHalfAngle);
                Vector2 from = player.Center + a.ToRotationVector2() * Main.rand.NextFloat(80f, Range);
                int d = Dust.NewDust(from, 4, 4, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 120, default, 1.1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (mouth - from) * 0.08f;
            }
            Lighting.AddLight(mouth, 0.5f, 0.12f, 0.65f);
            if (Projectile.localAI[0] % 16 == 0) SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = -0.4f }, player.Center);
        }

        // Restrict the big broadphase box to the actual cone.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 target = targetHitbox.Center.ToVector2();
            Vector2 to = target - Projectile.Center;
            if (to.Length() > Range) return false;
            return Vector2.Dot(to.SafeNormalize(Vector2.Zero), dir) >= Math.Cos(ConeHalfAngle);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Soul-eating lifesteal: a small, rate-capped trickle.
            if (Main.myPlayer == Projectile.owner && HealCooldown <= 0f && Main.player[Projectile.owner].statLife < Main.player[Projectile.owner].statLifeMax2)
            {
                HealCooldown = 18f;
                Player player = Main.player[Projectile.owner];
                player.statLife = Math.Min(player.statLifeMax2, player.statLife + 1);
                player.HealEffect(1);
            }
        }
    }
}
