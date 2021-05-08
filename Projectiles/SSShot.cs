using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SSShot : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 12;
            projectile.height = 12;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 30;
            projectile.scale = 0.85f;
            projectile.extraUpdates = 1;
        }
        public override void AI()
        {

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2; // projectile faces sprite right

            if (projectile.owner == Main.myPlayer && projectile.timeLeft == 28)
            {
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 63, projectile.velocity.X * 0, projectile.velocity.Y * 0, 70, default(Color), 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 6f);
                }
            }
            if (Main.rand.Next(10) == 0)
            {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 245, projectile.velocity.X * -0.2f, projectile.velocity.Y * -0.2f, 70, default(Color), .3f);
                    Main.dust[dust].noGravity = true;
            }
            {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 91, projectile.velocity.X * -0.2f, projectile.velocity.Y * -0.2f, 70, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
            }
            
            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 6)
            {
                projectile.alpha += 28;

                if (projectile.alpha > 225)
                {
                    projectile.alpha = 225;
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if ((target.type == NPCID.SandsharkCorrupt) | (target.type == NPCID.SandsharkCrimson) | (target.type == NPCID.EaterofSouls) | (target.type == NPCID.BigEater) | (target.type == NPCID.LittleEater) | (target.type == NPCID.CorruptGoldfish) | (target.type == NPCID.DevourerBody) | (target.type == NPCID.DevourerHead) | (target.type == NPCID.DevourerTail) | (target.type == NPCID.EaterofWorldsBody) | (target.type == NPCID.EaterofWorldsHead) | (target.type == NPCID.EaterofWorldsTail) | (target.type == NPCID.Corruptor) | (target.type == NPCID.CorruptSlime) | (target.type == NPCID.Slimeling) | (target.type == NPCID.Slimer) | (target.type == NPCID.Slimer2) | (target.type == NPCID.SeekerBody) | (target.type == NPCID.SeekerHead) | (target.type == NPCID.SeekerTail) | (target.type == NPCID.DarkMummy) | (target.type == NPCID.CorruptPenguin) | (target.type == NPCID.CursedHammer) | (target.type == NPCID.Clinger) | (target.type == NPCID.BigMimicCorruption) | (target.type == NPCID.DesertGhoulCorruption) | (target.type == NPCID.DesertLamiaDark) | (target.type == NPCID.CrimsonGoldfish) | (target.type == NPCID.CrimsonPenguin) | (target.type == NPCID.BloodCrawler) | (target.type == NPCID.BloodCrawlerWall) | (target.type == NPCID.FaceMonster) | (target.type == NPCID.Crimera) | (target.type == NPCID.BigCrimera) | (target.type == NPCID.LittleCrimera) | (target.type == NPCID.Creeper) | (target.type == NPCID.BrainofCthulhu) | (target.type == NPCID.Herpling) | (target.type == NPCID.Crimslime) | (target.type == NPCID.BigCrimslime) | (target.type == NPCID.LittleCrimslime) | (target.type == NPCID.BloodJelly) | (target.type == NPCID.BloodFeeder) | (target.type == NPCID.CrimsonAxe) | (target.type == NPCID.IchorSticker) | (target.type == NPCID.FloatyGross) | (target.type == NPCID.BigMimicCrimson) | (target.type == NPCID.DesertGhoulCrimson) | (target.type == NPCID.DesertDjinn))
            {
                damage += 10;
            }
        }
    }
}
