using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Witchking : ModNPC
    {
        float customAi1;
        float customspawn1;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = 3;
            NPC.height = 45;
            NPC.width = 30;
            NPC.damage = 150;
            NPC.defense = 10;
            NPC.lifeMax = 60000;
            NPC.scale = 1.05f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 350000;
            NPC.knockBackResist = 0.001f;
            NPC.boss = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            AnimationType = NPCID.PossessedArmor;
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PossessedArmor];
            despawnHandler = new NPCDespawnHandler("The Witchking claims another victim...", Color.Purple, DustID.PurpleTorch);
        }

        int blackBreathDamage = 53;
        bool defenseBroken = false;


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            blackBreathDamage = (int)(blackBreathDamage / 2);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 7200);
                target.AddBuff(BuffID.Bleeding, 1200);
                target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 8200);
            }
        }



        #region AI
        #region Movement
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }

            bool flag2 = false;
            int num5 = 60;
            bool flag3 = true;
            if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0))
            {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X -= 2f;
            }
            else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0))
            {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X += 2f;
            }
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
            {
                flag2 = true;
            }
            if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= (float)num5 || flag2)
            {
                NPC.ai[3] += 1f;
            }
            else
            {
                if ((double)Math.Abs(NPC.velocity.X) > 0.9 && NPC.ai[3] > 0f)
                {
                    NPC.ai[3] -= 1f;
                }
            }
            if (NPC.ai[3] > (float)(num5 * 10))
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.justHit)
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.ai[3] == (float)num5)
            {
                NPC.netUpdate = true;
            }
            else
            {
                if (NPC.velocity.X == 0f)
                {
                    if (NPC.velocity.Y == 0f)
                    {
                        NPC.ai[0] += 1f;
                        if (NPC.ai[0] >= 2f)
                        {
                            NPC.direction *= -1;
                            NPC.spriteDirection = NPC.direction;
                            NPC.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    NPC.ai[0] = 0f;
                }
                if (NPC.direction == 0)
                {
                    NPC.direction = 1;
                }
            }
            if (NPC.velocity.X < -1.5f || NPC.velocity.X > 1.5f)
            {
                if (NPC.velocity.Y == 0f)
                {
                    NPC.velocity *= 0.8f;
                }
            }
            else
            {
                if (NPC.velocity.X < 1.5f && NPC.direction == 1)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.07f;
                    if (NPC.velocity.X > 1.5f)
                    {
                        NPC.velocity.X = 1.5f;
                    }
                }
                else
                {
                    if (NPC.velocity.X > -1.5f && NPC.direction == -1)
                    {
                        NPC.velocity.X = NPC.velocity.X - 0.07f;
                        if (NPC.velocity.X < -1.5f)
                        {
                            NPC.velocity.X = -1.5f;
                        }
                    }
                }
            }
            bool flag4 = false;
            if (NPC.velocity.Y == 0f)
            {
                int num29 = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                int num30 = (int)NPC.position.X / 16;
                int num31 = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = num30; l <= num31; l++)
                {
                    if (Main.tile[l, num29] == null)
                    {
                        return;
                    }
                    if (Main.tile[l, num29].HasTile && Main.tileSolid[(int)Main.tile[l, num29].TileType])
                    {
                        flag4 = true;
                        break;
                    }
                }
            }
            if (flag4)
            {
                int num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
                int num33 = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);
                if (Main.tile[num32, num33] == null)
                {
                    Main.tile[num32, num33].ClearTile();
                }
                if (Main.tile[num32, num33 - 1] == null)
                {
                    Main.tile[num32, num33 - 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 2] == null)
                {
                    Main.tile[num32, num33 - 2].ClearTile();
                }
                if (Main.tile[num32, num33 - 3] == null)
                {
                    Main.tile[num32, num33 - 3].ClearTile();
                }
                if (Main.tile[num32, num33 + 1] == null)
                {
                    Main.tile[num32, num33 + 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 - 1] == null)
                {
                    Main.tile[num32 + NPC.direction, num33 - 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 + 1] == null)
                {
                    Main.tile[num32 + NPC.direction, num33 + 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 1].HasTile && Main.tile[num32, num33 - 1].TileType == 10 && flag3)
                {
                    NPC.ai[2] += 1f;
                    NPC.ai[3] = 0f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.velocity.X = 0.5f * (float)(-(float)NPC.direction);
                        NPC.ai[1] += 1f;
                        NPC.ai[2] = 0f;
                        bool flag5 = false;
                        if (NPC.ai[1] >= 10f)
                        {
                            flag5 = true;
                            NPC.ai[1] = 10f;
                        }
                        WorldGen.KillTile(num32, num33 - 1, true, false, false);
                        if ((Main.netMode != NetmodeID.MultiplayerClient || !flag5) && flag5 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.type == NPCID.GoblinPeon)
                            {
                                WorldGen.KillTile(num32, num33 - 1, false, false, false);
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
                                }
                            }
                            else
                            {
                                bool flag6 = WorldGen.OpenDoor(num32, num33, NPC.direction);
                                if (!flag6)
                                {
                                    NPC.ai[3] = (float)num5;
                                    NPC.netUpdate = true;
                                }
                                if (Main.netMode == NetmodeID.Server && flag6)
                                {
                                    NetMessage.SendData(MessageID.ChangeDoor, -1, -1, null, 0, (float)num32, (float)num33, (float)NPC.direction, 0);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                    {
                        if (Main.tile[num32, num33 - 2].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 2].TileType])
                        {
                            if (Main.tile[num32, num33 - 3].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 3].TileType])
                            {
                                NPC.velocity.Y = -8f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                NPC.velocity.Y = -7f;
                                NPC.netUpdate = true;
                            }
                        }
                        else
                        {
                            if (Main.tile[num32, num33 - 1].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 1].TileType])
                            {
                                NPC.velocity.Y = -6f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                if (Main.tile[num32, num33].HasTile && Main.tileSolid[(int)Main.tile[num32, num33].TileType])
                                {
                                    NPC.velocity.Y = -5f;
                                    NPC.netUpdate = true;
                                }
                                else
                                {
                                    if (NPC.directionY < 0 && (!Main.tile[num32, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].TileType]) && (!Main.tile[num32 + NPC.direction, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32 + NPC.direction, num33 + 1].TileType]))
                                    {
                                        NPC.velocity.Y = -8f;
                                        NPC.velocity.X = NPC.velocity.X * 1.5f;
                                        NPC.netUpdate = true;
                                    }
                                    else
                                    {
                                        if (flag3)
                                        {
                                            NPC.ai[1] = 0f;
                                            NPC.ai[2] = 0f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (flag3)
                {
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                }
            }
            #endregion
            #region Charge
            //if(Main.netMode != 1)
            //{
            if (Main.rand.NextBool(300))
            {
                chargeDamageFlag = true;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                NPC.ai[1] = 1f;
                NPC.netUpdate = true;
            }
            if (chargeDamageFlag == true)
            {
                NPC.damage = 150;
                chargeDamage++;
            }
            if (chargeDamage >= 101)
            {
                chargeDamageFlag = false;
                NPC.damage = 100;
                chargeDamage = 0;
            }
            #endregion
            #region Projectiles
            customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;

            //Proximity Debuffs
            Player player = Main.player[NPC.target];
            if (NPC.Distance(player.Center) < 600)
            {
                player.AddBuff(BuffID.Slow, 60, false); 
                player.AddBuff(ModContent.BuffType<Buffs.TornWings>(), 60, false);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60, false);
               
            }
            if (NPC.Distance(player.Center) < 150)
            {
                player.AddBuff(BuffID.Silenced, 180, false);
                player.AddBuff(BuffID.Bleeding, 180, false);
            }


            if (customAi1 >= 10f)
            {
                if ((customspawn1 < 36) && Main.rand.NextBool(800))
                { //was 2 and 900
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.GhostOfTheDarkmoonKnight>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    NPC.ai[0] = 20 - Main.rand.Next(180); //was 80
                    customspawn1 += 1f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }



                if (Main.rand.NextBool(65))
                {
                    float num48 = 8f; //was 8f
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.BlackBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blackBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 40;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }
            }
            //}

            #region Phase Through Walls
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
            {
                NPC.noTileCollide = false;
                NPC.noGravity = false;
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                NPC.velocity.Y = 0f;
                if (NPC.position.Y > Main.player[NPC.target].position.Y)
                {
                    NPC.velocity.Y -= 3f;
                }
                if (NPC.position.Y < Main.player[NPC.target].position.Y)
                {
                    NPC.velocity.Y += 8f;
                }
            }
            #endregion           
        }
        #endregion
        #endregion
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
                Vector2 offset = new Vector2(16, 0);
                spriteBatch.Draw(texture,NPC.position - Main.screenPosition + offset, NPC.frame, Color.White, NPC.rotation, origin, 1.1f, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (//item.type == ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>() see artorias for an explanation
                item.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Immune!", true, false);
                damage = 1;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.type == ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Immune!", true, false);
                damage = 1;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.WitchkingBag>()));
        }
        public override void OnKill()
        {
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<BrokenStrangeMagicRing>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Summon.WitchkingHelmet>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Summon.WitchkingTop>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Summon.WitchkingBottoms>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<GuardianSoul>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BossItems.DarkMirror>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.CovenantOfArtorias>(), 1, false, -1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 2500);
            }            
        }
    }
}
