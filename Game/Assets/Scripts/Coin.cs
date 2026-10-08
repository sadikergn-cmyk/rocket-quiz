using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 120f;

    private bool collected;

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        collected = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectCoin();
        }
        Destroy(gameObject);
    }
}
