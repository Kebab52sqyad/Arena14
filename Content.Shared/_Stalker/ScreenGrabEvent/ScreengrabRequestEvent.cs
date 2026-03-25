using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.ScreenGrabEvent
{
    [Serializable, NetSerializable]
    public sealed class ScreengrabRequestEvent : EntityEventArgs
    {
        public Guid guid { get; set; }

        public int i { get; set; }
    }
}
