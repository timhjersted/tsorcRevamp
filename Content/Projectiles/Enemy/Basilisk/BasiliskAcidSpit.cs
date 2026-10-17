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
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 24;
            Projectile.width = 24;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 80;
        }

        public override void AI()
        {
            Projectile.rotation += 1f;

            if (Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0, 0, 50, Color.Green, 2.0f);
                trail.noGravity = false;
            }

            // Curse telegraph: after Skeletron the spit builds curse, so pink motes ride the trail.
            if (NPC.downedBoss3 && Main.rand.NextBool(3))
            {
                Dust curseMote = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 80, default, 1.4f);
                curseMote.noGravity = true;
                curseMote.velocity *= 0.4f;
            }

            Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.1f);

            // Accelerates slightly while slow (|v| <= 4 on both axes), so lobbed spits don't hang in the air.
            bool isSlow = Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4;
            if (isSlow)
            {
                float acceleration = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity *= acceleration;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.04f }, Projectile.Center);

            Dust splash = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0, 0, 50, Color.Green, 2.0f);
            splash.noGravity = false;
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 71, 0.3f, 0.3f, 200, default, 1f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 71, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 71, 0.2f, 0.2f, 200, default, 2f);

            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
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
