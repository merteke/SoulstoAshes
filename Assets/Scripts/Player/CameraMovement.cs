using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target; // Karakterinizin Transform'u
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Uzaklık ve yükseklik ayarı

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            // İsteğe bağlı olarak kameranın karaktere bakmasını sağlayabilirsiniz:
            // transform.LookAt(target);
        }
    }
}
