using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class BarrowBlade : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A semi-invisible blade wrought with spells of a fierce power." +
                                "\n[c/ffbf00:Dispels the defensive shields of Artorias and the Witchking]" +
                                "\nThe night reveals its connection to the Abyss..." +
                                "\nSpreads Ichor to those it pierces");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest; //so people know it's important
            Item.damage = 186;
            Item.height = 32;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 0.8f;
            Item.useAnimation = 26;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            //Item.useStyle = ItemUseStyleID.Rapier;
            //Item.noUseGraphic = false;
            //Item.noMelee = true;
            Item.useTime = 26;
            Item.alpha = 150;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 32;
            Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
            //Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 2.5f; // Was 2.1 - This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (!Main.dayTime)
            {
                //Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>();
                Item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                Item.shootSpeed = 10.5f;
                Item.useTime = 50;
                Item.useAnimation = 50;
                Item.damage = 175;
                Item.UseSound = SoundID.Item20;
            }
            else
            {
                //Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>();
                Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
                Item.shootSpeed = 2.5f;
                Item.useTime = 25;
                Item.useAnimation = 25;
                Item.damage = 96;
                Item.UseSound = SoundID.Item28;
            }

            return true;
        }
        
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Ichor, 1800);
            target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.AddNPCBuff, number: target.whoAmI, number2: ModContent.BuffType<Buffs.DispelShadow>(), number3: 36000);
                ModPacket shadowPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                shadowPacket.Write((byte)tsorcPacketID.DispelShadow);
                shadowPacket.Write(target.whoAmI);
                shadowPacket.Send();
            }
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            target.AddBuff(BuffID.Ichor, 1800);
            target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.AddNPCBuff, number: target.whoAmI, number2: ModContent.BuffType<Buffs.DispelShadow>(), number3: 36000);
                ModPacket shadowPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                shadowPacket.Write((byte)tsorcPacketID.DispelShadow);
                shadowPacket.Write(target.whoAmI);
                shadowPacket.Send();
            }
        }
    }
}
