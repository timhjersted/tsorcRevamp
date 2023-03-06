using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage
{
    [AutoloadBossHead]
    class MechaDragonHead : ModNPC
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wyvern Mage Disciple");
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = 6;
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 45;
            NPC.height = 45;
            NPC.timeLeft = 22750;
            NPC.damage = 210;
            NPC.defense = 6;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath10;
            NPC.lifeMax = 61000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.boss = true;
            NPC.value = 25000;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;

            despawnHandler = new NPCDespawnHandler(DustID.OrangeTorch);

            bodyTypes = new int[] { ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
                ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
                ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),

                
                //ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
                
                ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>() };

        }

        int breathCD = 150;
        bool breath = false;
        int breathDamage = 30;
        public static int[] bodyTypes;

        /**public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return head ? (bool?)null : false;
		}**/

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            //Generic Worm Part Code:
            NPC.behindTiles = true;
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<MechaDragonHead>(), bodyTypes, ModContent.NPCType<MechaDragonTail>(), 18, -1f, 11f, 0.13f, true, false, true, false, false); //3 was 12, the speed, 18 was 23

            //Code unique to this body part:
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;

            /*
            //Can phase through walls if can't reach the player, + 100 / + 200 works great! but it goes into walls too easily (+10 and +100 is better, but could be tweaked further)
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 20)))
            {
                NPC.noTileCollide = false;
                NPC.noGravity = true;
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 200)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                //NPC.velocity.Y *= 0.9f;
                
            }
            */



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

                if (breathCD == 150)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.7f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && breathCD <= 90)
                {


                    Vector2 spawnOffset = NPC.velocity; //Create a vector pointing in whatever direction the NPC is moving. We can transform this into an offset we can use.
                    spawnOffset.Normalize(); //Shorten the vector to make it have a length of 1
                    spawnOffset *= 80; //Multiply it so it has a length of 16. The length determines how far offset the projectile will be, 16 units = 1 tile

                    //float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)(NPC.Center.X + spawnOffset.X), (int)(NPC.Center.Y + spawnOffset.Y), NPC.velocity.X * 2f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 2f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);




                }
                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, Pitch = -0.6f }, NPC.Center); //flame thrower
                }

                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 150;

            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 origin = new Vector2(TextureAssets.Npc[NPC.type].Value.Width / 2, TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] / 2);
            Color alpha = Color.White;
            SpriteEffects effects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                effects = SpriteEffects.FlipHorizontally;
            }
            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, new Vector2(NPC.position.X - Main.screenPosition.X + NPC.width / 2 - (float)TextureAssets.Npc[NPC.type].Value.Width * NPC.scale / 2f + origin.X * NPC.scale, NPC.position.Y - Main.screenPosition.Y + (float)NPC.height - (float)TextureAssets.Npc[NPC.type].Value.Height * NPC.scale / (float)Main.npcFrameCount[NPC.type] + 4f + origin.Y * NPC.scale + 56f), NPC.frame, alpha, NPC.rotation, origin, NPC.scale, effects, 0f);
            NPC.alpha = 255;
            return true;
        }

        private static int ClosestSegment(NPC head, params int[] segmentIDs)
        {
            List<int> segmentIDList = new List<int>(segmentIDs);
            Vector2 targetPos = Main.player[head.target].Center;
            int closestSegment = head.whoAmI; //head is default, updates later
            float minDist = 1000000f; //arbitrarily large, updates later
            for (int i = 0; i < Main.npc.Length; i++)
            { //iterate through every NPC
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && segmentIDList.Contains(npc.type))
                { //if the npc is part of the wyvern
                    float targetDist = (npc.Center - targetPos).Length();
                    if (targetDist < minDist)
                    { //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
                        minDist = targetDist; //update minDist. future iterations will compare against the updated value
                        closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
                    }
                }
            }
            return closestSegment; //the whoAmI of the closest segment
        }

        public override bool PreKill()
        {
            int closestSegmentID = ClosestSegment(NPC, ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonTail>());
            NPC.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot

            return true;        
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.WyvernMageBag>(), 1, 1, 1, new WyvernDiscipleDropCondition()));
        }

        public override void OnKill()
        {

            //Kind of like EoW, it always drops this many extra souls whether it's been killed or not.
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 900);
            if (!Main.expertMode)
            {
                if (!(tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<MechaDragonHead>()))))
                { //If the boss has not yet been killed
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 5000); //Then drop the souls
                }
            }
        }
    }

    public class WyvernDiscipleDropCondition : Terraria.GameContent.ItemDropRules.IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<WyvernMage>());
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
