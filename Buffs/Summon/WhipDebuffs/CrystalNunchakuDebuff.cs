using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.Whips;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class CrystalNunchakuDebuff : ModBuff
	{

        public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().markedByCrystalNunchaku = true;
			npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().updateTick += 0.0167f;
            if (npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().updateTick > 15.02f)
            {
                npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().updateTick = 0f;
                npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().stacks = 10;
                npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().proc = false;
            }
			if (npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().updateTick >= 5f && npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().updateTick <= 5.2f)
			{
                Dust.NewDust(npc.TopLeft, 10, 10, DustID.CrystalPulse);
                npc.GetGlobalNPC<CrystalNunchakuDebuffNPC>().proc = true;
            }
        }
	}

	public class CrystalNunchakuDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByCrystalNunchaku;

		public float stacks = 10;

		public bool proc = false; 
		
		public float updateTick = 0f;


        public override void ResetEffects(NPC npc)
		{
			markedByCrystalNunchaku = false;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
			if (!proc && !(stacks == 0) && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku && projectile.type != ModContent.ProjectileType<CrystalNunchakuProjectile>())
			{
                stacks -= 1;
            }
			if (proc && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku)
            {
                Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage = 15f - (stacks * 1.5f);
                Main.player[projectile.owner].AddBuff(ModContent.BuffType<CrystalShield>(), 4 * 60);
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (!proc && !(stacks == 0) && markedByCrystalNunchaku)
            {
                stacks -= 1;
            }
            if (proc && markedByCrystalNunchaku)
            {
                player.GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage = 15f - (stacks * 1.5f);
                player.AddBuff(ModContent.BuffType<CrystalShield>(), 4 * 60);
            }
        }

        // TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByCrystalNunchaku && !projectile.npcProj && !projectile.trap /*&& (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type])*/)
			{
				if (proc)
				{
					modifiers.FlatBonusDamage += (int)((float)projectile.damage * (stacks / 20f));
				}
			}
		}
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (markedByCrystalNunchaku)
            {
                if (proc)
                {
                    modifiers.FlatBonusDamage += (int)((float)item.damage * (stacks / 40f));
                }
            }
        }
    }
}