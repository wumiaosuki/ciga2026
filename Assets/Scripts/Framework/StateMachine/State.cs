namespace Ciga2026.Framework.StateMachine
{
    public abstract class State : IState
    {
        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void FixedTick(float fixedDeltaTime)
        {
        }

        public virtual void LateTick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}
