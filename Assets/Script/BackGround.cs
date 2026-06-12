using UnityEngine;

public class BackGround : MonoBehaviour
{
    [SerializeField] private float speed = 0.2f;

    private Material material;

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            material = renderer.material;
    }

    private void Update()
    {
        if (material == null) return;

        material.mainTextureOffset += Vector2.up * speed * Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
