using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class BloomShards : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bloom Shards");
            /* Tooltip.SetDefault("Evokes blooming shards of radiant light\n" +
                                "Close range"); */

        }

        public override void SetDefaults()
        {

            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.damage = 130;
            Item.autoReuse = true;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            Item.expert = true;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 11f;
            Item.mana = 5;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.value = PriceByRarity.Cyan_9;
        }

        /*public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int healEffect = damage / 10;

            if (crit)
            {
                healEffect *= 2;
            }

            player.statLife += healEffect;
            player.HealEffect(healEffect);
        }

        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            int healEffect = damage / 20;

            /*if (crit) //you can't crit in pvp?
            {
                healEffect *= 2;
            }*

            player.statLife += healEffect;
            player.HealEffect(healEffect);
        }*/

    }
}
