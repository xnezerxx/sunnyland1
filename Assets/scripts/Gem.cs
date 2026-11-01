using UnityEngine;

public class Gem : MonoBehaviour
{
    private int gemValue = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GemManager.instance != null)
            {
                GemManager.instance.AddGems(gemValue);
            }
            Destroy(gameObject);
        }
    }
}
