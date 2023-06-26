using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using System;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items;
using Terraria.DataStructures;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.BossItems;
using tsorcRevamp.Items.Weapons.Melee.Spears;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon
{
    [AutoloadBossHead]
    class HellkiteDragonHead : ModNPC
    {
        int breathDamage = 17;
        int flameRainDamage = 16; //was 37
        int meteorDamage = 32;
        public override void SetStaticDefaults()
        {
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.OnFire3,
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 6;
            NPC.width = 60;
            NPC.height = 60;
            DrawOffsetY = 42;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 145;
            NPC.defense = 100;
            NPC.HitSound = SoundID.NPCHit13; //better flesh hit
            NPC.DeathSound = SoundID.Item119;//dragon death
            NPC.lifeMax = 100000;
            Music = 12;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 250000;
            NPC.lavaImmune = true;
            Color textColor = new Color(175, 75, 255);
            despawnHandler = new NPCDespawnHandler(LaUtils.GetTextValue("NPCs.HellkiteDragonHead.DespawnHandler"), textColor, 174);

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.damage = 145;
                NPC.value = 100000;
                breathDamage = 58;
                flameRainDamage = 50;
                meteorDamage = 60;
            }
        }
        public float flapWings;
        //oolicile sorcerer
        public float DarkBeadShotTimer;
        public float DarkBeadShotCounter;

        public float MeteorShotTimer;
        public float MeteorShotCounter;

        public float CollisionTimer;
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

            DarkBeadShotTimer++;
            MeteorShotTimer++;

            flapWings++;
            

            //Flap Wings
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, PitchVariance = 0.2f }, NPC.position); //wing flap sound

            }
            if (flapWings == 90)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.2f }, NPC.position);
                flapWings = 0;
            }

            //Hellkite now makes contact with earth but can phase through walls if it can't reach the player, + 100 / + 200 works great! but it goes into walls too easily (+10 and +100 is better, but could be tweaked further)
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 10)))
            {

                NPC.noTileCollide = false;
                NPC.noGravity = true;

            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 100)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                //NPC.velocity.Y = 0f;
            }


            Player nT = Main.player[NPC.target];
            if (Main.rand.NextBool(175))
            {
                breath = true;
                
            }
            if (breath)
            {
                Vector2 spawnOffset = NPC.velocity; //Create a vector pointing in whatever direction the NPC is moving. We can transform this into an offset we can use.
                spawnOffset.Normalize(); //Shorten the vector to make it have a length of 1
                spawnOffset *= 64; //Multiply it so it has a length of 16. The length determines how far offset the projectile will be, 16 units = 1 tile

                //float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)(NPC.Center.X + spawnOffset.X), (int)(NPC.Center.Y + spawnOffset.Y), NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), breathDamage, 3.2f, Main.myPlayer);


                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = -0.6f }, NPC.Center); //flame thrower
                }

                NPC.netUpdate = true; 
                breathCD--;
                
            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 160;

                if (Main.rand.NextBool(2))
                {
                    MeteorShotCounter = 0;
                }
                
            }

            //FIRE FROM ABOVE ATTACK
            //Counts up each tick. Used to space out shots
            if (DarkBeadShotTimer >= 25 && DarkBeadShotCounter < 8)
            {
                    
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 600 + Main.rand.Next(1200), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 4.5f, ProjectileID.Fireball, flameRainDamage, 2f, Main.myPlayer); //6.5 too fast
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 600 + Main.rand.Next(1200), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 6.5f, ModContent.ProjectileType<Projectiles.Enemy.FlameRain>(), flameRainDamage, 2f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.1f, PitchVariance = 0.2f }, NPC.Center);
                        NPC.netUpdate = true; //new
                    
                    DarkBeadShotTimer = 0;
                    DarkBeadShotCounter++;

            }


            //METEOR SPACED OUT ATTACK
            if (MeteorShotTimer >= 58 && MeteorShotCounter < 9)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 200 + Main.rand.Next(500), (float)nT.position.Y - 600f, (float)(-50 + Main.rand.Next(100)) / Main.rand.Next(3, 10), 5.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //8.9f is speed, 4.9 too slow, (float)nT.position.Y - 400f starts projectile closer above the player vs 500?
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.1f, PitchVariance = 0.2f}, NPC.Center);
                NPC.netUpdate = true; //new      
                MeteorShotTimer = 0;
                MeteorShotCounter++;

                if (Main.rand.NextBool(2))
                {
                    DarkBeadShotCounter = 0;
                }
            }
            
            if (Main.rand.NextBool(200) && NPC.life < NPC.lifeMax / 10) //200 was pretty awesome but a bit crazy, and now we're doing it for last 10% of life
            {
                for (int pcy = 0; pcy < 8; pcy++)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 200 + Main.rand.Next(500), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / Main.rand.Next(3, 10), 5.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //8.9f is speed, 4.9 too slow, (float)nT.position.Y - 400f starts projectile closer above the player vs 500?
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    NPC.netUpdate = true; //new
                    DarkBeadShotCounter = 0;
                }
            }
            
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }

            int[] bodyTypes = new int[] { ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>() };
            //speed of dragon is hiding here, 22
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, HellkiteDragonHead.hellkitePieceSeperation, 18, 0.25f, true, false, true, false, false); //18 was 22, sooooo fast before

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
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if(projectile.DamageType != DamageClass.Melee)
            {
                SetImmune(projectile, NPC);
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.HellkiteBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HellkiteStone>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HiRyuuSpear>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 10, 20));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarrowBlade>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.Center, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hellkite Dragon Head Gore").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
        }
    }
}