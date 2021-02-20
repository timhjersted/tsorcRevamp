using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class WolfRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("One of the rings worn by Artorias." +
                                "\nImmunity to the on-fire and broken-armor debuffs." + 
								"\n+23 defense within the Abyss, +16 defense otherwise." + 
								"\nWhen awakening from death, you regain full health." + 
								"\nSwords inflict fire damage.");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.defense = 16;
			item.lifeRegen = 8;
            item.accessory = true;
            item.value = 1000000;
            item.rare = ItemRarityID.Pink;
        }


        public override void UpdateEquip(Player player) {
            player.AddBuff(BuffID.WeaponImbueFire, 60, false);   
			player.spawnMax = true;
			player.buffImmune[BuffID.OnFire] = true;
			player.buffImmune[BuffID.BrokenArmor] = true;
			
			if(Main.bloodMoon) { // Apparently this is the flag used in the Abyss?
				player.statDefense += 7;
			}
        }

    }
}