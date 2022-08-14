using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Items.VanillaItems
{
    class VanillaPotions : GlobalItem
    {
        public static float WellFed1Consumed = 0; 
        public static float WellFed2Consumed = 0; 
        public static float WellFed3Consumed = 0;
        public static float IronskinConsumed = 0;
        public static float RegenConsumed = 0;
        public static float SwiftnessConsumed = 0;
        public static float AlcoholConsumed = 0;
        public static float FlaskPoisonConsumed = 0;
        public static float FlaskFConsumed = 0;
        public static float FlaskCConsumed = 0;
        public static float FlaskIConsumed = 0;
        public static float FlaskVConsumed = 0;
        public static float FlaskGConsumed = 0;
        public static float FlaskNConsumed = 0;
        public static float FlaskPartyConsumed = 0;
        public static float ArcheryConsumed = 0;
        public static float AmmoResConsumed = 0;
        public static float MagicPowerConsumed = 0;
        public static float ManaRegenConsumed = 0;
        public static float SummoningConsumed = 0;
        public static float EnduranceConsumed = 0;
        public static float LuckSConsumed = 0;
        public static float LuckMConsumed = 0;
        public static float LuckGConsumed = 0;
        public static float LifeforceConsumed = 0;
        public static float MiningConsumed = 0;
        public static float ShineConsumed = 0;
        public static float WrathConsumed = 0;
        public static float RageConsumed = 0;
        public static float ThornsConsumed = 0;
        public override bool? UseItem(Item item, Player player)
        {
            if(item.type == ItemID.Teacup)
            {
                WellFed1Consumed += 1;
            }
            if (item.type == ItemID.BowlofSoup)
            {
                WellFed2Consumed += 1;
            }
            if (item.type == ItemID.GoldenDelight)
            {
                WellFed3Consumed += 1;
            }
            if (item.type == ItemID.IronskinPotion)
            {
                IronskinConsumed += 1;
            }
            if (item.type == ItemID.RegenerationPotion)
            {
                RegenConsumed += 1;
            }
            if (item.type == ItemID.SwiftnessPotion)
            {
                SwiftnessConsumed += 1;
            }
            if (item.type == ItemID.Ale | item.type == ItemID.Sake)
            {
                AlcoholConsumed += 1;
            }
            if (item.type == ItemID.FlaskofPoison)
            {
                FlaskPoisonConsumed += 1;
            }
            if (item.type == ItemID.FlaskofFire)
            {
                FlaskFConsumed += 1;
            }
            if (item.type == ItemID.FlaskofCursedFlames)
            {
                FlaskCConsumed += 1;
            }
            if (item.type == ItemID.FlaskofIchor)
            {
                FlaskIConsumed += 1;
            }
            if (item.type == ItemID.FlaskofVenom)
            {
                FlaskVConsumed += 1;
            }
            if (item.type == ItemID.FlaskofGold)
            {
                FlaskGConsumed += 1;
            }
            if (item.type == ItemID.FlaskofNanites)
            {
                FlaskNConsumed += 1;
            }
            if (item.type == ItemID.FlaskofParty)
            {
                FlaskPartyConsumed += 1;
            }
            if (item.type == ItemID.ArcheryPotion)
            {
                ArcheryConsumed += 1;
            }
            if (item.type == ItemID.AmmoReservationPotion)
            {
                AmmoResConsumed += 1;
            }
            if (item.type == ItemID.MagicPowerPotion)
            {
                MagicPowerConsumed += 1;
            }
            if (item.type == ItemID.ManaRegenerationPotion)
            {
                ManaRegenConsumed += 1;
            }
            if (item.type == ItemID.SummoningPotion)
            {
                SummoningConsumed += 1;
            }
            if (item.type == ItemID.EndurancePotion)
            {
                EnduranceConsumed += 1;
            }
            if (item.type == ItemID.LuckPotionLesser)
            {
                LuckSConsumed += 1;
            }
            if (item.type == ItemID.LuckPotion)
            {
                LuckMConsumed += 1;
            }
            if (item.type == ItemID.LuckPotionGreater)
            {
                LuckGConsumed += 1;
            }
            if (item.type == ItemID.LifeforcePotion)
            {
                LifeforceConsumed += 1;
            }
            if (item.type == ItemID.MiningPotion)
            {
                MiningConsumed += 1;
            }
            if (item.type == ItemID.ShinePotion)
            {
                ShineConsumed += 1;
            }
            if (item.type == ItemID.WrathPotion)
            {
                WrathConsumed += 1;
            }
            if (item.type == ItemID.RagePotion)
            {
                RageConsumed += 1;
            }
            if (item.type == ItemID.ThornsPotion)
            {
                ThornsConsumed += 1;
            }
            return true;
        }
    }
}