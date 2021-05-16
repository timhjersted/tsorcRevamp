using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            if (Main.rand.Next(6) == 0)
            {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 245, projectile.velocity.X * -0.2f, projectile.velocity.Y * -0.2f, 70, default(Color), .5f);
                    Main.dust[dust].noGravity = true;
            }
            {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 91, projectile.velocity.X * -0.2f, projectile.velocity.Y * -0.2f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
            }

            if (projectile.owner == Main.myPlayer && projectile.timeLeft == 3)
            {
                for (int d = 0; d < 10; d++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 91, projectile.velocity.X * 0.5f, projectile.velocity.Y * 0.5f, 30, default(Color), 0.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame, 14, 38), Color.White, projectile.rotation, new Vector2(7, 0), projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 91, projectile.velocity.X, projectile.velocity.Y, 30, default(Color), 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 91, projectile.velocity.X, projectile.velocity.Y, 30, default(Color), 0.5f);
                Main.dust[dust].noGravity = true;
            }

            return true;
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if ((target.type == NPCID.SandsharkCorrupt) | (target.type == NPCID.SandsharkCrimson) | (target.type == NPCID.EaterofSouls) | (target.type == NPCID.BigEater) | (target.type == NPCID.LittleEater) | (target.type == NPCID.CorruptGoldfish) | (target.type == NPCID.DevourerBody) | (target.type == NPCID.DevourerHead) | (target.type == NPCID.DevourerTail) | (target.type == NPCID.EaterofWorldsBody) | (target.type == NPCID.EaterofWorldsHead) | (target.type == NPCID.EaterofWorldsTail) | (target.type == NPCID.Corruptor) | (target.type == NPCID.CorruptSlime) | (target.type == NPCID.Slimeling) | (target.type == NPCID.Slimer) | (target.type == NPCID.Slimer2) | (target.type == NPCID.SeekerBody) | (target.type == NPCID.SeekerHead) | (target.type == NPCID.SeekerTail) | (target.type == NPCID.DarkMummy) | (target.type == NPCID.CorruptPenguin) | (target.type == NPCID.CursedHammer) | (target.type == NPCID.Clinger) | (target.type == NPCID.BigMimicCorruption) | (target.type == NPCID.DesertGhoulCorruption) | (target.type == NPCID.DesertLamiaDark) | (target.type == NPCID.CrimsonGoldfish) | (target.type == NPCID.CrimsonPenguin) | (target.type == NPCID.BloodCrawler) | (target.type == NPCID.BloodCrawlerWall) | (target.type == NPCID.FaceMonster) | (target.type == NPCID.Crimera) | (target.type == NPCID.BigCrimera) | (target.type == NPCID.LittleCrimera) | (target.type == NPCID.Creeper) | (target.type == NPCID.BrainofCthulhu) | (target.type == NPCID.Herpling) | (target.type == NPCID.Crimslime) | (target.type == NPCID.BigCrimslime) | (target.type == NPCID.LittleCrimslime) | (target.type == NPCID.BloodJelly) | (target.type == NPCID.BloodFeeder) | (target.type == NPCID.CrimsonAxe) | (target.type == NPCID.IchorSticker) | (target.type == NPCID.FloatyGross) | (target.type == NPCID.BigMimicCrimson) | (target.type == NPCID.DesertGhoulCrimson) | (target.type == NPCID.DesertDjinn))
            {
                damage += 10;
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                int option = Main.rand.Next(3);
                if (option == 0)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetUno").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                else if (option == 1)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetDos").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                else if (option == 2)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetTres").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
            }
        }
    }
}
