using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    class SeathTheScalelessBody3 : ModNPC
    {
        int breathDamage = 17;
        int flameRainDamage = 14;
        int meteorDamage = 17;
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused,
                    BuffID.Frostburn,
                    BuffID.Frostburn2
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 44;
            DrawOffsetY = 49;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 45;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 75000;
            Music = 12;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
        }


        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void AI()
        {
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialCrystal>()))
            {
                NPC.dontTakeDamage = true;
            }
            else
            {
                NPC.dontTakeDamage = false;
            }

            int[] bodyTypes = new int[] { ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<SeathTheScalelessHead>(), bodyTypes, ModContent.NPCType<SeathTheScalelessTail>(), 17, 6f, 10f, 0.17f, true, false);
        }
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathTheScalelessHead.SeathInvulnerableEffect(NPC, spriteBatch, ref texture);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathTheScalelessHead.SeathInvulnerableEffect(NPC, spriteBatch, ref texture);
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            SeathTheScalelessHead.SetImmune(projectile, NPC);
        }

        public override void OnKill()
        {

            //npc.netUpdate = true;
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Seath the Scaleless Body Gore 2").Type, 1f);
                }
            }
        }
    }
}