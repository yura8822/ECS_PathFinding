using Unity.Entities;

[GenerateAuthoringComponent]
public struct CharacterData : IComponentData
{
    public float movementSpeed;
    public float rotationSpeed;
}
