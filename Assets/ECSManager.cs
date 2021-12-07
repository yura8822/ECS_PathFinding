using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class ECSManager : MonoBehaviour
{
    [SerializeField] private int countCharacter = 100;
    [SerializeField] private GameObject characterPrefab;

    EntityManager manager;

    private BlobAssetStore blobAssetStore;
    void Start()
    {
        blobAssetStore = new BlobAssetStore();
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);

        //преобразуем префабы в сущности
        Entity character = GameObjectConversionUtility.ConvertGameObjectHierarchy(characterPrefab, settings);

        //создаем сущности, инициализируем компоненты
        for (int i = 0; i < countCharacter; i++)
        {
            Entity characterEntity = manager.Instantiate(character);

            //устанавливаем стартовую позицию
            float x = UnityEngine.Random.Range(20, 200);
            float y = 1.5f;
            float z = UnityEngine.Random.Range(20, 200);
            manager.SetComponentData(characterEntity, new Translation { Value = new float3(x, y, z) });

            //устанавливаем случайный угол поворота
            Quaternion quaternion = Quaternion.Euler(new Vector3(0,UnityEngine.Random.Range(0f, 360f),0));
            manager.SetComponentData(characterEntity, new Rotation{Value = quaternion});

            //добавляем компонент с данными персонажа
            float speedMovement = UnityEngine.Random.Range(600, 900);
            float speedRotation = UnityEngine.Random.Range(3, 5);
            manager.AddComponentData(characterEntity, new CharacterData { movementSpeed = speedMovement, rotationSpeed = speedRotation });

            //добавляем динамический буфер для хранения путевых точек
            manager.AddBuffer<PathPositionBuffer>(characterEntity);
            //добавляем компонент для хранения, индекса соответствующего текущей точки в буфере
            manager.AddComponentData(characterEntity, new PathFollowData { pathIndex = -1 });
        }
    }

    void OnDestroy()
    {
        blobAssetStore.Dispose();
    }


}
