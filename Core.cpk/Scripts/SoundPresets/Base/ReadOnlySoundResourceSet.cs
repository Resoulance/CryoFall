﻿namespace AtomicTorch.CBND.CoreMod.SoundPresets
{
    using System.Collections;
    using System.Collections.Generic;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.GameEngine.Common.DataStructures;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class ReadOnlySoundResourceSet : IEnumerable<SoundResource>
    {
        private readonly ArrayWithWeights<SoundResource> sounds;

        public ReadOnlySoundResourceSet(List<ValueWithWeight<SoundResource>> sounds)
        {
            this.sounds = new ArrayWithWeights<SoundResource>(sounds);
        }

        public int Count => this.sounds.Count;

        public SoundResourceSet Clone()
        {
            return new SoundResourceSet(this.sounds.ToList());
        }

        public IEnumerator<SoundResource> GetEnumerator()
        {
            foreach (var entry in this.sounds)
            {
                yield return entry.Value;
            }
        }

        public SoundResource GetSound(object soundPreset = null, object repetitionProtectionKey = null)
        {
            if (this.sounds.Count == 0)
            {
                return SoundResource.NoSound;
            }

            if (this.sounds.Count == 1)
            {
                return this.sounds.GetNonRandomFirst();
            }

            if (soundPreset == null
                || repetitionProtectionKey == null)
            {
                return this.sounds.GetSingleRandomElement();
            }

            return ClientSoundRepetitionProtectionManager.ClientGetSound(
                this.sounds,
                soundPreset,
                repetitionProtectionKey);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}