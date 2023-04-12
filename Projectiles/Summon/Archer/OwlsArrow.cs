using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    class OwlsArrow : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Owls Arrow");
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 6;
        }
        public override void SetDefaults()
        {
            Projectile.width = 11;
            Projectile.height = 25;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 37;
            Projectile.scale = 0.85f;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // projectile faces sprite right

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 35)
            {
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 63, Projectile.velocity.X * 0, Projectile.velocity.Y * 0, 70, default(Color), 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 6f);
                }
            }
            if (Main.rand.NextBool(6))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 245, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), .5f);
                Main.dust[dust].noGravity = true;
            }
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 91, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), .6f);
                Main.dust[dust].noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 3)
            {
                for (int d = 0; d < 10; d++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 91, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 30, default(Color), 0.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame, 14, 38), Color.White, Projectile.rotation, new Vector2(7, 0), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 159, Projectile.velocity.X, Projectile.velocity.Y, 30, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 159, Projectile.velocity.X, Projectile.velocity.Y, 30, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }

            return true;
        }
        /*public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if ((target.type == NPCID.SandsharkCorrupt) | (target.type == NPCID.SandsharkCrimson) | (target.type == NPCID.EaterofSouls) | (target.type == NPCID.BigEater) | (target.type == NPCID.LittleEater) | (target.type == NPCID.CorruptGoldfish) | (target.type == NPCID.DevourerBody) | (target.type == NPCID.DevourerHead) | (target.type == NPCID.DevourerTail) | (target.type == NPCID.EaterofWorldsBody) | (target.type == NPCID.EaterofWorldsHead) | (target.type == NPCID.EaterofWorldsTail) | (target.type == NPCID.Corruptor) | (target.type == NPCID.CorruptSlime) | (target.type == NPCID.Slimeling) | (target.type == NPCID.Slimer) | (target.type == NPCID.Slimer2) | (target.type == NPCID.SeekerBody) | (target.type == NPCID.SeekerHead) | (target.type == NPCID.SeekerTail) | (target.type == NPCID.DarkMummy) | (target.type == NPCID.CorruptPenguin) | (target.type == NPCID.CursedHammer) | (target.type == NPCID.Clinger) | (target.type == NPCID.BigMimicCorruption) | (target.type == NPCID.DesertGhoulCorruption) | (target.type == NPCID.DesertLamiaDark) | (target.type == NPCID.CrimsonGoldfish) | (target.type == NPCID.CrimsonPenguin) | (target.type == NPCID.BloodCrawler) | (target.type == NPCID.BloodCrawlerWall) | (target.type == NPCID.FaceMonster) | (target.type == NPCID.Crimera) | (target.type == NPCID.BigCrimera) | (target.type == NPCID.LittleCrimera) | (target.type == NPCID.Creeper) | (target.type == NPCID.BrainofCthulhu) | (target.type == NPCID.Herpling) | (target.type == NPCID.Crimslime) | (target.type == NPCID.BigCrimslime) | (target.type == NPCID.LittleCrimslime) | (target.type == NPCID.BloodJelly) | (target.type == NPCID.BloodFeeder) | (target.type == NPCID.CrimsonAxe) | (target.type == NPCID.IchorSticker) | (target.type == NPCID.FloatyGross) | (target.type == NPCID.BigMimicCrimson) | (target.type == NPCID.DesertGhoulCrimson) | (target.type == NPCID.DesertDjinn))
            {
                damage += 10;
            }
        }*/

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                int option = Main.rand.Next(3);
                if (option == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetUno") with { Volume = 0.6f, PitchVariance = 0.3f }, Projectile.Center);;
                }
                else if (option == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetDos") with { Volume = 0.6f, PitchVariance = 0.3f }, Projectile.Center);
                }
                else if (option == 2)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetTres") with { Volume = 0.6f, PitchVariance = 0.3f }, Projectile.Center);
                }
            }
        }
    }
}
