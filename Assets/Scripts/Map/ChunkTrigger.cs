using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    private MapController mapController;
    public GameObject targetMap;

    void Start()
    {
        mapController = FindAnyObjectByType<MapController>();
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            mapController.currentChunk = targetMap;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && mapController.currentChunk == targetMap)
        {
            mapController.currentChunk = null;
        }
    }
}
