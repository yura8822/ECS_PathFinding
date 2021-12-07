using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

// данная система является аналогом параметра  Freeze Rotation у Rigidbody
public class FreezeverticalRotationSystem : SystemBase
{
    protected override void OnUpdate()
    {
       
        Entities
             .WithName("EnemyMovementSystem")
             .WithAll<CharacterData>()
             .ForEach((ref PhysicsMass mass) =>
             {
                 mass.InverseInertia[0] = 0;
                 mass.InverseInertia[1] = 0;
                 mass.InverseInertia[2] = 0;

             }).Schedule();
    }
}
