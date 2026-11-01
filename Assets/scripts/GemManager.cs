using UnityEngine;
using TMPro;
using NUnit.Framework.Internal;

public class GemManager : MonoBehaviour
{
    public static GemManager instance;
    public TextMeshProUGUI gemText;
    private int totalGem = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    public void AddGems(int amount)
    {
        totalGem = totalGem + amount;
        UpdateUI();
    }
    private void UpdateUI()
    {
        if (gemText != null)
        {
            gemText.text = totalGem+"";
        }
    }
}
