using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.JungleWyvern
{
    class JungleWyvernBody3 : ModNPC
    {
        public int Timer = -1000;

        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.npcSlots = 1;
            NPC.aiStyle = 6;
            NPC.width = 45;
            NPC.height = 45;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 1750;
            NPC.damage = 35;
            NPC.defense = 2;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 91000000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 70000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ancient Jungle Wyvern");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public int PoisonFlamesDamage = 25;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {

        }
        public override void AI()
        {
            if (Timer == -1000)
            {
                Timer = -Main.rand.Next(800);
            }
            NPC.TargetClosest();
            Timer++;
            if (!Main.npc[(int)NPC.ai[1]].active)
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                for (int num36 = 0; num36 < 50; num36++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 10f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 10.0f);
                    Main.dust[dust].noGravity = true;
                    //npc.netUpdate = true; //new
                }

                NPC.active = false;
            }
            if (NPC.position.X > Main.npc[(int)NPC.ai[1]].position.X)
            {
                NPC.spriteDirection = 1;
            }
            if (NPC.position.X < Main.npc[(int)NPC.ai[1]].position.X)
            {
                NPC.spriteDirection = -1;
            }
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y + 10), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }
            if (Timer >= 0 && Main.netMode != NetmodeID.Server)
            {
                float num48 = 1f;
                Vector2 vector8 = new Vector2(NPC.position.X + (float)(NPC.width / 2), NPC.position.Y + (float)(NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f), vector8.X - (Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f));
                rotation += (float)(Main.rand.Next(-50, 50) / 100);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 400)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)(Math.Cos(rotation) * (double)num48 * -1.0), (float)(Math.Sin(rotation) * (double)num48 * -1.0), ModContent.ProjectileType<Projectiles.Enemy.PoisonFlames>(), PoisonFlamesDamage, 0f, Main.myPlayer); //enemy cursed flamess
                    }
                }
                Timer = -800 - Main.rand.Next(800);
            }
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            damage *= 2;
            base.OnHitByItem(player, item, damage, knockback, crit);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 origin = new Vector2(TextureAssets.Npc[NPC.type].Value.Width / 2, TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] / 2);
            Color alpha = Color.White;
            SpriteEffects effects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                effects = SpriteEffects.FlipHorizontally;
            }
            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, new Vector2(NPC.position.X - Main.screenPosition.X + (float)(NPC.width / 2) - (float)TextureAssets.Npc[NPC.type].Value.Width * NPC.scale / 2f + origin.X * NPC.scale, NPC.position.Y - Main.screenPosition.Y + (float)NPC.height - (float)TextureAssets.Npc[NPC.type].Value.Height * NPC.scale / (float)Main.npcFrameCount[NPC.type] + 4f + origin.Y * NPC.scale + 56f), NPC.frame, alpha, NPC.rotation, origin, NPC.scale, effects, 0f);
            NPC.alpha = 255;
            return true;
        }
    }
}
