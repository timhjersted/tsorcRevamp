using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rail;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.BossItems;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Projectiles.Melee.Shortswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Witchking : ModNPC
    {
        float customAi1;
        float customspawn1;
        bool chargeDamageFlag = false;
        float lifeThreshold = 80f;
        bool beingPulled = false;
        bool lastStand = false;
        const int TIME_BEFORE_STORMBALL = 300;
        const int TIME_BEFORE_FIREBALL = 120;
        const int TIME_BEFORE_SUMMON = 20 * 60;

        int frameCount = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PossessedArmor];
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.height = 45;
            NPC.width = 30;
            NPC.damage = 110;
            NPC.defense = 65;
            NPC.lifeMax = 170000;
            NPC.scale = 1.3f;
            NPC.HitSound = SoundID.NPCHit4; 
            NPC.DeathSound = new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream");
            NPC.value = 350000;
            NPC.knockBackResist = 0.0f;
            NPC.rarity = 32;
            NPC.boss = true;
            AnimationType = NPCID.PossessedArmor;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().NavSearchRadius = 100; // Phase 2: SmartFighter4AI movement (boss)
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Witchking.DespawnHandler"), Color.Purple, DustID.PurpleTorch);

            // Allows the witchking to properly spawn in the arena
            NPC.lavaImmune = true;

            UsefulFunctions.AddAttack(NPC, TIME_BEFORE_FIREBALL, ModContent.ProjectileType<Projectiles.Enemy.PoisonFlames>(), 75, 8, SoundID.Item20);
            UsefulFunctions.AddAttack(NPC, TIME_BEFORE_STORMBALL, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 95, 0, SoundID.Item100, needsLineOfSight: false);
        }

        int chargeTelegraphTimer = 0;
        bool defenseBroken = false;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Weak, 120 * 60);
            target.AddBuff(BuffID.Bleeding, 20 * 60);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 137 * 60);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // Only apply this effect around every 12 hits
            if (!NPC.justHit || !Main.rand.NextBool(12))
                return;
            
            // If the player is close enough, get knocked back
            if (NPC.Distance(player.Center) < 200)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); // was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = v;
                NPC.netUpdate = true;
            }
            else
            {
                tsorcRevampAIs.QueueTeleport(NPC, 25, true, 60);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            // Only apply this effect around every 20 hits
            if (!NPC.justHit || !Main.rand.NextBool(20))
                return;

            
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackIndex != 0)
            {
                tsorcRevampAIs.QueueTeleport(NPC, 25, true, 60);
            }
            else // Get knocked back
            {
                float projTimer = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer;
                // Reduce the time before the next fireball by 70f
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = Math.Min(projTimer + 70f, TIME_BEFORE_FIREBALL - 5f);
                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 4f);

                NPC.netUpdate = true;
            }
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 0.8f, canTeleport: false, enragePercent: 0.5f, enrageTopSpeed: 1.6f);

            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;

                // If the Witchking's life drops below 10%, his shields go back up again.
                if (!lastStand && NPC.lifeMax > NPC.life * 10 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    defenseBroken = false;
                    lastStand = true;

                    NPC.DelBuff(NPC.FindBuffIndex(ModContent.BuffType<Buffs.DispelShadow>()));
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Witchking.Restore"), Color.Purple);
                }
            }

            // SCREAM BABY!!!
            if (lifeThreshold > 0f && NPC.life < (int)((float)NPC.lifeMax * lifeThreshold / 100f))
            {
                lifeThreshold -= 20f;

                if (lifeThreshold == 0f)
                {
                    lifeThreshold = 10f;
                }

                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream"), NPC.Center);

                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    int dustIndex = Dust.NewDust(NPC.Center, 0, 0, 114, dustSpeed.X, dustSpeed.Y, 0, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                }
                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    int dustIndex = Dust.NewDust(NPC.Center, 0, 0, 130, dustSpeed.X, dustSpeed.Y, 0, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                } 
                    Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    NPC.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(),
                    0,
                    0,
                    Main.myPlayer,
                    550, // ai0
                    20  // ai1
                );
                    Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    NPC.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(),
                    0,
                    0,
                    Main.myPlayer,
                    520, // ai0
                    60   // ai1
                );

                beingPulled = true;
                frameCount = 0;
            }

            if (beingPulled)
            {
                beingPulled = Pull();
            }

            // Storm Ball 
            if (Main.rand.NextBool(350) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                chargeDamageFlag = true;
            }

            // SHRINKING CIRCLE DUST
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= TIME_BEFORE_STORMBALL - 120 && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackIndex == 1)
            {
                UsefulFunctions.DustRing(NPC.Center, TIME_BEFORE_STORMBALL - NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer, DustID.CrystalSerpent, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Blue.ToVector3() * 5);
            }

            // White flash before the stormball
            if (chargeDamageFlag == true)
            {
                chargeTelegraphTimer++;
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); // Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1f);

                    Main.dust[pink].noGravity = true;
                }

                if (chargeTelegraphTimer >= 80 && chargeTelegraphTimer <= 90)
                {

                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 11) * -1; //7 was 11
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
                    NPC.ai[1] = 1f;

                    NPC.netUpdate = true;
                }
                if (chargeTelegraphTimer > 90)
                {
                    chargeDamageFlag = false;
                    chargeTelegraphTimer = 0;
                }
            }


            customAi1++;

            //Proximity Debuffs
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Player player = Main.player[i];

                    if (NPC.Distance(player.Center) < 600)
                    {
                        player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 20, false);
                        player.AddBuff(BuffID.Slow, 20, false);
                    }
                    if (NPC.Distance(player.Center) < 200)
                    {
                        player.AddBuff(BuffID.Silenced, 20, false);
                        player.AddBuff(BuffID.Bleeding, 600, false);
                        player.AddBuff(ModContent.BuffType<TornWings>(), 20, false);
                    }
                }
            }

            if (customAi1 >= TIME_BEFORE_SUMMON)
            {
                if ((customspawn1 < 36) && Main.rand.NextBool(20 + (int)lifeThreshold / 2))
                { //was 2 and 900
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.GhostFighter.GhostOfTheDarkmoonKnight>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    NPC.ai[0] = 20 - Main.rand.Next(180); //was 80
                    customspawn1 += 1f;
                    customAi1 = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }

        public bool Pull()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                // Skip over invalid players
                if (!Main.player[i].active || Main.player[i].dead)
                {
                    continue;
                }

                Player player = Main.player[i];

                // A positive xDiff = the player is to the right of the witchking
                float xDiff = player.Center.X - NPC.Center.X;

                // A negative yDiff = the player is above the witchking
                float yDiff = player.Center.Y - NPC.Center.Y;

                /* // Debug code
                if (frameCount % 60 == 0)
                {
                    Main.NewText("Pullx = " + string.Format("{0:N3}", -1f * xDiff) + "yDiff = " + string.Format("{0:N3}", -1f * yDiff), Color.White);
                }
                */

                float xSign = xDiff > 0 ? 1 : -1;
                float ySign = yDiff > 0 ? 1 : -1;

                // Will range from 1 - 5 in practice, 5 being the lowest threshold before the witchking dies
                float strength = (100f - lifeThreshold) / 20f;



                // The pull gets stronger as the witchking is lower on life and the player is further away
                player.velocity.X -= (xDiff / 4000f + strength * .050f * xSign) * (1 + strength * .115f);

                // Only apply a Y velocity if the player is currently midair - prevents no jump bug along with torn wings
                if (player.velocity.Y != 0f)
                {
                    player.velocity.Y -= yDiff / 800f + strength * .03f * ySign;
                }

                // Creates the player visual for the pull
                player.AddBuff(ModContent.BuffType<WitchkingsGrasp>(), 5, false);
                player.GetModPlayer<tsorcRevampPlayer>().PullStrength = (int)strength;

                // Creates pull effect of dust flying towards the witchking
                Rectangle playerRect = new Rectangle(0, 0, 20, 45);

                // More dust at lower life thresholds to represent the stronger pull
                for (int x = 0; x < (int)strength; x++)
                {
                    Vector2 offset = Main.rand.NextVector2FromRectangle(playerRect);
                    Vector2 dustPosition = player.position + offset;

                    Vector2 velocity = new Vector2(10f, 0).RotatedBy(dustPosition.AngleTo(NPC.Center)) * Main.rand.NextFloat(1f, Math.Abs(xDiff) / 45f);

                    // Moves the dust off of the player before it visually spawns
                    dustPosition.X += velocity.X;
                    dustPosition.Y += velocity.Y;

                    Dust.NewDustPerfect(dustPosition, DustID.ShadowbeamStaff, velocity, Scale: Main.rand.NextFloat(0.8f, 1.5f)).noGravity = true;
                }
            }

            frameCount++;

            const int pullTimeInSeconds = 30;

            // The witchking's final pull does not give way until death.
            return frameCount < pullTimeInSeconds * 60 || lastStand;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }
            if (defenseBroken)
            {
                writer.Write(true);
            }
            else
            {
                writer.Write(false);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool recievedBrokenDef = reader.ReadBoolean();
            if (recievedBrokenDef == true)
            {
                defenseBroken = true;
                // NPC.defense = 0; // This line of code appears to cause a net exclusive bug where the witchking has no defense if their barrier is dispelled.
            }
        }

        public static Texture2D texture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }
            if (!defenseBroken || lastStand)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = NPC.frame.Size() / 2f;
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition + new Vector2(5, -35), NPC.frame, Color.White, NPC.rotation, origin, 1.25f * NPC.scale, effects, 0f);
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (!defenseBroken)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Witchking.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!defenseBroken)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Witchking.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.3f, 0.3f, 200, default(Color), 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.WitchkingBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeSapphire));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonAdventureModeRule, ModContent.ItemType<BrokenDarkMagicRing>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WitchkingsSword>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WitchkingMace>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BewitchedTitanite>(), 1, 15, 20));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CovenantOfArtorias>()));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
        }
    }
}
