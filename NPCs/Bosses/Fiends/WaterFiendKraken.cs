using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
	[AutoloadBossHead]
	class WaterFiendKraken : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 8;
			npc.width = 120;
			npc.height = 190;
			drawOffsetY = 50;
			npc.damage = trueContactDamage;
			npc.defense = 35;
			npc.aiStyle = -1;
			animationType = -1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 200000;
			npc.timeLeft = 22500;
			npc.friendly = false;
			npc.noTileCollide = true;
			npc.noGravity = true;
			npc.knockBackResist = 0f;
			npc.lavaImmune = true;
			npc.boss = true;
			npc.value = 600000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			bossBag = ModContent.ItemType<Items.BossBags.KrakenBag>();
			despawnHandler = new NPCDespawnHandler("Water Fiend Kraken submerges into the depths...", Color.DeepSkyBlue, 180);
			InitializeMoves();
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Water Fiend Kraken");
		}


		int cursedFlamesDamage = 45;
		int plasmaOrbDamage = 75;
		int hypnoticDisruptorDamage = 35;
		int trueContactDamage = 185;
		int chargeContactDamage = 240;

		float[] wraithAI = new float[4];

		//If this is set to anything but -1, the boss will *only* use that attack ID
		readonly int testAttack = -1;
		KrakenMove CurrentMove
        {
			get => MoveList[MoveIndex];
        }

		List<KrakenMove> MoveList;

		//Controls what move is currently being performed
		public int MoveIndex
		{
			get => (int)npc.ai[0];
			set => npc.ai[0] = value;
		}

		//Used by moves to keep track of how long they've been going for
		public int MoveCounter
		{
			get => (int)npc.ai[1];
			set => npc.ai[1] = value;
		}

		public Player Target
        {
			get => Main.player[npc.target];
        }

		bool charging = false;
		NPCDespawnHandler despawnHandler;

		#region AI
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0.25f);
			if (testAttack != -1)
            {
				//MoveIndex = testAttack;
            }
			if(MoveList == null)
            {
				InitializeMoves();
            }
			if(MoveIndex >= MoveList.Count)
            {
				//MoveIndex = 0;
            }

			//CurrentMove.Move();
			CursedFireSpam();
		}

		Vector2 chargeVelocity = new Vector2(0, 0);
		float ChargeTimer = 0;
		private void CursedFireSpam()
        {
			ChargeTimer++;
			if (ChargeTimer >= 500)
			{
				int dust = Dust.NewDust(npc.position, npc.width, npc.height, 29, npc.velocity.X, npc.velocity.Y, 200, new Color(), 5);
				Main.dust[dust].velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.dust[dust].position, 5);
			}
			if (ChargeTimer == 600)
            {
				chargeVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 19);
				charging = true;
			}
			if (charging)
			{
				npc.velocity = chargeVelocity;
				npc.damage = chargeContactDamage;

				//Check if it's passed the player by at least 500 units while charging, and if so stop
				if (Vector2.Distance(npc.Center, Target.Center) > 500)
				{
					Vector2 vectorDiff = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 1);
					double angleDiff = UsefulFunctions.CompareAngles(npc.velocity, vectorDiff);

					if (angleDiff > MathHelper.Pi / 2)
					{
						charging = false;
						npc.damage = trueContactDamage;
						ChargeTimer = 0;
						MoveCounter++;
					}
				}
			}

			//Most of its movement is done here
			if (!charging)
			{
				FloatOminouslyTowardPlayer();
			}
			if(MoveCounter >= 3)
            {
				NextAttack();
            }

            #region Projectiles and NPCs
            if (Main.netMode != NetmodeID.MultiplayerClient && !charging)
			{
				if (Main.rand.Next(50) == 1)
				{
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 10);
					projVector += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedFlames>(), cursedFlamesDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}
				if (Main.rand.Next(120) == 1)
				{
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 15);
					projVector += (Main.player[npc.target].velocity / 2);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}
				if (Main.rand.Next(200) == 1)
				{
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 5);
					projVector += Main.rand.NextVector2Circular(10, 10);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}

				if (Main.rand.Next(900) == 0)
				{
					NPC.NewNPC((int)Main.player[this.npc.target].position.X - 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 216 - this.npc.width / 2, NPCID.CursedHammer, 0);
					NPC.NewNPC((int)Main.player[this.npc.target].position.X + 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y + 216 - this.npc.width / 2, NPCID.CursedHammer, 0);
				}
			}            
		}
        #endregion

        private void FloatOminouslyTowardPlayer()
        {
			Vector2 krakenMaxSpeed = new Vector2(3, 4);
			float krakenAccelerationX = 0.05f;
			float krakenAccelerationY = 0.05f;

			if(npc.Center.X < Target.Center.X)
			{
				npc.velocity.X += krakenAccelerationX;
			}
			else
			{
				npc.velocity.X -= krakenAccelerationX;
			}

			//This is the part that makes it bob up and down as it moves
			//If it's moving up
			if(npc.velocity.Y < 0)
			{
				//And it's not more than 120 units above the player
				if(Target.Center.Y - npc.Center.Y <= 120)
                {
					//Keep moving up
					npc.velocity.Y -= krakenAccelerationY;
                }
				//If we are more than 120 units above the player, start accelerating down
				else
				{
					npc.velocity.Y += krakenAccelerationY;
				}
			}
			else
			{
				//Do the same thing, but reversed if it's moving down. Could probably simplify this, but this format makes it clear what it's doing.
				if (Target.Center.Y - npc.Center.Y <= -120)
				{
					npc.velocity.Y -= krakenAccelerationY;
				}
				else
				{
					npc.velocity.Y += krakenAccelerationY;
				}
			}

			npc.velocity = Vector2.Clamp(npc.velocity, -krakenMaxSpeed, krakenMaxSpeed);
		}


		private void NextAttack()
        {
			MoveIndex++;
			if(MoveIndex > MoveList.Count)
            {
				MoveIndex = 0;
            }

			MoveCounter = 0;
        }
		private void InitializeMoves(List<int> validMoves = null)
		{
			MoveList = new List<KrakenMove> {
				new KrakenMove(CursedFireSpam, KrakenAttackID.CursedFireSpam, "Cursed Fire"),
                };
		}

		private class KrakenAttackID
		{
			public const short CursedFireSpam = 0;

		}
		private class KrakenMove
		{
			public Action Move;
			public int ID;
			public Action<SpriteBatch, Color> Draw;
			public string Name;

			public KrakenMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
			{
				Move = MoveAction;
				ID = MoveID;
				Draw = DrawAction;
				Name = AttackName;
			}
		}

		#endregion




		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			if (npc.velocity.X < 0)
			{
				npc.spriteDirection = -1;
			}
			else
			{
				npc.spriteDirection = 1;
			}
			npc.rotation = npc.velocity.X * 0.08f;
			npc.frameCounter += 1.0;
			if (npc.frameCounter >= 5.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
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
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 4"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 5"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 6"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 7"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Water Fiend Kraken Gore 8"), 1f);

			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.DragonHorn>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenRisingSun>(), 10);
				if (!tsorcRevampWorld.Slain.ContainsKey(npc.type))
				{
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
				}
			}
		}
	}
}