﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Fridges
{
    using AtomicTorch.CBND.CoreMod.ItemContainers;
    using AtomicTorch.CBND.CoreMod.Items.Food;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectFridgeEvaporator : ProtoObjectFridge
    {
        public override string Description =>
            "Primitive cooler that uses evaporation as its primary cooling method. Isn't as effective as modern fridges, but will still keep your food fresh for longer.";

        public override byte ItemsSlotsCount => 4;

        public override string Name => "Primitive fridge";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.Wood;

        public override double ObstacleBlockDamageCoef => 1;

        public override float StructurePointsMax => 150;

        // different type of fridge
        protected override IProtoItemsContainer ItemsContainerType
            => Api.GetProtoEntity<ItemsContainerFridgeEvaporator>();

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY += 0.25;
            renderer.PositionOffset += (0, 0.1);
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryStorage>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemPlanks>(count: 5);
            build.AddStageRequiredItem<ItemIngotCopper>(count: 1);
            build.AddStageRequiredItem<ItemBottleWater>(count: 1);

            repair.StagesCount = 5;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemPlanks>(count: 5);
            repair.AddStageRequiredItem<ItemIngotCopper>(count: 1);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle(
                    size: (0.7, 0.45),
                    offset: (0.15, 0.15))
                .AddShapeRectangle(
                    size: (0.8, 1.1),
                    offset: (0.1, 0.1),
                    group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle(
                    size: (0.8, 1.1),
                    offset: (0.1, 0.1),
                    group: CollisionGroups.HitboxRanged)
                .AddShapeRectangle(
                    size: (0.8, 1.1),
                    offset: (0.1, 0.1),
                    group: CollisionGroups.ClickArea);
        }
    }
}