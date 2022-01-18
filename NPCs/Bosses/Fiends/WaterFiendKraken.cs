using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
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
			npc.width = 110;
			npc.height = 170;
			drawOffsetY = 50;
			npc.damage = trueContactDamage;
			npc.defense = 35;
			npc.aiStyle = -1;
			animationType = -1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 400000;
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
		int plasmaOrbDamage = 65;
		int hypnoticDisruptorDamage = 35;
		int trueContactDamage = 185;
		int chargeContactDamage = 240;

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

		int MoveTimer = 0;
		NPCDespawnHandler despawnHandler;

		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			Lighting.AddLight((int)npc.Center.X / 16, (int)npc.Center.Y / 16, 0.4f, 0f, 0.25f);

			if (testAttack != -1)
            {
				MoveIndex = testAttack;
            }
			if(MoveList == null)
            {
				InitializeMoves();
            }
			if(MoveIndex >= MoveList.Count)
            {
				MoveIndex = 0;
            }

			CurrentMove.Move();
		}

		Vector2 chargeVelocity = new Vector2(0, 0);
		float ChargeTimer = 0;
		float projectileTimer = -120;
		float projectileType = 0;
		bool charging = false;
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
			if(MoveCounter >= 3 && projectileTimer == 0)
            {
				NextAttack();
            }

			#region Projectiles and NPCs
			if (Main.netMode != NetmodeID.MultiplayerClient && !charging)
			{
				projectileTimer++;

				if (projectileTimer == 0)
				{
					projectileType = Main.rand.Next(10);

				}
				if (projectileTimer >= 0)
				{
					float offset = MathHelper.ToRadians(-15 + 5 * projectileTimer);
					if (projectileType < 6)
					{
						Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 10);
						projVector = projVector.RotatedBy(offset);
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedFlames>(), cursedFlamesDamage, 0f, Main.myPlayer);
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					}
					if (projectileType >= 6 && projectileType != 9)
					{
						Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 5);
						projVector = projVector.RotatedBy(offset);
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, npc.target, 1f);
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					}
					if (projectileType == 9)
					{
						Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
						projVector = projVector.RotatedBy(offset);
						projVector += (Main.player[npc.target].velocity / 2);
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					}

					if(projectileTimer == 6)
                    {
						projectileTimer = -120;
                    }
				}
			}
			#endregion

			//If low on life, start flooding the chamber constantly
			if(npc.life < (npc.lifeMax / 2)){

				//Don't start a new flood if it's below the normal water line, to avoid fucking with it as much as possible
				if (radius != 0 || ((npc.Center.Y / 16) < 1713 && !UsefulFunctions.IsTileReallySolid(npc.Center / 16)))
				{
					radius++;
					FloodArena();
				}
				if (radius > 300)
				{
					chamberFlooded = !chamberFlooded;
					radius = 0;
				}
			}
		}

		float radius = 0;
		bool chamberFlooded;
		//Rectangle arena = new Rectangle(1557, 1639, 467, 103);
		List<Vector2> activeTiles;
		List<Vector2> nextTiles;

		private void AquaWave()
		{
			npc.velocity = Vector2.Zero;

            if (Main.GameUpdateCount % 30 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
				int projType = Main.rand.Next(10);			
			
				if (projType < 5)
				{
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 10);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedFlames>(), cursedFlamesDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}
				if (projType >= 5 && projType < 8)
				{

					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 5);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, npc.target, 1f);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}
				if (projType >= 8)
				{
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 15);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
				}				
			}


			radius++;
			FloodArena();


			//Check if we're done
			if (radius > 300)
			{
				//Self-correcting: If the chamber starts out flooded then the flooding algorithm will by nature do nothing
				chamberFlooded = !chamberFlooded;
				radius = 0;
				
				MoveTimer++; 
			
				if (MoveTimer >= 5)
				{
					MoveTimer = 0;
					NextAttack();
				}
			}			
		}

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

		Vector2 ArenaCenter = new Vector2(1820 * 16, 1702 * 16);
		private void DashToArenaCenter()
        {
			
        }		
		private void DashToArenaMidline()
		{
			MoveCounter++;
			int dust = Dust.NewDust(npc.position, npc.width, npc.height, 29, npc.velocity.X, npc.velocity.Y, 200, new Color(), 5);
			Main.dust[dust].velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.dust[dust].position, 5);
			if (MoveCounter > 60)
			{
				if(npc.Center.Y < ArenaCenter.Y)
                {
					npc.velocity.Y = 12;
                }
				else
                {
					npc.velocity.Y = -12;
                }
			}
			if (Math.Abs(npc.Center.Y - ArenaCenter.Y) < 16)
			{
				NextAttack();
			}
		}
		private void GeyserSpam()
        {
			//TODO
        }
		private void FloodArena()
		{
			Vector2 centerOver16 = npc.Center / 16;
			//Initialize some things
			if (radius == 1)
			{
				activeTiles = new List<Vector2>();
				nextTiles = new List<Vector2>();
				activeTiles.Add(centerOver16);
			}
			//Perform the flooding algorithm
			else
			{
				//Most things here work in vector2s, so declaring these here simplifies calculations below
				Vector2 up = new Vector2(0, 1);
				Vector2 right = new Vector2(1, 0);
				Vector2 left = new Vector2(-1, 0);
				Vector2 down = new Vector2(0, -1);

				//Pick whether we're flooding or emptying
				int liquidLevel = 255;
				if (chamberFlooded)
				{
					liquidLevel = 0;
				}

				//For every tile on the list
				for (int i = 0; i < activeTiles.Count; i++)
				{
					//Set it to full/empty
					Main.tile[(int)activeTiles[i].X, (int)activeTiles[i].Y].liquid = (byte)liquidLevel;

					//And add any adjacent unchanged tiles to the nextTiles list
					if (!nextTiles.Contains(activeTiles[i] + up) && Main.tile[(int)(activeTiles[i] + up).X, (int)(activeTiles[i] + up).Y].liquid != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + up))
					{
						nextTiles.Add(activeTiles[i] + up);
					}
					if (!nextTiles.Contains(activeTiles[i] + right) && Main.tile[(int)(activeTiles[i] + right).X, (int)(activeTiles[i] + right).Y].liquid != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + right))
					{
						nextTiles.Add(activeTiles[i] + right);
					}
					if (!nextTiles.Contains(activeTiles[i] + left) && Main.tile[(int)(activeTiles[i] + left).X, (int)(activeTiles[i] + left).Y].liquid != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + left))
					{
						nextTiles.Add(activeTiles[i] + left);
					}
					if (!nextTiles.Contains(activeTiles[i] + down) && Main.tile[(int)(activeTiles[i] + down).X, (int)(activeTiles[i] + down).Y].liquid != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + down))
					{
						nextTiles.Add(activeTiles[i] + down);
					}
				}

				//Push tiles that got queued in nextTiles into activeTiles to be operated on next tick, then wipe it
				activeTiles = nextTiles;
				nextTiles = new List<Vector2>();


				//Dust effect
				ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.CyanGradientDye), Main.LocalPlayer);
				for (int i = 0; i < 90; i++)
				{
					float offset = Main.rand.NextFloat(-720, 0);
					Vector2 velocity = new Vector2(-16, 0);
					if (radius < 20)
					{
						velocity *= radius / 20;
					}
					Vector2 positionOffset = Vector2.Zero;
					positionOffset.X += radius * 16;
					positionOffset.X += offset;
					positionOffset.Y += offset;

					Vector2 offset1 = positionOffset + npc.Center;
					Vector2 offset2 = positionOffset;
					offset2.X *= -1;
					offset2 += npc.Center;
					Vector2 offset3 = positionOffset;
					offset3.Y *= -1;
					offset3 += npc.Center;
					Vector2 offset4 = positionOffset;
					offset4 *= -1;
					offset4 += npc.Center;

					if (!UsefulFunctions.IsTileReallySolid(offset1 / 16))
					{
						Dust r = Dust.NewDustPerfect(offset1, 174, velocity, 10, default, 2);
						r.noGravity = true;
						r.shader = shader;
					}
					if (!UsefulFunctions.IsTileReallySolid(offset2 / 16))
					{
						Dust l = Dust.NewDustPerfect(offset2, 174, velocity, 10, default, 2);
						l.noGravity = true;
						l.shader = shader;
					}
					if (!UsefulFunctions.IsTileReallySolid(offset3 / 16))
					{
						Dust r = Dust.NewDustPerfect(offset3, 174, velocity, 10, default, 2);
						r.noGravity = true;
						r.shader = shader;
					}
					if (!UsefulFunctions.IsTileReallySolid(offset4 / 16))
					{
						Dust l = Dust.NewDustPerfect(offset4, 174, velocity, 10, default, 2);
						l.noGravity = true;
						l.shader = shader;
					}
				}
			}
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
				new KrakenMove(DashToArenaMidline, KrakenAttackID.CenterDash, "Dash to Center"),
				new KrakenMove(AquaWave, KrakenAttackID.AquaWave, "Aqua Wave"),
				//new KrakenMove(GeyserSpam, KrakenAttackID.GeyserSpam, "Geysers"),
				};
		}

		private class KrakenAttackID
		{
			public const short CursedFireSpam = 0;
			public const short AquaWave = 1;
			public const short CenterDash = 2;
			public const short GeyserSpam = 3;
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