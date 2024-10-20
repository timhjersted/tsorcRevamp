using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    public class AttraidiesShield : ModNPC
    {
        private bool initiate;

        public int TimerHeal;

        public float TimerAnim;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }
        public override void SetDefaults()
        {
            NPC.alpha = 50;
            NPC.width = 50;
            NPC.height = 50;
            NPC.aiStyle = -1;
            NPC.damage = 40;
            NPC.defense = 18;
            NPC.noGravity = true;
            NPC.boss = false;
            NPC.noTileCollide = true;
            NPC.lifeMax = 17250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(54);
        }


        public int ObscureShotDamage = 30;

        public NPC Attraidies
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public DarkShogunMask AttraidiesMask
        {
            get => Attraidies.ModNPC as DarkShogunMask;
        }
        public float ImmuneTimer
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        public int ShotTimer;

        List<float> foundIndicies = new List<float>();
        float RotSpeed = 0.015f;
        bool RotDir = false;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.Center = Main.npc[(int)NPC.ai[1]].Center;
            NPC.dontTakeDamage = true;
        }
                

        public override bool CheckDead()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);
            }
            return true;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale *= 1.5f;
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }
    }
}
