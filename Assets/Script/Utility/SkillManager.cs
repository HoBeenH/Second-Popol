using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script
{
    // 스킬 쿨타임 계산 스크립트
    public class SkillManager : MonoBehaviour
    {
        private static readonly List<Skill> SkillList = new List<Skill>();

        public class Skill
        {
            public readonly Type name;
            public float coolTime;
            public readonly float maxTime;
            public bool isActive = true;

            public Skill(Type type, float time)
            {
                this.name = type;
                this.coolTime = time;
                this.maxTime = time;
            }
        }

        private void Update()
        {
            CheckCoolDown();
        }

        public static void AddSkill(Type type, float time)
        {
            SkillList.Add(new Skill(type, time));
        }

        public static Skill FindSkill(Type type)
        {
            return SkillList.FirstOrDefault(value => value.name == type);
        }

        private void CheckCoolDown()
        {
            foreach (var skill in SkillList.Where(skill => skill.isActive == false))
            {
                skill.coolTime -= Time.deltaTime;
                if (skill.coolTime <= 0f)
                {
                    skill.coolTime = skill.maxTime;
                    skill.isActive = true;
                }
            }
        }
    }
}