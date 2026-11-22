using Terraria;
using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Defiled Demon's moving breath: shared flame motion and sprite animation, with continuous Blight exposure.
    public class BlightBreath : DemonBreath
    {
        public const int BuildupIntervalTicks = 1;
        public const int BuildupPerInterval = 1;
        public const int ExtraLifetimeTicks = 30;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(BlightBreath));

        protected override DemonTier Tier => DemonTier.Defiled;

        protected override int PuffLifetimeTicks => LifetimeTicks + ExtraLifetimeTicks;

        public override void AI()
        {
            base.AI();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            ulong currentTick = Main.GameUpdateCount;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    continue;
                }

                BlightPlayer blightPlayer = player.GetModPlayer<BlightPlayer>();
                if (currentTick < blightPlayer.NextBlightBreathBuildupTick)
                {
                    continue;
                }

                blightPlayer.NextBlightBreathBuildupTick = currentTick + BuildupIntervalTicks;
                blightPlayer.ApplyBuildupAuthoritative(BuildupPerInterval, syncImmediately: false);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Buildup uses server-side overlap in AI, independent of hurt immunity and the number of touching puffs.
        }
    }
}
