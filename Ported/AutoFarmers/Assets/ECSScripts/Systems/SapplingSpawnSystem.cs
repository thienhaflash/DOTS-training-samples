﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SapplingSpawnSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var gameState = GetSingletonEntity<GameState>();
        var prefab = GetComponent<GameState>(gameState).PlantPrefab;
        
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<Tilled>()
            .WithAll<Plains>()
            .WithNone<CropReference>()
            .ForEach((
                Entity entity
                , int entityInQueryIndex
                , ref Tilled tilled
                , in Translation translation
            ) =>
            {
                var sapplingEntity = ecb.Instantiate(entityInQueryIndex, prefab);
                ecb.AddComponent<Sappling>(entityInQueryIndex, sapplingEntity);
                ecb.SetComponent(entityInQueryIndex, sapplingEntity, new Sappling {
                    age = 0.0f,
                    tileEntity = entity
                });
                ecb.AddComponent<MaterialOverride>(entityInQueryIndex, sapplingEntity);
                ecb.SetComponent(entityInQueryIndex, sapplingEntity, new Translation {Value = translation.Value});
                ecb.AddComponent<NonUniformScale>(entityInQueryIndex, sapplingEntity);
                
                ecb.AddComponent<CropReference>(entityInQueryIndex, entity);
                ecb.SetComponent(entityInQueryIndex, entity, new CropReference {crop = sapplingEntity});
                
                tilled.FertilityLeft--;
                if (tilled.FertilityLeft <= 0)
                {
                    ecb.RemoveComponent<Tilled>(entityInQueryIndex, entity);
                }

            }).ScheduleParallel();
        
        m_ECBSystem.AddJobHandleForProducer(Dependency);

    }
}
