using System.Collections;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script
{
    #region DefaultStatus

    public class Status
    {
        public float rotSpeed;
        public int currentHealth;
        public int maxHealth;
        public int damage;
        public int skillDamage;
        public float moveSpeed;
        public float moveAnimDamp;

        public int Health
        {
            get => currentHealth;
            set
            {
                currentHealth = value;
                if (currentHealth > maxHealth)
                {
                    currentHealth = maxHealth;
                }
            }
        }
    }

    #endregion

    #region PlayerStatus

    public class PlayerStatus : Status
    {
        public PlayerStatus()
        {
            this.rotSpeed = 10f;
            this.moveAnimDamp = 0.01f;
            this.skillDamage = 10;
            this.maxHealth = 100;
            this.damage = 10;
            this.moveSpeed = 2f;
            this.Health = maxHealth;
        }
    }

    #endregion

    #region DragonStatus

    public class DragonInfo : Status
    {
        private readonly WaitForSeconds m_RecoverySpeed = new WaitForSeconds(2f);
 
        public float animSpeed = 1.0f;
        public int magicDefence = 3;
        public int defence = 3;
        public int recovery = 1;

        public DragonInfo()
        {
            this.rotSpeed = 2f;
            this.moveAnimDamp = 0.1f;
            this.maxHealth = 200;
            this.skillDamage = 5;
            this.damage = 5;
            this.moveSpeed = 3.5f;
            this.currentHealth = maxHealth;
        }

        public IEnumerator DragonRecovery()
        {
            while (_DragonController.currentPhaseFlag != EDragonPhaseFlag.Dead)
            {
                yield return m_RecoverySpeed;
                Health += recovery;
            }
        }
    }
    #endregion
}