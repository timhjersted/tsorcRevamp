using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FrozenTear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Frozen Orb");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.timeLeft = 150;
            Projectile.light = .3f;
            Main.projFrames[Projectile.type] = 4;
            Projectile.coldDamage = true;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44;
            return true;
        }

        public override void AI()
        {
            Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 250, default(Color), 1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int expertMultiplier = 1;
            if (Main.expertMode) expertMultiplier = 2;
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<DarkInferno>(), 600, false);
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<DarkInferno>(), 30, false);
            Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600 / expertMultiplier, false);
        }
    }
}