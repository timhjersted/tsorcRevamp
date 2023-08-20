using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs
{
    public class DetonationSignalBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
            // Other mods may check it for different purposes.
            BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<DetonationSignalBuffNPC>().exploded = true;
        }
    }

    public class DetonationSignalBuffNPC : GlobalNPC
    {
        // This is required to store information on entities that isn't shared between them.
        public override bool InstancePerEntity => true;

        public bool exploded;

        public override void ResetEffects(NPC npc)
        {
            exploded = false;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (exploded)
            {
                modifiers.SourceDamage *= DetonationSignal.BonusContactDamage / 100f;
            }
        }
    }
}