using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class WeaponImbueFire : GlobalBuff {

        public override void Update(int type, Player player, ref int buffIndex) {
            if (type == BuffID.WeaponImbueFire) {
                player.meleeDamage += 0.1f;
            }
        }

        public override void ModifyBuffTip(int type, ref string tip, ref int rare) {
            if (type == BuffID.WeaponImbueFire) { 
                tip = "Gives 10% melee damage and melee attacks set enemies on fire";
            }
        }

    }
}
