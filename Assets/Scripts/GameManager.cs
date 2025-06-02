using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Singleton instance

    // Oyunun farklı durumlarını tanımlar
    public enum GameState
    {
        Gameplay,   // Normal oyun akışı
        Paused,     // Oyun duraklatıldı
        GameOver,   // Oyun bitti (kaybedildi)
        LevelUp,    // Oyuncu seviye atlıyor
        BossFight,  // Boss ile savaş durumu
        Victory     // Boss yenildiğinde zafer durumu
    }

    // Oyunun mevcut durumunu saklar
    public GameState currentState;

    // Duraklatılmadan önceki oyun durumunu saklar
    public GameState previousState;

    [Header("Hasar Yazısı Ayarları")]
    public Canvas damageTextCanvas; // Hasar yazılarının gösterileceği canvas
    public float textFontSize = 20; // Yazı boyutu
    public TMP_FontAsset textFont; // Yazı fontu
    public Camera referenceCamera; // Dünya pozisyonunu ekran pozisyonuna çevirmek için kamera

    [Header("Ekranlar")]
    public GameObject pauseScreen; // Duraklatma ekranı
    public GameObject resultsScreen; // Sonuç ekranı
    public GameObject levelUpScreen; // Seviye atlama ekranı
    public GameObject victoryScreen; // Boss yenildiğinde gösterilecek zafer ekranı
    int stackedLevelUps = 0; // Birden fazla seviye atlama birikirse

    [Header("Sonuç Ekranı Göstergeleri")]
    public Image chosenCharacterImage; // Seçilen karakterin resmi
    public TMP_Text chosenCharacterName; // Seçilen karakterin adı
    public TMP_Text levelReachedDisplay; // Ulaşılan seviye
    public TMP_Text timeSurvivedDisplay; // Hayatta kalınan süre
    public TMP_Text gameResultText; // Oyunun sonucunu gösterecek metin (Kazandınız/Kaybettiniz)

    [Header("Kronometre")]
    public float timeLimit; // Süre limiti (saniye cinsinden) - Boss'un çıkacağı zaman
    float stopwatchTime; // Kronometre başladığından beri geçen süre
    public TMP_Text stopwatchDisplay; // Kronometreyi gösteren TextMeshPro objesi

    [Header("Boss Savaşı Ayarları")]
    public GameObject bossPrefab; // Boss prefab'ını buraya sürükleyin
    public Transform bossSpawnPoint; // Boss'un nerede spawn olacağını belirleyen bir Transform
    private bool bossHasSpawned = false; // Boss'un sadece bir kere spawn olmasını sağlar
    private GameObject currentBoss; // Oluşturulan mevcut boss'a referans
    // public AudioClip gameplayMusic; // Opsiyonel: Normal oyun müziği
    // public AudioClip bossFightMusic; // Opsiyonel: Boss savaşı müziği
    // public AudioClip victoryMusic; // Opsiyonel: Zafer müziği
    // private AudioSource musicAudioSource; // Opsiyonel: Müzik çalmak için AudioSource component'i

    [Header("Victory Settings")]
    public float victoryDelay = 2f; // Boss öldükten sonra zafer ekranının gösterilmesi için gecikme süresi
    public Color victoryTextColor = Color.green; // Zafer metni rengi
    public string victoryMessage = "VICTORY!"; // Zafer mesajı

    PlayerStats[] players; // Sahnedeki tüm oyuncuları takip eder

    public bool isGameOver { get { return currentState == GameState.GameOver; } }
    public bool isVictory { get { return currentState == GameState.Victory; } }
    public bool choosingUpgrade { get { return currentState == GameState.LevelUp; } }
    public float GetElapsedTime() { return stopwatchTime; }

    // Sums up the curse stat of all players and returns the value.
    public static float GetCumulativeCurse()
    {
        if (!instance) return 1;

        float totalCurse = 0;
        foreach(PlayerStats p in instance.players)
        {
            totalCurse += p.Actual.curse;
        }
        return Mathf.Max(1, totalCurse);
    }

    // Sum up the levels of all players and returns the value.
    public static int GetCumulativeLevels()
    {
        if (!instance) return 1;

        int totalLevel = 0;
        foreach (PlayerStats p in instance.players)
        {
            totalLevel += p.level;
        }
        return Mathf.Max(1, totalLevel);
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        DisableScreens(); 
    }

    void Start()
    {
        bossHasSpawned = false; 
        ChangeState(GameState.Gameplay); 
        stopwatchTime = 0f; 
        UpdateStopwatchDisplay(); 
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.Gameplay:
                CheckForPauseAndResume(); 
                UpdateStopwatch(); 
                break;
            case GameState.BossFight: 
                CheckForPauseAndResume();
                CheckBossStatus();
                break;
            case GameState.Paused:
                CheckForPauseAndResume(); 
                break;
            case GameState.GameOver:
            case GameState.LevelUp:
            case GameState.Victory:
                break;
            default:
                break;
        }
    }

    // Checks if the boss is still alive
    void CheckBossStatus()
    {
        if (currentBoss == null && bossHasSpawned)
        {
            // Boss is defeated, trigger victory
            StartCoroutine(TriggerVictorySequence());
        }
    }

    // Handles the victory sequence when boss is defeated
    IEnumerator TriggerVictorySequence()
    {
        // Wait for a short delay
        yield return new WaitForSeconds(victoryDelay);

        // Trigger victory state
        Victory();
    }

    // Victory state - called when player defeats the boss
    public void Victory()
    {
        if (currentState == GameState.Victory) return;

        ChangeState(GameState.Victory);
        Time.timeScale = 0f;

        // Display final time
        timeSurvivedDisplay.text = stopwatchDisplay.text;

        // Get character data and assign to UI
        CharacterData chosen = UICharacterSelector.GetData();
        AssignChosenCharacterUI(chosen);

        // Set victory message if gameResultText exists
        if (gameResultText != null)
        {
            gameResultText.text = victoryMessage;
            gameResultText.color = victoryTextColor;
        }

        // Display victory screen (use results screen if no dedicated victory screen)
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
        else
        {
            DisplayResults();
        }

        // Optional: Play victory music
        // PlayMusic(victoryMusic);
    }

    IEnumerator GenerateFloatingTextCoroutine(string text, Transform target, float duration = 1f, float speed = 1f)
    {
        GameObject textObj = new GameObject("Damage Floating Text"); 
        RectTransform rect = textObj.AddComponent<RectTransform>(); 
        TextMeshProUGUI tmPro = textObj.AddComponent<TextMeshProUGUI>(); 
        tmPro.text = text; 
        tmPro.horizontalAlignment = HorizontalAlignmentOptions.Center; 
        tmPro.verticalAlignment = VerticalAlignmentOptions.Middle; 
        tmPro.fontSize = textFontSize; 
        if (textFont) tmPro.font = textFont; 
        
        Camera currentCamera = referenceCamera != null ? referenceCamera : Camera.main;
        if (currentCamera == null) { 
            Destroy(textObj);
            yield break;
        }
        if(target != null) rect.position = currentCamera.WorldToScreenPoint(target.position);
        else { 
             Destroy(textObj);
             yield break;
        }

        Destroy(textObj, duration); 
        
        if (instance.damageTextCanvas != null) {
            textObj.transform.SetParent(instance.damageTextCanvas.transform);
        }

        WaitForEndOfFrame w = new WaitForEndOfFrame(); 
        float t = 0; 
        float yOffset = 0; 
        Vector3 lastKnownPosition = target.position; 

        while (t < duration)
        {
            if (!rect) break; 

            tmPro.color = new Color(tmPro.color.r, tmPro.color.g, tmPro.color.b, 1 - (t / duration));
            if (target) lastKnownPosition = target.position;
            yOffset += speed * Time.deltaTime;
            rect.position = currentCamera.WorldToScreenPoint(lastKnownPosition + new Vector3(0, yOffset));
            yield return w; 
            t += Time.deltaTime; 
        }
    }

    public static void GenerateFloatingText(string text, Transform target, float duration = 1f, float speed = 1f) 
    {
        if (!instance || !instance.damageTextCanvas) return;
        if (!instance.referenceCamera) instance.referenceCamera = Camera.main;
        if (!instance.referenceCamera) {
            return;
        }
        if (target == null) {
            return;
        }
        instance.StartCoroutine(instance.GenerateFloatingTextCoroutine(text, target, duration, speed));
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState && currentState != GameState.LevelUp) return; 
        previousState = currentState; 
        currentState = newState; 
    }

    public void PauseGame()
    {
        if (currentState == GameState.Gameplay || currentState == GameState.BossFight)
        {
            ChangeState(GameState.Paused); 
            Time.timeScale = 0f; 
            if (pauseScreen) pauseScreen.SetActive(true); 
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            if (previousState != GameState.LevelUp && previousState != GameState.GameOver && previousState != GameState.Victory)
            {
                ChangeState(previousState);
            }
            else 
            {
                ChangeState(GameState.Gameplay); 
            }
            Time.timeScale = 1f; 
            if (pauseScreen) pauseScreen.SetActive(false); 
        }
    }

    void CheckForPauseAndResume()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            if (currentState == GameState.Paused)
            {
                ResumeGame(); 
            }
            else if (currentState == GameState.Gameplay || currentState == GameState.BossFight)
            {
                PauseGame(); 
            }
        }
    }

    void DisableScreens()
    {
        if (pauseScreen) pauseScreen.SetActive(false);
        if (resultsScreen) resultsScreen.SetActive(false);
        if (levelUpScreen) levelUpScreen.SetActive(false);
        if (victoryScreen) victoryScreen.SetActive(false);
    }

    public void GameOver()
    {
        if (currentState == GameState.Victory) return; // Don't override victory with game over

        timeSurvivedDisplay.text = stopwatchDisplay.text;

        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;

        // Set game over message if gameResultText exists
        if (gameResultText != null)
        {
            gameResultText.text = "GAME OVER";
            gameResultText.color = Color.red;
        }

        // 💡 Karakter verisini çek ve UI'ye ata
        CharacterData chosen = UICharacterSelector.GetData();
        AssignChosenCharacterUI(chosen);

        DisplayResults();
    }

    void DisplayResults()
    {
        if (resultsScreen) resultsScreen.SetActive(true);
    }

    public void AssignChosenCharacterUI(CharacterData chosenCharacterData)
    {
        if (chosenCharacterImage && chosenCharacterName && chosenCharacterData != null)
        {
            chosenCharacterImage.sprite = chosenCharacterData.Icon;
            chosenCharacterName.text = chosenCharacterData.Name;
        }
    }

    public void AssignLevelReachedUI(int levelReachedData)
    {
        if (levelReachedDisplay)
        {
            levelReachedDisplay.text = "LV " + levelReachedData.ToString(); 
        }
    }

    void UpdateStopwatch()
    {
        if (currentState != GameState.Gameplay) return;

        stopwatchTime += Time.deltaTime; 
        UpdateStopwatchDisplay(); 

        if (stopwatchTime >= timeLimit && !bossHasSpawned)
        {
            SpawnBoss(); 
        }
    }

    void UpdateStopwatchDisplay()
    {
        if (stopwatchDisplay == null) return; 
        int minutes = Mathf.FloorToInt(stopwatchTime / 60);
        int seconds = Mathf.FloorToInt(stopwatchTime % 60);
        stopwatchDisplay.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void SpawnBoss()
    {
        if (bossPrefab == null) 
        {
            return; 
        }

        Vector3 spawnPosition;
        if (bossSpawnPoint != null) 
        {
            spawnPosition = bossSpawnPoint.position; 
        }
        else 
        {
            return; 
        }

        // Create the boss and store reference
        currentBoss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        bossHasSpawned = true; 
        ChangeState(GameState.BossFight); 

        // Clear existing enemies
        ClearExistingEnemies(currentBoss);

        // Optional: Play boss fight music
        // PlayMusic(bossFightMusic);
    }

    // Public method to be called by the boss's death script when it's defeated
    public void OnBossDefeated()
    {
        if (currentState == GameState.BossFight)
        {
            StartCoroutine(TriggerVictorySequence());
        }
    }

    void ClearExistingEnemies(GameObject bossToKeep)
    {
        // "Enemy" tag'ine sahip tüm objeleri bul. Normal düşmanlarınızın bu tag'e sahip olduğundan emin olun.
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); 
        int clearedCount = 0;

        foreach (GameObject enemy in enemies)
        {
            // Eğer bulunan düşman, yeni spawn olan boss değilse yok et.
            if (enemy != bossToKeep) 
            {
                // Düşmanların kendi ölüm efektleri, skor verme vb. işlemleri varsa,
                // doğrudan Destroy yerine düşmanın kendi Kill() metodunu çağırmak daha iyi olabilir.
                // Örneğin: enemy.GetComponent<EnemyStats>()?.Kill();
                Destroy(enemy);
                clearedCount++;
            }
        }
    }

    public void StartLevelUp()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        ChangeState(GameState.LevelUp); 
        if (levelUpScreen != null && levelUpScreen.activeSelf)
        {
            stackedLevelUps++;
        }
        else 
        {
            if (levelUpScreen) levelUpScreen.SetActive(true);
            Time.timeScale = 0f; 
            foreach (PlayerStats p in players)
            {
                if (p != null) p.SendMessage("RemoveAndApplyUpgrades", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void EndLevelUp()
    {
        Time.timeScale = 1f; 
        if (levelUpScreen) levelUpScreen.SetActive(false); 
        
        if (bossHasSpawned) 
        {
             ChangeState(GameState.BossFight);
        }
        else 
        {
             ChangeState(GameState.Gameplay);
        }

        if (stackedLevelUps > 0)
        {
            stackedLevelUps--;
            StartLevelUp();
        }
    }
}