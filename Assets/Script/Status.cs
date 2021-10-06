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
        public int health;
        public int damage;
        public int skillDamage;
        public float moveSpeed;
        public float moveAnimDamp;
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
            this.damage = 10;
            this.moveSpeed = 2f;
            this.health = 100;
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


        public DragonInfo()
        {
            this.rotSpeed = 2f;
            this.moveAnimDamp = 0.1f;
            this.health = 200;
            this.damage = 5;
            this.moveSpeed = 3.5f;
        }
    }
    #endregion
}