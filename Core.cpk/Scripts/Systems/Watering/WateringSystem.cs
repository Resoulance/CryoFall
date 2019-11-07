﻿namespace AtomicTorch.CBND.CoreMod.Systems.Watering
{
    using System;
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.ClientComponents.StaticObjects;
    using AtomicTorch.CBND.CoreMod.Items;
    using AtomicTorch.CBND.CoreMod.Items.Tools.WateringCans;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Vegetation.Plants;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Systems.ItemDurability;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.CoreMod.Systems.WateringCanRefill;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting.Network;

    public class WateringSystem
        : ProtoActionSystem<
            WateringSystem,
            ItemWorldActionRequest,
            WateringActionState,
            WateringActionState.PublicState>
    {
        public const string NotificationAlreadyWatered = "Already watered";

        public const string NotificationCannotWater_MessageHarvestReady =
            "The plant is ready to be harvested. No point in watering it now.";

        public const string NotificationCannotWater_MessageOnlyFarmPlants =
            "Only farm plants can be watered.";

        public const string NotificationCannotWater_Title = "Cannot water";

        public override string Name => "Watering system";

        public static bool ServerIsWateringRequired(
            IWorldObject objectPlant,
            ICharacter character,
            IProtoItem protoItem,
            IProtoObjectPlant protoPlant,
            double proposedWateringDuration)
        {
            var plantPrivateState = objectPlant.GetPrivateState<PlantPrivateState>();
            var plantPublicState = objectPlant.GetPublicState<PlantPublicState>();
            if ((plantPrivateState.ProducedHarvestsCount == protoPlant.NumberOfHarvests
                 && protoPlant.NumberOfHarvests > 0)
                || plantPublicState.IsSpoiled)
            {
                // no need to water the plant
                Instance.CallClient(character, _ => _.ClientRemote_CannotWaterLastHarvestOrRotten(protoItem));
                return false;
            }

            if (plantPrivateState.ServerTimeWateringEnds >= double.MaxValue
                || (proposedWateringDuration < double.MaxValue
                    && (plantPrivateState.ServerTimeWateringEnds
                        >= Server.Game.FrameTime + proposedWateringDuration - 60)))
            {
                // the plant is already watered enough
                Instance.CallClient(character, _ => _.ClientRemote_CannotWanterAlreadyWatered(protoItem));
                return false;
            }

            return true;
        }

        protected override ItemWorldActionRequest ClientTryCreateRequest(ICharacter character)
        {
            var worldObject = ClientComponentObjectInteractionHelper.MouseOverObject
                                  as IStaticWorldObject;
            if (worldObject == null)
            {
                return null;
            }

            if (worldObject.ProtoWorldObject is IProtoObjectPlant)
            {
                var item = character.SharedGetPlayerSelectedHotbarItem();
                return new ItemWorldActionRequest(character, worldObject, item);
            }

            if (IsClient)
            {
                NotificationSystem.ClientShowNotification(
                    NotificationCannotWater_Title,
                    NotificationCannotWater_MessageOnlyFarmPlants,
                    NotificationColor.Bad,
                    worldObject.ProtoStaticWorldObject.Icon);
            }

            return null;
        }

        protected override void SharedOnActionCompletedInternal(WateringActionState state, ICharacter character)
        {
            var worldObject = (IStaticWorldObject)state.TargetWorldObject;
            var protoPlant = (IProtoObjectPlant)worldObject.ProtoWorldObject;
            var itemWateringCan = state.ItemWateringCan;
            var protoWateringCan = (IProtoItemToolWateringCan)itemWateringCan.ProtoItem;

            if (IsServer)
            {
                protoPlant.ServerOnWatered(character,
                                           worldObject,
                                           wateringDuration: protoWateringCan.WateringDuration.TotalSeconds);
            }

            protoWateringCan.SharedOnWatered(itemWateringCan, worldObject);

            if (IsServer)
            {
                // notify tool was used
                ServerItemUseObserver.NotifyItemUsed(character, itemWateringCan);

                // reduce watering can durability
                ItemDurabilitySystem.ServerModifyDurability(itemWateringCan, delta: -1);
            }
        }

        protected override WateringActionState SharedTryCreateState(ItemWorldActionRequest request)
        {
            var worldObject = request.WorldObject;
            var character = request.Character;

            var item = request.Item;
            if (!(item?.ProtoGameObject is IProtoItemToolWateringCan protoWateringCan))
            {
                // no watering can is selected
                return null;
            }

            if (!protoWateringCan.SharedCanWater(item))
            {
                if (IsClient)
                {
                    // try refill
                    WateringCanRefillSystem.Instance.ClientTryStartAction();
                }
                else
                {
                    Logger.Warning(
                        $"Cannot water now: no water in the watering can - {item} - {worldObject}",
                        character);

                    var waterAmount = item.GetPrivateState<ItemWateringCanPrivateState>()
                                          .WaterAmount;
                    this.CallClient(character,
                                    _ => _.ClientRemote_CannotWaterNotEnoughWaterAmount(item, waterAmount));
                }

                return null;
            }

            var durationSeconds = protoWateringCan.ActionDurationWateringSeconds;
            durationSeconds /= character.SharedGetFinalStatMultiplier(StatName.FarmingTasksSpeed);

            return new WateringActionState(
                character,
                worldObject,
                durationSeconds,
                item);
        }

        protected override void SharedValidateRequest(ItemWorldActionRequest request)
        {
            var character = request.Character;
            var objectPlant = request.WorldObject;
            if (objectPlant == null
                || !(objectPlant.ProtoWorldObject is IProtoObjectPlant protoPlant))
            {
                throw new Exception("The world object must be a plant");
            }

            if (!objectPlant.ProtoWorldObject.SharedCanInteract(character, objectPlant, true))
            {
                throw new Exception("Cannot interact with " + objectPlant);
            }

            if (request.Item != character.SharedGetPlayerSelectedHotbarItem())
            {
                throw new Exception("The item is not selected");
            }

            if (!(request.Item.ProtoItem is IProtoItemToolWateringCan protoItem))
            {
                throw new Exception("The item must be a watering can");
            }

            if (IsServer
                && !ServerIsWateringRequired(objectPlant,
                                             character,
                                             protoItem,
                                             protoPlant,
                                             protoItem.WateringDuration.TotalSeconds))
            {
                throw new Exception("No need to apply - last harvest/already watered/rotten");
            }
        }

        [RemoteCallSettings(DeliveryMode.ReliableSequenced)]
        private void ClientRemote_CannotWanterAlreadyWatered(IProtoItem protoItem)
        {
            NotificationSystem.ClientShowNotification(
                NotificationCannotWater_Title,
                NotificationAlreadyWatered,
                NotificationColor.Bad,
                protoItem.Icon);
        }

        [RemoteCallSettings(DeliveryMode.ReliableSequenced)]
        private void ClientRemote_CannotWaterLastHarvestOrRotten(IProtoItem protoItem)
        {
            NotificationSystem.ClientShowNotification(
                NotificationCannotWater_Title,
                NotificationCannotWater_MessageHarvestReady,
                NotificationColor.Bad,
                protoItem.Icon);
        }

        private void ClientRemote_CannotWaterNotEnoughWaterAmount(IItem item, byte waterAmount)
        {
            var privateState = item.GetPrivateState<ItemWateringCanPrivateState>();
            if (privateState.WaterAmount == waterAmount)
            {
                return;
            }

            Logger.Warning(
                $"Server notified that {item} has different remaining water amount: {waterAmount} (current client water amount: {privateState.WaterAmount}). Correcting the client value.");

            privateState.WaterAmount = waterAmount;

            if (waterAmount == 0)
            {
                // client requested watering but had no water
                // try refill
                WateringCanRefillSystem.Instance.ClientTryStartAction();
            }
        }
    }
}