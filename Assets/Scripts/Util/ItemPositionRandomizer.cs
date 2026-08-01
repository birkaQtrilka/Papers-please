using UnityEngine;

public class ItemPositionRandomizer : MonoBehaviour
{
    [SerializeField] Transform[] items;
    SphereCollider container;
#if UNITY_EDITOR
    [SerializeField] bool randomizeOnValidate = true;
    private void OnValidate()
    {
        if(randomizeOnValidate)
        {
            randomizeOnValidate = false;
            RandomizePosition();
        }
    }

#endif
    [ContextMenu("Randomize Position")]
    public void RandomizePosition()
    {
        container = GetComponent<SphereCollider>();

        for (int i = 0; i < items.Length; i++)
        {
            Vector3 randomDirection = Random.onUnitSphere;
            float randomDistance = Random.Range(0, container.radius);
            Vector3 randomPosition = transform.position + randomDirection * randomDistance;
            
            items[i].SetPositionAndRotation(randomPosition, Random.rotation);
        }

    }
}
