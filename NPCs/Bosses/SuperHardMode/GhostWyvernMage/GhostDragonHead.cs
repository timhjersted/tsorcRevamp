using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;
using tsorcRevamp.Projectiles.Enemy.WyvernMage;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class GhostDragonHead : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<MorgulPoisoning>()] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 45;
            NPC.height = 45;
            DrawOffsetY = drawOffset;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22750;
            NPC.damage = 100;
            NPC.defense = 120;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.Zombie97;
            NPC.lifeMax = 600000;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.alpha = 100;
            NPC.value = 660000;
            despawnHandler = new NPCDespawnHandler(DustID.OrangeTorch);
            UsefulFunctions.AddAttack(NPC, 120, ProjectileID.CultistBossFireBallClone, 45, 11, SoundID.Item20, stopBeforeFiring: false, needsLineOfSight: true, condition: (NPC npc) => { return npc.Distance(Main.player[npc.target].Center) > 700 && Main.rand.NextBool(200); });
        }
        int breathCD = 120;
        bool breath = false;
        int lightningDamage = 50;

        public static int drawOffset = 52;
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 300 * 60, false);
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false);
        }
        NPCDespawnHandler despawnHandler;
        int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, 10f, 15f, 0.13f, true, false, true, false, false);//.01 was .23, 0 was -2

            //AIWorm does not call SimpleProjectile on its own, so we've gotta do it manually here
            tsorcRevampAIs.SimpleProjectile(NPC);

            //this makes the head always stay in the same position even when it flips upside down
            if (NPC.velocity.X < 0f) { NPC.spriteDirection = 1; }
            else
            if (NPC.velocity.X > 0f) { NPC.spriteDirection = -1; }

            Player nT = Main.player[NPC.target];
            if ((Main.rand.NextBool(300) && NPC.life < NPC.lifeMax / 2) || Main.rand.NextBool(900))
            {
                if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 10)))
                {
                    breath = true;
                }

            }
            if (breath)
            {

                if (breathCD == 120)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.7f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && breathCD <= 70)
                {


                    Vector2 spawnOffset = NPC.velocity; //Create a vector pointing in whatever direction the NPC is moving. We can transform this into an offset we can use.
                    spawnOffset.Normalize(); //Shorten the vector to make it have a length of 1
                    spawnOffset *= 80; //Multiply it so it has a length of 16. The length determines how far offset the projectile will be, 16 units = 1 tile

                    //float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)(NPC.Center.X + spawnOffset.X), (int)(NPC.Center.Y + spawnOffset.Y), NPC.velocity.X * 2f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 2f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<ShadowDragonsBreath>(), lightningDamage, 1.3f, Main.myPlayer);




                }
                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.8f, Pitch = -0.9f }, NPC.Center); //flame thrower
                }

                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 150;

            }
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBagByCondition(new GhostDiscipleDropCondition(), ModContent.ItemType<Items.BossBags.WyvernMageShadowBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.ByCondition(new GhostDiscipleDropCondition(), ModContent.ItemType<HolyWarElixir>()));
            notExpertCondition.OnSuccess(ItemDropRule.ByCondition(new GhostDiscipleDropCondition(), ModContent.ItemType<GhostWyvernSoul>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
        }

        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 5.0f);
            Main.dust[dust].noGravity = true;
        }


        public static Texture2D texture;
        public static void GhostEffect(NPC npc, SpriteBatch spriteBatch, ref Texture2D texture, float scale = 1.5f)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(npc.ModNPC.Texture);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
            data.Apply(null);
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, Color.White, npc.rotation, origin, scale, effects, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
        }
        
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GhostEffect(NPC, spriteBatch, ref texture, 1.5f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // GhostDragonHead.GhostEffect(npc, spriteBatch, ref texture, 1.5f);
        }
    }
    public class GhostDiscipleDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<WyvernMageShadow>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return LangUtils.GetTextValue("NPCs.GhostDragonHead.Condition");
        }
    }
}