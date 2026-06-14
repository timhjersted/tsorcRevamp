using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon
{
    class HellkiteDragonTail : ModNPC
    {
        int fireDamage = 30;
        private int InfernoBlastCounter = 0;
        private int InfernoBlastDelay = 0; // Nouveau timer pour espacer les tirs
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Daybreak] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 2;
            NPC.width = 44;
            NPC.height = 22;
            DrawOffsetY = 60;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.scale = 1.3f;
            NPC.timeLeft = 22750;
            NPC.damage = 50;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 20000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 0;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void AI()
        {
            int[] bodyTypes = new int[] { ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, HellkiteDragonHead.hellkitePieceSeperation, 22, 0.25f, true, false);

            if (Main.rand.NextBool(600)) 
            {
                InfernoBlastCounter = 1; 
            }

            if (InfernoBlastCounter > 0 && InfernoBlastCounter <= 6)
            {
                InfernoBlastDelay++;
                if (InfernoBlastDelay >= 30 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(),
                        vector8.X,
                        vector8.Y,
                        0f, 0f, 
                        ProjectileID.InfernoHostileBlast,
                        fireDamage,
                        0f, Main.myPlayer);

                    InfernoBlastCounter++;
                    InfernoBlastDelay = 0; 
                }
            }

            if (InfernoBlastCounter >= 6)
            {
                InfernoBlastCounter = 0; // reset
            }

            if (!Main.npc[(int)NPC.ai[1]].active)
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                NPC.NPCLoot();
                NPC.active = false;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void OnKill()
        {

            NPC.netUpdate = true;
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (Main.player[NPC.target].active)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hellkite Dragon Body Gore 2").Type, 1.3f);
                }
            }
        }
    }
}