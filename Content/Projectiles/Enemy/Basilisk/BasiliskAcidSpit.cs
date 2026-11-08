using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Basilisk
{
    /// <summary>
    /// Basilisk-only copy of EnemyBioSpitBall (which other enemies still share). Poison spit; once Skeletron is
    /// down it also builds curse, telegraphed by pink dust mixed into the green trail.
    /// </summary>
    class BasiliskAcidSpit : ModProjectile
    {
        public const int BlackoutTicks = 2 * 60;
        public const int PoisonedTicks = 2 * 60;
        public const int BleedingTicks = 150;
        public const int CurseBuildupPerHit = 10;
        public const int CurseBuildupMarkerTicks = 300 * 60;

        // Intentional share: same glob sprite as the generic spit.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyBioSpitBall));

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.height = 24;
            Projectile.width = 24;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            // ai[0]: 0 straight spit, 1 solved lob, 2 desperate ballistic spray.
            // All peers integrate the same gravity; cosmetic randomness never alters the trajectory.
            if (Projectile.ai[0] != 0f)
            {
                Projectile.velocity.Y += 0.25f;
                if (Projectile.ai[0] == 1f && Projectile.timeLeft > 90)
                {
                    Projectile.timeLeft = 90;
                    // The former Drakin lob was a 10px, piercing projectile. Keep that contact
                    // geometry and penetration while giving its new acid sprite the same core size.
                    Projectile.penetrate = -1;
                    Projectile.Resize(10, 10);
                    Projectile.scale = 10f / 24f;
                    Projectile.DamageType = DamageClass.Default;
                }
            }

            Projectile.rotation += 0.15f;
            if (Projectile.timeLeft <= 12)
            {
                Projectile.alpha = 255 - Projectile.timeLeft * 255 / 12;
            }

            if (Main.dedServ)
            {
                return;
            }

            if (Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0, 0, 50, Color.Green, 2.0f);
                trail.noGravity = false;
            }

            // Curse telegraph: after Skeletron the spit builds curse, so pink motes ride the trail.
            if (Projectile.ai[0] != 1f && NPC.downedBoss3 && Main.rand.NextBool(3))
            {
                Dust curseMote = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 80, default, 1.4f);
                curseMote.noGravity = true;
                curseMote.velocity *= 0.4f;
            }

            Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.1f);

        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft <= 12)
            {
                return false;
            }

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.12f }, Projectile.Center);
            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 3f);
                Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch, velocity, 80, Color.GreenYellow, Main.rand.NextFloat(0.7f, 1.3f));
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[0] == 1f)
            {
                // Lobs formerly used DrakinShot: retain its basilisk-only Venom,
                // rather than inheriting the direct spit's blackout/poison/curse package.
                target.AddBuff(BuffID.Venom, tsorcGlobalProjectile.BasiliskLobAcidVenomTicks, false);
                return;
            }

            target.AddBuff(BuffID.Blackout, BlackoutTicks, false);
            target.AddBuff(BuffID.Poisoned, PoisonedTicks, false);

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.EaterofWorldsHead)))
            {
                target.AddBuff(BuffID.Bleeding, BleedingTicks, false);
            }

            if (NPC.downedBoss3)
            {
                CurseBuildup.ApplyExplicit(target, CurseBuildupPerHit, CurseBuildupMarkerTicks);
            }
        }
    }
}
