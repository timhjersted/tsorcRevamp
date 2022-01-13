using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class HumanityPhantom : ModNPC
	{
		public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[npc.type] = 8;
		}

		public override void SetDefaults()
		{
			//npc.CloneDefaults(NPCID.Wraith);
			npc.width = 20;
			npc.height = 46;
			npc.aiStyle = -1; //Unique AI
			npc.damage = 50;
			npc.knockBackResist = 0;
			npc.defense = 8;
			npc.scale = Main.rand.NextFloat(0.5f, 1f);
			if (!Main.hardMode) npc.lifeMax = (int)(150 * npc.scale);
			else npc.lifeMax = (int)(500 * npc.scale);
			if (tsorcRevampWorld.SuperHardMode) npc.lifeMax *= 5;
			npc.value = 1000;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Venom] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			npc.buffImmune[BuffID.Frostburn] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.ShadowFlame] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.CrescentMoonlight>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.DarkInferno>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.CrimsonBurn>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ToxicCatDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ViruCatDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.BiohazardDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ElectrocutedBuff>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.PolarisElectrocutedBuff>()] = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.HitSound = SoundID.NPCHit1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) //Spawns in extremely deep, dark places.
		{
			float chance = 0;

			if (spawnInfo.player.ZoneRockLayerHeight && Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].wall == WallID.SpiderUnsafe) //This is at the very bottom of the chasm. Accessible pre-HM. Still difficult to encounter them as the area isn't really big enough to allow them to spawn offscreen
			{
				chance = 2f;
			}

			if ((spawnInfo.player.ZoneRockLayerHeight || spawnInfo.player.ZoneUnderworldHeight) && (Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].wall == WallID.ObsidianBrickUnsafe || Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].wall == WallID.TitanstoneBlock)) //Gwyns tomb entrance, a SHM cave under the krakens arena, and the caves leading up to the Witchking
			{
				chance = 1.5f;
			}

			if (Math.Abs(spawnInfo.spawnTileX - Main.spawnTileX) < Main.maxTilesX / 3 && Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].wall == WallID.StarlitHeavenWallpaper) //Inner third of the map, abyss wall. This is the heart of the abyss, SHM
			{
				chance = 10f;
			}
			return chance;
		}

        public override void AI()
        {
			Player player = Main.LocalPlayer;
			npc.TargetClosest(true);

			/*Vector2 difference = Main.player[npc.target].Center - npc.Center; //Distance between player center and npc center
			Vector2 velocity = new Vector2(0.5f, 0.5f).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction

			if (Main.player[npc.target].Distance(npc.Center) < 500f)
			{
				npc.velocity = velocity;
			}
			else npc.velocity = new Vector2(0, 0);*/

			Vector2 targetPosition = Main.player[npc.target].position; // get a local copy of the targeted player's position
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));

			if (Main.player[npc.target].Distance(npc.Center) < 500f)
			{
				if (Main.player[npc.target].position.X < vector8.X)
				{
					if (npc.velocity.X > -7) { npc.velocity.X = - 0.5f; }
				}
				if (Main.player[npc.target].position.X > vector8.X)
				{
					if (npc.velocity.X < 7) { npc.velocity.X = 0.5f; }
				}

				if (Main.player[npc.target].position.Y < vector8.Y)
				{
					if (npc.velocity.Y > 0f) npc.velocity.Y = -0.5f;
					else npc.velocity.Y = -0.5f;
				}
				if (Main.player[npc.target].position.Y > vector8.Y)
				{
					if (npc.velocity.Y < 0f) npc.velocity.Y = 0.5f;
					else npc.velocity.Y = 0.5f;
				}
			}
			else
            {
				npc.velocity = Vector2.Zero;
            }
		}

        public override void NPCLoot()
        {
            if (Main.rand.NextFloat() <= npc.scale - .3f) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Humanity>(), 1); // 0.5f scale phantoms have 20% chance of dropping, scaling up towards 1f scale phantoms dropping humanity 70% of the time
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
			Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.HumanityPhantom];

			spriteBatch.Draw(texture, npc.Center - Main.screenPosition, new Rectangle(0, npc.frame.Y, 48, 68), Color.White, npc.rotation, new Vector2(24, 34), npc.scale, SpriteEffects.None, 0);

			return false;
		}

        public override void FindFrame(int frameHeight)
        {
			npc.frameCounter++;

			if (npc.frameCounter < 12)
			{
				npc.frame.Y = 0 * frameHeight;
			}
			else if (npc.frameCounter < 24)
			{
				npc.frame.Y = 1 * frameHeight;
			}
			else if (npc.frameCounter < 36)
			{
				npc.frame.Y = 2 * frameHeight;
			}
			else if (npc.frameCounter < 48)
			{
				npc.frame.Y = 3 * frameHeight;
			}
			else if (npc.frameCounter < 60)
			{
				npc.frame.Y = 4 * frameHeight;
			}
			else if (npc.frameCounter < 72)
			{
				npc.frame.Y = 5 * frameHeight;
			}
			else if (npc.frameCounter < 84)
			{
				npc.frame.Y = 6 * frameHeight;
			}
			else if (npc.frameCounter < 96)
			{
				npc.frame.Y = 7 * frameHeight;
			}
			else
			{
				npc.frameCounter = 0;
			}
		}
    }
}
