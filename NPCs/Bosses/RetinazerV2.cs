using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    class RetinazerV2 : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Retinazer);
            Main.npcFrameCount[NPC.type] = 6;
            NPC.defense = 25;
            AnimationType = -1;
            NPC.lifeMax = (int)(32500 * (1 + (0.25f * (Main.CurrentFrameFlags.ActivePlayersCount - 1))));
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;

            NPC.value = 600000;
            AnimationType = NPCID.Retinazer;
            NPC.aiStyle = -1;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            despawnHandler = new NPCDespawnHandler("", Color.Cyan, 180);
            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Retinazer");
        }

        int DeathLaserDamage = 25;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        CataMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<CataMove> MoveList;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveCounter
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public Player target
        {
            get => Main.player[NPC.target];
        }

        int MoveTimer = 0;
        NPCDespawnHandler despawnHandler;

        public override void AI()
        {
            //Main.NewText(CurrentMove.Name + ": " + MoveTimer);
            MoveTimer++;
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }

            CurrentMove.Move();

            if (MoveTimer >= 900)
            {
                NextAttack();
            }

            if (NPC.Distance(target.Center) > 4000)
            {
                NPC.Center = Main.player[NPC.target].Center + new Vector2(-1000, 0);
                UsefulFunctions.BroadcastText("Retinazer Closes In...");
            }
        }

        //Trails off behind the player, before firing an enormous OHKO laser repeatedly
        void BigIron()
        {
            UsefulFunctions.SmoothHoming(NPC, Main.player[NPC.target].Center + new Vector2(-700, 0), 0.7f, 20);

            if (MoveTimer % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //TODO: Replace this with an enormous fuck you laser
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ProjectileID.DeathLaser, DeathLaserDamage, 0.5f, Main.myPlayer);
            }
        }

        //Dashes at (or maybe intentionally past?) the player, aiming at them and firing a barrage of lasers as it does
        void Charging()
        {
            if (MoveTimer % 60 < 15)
            {
                UsefulFunctions.DustRing(NPC.Center, (15 - MoveTimer % 60) * 20, DustID.GemRuby, 100, 2);
            }

            if (MoveTimer % 60 == 15)
            {
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 15);
            }
            else if (MoveTimer % 60 > 15 && MoveTimer % 60 < 35)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 11), ProjectileID.DeathLaser, DeathLaserDamage, 0.5f, Main.myPlayer);
                }
            }

        }

        //Hovers top right of the player and fires hitscan lingering lasers repeatedly
        void Firing()
        {
            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(-250, -250), 0.7f, 20);

            if (MoveTimer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //TODO: Replace this with a hitscan laser
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6), ProjectileID.DeathLaser, DeathLaserDamage, 0.5f, Main.myPlayer);
            }

        }

        void TBD()
        {
            NextAttack();
        }

        private void NextAttack()
        {
            MoveIndex++;
            if (MoveIndex > MoveList.Count - 1)
            {
                MoveIndex = 0;
            }

            MoveTimer = 0;
            MoveCounter = 0;
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<CataMove> {
                new CataMove(BigIron, CataMoveID.BigIron, "Big Iron"),
                new CataMove(Charging, CataMoveID.Charging, "Charging"),
                new CataMove(Firing, CataMoveID.Firing, "Firing"),
                new CataMove(TBD, CataMoveID.TBD, "TBD"),
                };
        }

        private class CataMoveID
        {
            public const short BigIron = 0;
            public const short Firing = 1;
            public const short Charging = 2;
            public const short TBD = 3;
        }
        private class CataMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public CataMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }

            if (NPC.life >= NPC.lifeMax / 2f)
            {
                if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type] / 2f)
                {
                    NPC.frame.Y = 0;
                }
            }
            else
            {
                if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = num * Main.npcFrameCount[NPC.type] / 2;
                }
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = NPC.frame;
            Vector2 origin = sourceRectangle.Size() / 2f;
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, origin, 1, effects, 0f);
            return false;
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot NPCLoot) {
            NPCLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.KrakenBag>()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 8").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenRisingSun>(), 10);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
                }
            }
        }
    }
}