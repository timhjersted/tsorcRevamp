using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;

namespace tsorcRevamp.Content.Items.Debug
{
    class BetterGodmode : ModItem
    {

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HolyWarElixir));

        public override void SetDefaults()
        {
            Item.width = 1;
            Item.height = 1;
            Item.value = 69; //i am the master of comedy
            Item.accessory = true;
            Item.rare = ItemRarityID.Expert;
        }

        public override void SetStaticDefaults()
        {
        }

        public override void UpdateEquip(Player player)
        {
            player.immune = true;
            player.lifeRegen += 8000;
        }
    }
}
