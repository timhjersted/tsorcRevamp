using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class GhostDragonHead : ModNPC
    {
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
            NPC.damage = 185;
            NPC.defense = 120;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath10;
            NPC.lifeMax = 700000;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.alpha = 100;
            NPC.value = 660000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler(DustID.OrangeTorch);
        }
        int lightningDamage = 50;

        public static int drawOffset = 52;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            
            target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 18000, false);
            target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false);
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ghost Wyvern");
        }

        NPCDespawnHandler despawnHandler;
        int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.localAI[1]++;
            NPC.localAI[2]++;
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, 10f, 15f, 0.13f, true, false, true, false, false);//.01 was .23, 0 was -2

            Player player = Main.player[NPC.target];
            if (NPC.Distance(player.Center) > 700)
            {
                tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[2], 1500, ProjectileID.CultistBossLightningOrb, lightningDamage, 10, Main.rand.NextBool(200), false, SoundID.Item17);
            }
            //tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 660, ProjectileID.FrostWave, lightningDamage, 1, Main.rand.NextBool(200), false, SoundID.Item20);
            
            //this makes the head always stay in the same position even when it flips upside down
            if (NPC.velocity.X < 0f) { NPC.spriteDirection = 1; }
            else  
            if (NPC.velocity.X > 0f) { NPC.spriteDirection = -1; }
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
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.WyvernMageShadowBag>(), 1, 1, 1, new GhostDiscipleDropCondition()));
        }

        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 5.0f);
            Main.dust[dust].noGravity = true;

            if (!Main.expertMode)
            {
                //Only drop the loot if the mage is already dead. If it's not, then he will drop it instead.
                if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
                }
                else
                {

                    UsefulFunctions.BroadcastText("The souls of " + NPC.GivenOrTypeName + " have been released!", 175, 255, 75);
                    tsorcRevampWorld.NewSlain[new Terraria.ModLoader.Config.NPCDefinition(ModContent.NPCType<GhostDragonHead>())] = 1;
                }
            }
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
    public class GhostDiscipleDropCondition : Terraria.GameContent.ItemDropRules.IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<WyvernMageShadow>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return "Drops if the mage is dead";
        }
    }
}