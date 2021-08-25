using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    //Intentionally no minimap icon, to keep it mysterious
    //[AutoloadBossHead]
    class Blight : ModNPC {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 4;
        }
        public override void SetDefaults() {
            npc.npcSlots = 5;
            npc.width = 40;
            npc.height = 110;
            npc.aiStyle = -1;
            npc.timeLeft = 22500;
            npc.damage = 105;
            npc.defense = 90;
            npc.HitSound = SoundID.NPCHit3;
            npc.DeathSound = new LegacySoundStyle(29,53);
           // npc.DeathSound = SoundID.NPCDeath43;
            npc.lifeMax = 300000;
            npc.knockBackResist = 0f;
            npc.scale = 1f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 500000;
            npc.friendly = false;
            npc.alpha = 255;
            npc.boss = true;
            npc.buffImmune[BuffID.Confused] = true;
            bossBag = ModContent.ItemType<Items.BossBags.BlightBag>();
            despawnHandler = new NPCDespawnHandler("Inevitable.", new Color(255, 50, 50), DustID.Firework_Blue);
        }

        int phantomSeekerDamage = 58;
        int cometDamage = 50;
        int darkAstronomyDamage = 60;
        int antimatterCannonDamage = 140;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            phantomSeekerDamage = (int)(phantomSeekerDamage / 2);
            cometDamage = (int)(cometDamage / 2);
            darkAstronomyDamage = (int)(darkAstronomyDamage / 2);
            antimatterCannonDamage = (int)(antimatterCannonDamage / 2);
        }

        int chargeDamage = 0;
        bool chargeDamageFlag = false;


        int phase = 700;
        int attackindex = 0;
        float spazzlevel;
        float targetspazzlevel;
        public override void OnHitPlayer(Player target, int damage, bool crit) {

            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;
            if (Main.rand.Next(4) == 0) {

                target.AddBuff(36, 180 / expertScale, false); //broken armor
                target.AddBuff(20, 3600 / expertScale, false); //poisoned
                target.AddBuff(30, 1800 / expertScale, false); //bleeding

            }

            if (Main.rand.Next(2) == 0) {

                target.AddBuff(BuffID.BrokenArmor, 180 / expertScale, false); //broken armor
                target.AddBuff(BuffID.CursedInferno, 180 / expertScale, false); //cursed inferno
                //player.AddBuff("Powerful Curse Buildup", 18000, false); //chance to lose -20 life for 5 minutes
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false);
            }
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            int num54;

            //If it's too far away, target the closest player and charge them
            if (Math.Abs(Main.player[npc.target].position.X - npc.position.X) > 2800 || Math.Abs(Main.player[npc.target].position.Y - npc.position.Y) > 2200)
            {
                if (Main.rand.Next(450) == 1)
                {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 12) * -1;
                    npc.ai[1] = 1f;
                    npc.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    npc.damage = 130;
                    chargeDamage++;
                }
                if (chargeDamage >= 50)
                {
                    chargeDamageFlag = false;
                    npc.damage = 80;
                    chargeDamage = 0;
                }
            }

            if (Main.player[npc.target].position.Y - 100 > npc.position.Y) {

                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 15, npc.velocity.X, npc.velocity.Y, 250, color, 5f);
                Main.dust[dust].noGravity = true;


                npc.directionY = 1;
            }
            else {

                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 15, npc.velocity.X, npc.velocity.Y, 250, color, 4f);
                Main.dust[dust].noGravity = false;


                npc.directionY = -1;
            }

            if (Main.player[npc.target].position.X - 250 > npc.position.X) {

                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 15, npc.velocity.X, npc.velocity.Y, 250, color, 2f);
                Main.dust[dust].noGravity = false;


                npc.direction = 1;
            }
            else {

                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 15, npc.velocity.X, npc.velocity.Y, 250, color, 5f);
                Main.dust[dust].noGravity = false;


                npc.direction = -1;
            }

            npc.spriteDirection = 1;

            npc.frame.Width = 40;


            if (attackindex == 0) {
                npc.frame.Y = 58;
            }

            npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
            if (npc.ai[1] >= 8f) {
                if (Main.rand.Next(450) == 1) {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 12) * -1;
                    npc.ai[1] = 1f;
                    npc.netUpdate = true;
                }
                if (chargeDamageFlag == true) {
                    npc.damage = 130;
                    chargeDamage++;
                }
                if (chargeDamage >= 50) {
                    chargeDamageFlag = false;
                    npc.damage = 80;
                    chargeDamage = 0;
                }
            }


            if (npc.direction == -1 && npc.velocity.X > -2f) {
                npc.velocity.X = npc.velocity.X - 0.1f;
                if (npc.velocity.X > 2f) {
                    npc.velocity.X = npc.velocity.X - 0.1f;
                }
                else {
                    if (npc.velocity.X > 0f) {
                        npc.velocity.X = npc.velocity.X + 0.05f;
                    }
                }
                if (npc.velocity.X < -2f) {
                    npc.velocity.X = -2f;
                }
            }
            else {
                if (npc.direction == 1 && npc.velocity.X < 2f) {
                    npc.velocity.X = npc.velocity.X + 0.1f;
                    if (npc.velocity.X < -2f) {
                        npc.velocity.X = npc.velocity.X + 0.1f;
                    }
                    else {
                        if (npc.velocity.X < 0f) {
                            npc.velocity.X = npc.velocity.X - 0.05f;
                        }
                    }
                    if (npc.velocity.X > 2f) {
                        npc.velocity.X = 2f;
                    }
                }
            }
            if (npc.directionY == -1 && (double)npc.velocity.Y > -1.5) {
                npc.velocity.Y = npc.velocity.Y - 0.05f;

                if ((double)npc.velocity.Y < -1.5) {
                    npc.velocity.Y = -1.5f;
                }
            }
            else {
                if (npc.directionY == 1 && (double)npc.velocity.Y < 1.5) {
                    npc.velocity.Y = npc.velocity.Y + 0.05f;
                    if ((double)npc.velocity.Y > 1.5) {
                        npc.velocity.Y = 1.5f;
                    }
                }
            }




            //Deal with dat crazy spazzin' out;
            spazzlevel += (targetspazzlevel - spazzlevel) / 60f;

            //Cycle through all attacks linearly		
            if (phase > 1000) {
                phase = 0;

                //Chill for a few seconds after either of these attacks, because their projectiles linger
                if(attackindex == 2 || attackindex == 3)
                {
                    phase += 150;
                    attackindex = 0;
                } else
                {
                    attackindex = Main.rand.Next(1, 5);
                }
            }


            phase++;

            // If we're almost dead, activate the Cataclysm
            if (npc.life < 2000) {
                attackindex = 5;
            }

            //Actual attacks
            //Condemnation - Phantom Seeker
            if (attackindex == 1) {
                targetspazzlevel = 0;


                if (((int)Main.time % 60) < 1) {
                    for (int i = 0; i < 5; i++) {
                        num54 = Projectile.NewProjectile(new Vector2(npc.position.X + 20, npc.position.Y + 50), new Vector2(Main.rand.Next(-5, 5), Main.rand.Next(-5, 5)), ModContent.ProjectileType<Projectiles.PhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer); //Phantom Seeker
                        Main.projectile[num54].timeLeft = 400;
                        Main.projectile[num54].rotation = Main.rand.Next(700) / 100f;
                        Main.projectile[num54].ai[0] = npc.target;
                    }
                }
            }


            //Antimatter - Black Comet
            else if (attackindex == 3) {
                targetspazzlevel = 10;                
                if (((int)Main.time % 5) < 1)
                {
                    float posX = Main.player[npc.target].position.X + Main.rand.Next(-1400, 1400);
                   /** int spread = Main.rand.Next(3);
                    if (spread < 2) {
                        if (spread == 1){
                            posX += 1200;
                        } else
                        {
                            posX -= 1200;
                        }
                     }**/
                    num54 = Projectile.NewProjectile(posX, Main.player[npc.target].position.Y - 650, 0, 5, ModContent.ProjectileType<Projectiles.Comet>(), cometDamage, 0f, Main.myPlayer); //Comet
                    Main.projectile[num54].ai[1] = 5.5f; //Velocity
                }
            }


            //Dark Astronomy - Black Spiral
            else if (attackindex == 2) {
                targetspazzlevel = 25;

                if (((int)Main.time % 60) < 1) {
                    for (int i = 0; i < 3; i++) {
                        num54 = Projectile.NewProjectile(new Vector2(npc.position.X + 20, npc.position.Y + 50), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.PhantomSpiral>(), darkAstronomyDamage, 0f, Main.myPlayer); //Phantom Spiral
                        Main.projectile[num54].timeLeft = 1000;
                        Main.projectile[num54].rotation = Main.rand.Next(700) / 100f;
                        Main.projectile[num54].ai[0] = npc.whoAmI;
                        Main.projectile[num54].ai[1] = Main.rand.Next(200, 2500);
                    }
                }
            }

            //Annihilation - Antimatter Cannon
            else if (attackindex == 4) {
                float j = (float)Math.Atan2((double)(npc.position.X - Main.player[npc.target].position.X), (double)(npc.position.Y - Main.player[npc.target].position.Y + 48));

                targetspazzlevel = 50;
                npc.velocity.Y = 0;
                npc.velocity.X = 0;
                if (((int)Main.time % 5) < 1 && phase > 100) {
                    phase += 10;
                    for (int i = 0; i < 6; i++) {
                        int s = Main.rand.Next(2, 10);
                        float m = (float)Math.Sin(j) * -s;
                        float n = (float)Math.Cos(j) * -s;
                        num54 = Projectile.NewProjectile(new Vector2(npc.position.X + Main.rand.Next(-25, 25), npc.position.Y + Main.rand.Next(50, 150)), new Vector2(m, n), ModContent.ProjectileType<Projectiles.Comet>(), antimatterCannonDamage, 0f, Main.myPlayer); //Antimatter Cannon
                        Main.projectile[num54].scale = (Main.rand.Next(50, 100)) / 75f;
                        Main.projectile[num54].timeLeft = 300;
                        Main.projectile[num54].ai[1] = 10; //Velocity
                    }
                }
            }
            else {
                targetspazzlevel = 0;
            }

        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {

            Texture2D Noise_Texture = Main.npcTexture[this.npc.type];
            Random rand1 = new Random((int)Main.time);
            int height = this.npc.frame.Height;
            int width = this.npc.frame.Width;
            int offsetx = this.npc.frame.X;
            int offsety = this.npc.frame.Y;
            float targetscale = 1f;
            Rectangle fromrect = new Rectangle(offsetx, offsety, width, height);
            Vector2 PC;
            SpriteEffects mydirection;
            if (this.npc.spriteDirection >= 0) mydirection = SpriteEffects.FlipHorizontally;
            else mydirection = SpriteEffects.None;
            for (int i = 0; i < 5; i++) {

                PC = this.npc.position;
                PC.Y += this.npc.height * 2.5f;

                PC.X += rand1.Next((int)-spazzlevel, (int)spazzlevel);
                PC.Y += rand1.Next((int)-spazzlevel, (int)spazzlevel);
                Color targetColor = new Color(0, 0, 0, 0);
                spriteBatch.Draw(
                            Noise_Texture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.npc.rotation,//Main.rand.Next(600)/100, 
                            new Vector2(0, 0),
                            targetscale * 1.04f,
                            mydirection,
                            0f);
            }

            rand1 = new Random((int)Main.time);
            for (int i = 0; i < 5; i++) {
                PC = this.npc.position;

                PC.Y += this.npc.height * 0.1f;

                PC.X += rand1.Next((int)-spazzlevel, (int)spazzlevel);
                PC.Y += rand1.Next((int)-spazzlevel, (int)spazzlevel);
                Color targetColor = new Color(0, 0, 0, 0);
                spriteBatch.Draw(
                            Noise_Texture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.npc.rotation,//Main.rand.Next(600)/100, 
                            new Vector2(0, 0),
                            targetscale,
                            mydirection,
                            0f);
            }

            return false;
        }

        public override void NPCLoot() {
            if (Main.expertMode)
            {
                npc.DropBossBags();
            }
            else
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.DivineSpark>());
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulOfBlight>(), Main.rand.Next(3, 5));
            }
        }
    }
}
