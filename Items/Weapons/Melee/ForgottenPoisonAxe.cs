using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class ForgottenPoisonAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The blade has been dipped in poison.");
        }
        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Pink;
            Item.damage = 76;
            Item.height = 46;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.2f;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 50;
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                npc.AddBuff(BuffID.Poisoned, 360, false);
            }
        }
    }
}
