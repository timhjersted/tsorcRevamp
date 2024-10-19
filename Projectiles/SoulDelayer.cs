using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Projectiles
{
    class SoulDelayer : ModProjectile
    {
        public override void SetDefaults()
        {

            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.width = 112;
            Projectile.height = 112;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 1000;
            Projectile.alpha = 100;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.ai[1] != 0)
            {
                Projectile.timeLeft = (int)Projectile.ai[1];
                Projectile.ai[1] = 0;
            }

            Projectile.Center = Main.player[Projectile.owner].Center;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Item.NewItem(Projectile.GetSource_FromThis(), Main.player[Projectile.owner].Top, ModContent.ItemType<DarkSoul>(), (int)Projectile.ai[0]);
            }
        }
    }
}
