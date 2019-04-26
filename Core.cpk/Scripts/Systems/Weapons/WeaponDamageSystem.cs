﻿namespace AtomicTorch.CBND.CoreMod.Systems.Weapons
{
    using System;
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.Items;
    using AtomicTorch.CBND.CoreMod.StaticObjects;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Explosives;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Systems.CharacterDeath;
    using AtomicTorch.CBND.CoreMod.Systems.NewbieProtection;
    using AtomicTorch.CBND.CoreMod.Systems.Party;
    using AtomicTorch.CBND.GameApi.Data;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Weapons;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Helpers;

    public static class WeaponDamageSystem
    {
        public static double ServerCalculateTotalDamage(
            WeaponFinalCache weaponCache,
            IWorldObject targetObject,
            FinalStatsCache targetFinalStatsCache,
            double damagePreMultiplier,
            bool clampDefenseTo1)
        {
            if (weaponCache.ProtoObjectExplosive != null
                && targetObject.ProtoWorldObject is IProtoStaticWorldObject targetStaticWorldObjectProto)
            {
                // special case - apply the explosive damage
                return ServerCalculateTotalDamageByExplosive(weaponCache.ProtoObjectExplosive,
                                                             targetStaticWorldObjectProto,
                                                             damagePreMultiplier);
            }

            bool isPvPcase = false,
                 isFriendlyFireCase = false;

            var damagingCharacter = weaponCache.Character;

            if (targetObject is ICharacter targetCharacter
                && !targetCharacter.IsNpc
                && damagingCharacter != null
                && !damagingCharacter.IsNpc
                && targetCharacter != damagingCharacter)
            {
                // PvP detected
                isPvPcase = true;
                if (WeaponConstants.DamagePvpMultiplier <= 0)
                {
                    // PvP damage disabled
                    return 0;
                }

                // let's check whether the players in the same party to detect the friendly fire
                var targetParty = PartySystem.ServerGetParty(targetCharacter);
                if (targetParty != null)
                {
                    var damagingParty = PartySystem.ServerGetParty(damagingCharacter);
                    if (targetParty == damagingParty)
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (WeaponConstants.DamageFriendlyFireMultiplier <= 0)
                        {
                            // no damage as friendly fire is completely disabled
                            return 0;
                        }

                        // friendly fire detected
                        isFriendlyFireCase = true;
                    }
                }
            }

            var damageValue = damagePreMultiplier * weaponCache.DamageValue;
            var invertedArmorPiercingCoef = weaponCache.InvertedArmorPiercingCoef;

            var totalDamage = 0d;

            // calculate total damage by summing all the damage components
            foreach (var damageDistribution in weaponCache.DamageDistributions)
            {
                var defenseStatName = SharedGetDefenseStatName(damageDistribution.DamageType);
                var defenseFraction = targetFinalStatsCache[defenseStatName];
                defenseFraction = MathHelper.Clamp(defenseFraction, 0, clampDefenseTo1 ? 1 : double.MaxValue);

                totalDamage += ServerCalculateDamageComponent(
                    damageValue,
                    invertedArmorPiercingCoef,
                    damageDistribution,
                    defenseFraction);
            }

            // multiply on final multiplier (usually used for expanding projectiles)
            totalDamage *= weaponCache.FinalDamageMultiplier;

            if (isPvPcase)
            {
                // apply PvP damage multiplier
                totalDamage *= WeaponConstants.DamagePvpMultiplier;
            }
            else if (damagingCharacter != null
                     && !damagingCharacter.IsNpc)
            {
                // apply PvE damage multiplier
                totalDamage *= WeaponConstants.DamagePveMultiplier;
            }
            else if (damagingCharacter != null
                     && damagingCharacter.IsNpc)
            {
                // apply creature damage multiplier
                totalDamage *= WeaponConstants.DamageCreaturesMultiplier;
            }

            if (isFriendlyFireCase)
            {
                totalDamage *= WeaponConstants.DamageFriendlyFireMultiplier;
            }

            return totalDamage;
        }

        public static bool SharedOnDamageToCharacter(
            ICharacter targetCharacter,
            WeaponFinalCache weaponCache,
            double damageMultiplier,
            out double damageApplied)
        {
            var targetPublicState = targetCharacter.GetPublicState<ICharacterPublicState>();
            var targetCurrentStats = targetPublicState.CurrentStats;

            if (targetCurrentStats.HealthCurrent <= 0)
            {
                // target character is dead, cannot apply damage to it
                damageApplied = 0;
                return false;
            }

            if (!targetCharacter.IsNpc
                && weaponCache.Character is ICharacter damagingCharacter
                && NewbieProtectionSystem.SharedIsNewbie(damagingCharacter))
            {
                // no damage from newbie
                damageApplied = 0;
                if (Api.IsClient)
                {
                    // display message to newbie
                    NewbieProtectionSystem.ClientShowNewbieCannotDamageOtherPlayersOrLootBags(isLootBag: false);
                }

                return false;
            }

            if (Api.IsClient)
            {
                // we don't simulate the damage on the client side
                damageApplied = 0;
                return true;
            }

            var attackerCharacter = weaponCache.Character;

            // calculate and apply damage on server
            var targetFinalStatsCache =
                targetCharacter.GetPrivateState<BaseCharacterPrivateState>()
                               .FinalStatsCache;

            var totalDamage = ServerCalculateTotalDamage(
                weaponCache,
                targetCharacter,
                targetFinalStatsCache,
                damageMultiplier,
                clampDefenseTo1: true);
            if (totalDamage <= 0)
            {
                // damage suppressed by armor
                damageApplied = 0;
                return true;
            }

            // Clamp the max receivable damage to x5 from the max health.
            // This will help in case when the too much damage is dealt (mega-bomb!) 
            // to ensure the equipment will not receive excessive damaged.
            totalDamage = Math.Min(totalDamage, 5 * targetCurrentStats.HealthMax);

            // apply damage
            if (attackerCharacter != null)
            {
                targetCurrentStats.ServerReduceHealth(totalDamage, damageSource: attackerCharacter);
            }
            else if (weaponCache.ProtoObjectExplosive != null)
            {
                targetCurrentStats.ServerReduceHealth(totalDamage, damageSource: weaponCache.ProtoObjectExplosive);
            }
            else
            {
                Api.Logger.Warning("Unknown damage kind, unable to add it to damage tracking stats", targetCharacter);
                targetCurrentStats.ServerReduceHealth(totalDamage, damageSource: (IProtoGameObject)null);
            }

            Api.Logger.Info(
                $"Damage applied to {targetCharacter} by {attackerCharacter}:\n{totalDamage} dmg, current health {targetCurrentStats.HealthCurrent}/{targetCurrentStats.HealthMax}, {weaponCache.Weapon}");

            if (targetCurrentStats.HealthCurrent <= 0)
            {
                // killed!
                ServerCharacterDeathMechanic.OnCharacterKilled(
                    targetCharacter,
                    attackerCharacter,
                    weaponCache.Weapon,
                    weaponCache.ProtoWeapon);
            }

            damageApplied = totalDamage;
            ServerApplyDamageToEquippedItems(targetCharacter, damageApplied);

            return true;
        }

        private static void ServerApplyDamageToEquippedItems(ICharacter targetCharacter, double damageApplied)
        {
            if (targetCharacter.IsNpc)
            {
                return;
            }

            // reduce equipped items durability
            // we're using temporary list here to prevent issues
            // when an item is destroyed during enumeration
            using (var tempList = Api.Shared.WrapInTempList(targetCharacter.SharedGetPlayerContainerEquipment()
                                                                           .Items))
            {
                foreach (var item in tempList)
                {
                    if (item.ProtoItem is IProtoItemWithDurablity protoItemWithDurablity)
                    {
                        try
                        {
                            protoItemWithDurablity.ServerOnItemDamaged(item, damageApplied);
                        }
                        catch (Exception ex)
                        {
                            Api.Logger.Exception(ex);
                        }
                    }
                }
            }
        }

        private static double ServerCalculateDamageComponent(
            double damageValue,
            double invertedArmorPiercingCoef,
            DamageProportion damageProportion,
            double defenseFraction)
        {
            if (defenseFraction >= ObjectDefensePresets.Max)
            {
                // non-damageable defense
                return 0;
            }

            // decrease defense according to the armor piercing coefficient
            defenseFraction *= invertedArmorPiercingCoef;

            // calculate resistance parameters
            var resistanceValue = defenseFraction * 10;
            var resistanceFraction = MathHelper.Clamp(defenseFraction, 0, 1) * 0.75;

            // subtract resistance absolute value
            var damage = damageValue - resistanceValue;
            if (damage <= 0)
            {
                // damage is completely blocked by the armor
                return 0;
            }

            // subtract resistance fraction value
            damage *= 1 - resistanceFraction;

            // multiply on the damage proportion for current damage type
            damage *= damageProportion.Proportion;

            return damage;
        }

        private static double ServerCalculateTotalDamageByExplosive(
            IProtoObjectExplosive protoObjectExplosive,
            IProtoStaticWorldObject targetStaticWorldObjectProto,
            double damagePreMultiplier)
        {
            var damage = protoObjectExplosive.ServerCalculateTotalDamageByExplosive(targetStaticWorldObjectProto);
            damage *= damagePreMultiplier;
            return damage;
        }

        private static StatName SharedGetDefenseStatName(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Impact:
                    return StatName.DefenseImpact;

                case DamageType.Kinetic:
                    return StatName.DefenseKinetic;

                case DamageType.Heat:
                    return StatName.DefenseHeat;

                case DamageType.Cold:
                    return StatName.DefenseCold;

                case DamageType.Chemical:
                    return StatName.DefenseChemical;

                case DamageType.Electrical:
                    return StatName.DefenseElectrical;

                case DamageType.Radiation:
                    return StatName.DefenseRadiation;

                case DamageType.Psi:
                    return StatName.DefensePsi;

                default:
                    throw new ArgumentOutOfRangeException(nameof(damageType), damageType, null);
            }
        }
    }
}