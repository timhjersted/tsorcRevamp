using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon
{
    [AutoloadBossHead]
    class HellkiteDragonHead : ModNPC
    {

        int breathDamage = 33;
        int flameRainDamage = 32; //was 37
        int meteorDamage = 63;

        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 6;
            NPC.width = 60;
            NPC.height = 60;
            drawOffsetY = 42;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 100;
            NPC.defense = 10;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 100000;
            music = 12;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 200000;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            bossBag = ModContent.ItemType<Items.BossBags.HellkiteBag>();

            Color textColor = new Color(175, 75, 255);
            despawnHandler = new NPCDespawnHandler("The Hellkite Dragon claims its prey...", textColor, 174);

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.damage = 120;
                NPC.value = 100000;
                breathDamage = 45;
                flameRainDamage = 37;
                meteorDamage = 73;
            }
        }



        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            flameRainDamage = (int)(flameRainDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hellkite Dragon");
        }

        int breathCD = 90;
        //int previous = 0;
        bool breath = false;
        //bool tailD = false;
        public override bool CheckActive()
        {
            return false;
        }

        NPCDespawnHandler despawnHandler;
        public static int hellkitePieceSeperation = -5;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            Player nT = Main.player[NPC.target];
            if (Main.rand.Next(175) == 0)
            {
                breath = true;
                //Terraria.Audio.SoundEngine.PlaySound(15, -1, -1, 0);
            }
            if (breath)
            {
                //while (breathCD > 0) {
                //for (int pcy = 0; pcy < 10; pcy++) {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + (float)NPC.width / 2f, NPC.position.Y + (float)NPC.height / 2f, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);
                //}
                NPC.netUpdate = true; //new
                breathCD--;
                //}
            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 90;
                Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
            }
            if (Main.rand.Next(303) == 0)//was 833
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 600 + Main.rand.Next(1200), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 4.5f, ProjectileID.Fireball, flameRainDamage, 2f, Main.myPlayer); //6.5 too fast
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 600 + Main.rand.Next(1200), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 6.5f, ModContent.ProjectileType<Projectiles.Enemy.FlameRain>(), flameRainDamage, 2f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
                    NPC.netUpdate = true; //new
                }
            }
            if (Main.rand.Next(400) == 0)//1460, 200 was pretty awesome but a bit crazy
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 200 + Main.rand.Next(500), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / Main.rand.Next(3, 10), 5.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //8.9f is speed, 4.9 too slow, (float)nT.position.Y - 400f starts projectile closer above the player vs 500?
                    Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
                    NPC.netUpdate = true; //new
                }
            }
            if (Main.rand.Next(2) == 0)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }

            int[] bodyTypes = new int[] { ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, HellkiteDragonHead.hellkitePieceSeperation, 22, 0.25f, true, false, true, false, false); //30f was 10f

        }
        public static void SetImmune(Projectile projectile, NPC hitNPC)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC currentNPC = Main.npc[i];
                if (currentNPC.type == ModContent.NPCType<HellkiteDragonHead>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody2>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody3>() || currentNPC.type == ModContent.NPCType<HellkiteDragonLegs>() || currentNPC.type == ModContent.NPCType<HellkiteDragonTail>())
                {
                    currentNPC.immune[projectile.owner] = 10;
                }
            }
        }
        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            SetImmune(projectile, NPC);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.Center, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hellkite Dragon Head Gore").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("Gores/Blood Splat").Type, 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("Gores/Blood Splat").Type, 0.9f);

            if (Main.expertMode)
            {
                NPC.DropBossBags();
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DragonEssence>(), 22 + Main.rand.Next(6));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 4000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BossItems.HellkiteStone>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.DragonStone>());
            }
        }
    }
}