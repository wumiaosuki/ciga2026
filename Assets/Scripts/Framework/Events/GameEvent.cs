namespace Ciga2026.Framework.Events
{
    public readonly struct GameEvent
    {
        public GameEvent(string name, object payload = null)
        {
            Name = name;
            Payload = payload;
        }

        public string Name { get; }
        public object Payload { get; }
    }
}
