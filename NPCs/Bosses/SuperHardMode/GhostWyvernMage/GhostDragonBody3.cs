using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;

class GhostDragonBody3 : ModNPC
{
    public override void SetDefaults()
    {
        NPC.netAlways = true;
        NPC.npcSlots = 1;
        NPC.width = 45;
        NPC.height = 45;
        DrawOffsetY = GhostDragonHead.drawOffset;
        NPC.aiStyle = 6;
        NPC.knockBackResist = 0;
        NPC.timeLeft = 750;
        NPC.damage = 0;
        NPC.defense = 25;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath10;
        NPC.lifeMax = 35000;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.value = 10000;
        NPC.alpha = 100;
        NPC.buffImmune[BuffID.Poisoned] = true;
        NPC.buffImmune[BuffID.Confused] = true;
        NPC.buffImmune[BuffID.OnFire] = true;
        NPC.buffImmune[BuffID.CursedInferno] = true;
    }

    int fireDamage = 50;
    public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
    {
        NPC.damage = (int)(NPC.damage / 2);
        fireDamage = (int)(fireDamage / 2);
    }
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Ghost Wyvern");
        NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
    }
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }
    int Timer = -Main.rand.Next(800);
    int Timer2 = -Main.rand.Next(200);

    public override bool CheckActive()
    {
        return false;
    }
    public override void AI()
    {

        NPC.noTileCollide = true;
        NPC.noGravity = true;
        NPC.behindTiles = true;
        int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };
        tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, -2f, 15f, 0.23f, true, false);

        Timer++;
        Timer2++;

        if (!Main.npc[(int)NPC.ai[1]].active)
        {
            NPC.life = 0;
            for (int num36 = 0; num36 < 50; num36++)

                NPC.HitEffect(0, 10.0);
            NPC.active = false;
        }
        
        if (Timer >= 0)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float num48 = 1f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                rotation += Main.rand.Next(-50, 50) / 100;
                //int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ProjectileID.VortexLightning, fireDamage, 0f, Main.myPlayer); //ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>()
                int num55 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(), fireDamage, 0f, Main.myPlayer); //ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>()
                Timer = -6000;
            }
        }
    }

    public static Texture2D texture;
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        GhostDragonHead.GhostEffect(NPC, spriteBatch, ref texture, 1.5f);
        return true;
    }
    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        //GhostDragonHead.GhostEffect(npc, spriteBatch, ref texture);
    }
}