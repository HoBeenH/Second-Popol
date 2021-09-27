using UnityEngine;

namespace Script
{
    public abstract class State<T>
    {
        public readonly int animToHash;
        protected StateMachine<T> machine;
        protected T owner;

        protected State()
        {
        }

        protected State(string animName) : this(Animator.StringToHash(animName))
        {
        }

        private State(int animHash) => animToHash = animHash;

        public void AddState(StateMachine<T> currentMachine, T currentOwner)
        {
            this.machine = currentMachine;
            this.owner = currentOwner;
            Init();
        }

        protected virtual void Init()
        {
        }

        public virtual void OnStateEnter()
        {
        }

        public virtual void OnStateUpdate()
        {
        }

        public virtual void OnStateFixedUpdate()
        {
        }

        public virtual void OnStateExit()
        {
        }

        public virtual void OnStateChangePoint()
        {
        }
    }
}