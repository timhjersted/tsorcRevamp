using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    //[AutoloadBossHead]
    class TheHunterChild : ModNPC
    {
        int sproutDamage = 33;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 6000;
            NPC.damage = 53;
            NPC.defense = 36;
            NPC.knockBackResist = 0f;
            NPC.value = 31500;
            NPC.npcSlots = 1;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            DrawOffsetY = +70;
            NPC.width = 50;
            NPC.height = 30;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.TheHunterChild.DespawnHandler"), Color.Green, 89);
        }

        int hitTime = 0;
        public float flapWings;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Bleeding, 30 * 60, false);
        }
        public float EnrageDamageCounter
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float SprouterShotTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public float AttackPhaseTimer
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        public float EnrageTimer
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            AttackPhaseTimer++;
            SprouterShotTimer++;
            hitTime++;
            if (EnrageDamageCounter > 0) EnrageDamageCounter -= hitTime / 10;


            // Dusts get bigger as damage taken increases
            Player player = Main.player[NPC.target];
            if (player.detectCreature)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 89, NPC.velocity.X, NPC.velocity.Y, 200, default, 0.01f + (5.5f * (EnrageDamageCounter / (NPC.lifeMax / 10)))); //.1 was 0.5f
                Main.dust[dust].noGravity = true;
            }

            // Flap Wings
            flapWings++;
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.7f }, NPC.position); //wing flap sound
            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.8f }, NPC.position);
                flapWings = 0;
            }

            // Getting close triggers hunter vision
            if (NPC.Distance(player.Center) < 80)
            {
                player.AddBuff(BuffID.Hunter, 60, false);
            }

            bool hunterAlive = NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheHunter>());


            if (!hunterAlive)
            {
                if (NPC.life > 500)
                { NPC.life = 500; }
            }

            if (EnrageTimer == 0)
            {
                // Normal Phase
                NPC.defense = 26;
                if (AttackPhaseTimer < 600)
                {
                    if (Main.player[NPC.target].position.X < NPC.Center.X)
                    {
                        if (NPC.velocity.X > -8) { NPC.velocity.X -= 0.22f; }
                    }
                    if (Main.player[NPC.target].position.X > NPC.Center.X)
                    {
                        if (NPC.velocity.X < 8) { NPC.velocity.X += 0.22f; }
                    }

                    if (Main.player[NPC.target].position.Y < NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].position.Y > NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }

                    if (SprouterShotTimer >= 0 && AttackPhaseTimer > 120 && AttackPhaseTimer < 600)
                    {
                        int type = ModContent.ProjectileType<MiracleSprouter>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, NPC.Center);
                        Vector2 aimVector = UsefulFunctions.Aim(NPC.Center, player.Center, 3);
                        Vector2 fireOriginOffset = new Vector2(0, -70);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector + player.velocity, type, sproutDamage, 0, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(-0.4) + player.velocity, type, sproutDamage, 0, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(0.4) + player.velocity, type, sproutDamage, 0, Main.myPlayer);
                        }
                        SprouterShotTimer = -90;
                        NPC.netUpdate = true;
                    }
                    NPC.netUpdate = true;
                }
                else if (AttackPhaseTimer >= 600 && AttackPhaseTimer < 950)
                {
                    // Then chill for a few seconds.
                    // This exists to delay switching to the 'charging' pattern for 150 frames, because otherwise the way the sprouters linger can often make the first charge impossible to dodge
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 131, Main.rand.Next(-1, 1), Main.rand.Next(-10, 1), 200, default, 0.5f);
                }
                else if (AttackPhaseTimer >= 950 && AttackPhaseTimer < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((NPC.Center.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (NPC.Center.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = ((float)(Math.Cos(rotation) * 25) * -1) + Main.player[NPC.target].velocity.X;
                        NPC.velocity.Y = ((float)(Math.Sin(rotation) * 25) * -1) + Main.player[NPC.target].velocity.Y;
                    }
                }
                else AttackPhaseTimer = 0;
            }
            else
            {
                // Enrage phase
                EnrageTimer++;
                NPC.defense = 66;

                if (Main.player[NPC.target].position.X < NPC.Center.X)
                {
                    if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                }
                if (Main.player[NPC.target].position.X > NPC.Center.X)
                {
                    if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                }
                if (Main.player[NPC.target].position.Y < NPC.Center.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                    else NPC.velocity.Y -= 0.07f;
                }
                if (Main.player[NPC.target].position.Y > NPC.Center.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                    else NPC.velocity.Y += 0.07f;
                }
                if (SprouterShotTimer >= 0 && AttackPhaseTimer > 120 && AttackPhaseTimer < 600)
                {
                    float invulnDamageMult = 1.72f; //+0.20 for all birds
                    int type = ModContent.ProjectileType<MiracleSprouter>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    Vector2 aimVector = UsefulFunctions.Aim(NPC.Center, player.Center, 15);
                    Vector2 fireOriginOffset = new Vector2(0, -70);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 35);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(-0.4) + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 40);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(0.4) + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 35);
                    }
                    SprouterShotTimer = -90;
                    NPC.netUpdate = true;
                    SprouterShotTimer = -90;
                }
                if (EnrageTimer == 100)
                {
                    if (EnrageTimer == 100)
                    {
                        EnrageTimer = 1;
                    }

                    if (NPC.life > NPC.lifeMax)
                    {
                        NPC.life = NPC.lifeMax;
                    }
                }
                if (SprouterShotTimer >= 0)
                {
                    EnrageTimer = 0;
                    for (int i = 0; i < 40; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 0, default, 0.3f);//was 3f
                    }
                }
            }
        }
        public override void FindFrame(int currentFrame)
        {
            if (Main.dedServ)
            {
                return;
            }
            Player player = Main.player[NPC.target];

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (EnrageTimer == 0)
            {
                NPC.alpha = 0;

                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 0.5f);
            }
            else
            {
                if (Main.LocalPlayer.detectCreature)
                {
                    NPC.alpha = 0;
                }
                else
                {
                    NPC.alpha = 255;
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            }
        }



        public static Texture2D blurTexture;
        public static Texture2D glowmaskTexture;
        public static Effect hunterEffect;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);
            UsefulFunctions.EnsureLoaded(ref blurTexture, "tsorcRevamp/NPCs/Bosses/TheHunterChildBlur");
            UsefulFunctions.EnsureLoaded(ref glowmaskTexture, "tsorcRevamp/NPCs/Bosses/TheHunterChildGlowmask");

            if (hunterEffect == null || hunterEffect.IsDisposed)
            {
                hunterEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HunterEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            hunterEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            hunterEffect.Parameters["length"].SetValue(.07f * 1000);

            hunterEffect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            hunterEffect.Parameters["sourceRectY"].SetValue(NPC.frame.Y);

            float opacity = EnrageDamageCounter / (NPC.lifeMax / 10f) * 0.8f;
            if (EnrageTimer != 0)
            {
                opacity = 1;
            }

            hunterEffect.Parameters["opacity"].SetValue(opacity);
            hunterEffect.CurrentTechnique.Passes[0].Apply();

            //Only draw the bird if the player has a hunter potion
            if (Main.LocalPlayer.detectCreature)
            {
                Main.spriteBatch.Draw(blurTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                Main.spriteBatch.Draw(glowmaskTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            EnrageDamageCounter += hit.Damage;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            EnrageDamageCounter += hit.Damage;
        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            hitTime = 0;
            if (EnrageDamageCounter > (NPC.lifeMax / 10))
            {
                EnrageTimer = 1;
                Color color = new Color();
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, color, 0.5f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, color, 0.5f);
                }
                SprouterShotTimer = -200;
                EnrageDamageCounter = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 800, 50);
                }
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void OnKill()
        {
            for (int i = 0; i < 100; i++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 89, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 100; i++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 131, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.DarkGreen, 3f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 2, UsefulFunctions.ColorToFloat(Color.Green));
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                if (NPC.life <= 0)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Child_Gore_1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Child_Gore_2").Type, 1f);
                }
            }
        }
    }
}
