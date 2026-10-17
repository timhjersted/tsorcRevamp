using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class AbyssLurker : ModNPC, IHitReactor
    {
        const int MaxFlameOrbs = 3;
        const int FlameOrbSpawnRate = 3 * 60;
        const int FlameTrapRate = 10 * 60;
        const int ReleasedFlameOrbTime = 3 * 60;
        const float CombatRange = 50 * 16f;

        public int lostSoulDamage = 40;
        int flameOrbTimer;
        int flameTrapTimer;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<AbyssInferno>()] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;//was 3
            NPC.damage = 70;
            NPC.defense = 90;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 1600;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 6000; // was 43 for 100 life
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FireLurkerBanner>();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60; // Phase 2: SmartFighter4AI movement
            globalNPC.KiteRangeMin = 10f;
            globalNPC.KiteRangeMax = 25f;
            globalNPC.KiteLooseness = 0.5f;
            globalNPC.TeleportReacquireMaxRange = CombatRange;
            UsefulFunctions.AddAttack(NPC, 4 * 60, ModContent.ProjectileType<AbyssLurkerFlameOrb>(), lostSoulDamage, 2.4f, SoundID.Item20 with { Volume = 0.35f, Pitch = -0.2f }, 0, -1, -ReleasedFlameOrbTime, telegraphColor: Color.Purple, telegraphTime: 22, condition: HasCombatTarget, fireCondition: HasCombatTarget);
        }



        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;
            // P.townNPCs > 0f // is no town NPCs nearby

            if (spawnInfo.Invasion)
            {
                return 0;
            }

            if (spawnInfo.Water) return 0f;

            if (tsorcRevampWorld.SuperHardMode && Corruption && Main.rand.NextBool(10)) return 1;

            if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.NextBool(5)) return 1; //8 is 3%, 5 is 5, 3 IS 3%???
            return 0;
        }
        #endregion

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int previousTarget = NPC.target;
                NPC.TargetClosest(true);
                if (NPC.target != previousTarget)
                {
                    NPC.netUpdate = true;
                }
            }
            tsorcRevampAIs.FighterAI(NPC, 1.9f, 0.08f, canTeleport: true, randomSound: SoundID.Mummy, soundFrequency: 900, enragePercent: 0.4f, enrageTopSpeed: 4f, lavaJumping: true); //sound type was 26

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (!HasCombatTarget(NPC))
            {
                flameOrbTimer = 0;
                flameTrapTimer = 0;
                return;
            }

            SpawnFlameOrbs();
            SpawnFlameTrap();
        }

        static bool HasCombatTarget(NPC npc)
        {
            if (npc.target < 0 || npc.target >= Main.maxPlayers)
            {
                return false;
            }

            Player target = Main.player[npc.target];
            return target.active && !target.dead && npc.Distance(target.Center) <= CombatRange;
        }

        void SpawnFlameOrbs()
        {
            flameOrbTimer++;
            if (flameOrbTimer < FlameOrbSpawnRate)
            {
                return;
            }
            flameOrbTimer = 0;

            int slot = GetOpenFlameOrbSlot();
            if (slot == -1)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Top + new Vector2(0, -56), Vector2.Zero, ModContent.ProjectileType<AbyssLurkerFlameOrb>(), lostSoulDamage, 0, Main.myPlayer, NPC.whoAmI, slot);
        }

        int GetOpenFlameOrbSlot()
        {
            bool[] occupiedSlots = new bool[MaxFlameOrbs];
            int activeOrbs = 0;
            int orbType = ModContent.ProjectileType<AbyssLurkerFlameOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != orbType || (int)projectile.ai[0] != NPC.whoAmI)
                {
                    continue;
                }

                activeOrbs++;
                if (projectile.ai[1] >= 0)
                {
                    int slot = (int)projectile.ai[1];
                    if (slot >= 0 && slot < MaxFlameOrbs)
                    {
                        occupiedSlots[slot] = true;
                    }
                }
            }

            if (activeOrbs >= MaxFlameOrbs)
            {
                return -1;
            }

            for (int i = 0; i < MaxFlameOrbs; i++)
            {
                if (!occupiedSlots[i])
                {
                    return i;
                }
            }

            return -1;
        }

        void SpawnFlameTrap()
        {
            flameTrapTimer++;
            if (flameTrapTimer < FlameTrapRate)
            {
                return;
            }
            flameTrapTimer = 0;

            if (!HasCombatTarget(NPC))
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 flameTrapPosition = target.Center - new Vector2(target.direction * 62f, 0);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), flameTrapPosition, Vector2.Zero, ModContent.ProjectileType<AbyssLurkerMeteor>(), NPC.damage / 2, 0, Main.myPlayer, lostSoulDamage);
        }

        void ReleaseFlameOrbs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.TargetClosest(true);
            if (!HasCombatTarget(NPC))
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int orbType = ModContent.ProjectileType<AbyssLurkerFlameOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != orbType || (int)projectile.ai[0] != NPC.whoAmI || projectile.ai[1] < 0)
                {
                    continue;
                }

                projectile.ai[1] = -3 * 60;
                projectile.velocity = UsefulFunctions.Aim(projectile.Center, target.Center, 1.4f);
                projectile.timeLeft = 3 * 60;
                projectile.netUpdate = true;
            }
        }

        #region Find Frame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }

        #endregion

        #region Debuffs
        // Every Abyss Lurker hit drags the player into the Abyss (the Abyss debuff sets EnterTheAbyss) for this long.
        // Shared by contact, AbyssLurkerFlameOrb, AbyssLurkerMeteor and the death Lost Soul (tsorcGlobalProjectile).
        public const int AbyssDebuffTicks = 30 * 60;
        // Abyssal Inferno on every hit, same set as above. 3s; it's a LongerExpertDebuff, so Expert/Master make it 6s.
        public const int AbyssInfernoTicks = 3 * 60;
        // Contact only, guaranteed. Vanilla doubles both in Expert/Master (LongerExpertDebuff).
        private const int ContactBrokenArmorTicks = 3 * 60;
        private const int ContactDarknessTicks = 3 * 60;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), AbyssInfernoTicks, false);
            target.AddBuff(ModContent.BuffType<Abyss>(), AbyssDebuffTicks, false);
            target.AddBuff(BuffID.BrokenArmor, ContactBrokenArmorTicks, false);
            target.AddBuff(BuffID.Darkness, ContactDarknessTicks, false);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, projectile.DamageType == DamageClass.Melee);
        }

        void IHitReactor.OnServerHit(NPC npc, bool melee) => ReleaseFlameOrbs();
        #endregion

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore3").Type, 1.1f);
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                if (target.active && !target.dead)
                {
                    Vector2 lostSoulVelocity = UsefulFunctions.Aim(NPC.Center, target.Center, 6f);
                    Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, lostSoulVelocity, ProjectileID.LostSoulHostile, lostSoulDamage, 0, Main.myPlayer);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 12));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 100, 1, 1, 30));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 16));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2);
            SuperHardmodeRule SHM = new();
            IItemDropRule condition = new LeadingConditionRule(SHM);
            condition.OnSuccess(drop);
            npcLoot.Add(condition);
        }
    }
}
