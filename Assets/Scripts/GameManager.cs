using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int score { get; private set; }
    public int targetScore = 5;  // 需要收集的数量
    public bool isGameOver { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        score = 0; isGameOver = false;
    }

    public void AddScore(int v)
    {
        if (isGameOver) return;
        score += v;
        UIManager.Instance.UpdateScore(score, targetScore);
        if (score >= targetScore) Win();
    }

    public void Win()
    {
        if (isGameOver) return;
        isGameOver = true;
        UIManager.Instance.ShowResult("You Win!");
        Time.timeScale = 0f; // 暂停
    }

    public void Lose()
    {
        if (isGameOver) return;
        isGameOver = true;
        UIManager.Instance.ShowResult("You Lose!");
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
