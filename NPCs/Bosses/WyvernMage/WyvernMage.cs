using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage
{
    [AutoloadBossHead]
    class WyvernMage : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 6;
            Main.npcFrameCount[NPC.type] = 3;
            NPC.width = 28;
            NPC.height = 44;
            NPC.damage = 110;
            NPC.defense = 20;
            NPC.scale = 1.3f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 18200;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            NPC.knockBackResist = 0.2f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 150000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            //NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("The Wyvern Mage stands victorious...", Color.DarkCyan, DustID.Demonite);
            nextWarpPoint = Main.rand.NextVector2CircularEdge(320, 320);
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wyvern Mage");
        }

        int frozenSawDamage = 60;
        int lightningDamage = 80;
       
        int plasmaDamage = 40;
        int lifeTimer = 0;

        int holdTimer = 0;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            frozenSawDamage = (int)(frozenSawDamage / 2);
            lightningDamage = (int)(lightningDamage / 2);
        }

        //When this hits 5, the boss fires an orb and resets it back to 0. Only happens right at the start of its teleport.
        public int OrbTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //When this hits 200 (120 if dragon is dead) the boss teleports
        public int TeleportTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        //Counts up each time the boss fires an orb.
        public int ShotCount
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.WitheredWeapon, 3 * 60, false);
            target.AddBuff(BuffID.WitheredArmor, 3 * 60, false);
        }

        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            Lighting.AddLight(NPC.Center, Color.BlueViolet.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            //Proximity Debuffs
            if ( NPC.life < NPC.lifeMax / 2)//NPC.Distance(player.Center) < 3550 &&
            {

                
                player.AddBuff(BuffID.Chilled, 30, false);
                player.AddBuff(BuffID.Ichor, 30, false);
                //player.AddBuff(ModContent.BuffType<Buffs.Chilled>(), 60, false);
                

                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("The Wyvern Mage emits a a chilling wave of frost! The cold is unbearable!", 235, 199, 23);//deep yellow
                    holdTimer = 12000;
                }

            }
            
            


            //Count up the timers
            OrbTimer++;
            TeleportTimer++;

            //Check if the dragon is alive. If so, spawn less transparent dusts and fire more orbs
            bool dragonAlive = NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());

            int transparency = 100;
            if (!dragonAlive)
            {
                transparency += 50;
            }
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X, NPC.velocity.Y, transparency, Color.Black, 1f);
            Main.dust[dust].noGravity = true;

            if (OrbTimer >= 5 && ((ShotCount < 6) || (ShotCount < 9) && !dragonAlive))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 startPos = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speed;
                    float startRotation;

                    //Set the initial shot rotation offset and speed
                    if (dragonAlive)
                    {
                        speed = 4;
                        startRotation = MathHelper.ToRadians(-15);
                    }
                    else
                    {
                        speed = 3f;
                        startRotation = MathHelper.ToRadians(-60);
                    }

                    //Generate the velocity vector
                    Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(startPos, Main.player[NPC.target].Center, speed);

                    //Rotate it by the initial rotation
                    projVelocity = projVelocity.RotatedBy(startRotation);

                    //Rotate it again by 15 degrees per shot
                    projVelocity = projVelocity.RotatedBy(MathHelper.ToRadians(15) * ShotCount);

                    //Fire it
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), startPos.X, startPos.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), frozenSawDamage, 0f, Main.myPlayer);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                OrbTimer = 0;
                ShotCount++;
            }

            //Dramatically slow down shortly after teleporting
            if (TeleportTimer >= 10)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.27f;
            }

            //Every so often, teleport. Happens every 200 frames if the wyvern is alive, 120 if not
            if (TeleportTimer >= 260)
            {
                WyvernMageTeleport();
            }

            //end of W1k's Death code

            #region revamped
            int num58;
            if (TeleportTimer == 10) //If the boss just teleported
            {
                if (Main.rand.NextBool(2) || !dragonAlive) //1 in 2 chance boss will use attack when it flies down on top of you if the dragon is alive
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float projectileSpeed = 4f;
                        Vector2 startPos = NPC.Center;
                        startPos.Y -= 220;
                        Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(startPos, Main.player[NPC.target].Center, projectileSpeed);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), startPos.X, startPos.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), lightningDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                }

                if (NPC.life <= 16000 && lifeTimer < 1 || NPC.life <= 12000 && lifeTimer < 2 || NPC.life <= 8000 && lifeTimer < 3 || NPC.life <= 4000 && lifeTimer < 4 || NPC.life <= 2000 && lifeTimer < 5)
                {
                    UsefulFunctions.BroadcastText("The Wyvern Mage summons the Archdeacons!", 175, 75, 255);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.Archdeacon>(), 0);
                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                        Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.Archdeacon>(), 0);
                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn, 0f, 0f, 0f, 0);
                        }

                        num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaDamage, 0f, Main.myPlayer);
                        num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaDamage, 0f, Main.myPlayer);
                        num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaDamage, 0f, Main.myPlayer);
                        Main.projectile[num58].timeLeft = 560;
                        Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
                        Main.projectile[num58].ai[0] = this.NPC.target;
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    lifeTimer++;
                }



                if (Main.rand.NextBool(60) || (dragonAlive && Main.rand.NextBool(40))) //1 in 15 chance boss will summon an NPC, 1/7 if the dragon is dead
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > NPC.lifeMax / 2)
                    {
                        int Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                        Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    }
                }
            }

            #endregion
        }

        #endregion revamped

        Vector2 nextWarpPoint;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(nextWarpPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextWarpPoint = reader.ReadVector2();
        }

        public void WyvernMageTeleport()
        {
            if (nextWarpPoint != Vector2.Zero)
            {
                //Check if the player has line of sight to the warp point. If not, rotate it by 90 degrees and try again. After 4 checks, give up.
                //Lazy way to do this, but it's deterministic and works for 99% of cases so it works.
                //We can't check collision when we pre-select the warp point, because it moves the npc relative to the player and the player might move in the meantime
                if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    NPC.Center = Main.player[NPC.target].Center + nextWarpPoint;
                }
                else if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(90)), 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(90)), 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    NPC.Center = Main.player[NPC.target].Center + (nextWarpPoint.RotatedBy(MathHelper.ToRadians(90)));
                }
                else if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(270)), 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(270)), 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    NPC.Center = Main.player[NPC.target].Center + (nextWarpPoint.RotatedBy(MathHelper.ToRadians(270)));
                }
                else if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(180)), 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint.RotatedBy(MathHelper.ToRadians(180)), 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    NPC.Center = Main.player[NPC.target].Center + (nextWarpPoint.RotatedBy(MathHelper.ToRadians(180)));
                }
                else
                {
                    NPC.Center = Main.player[NPC.target].Center + nextWarpPoint;
                }
            }

            NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 13);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            TeleportTimer = 0;
            ShotCount = 0;
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                Main.dust[dust].noGravity = false;
            }

            nextWarpPoint = Main.rand.NextVector2CircularEdge(640, 640);
            NPC.netUpdate = true;
        }
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 2;
        }
        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.WyvernMageBag>(), 1, 1, 1, new WyvernMageDropCondition()));
        }
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.Broadswords.LionheartGunblade>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.LampTome>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Magic.GemBox>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.PoisonbiteRing>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BloodbiteRing>(), 1);

                if (!(tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMage>()))))
                { //If the boss has not yet been killed
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 15000); //Then drop the souls
                }
            }
        }
    }

    public class WyvernMageDropCondition : Terraria.GameContent.ItemDropRules.IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<MechaDragonHead>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return "Drops if his wyvern is dead";
        }
    }
}