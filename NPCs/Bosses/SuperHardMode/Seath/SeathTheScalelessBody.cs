using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    class SeathTheScalelessBody : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 44; //was 32
            NPC.height = 44; //tried 64 44 works
            DrawOffsetY = 49;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 250;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 75000;
            Music = 12;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }


        int breathDamage = 63; //was 33
        int flameRainDamage = 75; //27
        int meteorDamage = 123; //33
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            flameRainDamage = (int)(flameRainDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seath the Scaleless");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
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
        public override bool CheckActive()
        {
            return false;
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            SeathTheScalelessHead.SetImmune(projectile, NPC);
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
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                //if (npc.life <= 0){
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                int a = Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Seath the Scaleless Body Gore").Type, 1f);
                Main.gore[a].timeLeft = 3600;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                //}
            }
        }
    }
}