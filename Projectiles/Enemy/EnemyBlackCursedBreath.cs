using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyBlackCursedBreath : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cursed Breath");
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.alpha = 25;
            Projectile.aiStyle = 8; //8 with 96 AI Style works; with no AIType it rained down 5 streams like a firework, good if launched above player (23 is a orange flame)
            Projectile.timeLeft = 360;
            Projectile.friendly = false;
            Projectile.light = 0.8f;
            Projectile.penetrate = 4; //was 4, was causing curse buildup way too fast
            Projectile.tileCollide = false; 
            AIType = 96;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
        }
        public override void AI()
        {
            Projectile.rotation += 3f;

            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.DarkGray, 2.0f);
            Main.dust[dust].noGravity = true;
            for (int num36 = 0; num36 < 2; num36++)
            {
                int pink = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.Wraith, Projectile.velocity.X, Projectile.velocity.Y, Scale: 1.5f);
                Main.dust[pink].noGravity = true;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {         
                //Vanilla Debuffs cut in half to counter expert mode doubling them
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000, false);
                //target.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += 10;        
                target.AddBuff(33, 300, false); //weak          
        }
    }
}