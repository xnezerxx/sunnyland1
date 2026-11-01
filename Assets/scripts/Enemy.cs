using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health;
    public float speed;

    private void Update()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
        }
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
    }
}

