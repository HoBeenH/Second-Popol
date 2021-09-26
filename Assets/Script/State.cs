using Script.Player;
using UnityEngine;

namespace Script
{
    public abstract class State<T>
    {
        public int animToHash;
        public StateMachine<T> machine;
        public T owner;

        public State()
        {
        }

        public State(string animName) : this(Animator.StringToHash(animName))
        {
        }

        public State(int animHash) => animToHash = animHash;

        public void AddState(StateMachine<T> currentMachine, T currentOwner)
        {
            this.machine = currentMachine;
            this.owner = currentOwner;
            Init();
        }

        public virtual void Init()
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