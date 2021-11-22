using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Script
{
    public enum ESpawnType
    {
        Back,
        Down
    }
    public class Dragon_SkillSpawn : MonoBehaviour
    {
        [HideInInspector]public ESpawnType type;
        public float delay;
        public int loop = 10;
        protected Vector3 size;
        protected Vector3 pivot;

        protected WaitForSeconds patternDelay;

        protected void Init()
        {
            var _col = GetComponent<BoxCollider>();
            size = _col.size;
            pivot = transform.position;
            patternDelay = new WaitForSeconds(delay);
            this.gameObject.SetActive(false);
        }
        
        [BurstCompile]
        private struct GetRandomPosJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<Vector3> pos;
            [ReadOnly] public NativeArray<uint> seed;
            [ReadOnly] public float size1;
            [ReadOnly] public float size2;
            [ReadOnly] public Vector3 pivot;
            [ReadOnly] public ESpawnType type;

            public void Execute([ReadOnly] int i)
            {
                var _random1 = new Random(seed[i]).NextFloat(-size1 * 0.5f, size1 * 0.5f);
                var _random2 = new Random(seed[i + 1]).NextFloat(-size2 * 0.5f, size2 * 0.5f);
                pos[i] = type switch
                {
                    ESpawnType.Back => new Vector3(_random1, _random2, 0f) + pivot,
                    ESpawnType.Down => new Vector3(_random1, 1f, _random2) + pivot,
                    _ => pos[i]
                };
            }
        }

        protected static Vector3[] CreateRandomPos(ESpawnType spawnType, float between1, float between2, int length,
            Vector3 size)
        {
            var seeds = new NativeArray<uint>(length + 1, Allocator.TempJob);
            for (var i = 0; i < seeds.Length; i++)
            {
                seeds[i] = (uint) UnityEngine.Random.Range(uint.MinValue + 1, uint.MaxValue);
            }

            var _pos = new NativeArray<Vector3>(length, Allocator.TempJob);
            var _getRandom = new GetRandomPosJob
            {
                pos = _pos,
                seed = seeds,
                size1 = between1,
                size2 = between2,
                pivot = size,
                type = spawnType
            };
            var _posHandle = _getRandom.Schedule(length, length);
            _posHandle.Complete();
            seeds.Dispose();
            var _returnPos = new Vector3[length];
            for (var i = 0; i < length; i++)
            {
                _returnPos[i] = _pos[i];
            }

            _pos.Dispose();
            return _returnPos;
        }
    }
}