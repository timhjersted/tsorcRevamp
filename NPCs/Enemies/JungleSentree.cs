using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.NPCs.Enemies
{
    public class JungleSentree : ModNPC // Source of rich mahogany - drops extra wood when hit with axe - takes 2x damage from axes
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Jungle Sentree");
            Main.npcFrameCount[NPC.type] = 9;
        }

        public override void SetDefaults()
        {
            NPC.width = 48;
            NPC.height = 140;
            NPC.aiStyle = -1;
            NPC.damage = 25;
            NPC.knockBackResist = 0;
            NPC.defense = 12;
            NPC.lifeMax = 200;
            NPC.HitSound = new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/NPCHit/Dig");
            NPC.DeathSound = SoundID.NPCDeath33;
            NPC.value = 1000;
            NPC.buffImmune[BuffID.Confused] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.JungleSentreeBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (spawnInfo.Player.ZoneJungle && NPC.CountNPCS(ModContent.NPCType<JungleSentree>()) < 2
                && TileID.Sets.Conversion.Grass[spawnInfo.SpawnTileType]
                && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.None || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.MudUnsafe)
                && Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY].TileType == TileID.JungleGrass && !Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY].LeftSlope //all this is to prevent the npc spawning in really odd looking places
                && Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY].TileType == TileID.JungleGrass && !Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY].RightSlope//make sure block to left and right are jungle grass
                && Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY - 1].TileType != TileID.JungleGrass && Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY - 1].TileType != TileID.Mud && !Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY - 1].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX - 1, spawnInfo.SpawnTileY - 1].RightSlope //make sure blocks to left/right and above are empty
                && Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY - 1].TileType != TileID.JungleGrass && Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY - 1].TileType != TileID.Mud && !Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY - 1].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX + 1, spawnInfo.SpawnTileY - 1].LeftSlope
                && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 8].TileType != TileID.JungleGrass && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 8].TileType != TileID.Mud) //check the cieling is high enough

            {
                return 0.6f; // It's high because the chance of the conditions being right is pretty low
            }
            return chance;
        }

        #region AI
        private const int AI_State_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int State_Asleep = 0;
        private const int State_Notice = 1;
        private const int State_Angered = 2;

        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }

        public float AI_Timer
        {
            get => NPC.ai[AI_Timer_Slot];
            set => NPC.ai[AI_Timer_Slot] = value;
        }

        public int spawntimer = 0;

        // Our AI here makes our NPC sit waiting for a player to enter range then spawns minions to attack.
        public override void AI()
        {
            NPC.GivenName = "???";
            // The npc starts in the asleep state, waiting for a player to enter range
            if (AI_State == State_Asleep)
            {
                // TargetClosest sets npc.target to the player.whoAmI of the closest player. the faceTarget parameter means that npc.direction will automatically be 1 or -1 if the targeted player is to the right or left. This is also automatically flipped if npc.confused
                NPC.TargetClosest(true);
                // Now we check the make sure the target is still valid and within our specified notice range (350)
                if (NPC.HasValidTarget && Main.player[NPC.target].Distance(NPC.Center) < 400f)
                {
                    // Since we have a target in range, we change to the Notice state. (and zero out the Timer for good measure)
                    AI_State = State_Notice;
                    AI_Timer = 0;
                }
                if ((NPC.life < NPC.lifeMax) && (Main.rand.NextBool(4)))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 107, 0, 0, 0, default(Color), 1f); //regenerating hp
                }
            }
            // In this state, a player has been targeted
            else if (AI_State == State_Notice)
            {
                // If the targeted player is in attack range (250).
                if (Main.player[NPC.target].Distance(NPC.Center) < 300f)
                {
                    // Here we use our Timer to wait a fraction of a second before spawning babies.
                    AI_Timer++;
                    if (AI_Timer >= 20)
                    {
                        AI_State = State_Angered;
                        AI_Timer = 0;
                    }
                }
                else
                {
                    NPC.TargetClosest(true);
                    if (!NPC.HasValidTarget || Main.player[NPC.target].Distance(NPC.Center) > 550f)
                    {
                        // Out targeted player seems to have left our range, so we'll go back to sleep.
                        AI_State = State_Asleep;
                        AI_Timer = 0;
                    }
                }
            }
            // In this state, begin to spawn babies.
            else if (AI_State == State_Angered)
            {
                NPC.GivenName = "Jungle Sentree";
                //int randomness = Main.rand.Next(3);
                spawntimer++;
                if (NPC.life > NPC.lifeMax / 3)
                {
                    if (Main.rand.NextBool(20))
                    {
                        Dust.NewDust(NPC.position - new Vector2(20, 0), NPC.width / 3, NPC.height / 2, 18, Main.rand.Next(-2, 0), Main.rand.Next(-2, 0), 0, default(Color), 1f); //left branch
                    }
                    if (Main.rand.NextBool(20))
                    {
                        Dust.NewDust(NPC.position - new Vector2(-42, 0), NPC.width / 3, NPC.height / 2, 18, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f); //right branch
                    }
                }
                if (NPC.life <= NPC.lifeMax / 3)
                {
                    if (Main.rand.NextBool(10))
                    {
                        Dust.NewDust(NPC.position - new Vector2(20, 0), NPC.width / 3, NPC.height / 2, 18, Main.rand.Next(-2, 0), Main.rand.Next(-2, 0), 0, default(Color), 1f); //left branch
                    }
                    if (Main.rand.NextBool(10))
                    {
                        Dust.NewDust(NPC.position - new Vector2(-42, 0), NPC.width / 3, NPC.height / 2, 18, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f); //right branch
                    }
                }


                if (spawntimer >= 0 && spawntimer <= 40 && (NPC.CountNPCS(NPCID.JungleBat) < 6 || NPC.CountNPCS(NPCID.LittleHornetLeafy) < 6))
                {
                    if (Main.rand.NextBool(8))
                    {
                        Dust.NewDust(new Vector2((int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y)), 2, 2, 18, Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(-1.1f, 1.1f), 0, default(Color), 1f);
                    }
                }
                if (spawntimer > 40 && spawntimer <= 60 && (NPC.CountNPCS(NPCID.JungleBat) < 6 || NPC.CountNPCS(NPCID.LittleHornetLeafy) < 6))
                {
                    Dust.NewDust(NPC.position, NPC.width / 2, NPC.height / 4, 18, Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f), 0, default(Color), 1f);
                }
                if (spawntimer == 60 && (NPC.CountNPCS(NPCID.JungleBat) < 6) && NPC.Center.Y / 16 < Main.rockLayer) //wont spawn babies if there are already 6
                {
                    int npcIndex = -1;

                    npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y), NPCID.JungleBat);

                    if (npcIndex >= 0)
                    {
                        Main.npc[npcIndex].value /= 2;
                    }

                    //play sound, make dust
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item76, NPC.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2((int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y)), 2, 2, 18, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, default(Color), 1f)]; //glowy nature dust
                        dust.noGravity = true;
                        dust.fadeIn = .1f;
                    }
                }
                if (spawntimer == 60 && (NPC.CountNPCS(NPCID.JungleBat) < 4 || NPC.CountNPCS(NPCID.LittleHornetLeafy) < 4) && NPC.Center.Y / 16 >= Main.rockLayer) //wont spawn babies if there are already 6
                {
                    if (Main.rand.NextBool(3))
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item97, NPC.Center);
                        int npcIndex = -1;
                        //NPC.NewNPC(NPC.GetSource_FromAI(), (int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y), NPCID.LittleHornetLeafy);
                        npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y), NPCID.LittleHornetLeafy);

                        if (npcIndex >= 0)
                        {
                            Main.npc[npcIndex].value /= 2;
                        }
                    }
                    else
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item76, NPC.Center);
                        int npcIndex2 = -1;

                        npcIndex2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y), NPCID.JungleBat);

                        if (npcIndex2 >= 0)
                        {
                            Main.npc[npcIndex2].value /= 2;
                        }
                    }
                    //make dust
                    for (int i = 0; i < 30; i++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2((int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height - 118) + NPC.velocity.Y)), 2, 2, 18, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, default(Color), 1f)]; //glowy nature dust
                        dust.noGravity = true;
                        dust.fadeIn = .1f;
                    }
                }
                if (spawntimer == 300 && NPC.life > NPC.lifeMax / 3)
                {
                    spawntimer = 0;
                }
                if (spawntimer == 180 && NPC.life <= NPC.lifeMax / 3)
                {
                    spawntimer = 0;
                }

                NPC.TargetClosest(true);
                if (!NPC.HasValidTarget || Main.player[NPC.target].Distance(NPC.Center) > 550f)
                {
                    // Out targeted player seems to have left our range, so we'll go back to sleep.
                    AI_State = State_Asleep;
                    AI_Timer = 0;
                    spawntimer = 0;
                }

            }
        }
        #endregion

        #region Animation

        private const int Frame_Asleep = 0;
        private const int Frame_Notice = 1;
        private const int Frame_Angered_1 = 2;
        private const int Frame_Angered_2 = 3;
        private const int Frame_Angered_3 = 4;
        private const int Frame_Angered_4 = 5;
        private const int Frame_Angered_5 = 6;
        private const int Frame_Angered_6 = 7;
        private const int Frame_Angered_7 = 8;


        public override void FindFrame(int frameHeight)
        {
            // This makes the sprite flip horizontally in conjunction with the npc.direction.
            //npc.spriteDirection = npc.direction;

            // For the most part, our animation matches up with our states.
            if (AI_State == State_Asleep)
            {
                // npc.frame.Y is the goto way of changing animation frames. npc.frame starts from the top left corner in pixel coordinates, so keep that in mind.
                NPC.frame.Y = Frame_Asleep * frameHeight;
            }
            else if (AI_State == State_Notice)
            {
                // Going from Notice to Asleep makes our npc look like it's crouching to jump.
                if (AI_Timer < 10)
                {
                    NPC.frame.Y = Frame_Notice * frameHeight;
                }
                else
                {
                    NPC.frame.Y = Frame_Asleep * frameHeight;
                }
            }
            else if (AI_State == State_Angered)
            {
                // Cycle through all 8 frames
                if (NPC.life > NPC.lifeMax / 3)
                {
                    NPC.frameCounter += 1;
                }
                if (NPC.life <= NPC.lifeMax / 3)
                {
                    NPC.frameCounter += 2;
                }

                if (NPC.frameCounter < 10)
                {
                    NPC.frame.Y = Frame_Notice * frameHeight;
                }
                else if (NPC.frameCounter < 10)
                {
                    NPC.frame.Y = Frame_Angered_1 * frameHeight;
                }
                else if (NPC.frameCounter < 20)
                {
                    NPC.frame.Y = Frame_Angered_2 * frameHeight;
                }
                else if (NPC.frameCounter < 30)
                {
                    NPC.frame.Y = Frame_Angered_3 * frameHeight;
                }
                else if (NPC.frameCounter < 40)
                {
                    NPC.frame.Y = Frame_Angered_4 * frameHeight;
                }
                else if (NPC.frameCounter < 50)
                {
                    NPC.frame.Y = Frame_Angered_5 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = Frame_Angered_6 * frameHeight;
                }
                else if (NPC.frameCounter < 70)
                {
                    NPC.frame.Y = Frame_Angered_7 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }
        }

        #endregion

        #region Misc

        public override void UpdateLifeRegen(ref int damage)
        {
            //npc.lifeRegen = 2;

            if (AI_State == State_Asleep)
            {
                NPC.lifeRegen = NPC.lifeMax / 12;
            }
        }
        public int wooddropped = 0;
        public int resindropped = 0;
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (item.Name.Contains("Axe") || item.Name.Contains("axe") || item.Name.Contains("saw") || (item.type == ItemID.BloodLustCluster) || (item.type == ItemID.SawtoothShark) || (item.type == ItemID.Drax)
                || (item.type == ItemID.ShroomiteDiggingClaw) || item.ModItem.Name.Contains("Axe") || item.ModItem.Name.Contains("Halberd") && !item.ModItem.Name.Contains("Pick") && !item.Name.Contains("Pick"))
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                modifiers.FinalDamage *= 2; //I never want to see or hear the word "axe" again in my life
                /*if (damage < 20)
                {
                    damage = 20; //damage before defence
                }*/
                if (Main.rand.NextBool(2) && wooddropped < 5)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, ItemID.RichMahogany);
                    wooddropped++;
                }

            }

            //fire melee
            if (player.HasBuff(BuffID.WeaponImbueFire) || item.type == ModContent.ItemType<AncientFireSword>() || item.type == ModContent.ItemType<Items.Weapons.Melee.Axes.AncientFireAxe>()
                 || item.type == ModContent.ItemType<ForgottenRisingSun>() || item.type == ModContent.ItemType<MagmaTooth>()
                 || item.type == ItemID.FieryGreatsword || item.type == ItemID.MoltenHamaxe || item.type == ItemID.MoltenPickaxe || item.type == ModContent.ItemType<SunBlade>())
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                modifiers.FinalDamage *= 2;
                /*if (damage < 20)
                {
                    damage = 20; //damage before defence
                }*/
                if (Main.rand.NextBool(3) && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, ModContent.ItemType<CharcoalPineResin>());
                    resindropped++;
                }
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.LocalPlayer;

            if (projectile.DamageType != DamageClass.Melee)
            {
                modifiers.FinalDamage.Flat -= 5; //because lets face it, no one ever uprooted a tree with a bullet... A missle? Perhaps
            }
            //However... If it is a fire projectile...
            if (projectile.Name.Contains("Fire") || projectile.Name.Contains("fire") || projectile.Name.Contains("Flame") || projectile.Name.Contains("flame") || projectile.Name.Contains("Curse") ||
                projectile.Name.Contains("Flare") || projectile.Name.Contains("Molotov") || projectile.Name.Contains("Meteor") || projectile.type == ProjectileID.Hellwing || projectile.type == ModContent.ProjectileType<ScorchingPointFireball>() || projectile.type == ModContent.ProjectileType<SearingLashProjectile>() || projectile.type == ModContent.ProjectileType<DetonationSignalProjectile>() ||
                projectile.type == ProjectileID.Spark || projectile.type == ProjectileID.Cascade || projectile.type == ProjectileID.SolarWhipSword || projectile.type == ProjectileID.SolarWhipSwordExplosion ||
                projectile.type == ProjectileID.Daybreak || projectile.type == ProjectileID.DD2PhoenixBowShot ||
                (projectile.ModProjectile != null && (projectile.ModProjectile.Name.Contains("Fire") || projectile.ModProjectile.Name.Contains("Flame") || projectile.ModProjectile.Name.Contains("Explosion") || projectile.ModProjectile.Name.Contains("Meteor"))) ||
                projectile.type == ModContent.ProjectileType<DevilSickle>() || projectile.type == ModContent.ProjectileType<RedLaserBeam>() ||
                (projectile.DamageType == DamageClass.Melee && player.meleeEnchant == 3))
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                modifiers.FinalDamage *= 2;
                /*if (damage < 20)
                {
                    damage = 20; //damage before defence
                }*/
                if (Main.rand.NextBool(20) && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, ModContent.ItemType<CharcoalPineResin>());
                    resindropped++;
                }
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 7;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];

                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-3, 0);
                dust.noGravity = false;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 35; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 7, 0, Main.rand.Next(-3, 0), 0, default(Color), 1f);
                }
            }
        }

        #endregion

        public override void OnKill()
        {

            if (Main.rand.NextFloat() >= 0.2f) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>()); //80%

        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.RichMahogany, 1, 3, 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.BloodredMossClump>(), 1, 3, 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.CharcoalPineResin>(), 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.GreenBlossom>(), 5));
        }
    }
}
