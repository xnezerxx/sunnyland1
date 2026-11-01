using UnityEngine;

public class Spikes : MonoBehaviour
{
    public float damage;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            collision.GetComponent<health>().TakeDamage(damage);
        }
    }
}
