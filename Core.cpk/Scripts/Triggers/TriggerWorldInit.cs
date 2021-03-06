﻿namespace AtomicTorch.CBND.CoreMod.Triggers
{
    using AtomicTorch.CBND.GameApi;
    using AtomicTorch.CBND.GameApi.Scripting;

    public class TriggerWorldInit : ProtoTriggerNonConfigurable
    {
        [NotLocalizable]
        public override string Name => "World init trigger";

        public void OnWorldInit()
        {
            if (!Api.Server.Database.TryGet("Core", "IsWorldSpawned", out bool isWorldSpawned)
                || !isWorldSpawned)
            {
                Api.Server.Database.Set("Core", "IsWorldSpawned", true);
            }

            if (isWorldSpawned
                && Api.Shared.IsDebug)
            {
                // in debug mode, don't invoke world init trigger as the world is already spawned with objects
                return;
            }

            this.Invoke();
        }
    }
}