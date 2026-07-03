namespace Ciga2026.Framework.StateMachine
{
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float fixedDeltaTime);
        void LateTick(float deltaTime);
        void Exit();
    }
}
