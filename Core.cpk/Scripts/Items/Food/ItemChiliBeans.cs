﻿namespace AtomicTorch.CBND.CoreMod.Items.Food
{
    using System;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Buffs;

    public class ItemChiliBeans : ProtoItemFood
    {
        public override string Description => "The ultimate bachelor food.";

        public override float FoodRestore => 32;

        public override TimeSpan FreshnessDuration => ExpirationDuration.Normal;

        public override string Name => "Hot chili beans";

        public override ushort OrganicValue => 0;

        public override float StaminaRestore => 50;

        protected override void ServerOnEat(ItemEatData data)
        {
            data.Character.ServerAddStatusEffect<StatusEffectHeartyFood>(intensity: 0.3);
            data.Character.ServerAddStatusEffect<StatusEffectSavoryFood>(intensity: 0.1);

            base.ServerOnEat(data);
        }
    }
}