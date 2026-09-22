using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal class BasiliskLeechTongueTelegraph : ModProjectile
    {
        public const int Duration = 45;
        private static readonly Vector2 TelegraphHeightOffset = new(0f, -10f);
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/BasiliskLeechTongue";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.timeLeft = Duration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = BasiliskLeechTongue.GetMouthPosition(Main.npc[ownerIndex]) + TelegraphHeightOffset;
            float progress = MathHelper.Clamp((Duration - Projectile.timeLeft) / (float)(Duration - 1), 0f, 1f);
            Projectile.scale = MathHelper.Lerp(0.5f, 1f, progress);
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }
    }
}
