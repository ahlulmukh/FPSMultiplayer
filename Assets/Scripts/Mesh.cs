using UnityEngine;

public class AddMeshColliderToChildren : MonoBehaviour
{
    private void Start()
    {
        // Mendapatkan semua komponen Transform pada anak-anak objek
        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            // Menambahkan Mesh Collider ke setiap anak objek
            MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();
            // Atur pengaturan collider sesuai kebutuhan
        }
    }
}