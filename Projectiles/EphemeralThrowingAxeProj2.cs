using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.WyvernMage;
using tsorcRevamp.NPCs.Bosses.Okiku;
using tsorcRevamp.NPCs.Bosses.Okiku.FirstForm;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm;
using tsorcRevamp.NPCs.Bosses.Okiku.FinalForm;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Projectiles
{
    class EphemeralThrowingAxeProj2 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.width = 38;
            Projectile.height = 72;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //todo add mod NPCs to this list
            if (target.type == NPCID.Tim
                || target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                //|| target.type == ModContent.NPCType<UndeadCaster>()
                //|| target.type == ModContent.NPCType<MindflayerServant>()
                //|| target.type == ModContent.NPCType<DungeonMage>()
                //|| target.type == ModContent.NPCType<DemonSpirit>()
                //|| target.type == ModContent.NPCType<CrazedDemonSpirit>()
                //|| target.type == ModContent.NPCType<ShadowMage>()
                //|| target.type == ModContent.NPCType<AttraidiesIllusion>()
                //|| target.type == ModContent.NPCType<AttraidiesManifestation>()
                //|| target.type == ModContent.NPCType<DarkShogunMask>()
                //|| target.type == ModContent.NPCType<DarkDragonMask>()
                //|| target.type == ModContent.NPCType<BrokenOkiku>()
                //|| target.type == ModContent.NPCType<Okiku>()
                //|| target.type == ModContent.NPCType<WyvernMage>()
                //|| target.type == ModContent.NPCType<LichKingDisciple>()
                //|| target.type == ModContent.NPCType<Attraidies>()
                //|| target.type == ModContent.NPCType<GhostOfTheForgottenKnight>()
                //|| target.type == ModContent.NPCType<BarrowWight>()
                )
            {
                modifiers.FinalDamage *= 2;
            }
        }
        public override void AI()
        {
            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 91, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }
        
        public override void OnKill(int timeLeft)
        {

            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            Projectile.active = false;
        }
    }
}
