using System;
using System.Collections.Generic;
using Script.Dragon.FSM;
using UnityEngine;

namespace Script.Dragon
{
    public class Dragon_Pattern : MonoSingleton<Dragon_Pattern>
    {
        public enum EPattern
        {
            Bite,
            Tail,
            Breath,
            FlyBreath,
            FlyAttack,
            Ultimate
        }

        private readonly Dictionary<EPattern, Type> m_FindType = new Dictionary<EPattern, Type>
        {
            {EPattern.Bite, typeof(Dragon_Bite)},
            {EPattern.Tail, typeof(Dragon_Tail)},
            {EPattern.Breath, typeof(Dragon_Breath)},
            {EPattern.FlyBreath, typeof(Dragon_FlyBreath)},
            {EPattern.FlyAttack, typeof(Dragon_FlyAttack)},
            {EPattern.Ultimate, typeof(Dragon_Ultimate)},
        };

        public Pattern[] patternList;

        [System.Serializable]
        public class Pattern
        {
            public EPattern[] enumState;
            public Type[] nextPattern;
            [HideInInspector] public bool isEnd;
            [HideInInspector] public int index;
            [HideInInspector] public int length;

            public void Init(Dictionary<EPattern, Type> state)
            {
                index = 0;
                length = enumState.Length;
                nextPattern = new Type[length];
                for (var i = 0; i < length; i++)
                {
                    nextPattern[i] = state[enumState[i]];
                }
                isEnd = false;
            }

            public Type GetPattern()
            {
                var _pattern = nextPattern[index];
                index += 1;
                if (index == length)
                {
                    index = 0;
                    isEnd = true;
                }

                return _pattern;
            }
        }

        private int m_Index = 0;

        private void Awake()
        {
            foreach (var x in patternList)
            {
                x.Init(m_FindType);
            }
        }

        public Type NextPattern()
        {
            if (patternList[m_Index].isEnd)
            {
                patternList[m_Index].isEnd = false;
                m_Index += 1;
                if (m_Index == patternList.Length)
                {
                    m_Index = 0;
                }
            }

            return patternList[m_Index].GetPattern();
        }
    }
}