using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ForgottenSwordbreaker : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Striking an enemy may temporarily make you deflect attacks.");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Pink;
            Item.damage = 93;
            Item.height = 28;
            Item.knockBack = 3.5f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 28;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (Main.rand.Next(20) == 0)
            {
                player.AddBuff(ModContent.BuffType<Buffs.Invincible>(), 30);
            }
        }
    }
}
