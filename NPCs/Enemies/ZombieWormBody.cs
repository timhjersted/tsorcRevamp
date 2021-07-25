using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
	class ZombieWormBody : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Parasytic Worm");
		}

		public override void SetDefaults()
		{
			animationType = 10;
			npc.netAlways = true;
			npc.npcSlots = 1;
			npc.width = 38;
			npc.height = 24;
			npc.aiStyle = 6;
			npc.timeLeft = 750;
			npc.damage = 80;
			npc.defense = 28;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lavaImmune = true;
			npc.knockBackResist = 0;
			npc.lifeMax = 600;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.value = 460;
		}
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		public override void AI()
		{
			if (!Main.npc[(int)npc.ai[1]].active)
			{
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				npc.active = false;
				Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
				Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Worm Gore 2"), 1f);
			}
		}
	}
}