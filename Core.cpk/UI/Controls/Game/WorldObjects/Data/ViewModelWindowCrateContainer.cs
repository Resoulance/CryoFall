﻿namespace AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects.Data
{
    using System;
    using AtomicTorch.CBND.CoreMod.Helpers.Client;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Crates;
    using AtomicTorch.CBND.CoreMod.Systems.Creative;
    using AtomicTorch.CBND.CoreMod.Systems.WorldObjectOwners;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Items.Data;
    using AtomicTorch.CBND.GameApi.Data.World;

    public class ViewModelWindowCrateContainer : BaseViewModel
    {
        public ViewModelWindowCrateContainer(
            IStaticWorldObject worldObjectCrate,
            ObjectCratePrivateState privateState,
            Action callbackCloseWindow)
        {
            this.WorldObjectCrate = worldObjectCrate;

            this.ViewModelItemsContainerExchange = new ViewModelItemsContainerExchange(
                    privateState.ItemsContainer,
                    callbackCloseWindow)
                {
                    IsContainerTitleVisible = false
                };

            var isOwner = WorldObjectOwnersSystem.SharedIsOwner(
                ClientCurrentCharacterHelper.Character,
                worldObjectCrate);

            this.ViewModelOwnersEditor = new ViewModelWorldObjectOwnersEditor(
                privateState.Owners,
                canEditOwners: isOwner
                               || CreativeModeSystem.ClientIsInCreativeMode(),
                callbackServerSetOwnersList:
                ownersList => WorldObjectOwnersSystem.ClientSetOwners(this.WorldObjectCrate,
                                                                      ownersList),
                title: CoreStrings.ObjectOwnersList_Title + ":");

            this.ViewModelAccessModeEditor = new ViewModelWorldObjectAccessModeEditor(
                worldObjectCrate,
                canSetAccessMode: isOwner);
        }

        public bool HasOwnersList
        {
            get
            {
                var protoObject = (IProtoObjectWithOwnersList)this.WorldObjectCrate.ProtoStaticWorldObject;
                return protoObject.HasOwnersList;
            }
        }

        public ViewModelWorldObjectAccessModeEditor ViewModelAccessModeEditor { get; }

        public ViewModelItemsContainerExchange ViewModelItemsContainerExchange { get; }

        public ViewModelWorldObjectOwnersEditor ViewModelOwnersEditor { get; }

        public IStaticWorldObject WorldObjectCrate { get; }
    }
}