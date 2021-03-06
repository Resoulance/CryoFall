﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.CraftingStations
{
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectWeaponWorkbench : ProtoObjectCraftStation
    {
        public override string Description =>
            "Used to create a variety of different weapons, supplies and ammunition.";

        public override string Name => "Weapon workbench";

        public override ObjectMaterial ObjectMaterial => ObjectMaterial.Wood;

        public override double ObstacleBlockDamageCoef => 0.5;

        public override float StructurePointsMax => 1200;

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = 1.4;
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("##",
                         "##");
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryIndustry>();

            build.StagesCount = 5;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemPlanks>(count: 5);
            build.AddStageRequiredItem<ItemIngotIron>(count: 1);

            repair.StagesCount = 5;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemPlanks>(count: 3);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle((2, 1),      offset: (0, 1))
                .AddShapeRectangle((1, 1),      offset: (1, 0))
                .AddShapeRectangle((2, 1),      offset: (0, 1),   group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle((1, 1),      offset: (1, 0),   group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle((1.8, 0.25), offset: (0.1, 2), group: CollisionGroups.HitboxRanged)
                .AddShapeRectangle((2, 1),      offset: (0, 1),   group: CollisionGroups.ClickArea)
                .AddShapeRectangle((1, 1),      offset: (1, 0),   group: CollisionGroups.ClickArea);
        }
    }
}