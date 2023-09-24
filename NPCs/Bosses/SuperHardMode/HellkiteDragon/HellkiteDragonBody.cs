using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon
{
    class HellkiteDragonBody : ModNPC
    {
        int fireDamage = 25;
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 2;
            NPC.width = 44;
            NPC.height = 44;
            DrawOffsetY = 49;

            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22750;
            NPC.damage = 93;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 20000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 0;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 300 * 60, false);
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void AI()
        {
            int[] bodyTypes = new int[] { ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, HellkiteDragonHead.hellkitePieceSeperation, 22, 0.25f, true, false);

            
            if (!Main.npc[(int)NPC.ai[1]].active)
            {

                for (int num36 = 0; num36 < 50; num36++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, ProjectileID.GoldenShowerFriendly, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 10f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, ProjectileID.GoldenShowerFriendly, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
                    Main.dust[dust].noGravity = false;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 10.0f);
                    Main.dust[dust].noGravity = true;
                    //npc.netUpdate = true; //new
                }
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                OnKill();
                NPC.active = false;
            }
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.DamageType != DamageClass.Melee)
            {
                HellkiteDragonHead.SetImmune(projectile, NPC);
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
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hellkite Dragon Body Gore").Type, 1f);
                }
            }
        }
    }
}