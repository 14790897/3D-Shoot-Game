using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHP = 100f;
    public float hp;
    void Awake(){ hp = maxHP; UIManager.Instance.UpdateHP(hp, maxHP); }

    public void TakeDamage(float v)
    {
        if (GameManager.Instance.isGameOver) return;
        hp = Mathf.Max(0, hp - v);
        UIManager.Instance.UpdateHP(hp, maxHP);
        if (hp <= 0) GameManager.Instance.Lose();
    }
}
