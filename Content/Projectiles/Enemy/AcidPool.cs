using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    ///<summary>
    ///Lingering 3-tile-wide acid puddle left by VenomSpit impacts. Invisible projectile sold entirely by dust,
    ///borrowing the Abyss ground-mist approach (tsorcRevampPlayerUpdateLoops.SpawnAbyssMist: vanilla dust spawned
    ///just above the surface, no custom drawing) but much thicker: a dense boiling layer in the tile row sitting
    ///on the ground, and a sparser dissipating layer one tile higher so it reads as surface acid with rising gas.
    ///</summary>
    class AcidPool : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public const int PoolWidth = 48;  //3 tiles
        public const int PoolHeight = 16;
        public const int PoolLifetime = 3 * 60;

        //ai[0] = custom lifetime in ticks (0 = default PoolLifetime). AcidBody trails use 6s pools.
        int Lifetime => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : PoolLifetime;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PoolWidth;
            Projectile.height = PoolHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PoolLifetime;
            Projectile.light = 0.3f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ)
            {
                return; //all of this is purely cosmetic dust -- nothing to do server-side
            }

            // Anchor dust to the ground tile's top edge (Projectile.Bottom.Y) rather than the top of the 16px hitbox,
            // letting motes sit right on the floor and a few pixels into the ground tile as they boil upward.
            float surfaceY = Projectile.position.Y + PoolHeight;
            float remaining = Projectile.timeLeft / (float)Lifetime;

            // Midground boiling slick: hugs the floor seam (-4px above to +5px inside the ground tile) and drifts up.
            if (remaining > 0.2f || Main.rand.NextBool(2))
            {
                Vector2 slickPos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(PoolWidth),
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

            // Foreground venom bubbles popping up from the ground edge.
            if (Main.rand.NextBool(3))
            {
                Vector2 bubblePos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(PoolWidth),
                    surfaceY + Main.rand.NextFloat(-2f, 4f));
                Vector2 bubbleVel = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), -Main.rand.NextFloat(0.9f, 1.7f));
                Dust bubble = Dust.NewDustPerfect(
                    bubblePos,
                    DustID.Venom,
                    bubbleVel,
                    45,
                    default,
                    Main.rand.NextFloat(0.85f, 1.25f));
                bubble.noGravity = true;
            }

            // Background rising vapor: spawns just above the floor and billows via fadeIn > spawn scale as it rises.
            if (Main.rand.NextFloat() < 0.22f * remaining)
            {
                Vector2 gasPos = new Vector2(
                    Projectile.position.X + Main.rand.NextFloat(PoolWidth),
                    surfaceY - Main.rand.NextFloat(2f, 8f));
                Vector2 gasVel = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -Main.rand.NextFloat(0.9f, 1.5f));
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

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Keep Acid Venom at 5s real duration across Classic, Expert (2x), and Master (2.5x).
            float debuffMultiplier = Main.expertMode ? Main.GameModeInfo.DebuffTimeMultiplier : 1f;
            int venomTicks = System.Math.Max(1, (int)System.Math.Round(5f * 60f / debuffMultiplier));
            target.AddBuff(BuffID.Venom, venomTicks);
        }
    }
}
