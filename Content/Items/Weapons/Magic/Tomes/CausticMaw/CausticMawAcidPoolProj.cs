using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Tomes.CausticMaw
{
    /// <summary>
    /// Friendly boiling acid puddle created by Caustic Maw splashes.
    /// Uses the same ground-hugging 3-layer dust presentation as the boss's AcidPool.
    /// ai[0] carries reinforcement perk tier: 0 = base, 1 = +3 longer/faster Acid Venom, 2 = +6 wider pool.
    /// </summary>
    public class CausticMawAcidPoolProj : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public const int PoolWidth = 52;
        public const int PoolHeight = 16;
        public const int PoolLifetime = CausticMawItem.PoolDurationSeconds * 60;

        private int PerkTier => (int)Projectile.ai[0];
        private int ActiveLifetime => PerkTier >= 1 ? CausticMawItem.Tier3PoolDurationTicks : PoolLifetime;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = PoolWidth;
            Projectile.height = PoolHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PoolLifetime;
            Projectile.light = 0.35f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (PerkTier >= 1)
            {
                Projectile.timeLeft = CausticMawItem.Tier3PoolDurationTicks;
                Projectile.localNPCHitCooldown = CausticMawItem.Tier3PoolHitCooldown;
            }

            if (PerkTier >= 2)
            {
                int widthGain = CausticMawItem.Tier6PoolWidth - PoolWidth;
                Projectile.position.X -= widthGain * 0.5f;
                Projectile.width = CausticMawItem.Tier6PoolWidth;
            }
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ)
            {
                return;
            }

            // Anchor dust to the tile seam (Projectile.Bottom.Y) so acid boils out of the ground surface.
            float surfaceY = Projectile.position.Y + PoolHeight;
            float remaining = Projectile.timeLeft / (float)ActiveLifetime;
            float activeWidth = Projectile.width;

            if (remaining > 0.2f || Main.rand.NextBool(2))
            {
                Vector2 slickPos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(activeWidth),
                    surfaceY + Main.rand.NextFloat(-4f, 5f));
                Vector2 slickVel = new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), -Main.rand.NextFloat(0.55f, 1.15f));
                Dust slick = Dust.NewDustPerfect(
                    slickPos,
                    DustID.AncientLight,
                    slickVel,
                    55,
                    new Color(175, 65, 255),
                    Main.rand.NextFloat(0.95f, 1.3f));
                slick.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 bubblePos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(activeWidth),
                    surfaceY + Main.rand.NextFloat(-2f, 4f));
                Vector2 bubbleVel = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), -Main.rand.NextFloat(0.9f, 1.65f));
                Dust bubble = Dust.NewDustPerfect(
                    bubblePos,
                    DustID.Venom,
                    bubbleVel,
                    45,
                    default,
                    Main.rand.NextFloat(0.85f, 1.2f));
                bubble.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.22f * remaining)
            {
                Vector2 gasPos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(activeWidth),
                    surfaceY - Main.rand.NextFloat(2f, 8f));
                Vector2 gasVel = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -Main.rand.NextFloat(0.9f, 1.45f));
                Dust gas = Dust.NewDustPerfect(
                    gasPos,
                    DustID.Smoke,
                    gasVel,
                    150,
                    new Color(110, 50, 155),
                    Main.rand.NextFloat(0.55f, 0.75f));
                gas.noGravity = true;
                gas.noLight = true;
                gas.fadeIn = Main.rand.NextFloat(1.1f, 1.35f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (PerkTier >= 1)
            {
                target.AddBuff(BuffID.Venom, 3 * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 3 * 60);
            }
        }
    }
}
