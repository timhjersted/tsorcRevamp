using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class AbyssBreath : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 28;
            Projectile.width = 28;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 10;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.width = 28;
            Projectile.alpha = 255;
        }
        public override bool PreAI()
        {
            Projectile.ai[1]++;

            if (Projectile.ai[1] > 3)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.PinkTorch, Projectile.velocity.X * 2f, Projectile.velocity.Y * 2f, 70, default(Color), Main.rand.NextFloat(2.8f, 4.3f));
                Main.dust[dust].noGravity = true;
            }

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())) && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())))
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            }

            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
        }

    }
}
