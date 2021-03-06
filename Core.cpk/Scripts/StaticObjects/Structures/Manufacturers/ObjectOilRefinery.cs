﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Manufacturers
{
    using System.Windows.Media;
    using AtomicTorch.CBND.CoreMod.ClientComponents.Rendering.Lighting;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectOilRefinery : ProtoObjectOilRefinery
    {
        private readonly TextureAtlasResource textureAtlasOilPumpActive;

        public ObjectOilRefinery()
        {
            this.textureAtlasOilPumpActive = new TextureAtlasResource(
                this.GenerateTexturePath() + "Active",
                columns: 2,
                rows: 4,
                isTransparent: false);
        }

        public override byte ContainerInputSlotsCount => 1;

        public override byte ContainerOutputSlotsCount => 1;

        public override string Description =>
            "Refines raw petroleum oil into more useful components, such as fuel and mineral oil.";

        public override double ElectricityConsumptionPerSecondWhenActive => 2;

        public override double LiquidCapacityGasoline => 100;

        public override double LiquidCapacityMineralOil => 100;

        public override double LiquidCapacityRawPetroleum => 100;

        // A little larger number is used for liquids production speed
        // to fix an issue with the floating point precision
        // (e.g. a case when 0.0001 liquid level shortage prevents from collecting a full canister).
        // With the adjusted number we have a barely noticeable profit which resolves the issue.
        public override double LiquidGasolineProductionPerSecond => 0.2002; 

        public override double LiquidMineralOilProductionPerSecond => 0.2002;

        public override double LiquidRawPetroleumConsumptionPerSecond => 0.2;

        public override string Name => "Oil refinery";

        public override ObjectMaterial ObjectMaterial => ObjectMaterial.Metal;

        public override double ObstacleBlockDamageCoef => 1.0;

        public override float StructurePointsMax => 10000;

        protected override void ClientInitialize(ClientInitializeData data)
        {
            base.ClientInitialize(data);
            var worldObject = data.GameObject;

            // setup light source
            var lightSource = ClientLighting.CreateLightSourceSpot(
                worldObject.ClientSceneObject,
                color: Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF),
                size: 1,
                spritePivotPoint: (0.5, 0.5),
                positionOffset: (1.47, 0.78));

            var animatedSpritePositionOffset = (277 / (double)ScriptingConstants.TileSizeRealPixels,
                                                171 / (double)ScriptingConstants.TileSizeRealPixels);
            this.ClientSetupManufacturerActiveAnimation(
                worldObject,
                data.PublicState,
                this.textureAtlasOilPumpActive,
                animatedSpritePositionOffset,
                frameDurationSeconds: 0.3,
                autoInverseAnimation: true,
                randomizeInitialFrame: true,
                onRefresh: isActive => lightSource.IsEnabled = isActive);

            data.ClientState.SoundEmitter.CustomMaxDistance = 10f;
            data.ClientState.SoundEmitter.Volume = 0.5f;
        }

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = 1.8;
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("###",
                         "###",
                         "###");
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryIndustry>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Medium;
            build.AddStageRequiredItem<ItemCement>(count: 10);
            build.AddStageRequiredItem<ItemIngotSteel>(count: 3);
            build.AddStageRequiredItem<ItemIngotCopper>(count: 3);
            
            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Medium;
            repair.AddStageRequiredItem<ItemIngotSteel>(count: 2);
            repair.AddStageRequiredItem<ItemIngotCopper>(count: 2);
        }

        protected override void PrepareDefense(DefenseDescription defense)
        {
            defense.Set(ObjectDefensePresets.Tier1);
        }

        protected override void SharedCreatePhysics(
            CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle((3, 2),     (0, 0))
                .AddShapeRectangle((2.8, 1.7), (0.1, 0.7), CollisionGroups.HitboxMelee)
                .AddShapeRectangle((2.8, 1.6), (0.1, 0.8), CollisionGroups.HitboxRanged)
                .AddShapeRectangle((3, 2.6),   (0, 0),     CollisionGroups.ClickArea);
        }
    }
}