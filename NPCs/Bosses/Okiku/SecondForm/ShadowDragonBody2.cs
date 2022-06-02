using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
    public class ShadowDragonBody2 : ModNPC
    {
        public int Timer = -1000;

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.aiStyle = 6;
            NPC.damage = 80;
            NPC.defense = 20;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 91000000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.knockBackResist = 0f;
            NPC.dontCountMe = true;
            DrawOffsetY = 50;
            bodyTypes = new int[] {
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody2>(), ModContent.NPCType<ShadowDragonBody3>()
            };
        }
        public static int[] bodyTypes;


        public override void SetStaticDefaults()
        {
            base.DisplayName.SetDefault("Shadow Dragon");
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        int ObscureSawDamage = 45;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            ObscureSawDamage = ObscureSawDamage / 2;
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<ShadowDragonHead>(), bodyTypes, ModContent.NPCType<ShadowDragonTail>(), 25, 0.8f, 16f, 0.33f, true, false, true, false, false);

            if (Main.rand.Next(3) == 0)
            {
                int dust = Dust.NewDust(new Vector2(base.NPC.position.X, base.NPC.position.Y), base.NPC.width, base.NPC.height, 62, 0f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Timer == -1000)
            {
                Timer = -Main.rand.Next(800);
            }
            Timer++;
            if (Timer >= 0)
            {
                float speed = 1f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + Main.player[NPC.target].height * 0.5f), vector8.X - (Main.player[NPC.target].position.X + Main.player[NPC.target].width * 0.5f));
                rotation += (Main.rand.Next(-50, 50) / 100);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)(Math.Cos(rotation) * speed * -1.0), (float)(Math.Sin(rotation) * speed * -1.0), Mod.GetProjectile("ObscureSaw").projectile.type, ObscureSawDamage, 0f, Main.myPlayer);
                }
                Timer = -300 - Main.rand.Next(300);
            }
        }
    }
}
