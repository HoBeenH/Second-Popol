using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Dragon
{
    public class DragonPattern : MonoSingleton<DragonPattern>
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
            {EPattern.Bite, typeof(G_Dragon_Bite)},
            {EPattern.Tail, typeof(G_Dragon_Tail)},
            {EPattern.Breath, typeof(G_Dragon_Breath)},
            {EPattern.FlyBreath, typeof(G_Dragon_FlyBreath)},
            {EPattern.FlyAttack, typeof(G_Dragon_FlyAttack)},
            {EPattern.Ultimate, typeof(G_Dragon_Ultimate)},
        };

        public List<Pattern> patternList;

        [System.Serializable]
        public class Pattern
        {
            public EPattern[] enumState;
            public List<Type> nextPattern;
            [HideInInspector] public bool isEnd;
            [HideInInspector] public int index;
            [HideInInspector] public int length;

            public void Init(Dictionary<EPattern, Type> state)
            {
                nextPattern = new List<Type>();
                foreach (var i in enumState)
                {
                    nextPattern.Add(state[i]);
                }

                isEnd = false;
                index = 0;
                length = nextPattern.Count;
            }

            public Type GetPattern()
            {
                var _pattern = nextPattern[index];
                index += 1;
                if (length == index)
                {
                    index = 0;
                    isEnd = true;
                }

                return _pattern;
            }
        }

        private int m_PatternIndex = 0;

        private void Awake()
        {
            foreach (var x in patternList)
            {
                x.Init(m_FindType);
            }
        }

        public Type SetPattern()
        {
            if (patternList[m_PatternIndex].isEnd)
            {
                patternList[m_PatternIndex].isEnd = false;
                m_PatternIndex += 1;
                if (m_PatternIndex == patternList.Count)
                {
                    m_PatternIndex = 0;
                }
            }

            return patternList[m_PatternIndex].GetPattern();
        }
    }
}