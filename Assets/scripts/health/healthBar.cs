using UnityEngine;
using UnityEngine.UI;

public class healthBar : MonoBehaviour
{
    public health playerHealth;
    public Image totalHealthBar;
    public Image currentHealthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        totalHealthBar.fillAmount = playerHealth.currentHealth / 10;
    }

    // Update is called once per frame
    void Update()
    {
        currentHealthBar.fillAmount = playerHealth.currentHealth / 10;
    }
}
