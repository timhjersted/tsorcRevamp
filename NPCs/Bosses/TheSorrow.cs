using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheSorrow : ModNPC {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 7;
        }

        public override void SetDefaults() {
            npc.aiStyle = -1;
            npc.lifeMax = 19000;
            npc.damage = 85;
            baseContactDamage = npc.damage;
            npc.defense = 20;
            npc.knockBackResist = 0f;
            npc.scale = 1.4f;
            npc.value = 150000;
            npc.npcSlots = 180;
            npc.boss = true;
            npc.lavaImmune = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            bossBag = ModContent.ItemType<Items.BossBags.TheSorrowBag>();

            drawOffsetY = +70;
            npc.width = 140;
            npc.height = 60;

            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("The Sorrow claims you...", Color.DarkCyan, 29);
        }

        //npc.ai[0] = damage taken counter
        //npc.ai[1] = invulnerability timer
        //npc.ai[3] = state counter
        int hitTime = 0; //How long since it's last been hit (used for reducing damage counter)
        int waterTrailsDamage = 35;
        int baseContactDamage; //This stores its true contact damage stat, allowing us to modify it upon enrage and then return to the original value seamlessly
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax += (int)(npc.lifeMax * 0.7f * numPlayers);
            npc.defense = npc.defense += 12;
            npc.lifeMax = 28000;
            waterTrailsDamage = (int)(waterTrailsDamage * 1.3 / 2);
            baseContactDamage = (int)(npc.damage * 1.3 / 2);
        }
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            npc.netUpdate = true;
            npc.ai[2]++;
            npc.ai[1]++;
            hitTime++;
            if (npc.ai[0] > 0) npc.ai[0] -= hitTime / 10;
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, npc.velocity.X, npc.velocity.Y, 200, new Color(), 0.5f + (15.5f * (npc.ai[0] / (npc.lifeMax / 10))));
            Main.dust[dust].noGravity = true;


            if (npc.ai[3] == 0) {
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.damage = baseContactDamage;
                if (npc.ai[2] < 600)
                {
                    if (Main.player[npc.target].position.X < vector8.X) {
                        if (npc.velocity.X > -8) { npc.velocity.X -= 0.22f; }
                    }
                    if (Main.player[npc.target].position.X > vector8.X) {
                        if (npc.velocity.X < 8) { npc.velocity.X += 0.22f; }
                    }

                    if (Main.player[npc.target].position.Y < vector8.Y + 300) {
                        if (npc.velocity.Y > 0f) npc.velocity.Y -= 0.8f;
                        else npc.velocity.Y -= 0.07f;
                    }
                    if (Main.player[npc.target].position.Y > vector8.Y + 300) {
                        if (npc.velocity.Y < 0f) npc.velocity.Y += 0.8f;
                        else npc.velocity.Y += 0.07f;
                    }

                    if (npc.ai[1] >= 0 && npc.ai[2] > 120 && npc.ai[2] < 600)
                    {
                        float num48 = 9f;
                        int type = ModContent.ProjectileType<WaterTrail>();
                        Main.PlaySound(SoundID.Item, (int)vector8.X, (int)vector8.Y, 17);
                        float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                        //yes do it manually. im not using a loop. i don't care //Understandable, have a great day
                        Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, waterTrailsDamage, 0f);
                        Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -1), type, waterTrailsDamage, 0f);
                        Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, waterTrailsDamage, 0f);
                        Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, waterTrailsDamage, 0f);
                        Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, waterTrailsDamage, 0f);

                        npc.ai[1] = -180;
                    }
                }
                else if (npc.ai[2] >= 600 && npc.ai[2] < 690)
                {
                    //Then chill for a second.
                    //This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    npc.velocity.X *= 0.95f;
                    npc.velocity.Y *= 0.95f; 
                    Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 132, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);
                }
                else if (npc.ai[2] >= 690 && npc.ai[2] < 1290)
                {
                    int dashSpeed = 18;
                    npc.velocity.X *= 0.98f;
                    npc.velocity.Y *= 0.98f;
                    if ((npc.velocity.X < 2f) && (npc.velocity.X > -2f) && (npc.velocity.Y < 2f) && (npc.velocity.Y > -2f)) {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), (vector8.X) - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                        npc.velocity.X = (float)(Math.Cos(rotation) * dashSpeed) * -1;
                        npc.velocity.Y = (float)(Math.Sin(rotation) * dashSpeed) * -1;
                    }
                }
                else npc.ai[2] = 0;
            }
            else {
                npc.ai[3]++;
                npc.alpha = 200;
                npc.damage = (int)(baseContactDamage * 1.2);
                npc.dontTakeDamage = true;
                if (Main.player[npc.target].position.X < vector8.X) {
                    if (npc.velocity.X > -6) { npc.velocity.X -= 0.22f; }
                }
                if (Main.player[npc.target].position.X > vector8.X) {
                    if (npc.velocity.X < 6) { npc.velocity.X += 0.22f; }
                }
                if (Main.player[npc.target].position.Y < vector8.Y) {
                    if (npc.velocity.Y > 0f) npc.velocity.Y -= 0.8f;
                    else npc.velocity.Y -= 0.07f;
                }
                if (Main.player[npc.target].position.Y > vector8.Y) {
                    if (npc.velocity.Y < 0f) npc.velocity.Y += 0.8f;
                    else npc.velocity.Y += 0.07f;
                }
                if (npc.ai[1] >= 0 && npc.ai[2] > 120 && npc.ai[2] < 600)
                {
                    float num48 = 13f;
                    float invulnDamageMult = 1.2f;
                    int type = ModContent.ProjectileType<WaterTrail>();
                    Main.PlaySound(SoundID.Item, (int)vector8.X, (int)vector8.Y, 17);
                    float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    //yes do it manually. im not using a loop. i don't care //Understandable, have a nice day.
                    Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                    Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                    Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                    Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                    Projectile.NewProjectile(vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                    npc.ai[1] = -180;
                }
                if (npc.ai[3] == 100) {
                    npc.ai[3] = 1;
                    npc.life += 200; //amount boss heals when going invisible
                    if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
                }
                if (npc.ai[1] >= 0) {
                    npc.ai[3] = 0;
                    for (int i = 0; i < 40; i++) {
                        Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, 0, 0, 0, new Color(), 3f);
                    }
                }
            }
        }

        public override void FindFrame(int frameHeight) {
            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if (npc.velocity.X < 0) {
                npc.spriteDirection = -1;
            }
            else {
                npc.spriteDirection = 1;
            }
            npc.rotation = npc.velocity.X * 0.08f;
            npc.frameCounter += 1.0;
            if (npc.frameCounter >= 4.0) {
                npc.frame.Y = npc.frame.Y + num;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= num * Main.npcFrameCount[npc.type]) {
                npc.frame.Y = 0;
            }
            if (npc.ai[3] == 0) {
                npc.alpha = 0;
            }
            else {
                npc.alpha = 200;
            }
        }
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit) {
            hitTime = 0;
            npc.ai[0] += (float)damage;
            if (npc.ai[0] > (npc.lifeMax / 10)) {
                npc.ai[3] = 1; //begin invulnerability state
                for (int i = 0; i < 50; i++) { //dustsplosion on invulnerability
                    Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++) {
                    Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, 0, 0, 100, default, 3f);
                }
                npc.ai[1] = -180;
                npc.ai[0] = 0; //reset damage counter
            }
            return true;
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void NPCLoot() {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(npc.position, (int)(npc.width * 1.5), (int)(npc.height * 1.5), 29, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 100; num36++)
            {
                Dust.NewDust(npc.position, (int)(npc.width * 1.5), (int)(npc.height * 1.5), 132, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
            if (Main.expertMode) {
                npc.DropBossBags();
            }
            else {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.CrestOfWater>(), 2);
                Item.NewItem(npc.getRect(), ItemID.AdamantiteDrill, 1, false, -1); 
            }
        }
    }
}