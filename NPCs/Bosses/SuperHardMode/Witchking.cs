using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.BossItems;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
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
        int blackBreathDamage = 27;
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
            NPC.defense = 10;
            NPC.lifeMax = 60000;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 350000;
            NPC.knockBackResist = 0.0f;
            NPC.boss = true;
            AnimationType = NPCID.PossessedArmor;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Witchking.DespawnHandler"), Color.Purple, DustID.PurpleTorch);

            UsefulFunctions.AddAttack(NPC, 150, ModContent.ProjectileType<Projectiles.Enemy.PoisonFlames>(), 75, 8, SoundID.Item20);
            UsefulFunctions.AddAttack(NPC, 700, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 95, 0, SoundID.Item100, needsLineOfSight: false);
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
            if (NPC.justHit && Main.rand.NextBool(12))
            {
                tsorcRevampAIs.QueueTeleport(NPC, 25, true, 60);

            }
            if (NPC.justHit && NPC.Distance(player.Center) < 350 && Main.rand.NextBool(12))//
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = v;
                NPC.netUpdate = true;
            }

        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(8) && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackIndex == 0)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 70f;
                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 4f);

                NPC.netUpdate = true;

            }

            if (NPC.justHit && Main.rand.NextBool(25))
            {
                tsorcRevampAIs.QueueTeleport(NPC, 25, true, 60);
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
            }

            // charge forward code 
            if (Main.rand.NextBool(400) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                chargeDamageFlag = true;

            }
            if (chargeDamageFlag == true)
            {
                chargeTelegraphTimer++;
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1f);

                    Main.dust[pink].noGravity = true;
                }

                if (chargeTelegraphTimer >= 120 && chargeTelegraphTimer <= 130)
                {

                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 11) * -1; //7 was 11
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
                    NPC.ai[1] = 1f;

                    NPC.netUpdate = true;
                }

                if (chargeTelegraphTimer > 130)
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
                        player.AddBuff(BuffID.Slow, 60, false);
                        player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                        player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);

                    }
                    if (NPC.Distance(player.Center) < 150)
                    {
                        player.AddBuff(BuffID.Silenced, 180, false);
                        player.AddBuff(BuffID.Bleeding, 600, false);
                    }
                }
            }


            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 520 && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackIndex == 1)//SHRINKING CIRCLE DUST
            {
                UsefulFunctions.DustRing(NPC.Center, 700 - NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer, DustID.CrystalSerpent, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Blue.ToVector3() * 5);
            }

            //IF HIT BEFORE PINK DUST TELEGRAPH, RESET TIMER, BUT CHANCE TO BREAK STUN LOCK
            //(WORKS WITH 2 TELEGRAPH DUSTS, AT 60 AND 110)
            if (NPC.justHit && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer <= 109 && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackIndex == 0)
            {
                if (Main.rand.NextBool(9))
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 110;
                }
                else
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
                }
            }




            if (customAi1 >= 2000f)
            {
                if ((customspawn1 < 36) && Main.rand.NextBool(50))
                { //was 2 and 900
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.GhostOfTheDarkmoonKnight>(), 0);
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
                NPC.defense = 0;
            }
        }

        public static Texture2D texture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }
            if (!defenseBroken)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = NPC.frame.Size() / 2f;
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition + new Vector2(0, -13), NPC.frame, Color.White, NPC.rotation, origin, 1.1f * NPC.scale, effects, 0f);
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (//item.type == ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>() see artorias for an explanation
                item.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Witchking.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ModContent.ProjectileType<BarrowBladeProjectile>())
            {
                defenseBroken = true;
            }
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkMirror>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonAdventureModeRule, ModContent.ItemType<BrokenStrangeMagicRing>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WitchkingsSword>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BewitchedTitanite>(), 1, 15, 20));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CovenantOfArtorias>()));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
        }
    }
}
