using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("Character Data")]
    private CharacterData characterData;
    public CharacterData.Stats baseStats;
    [SerializeField] private CharacterData.Stats actualStats;

    [Header("Dash Stats")]
    public float currentDashSpeed = 10f;
    public float currentDashCooldown = 2f;
    public float currentDashDuration = 0.5f;

    [Header("State")]
    public bool isDead = false;
    private float health;

    [Header("Animation Settings")]
    public Animator animator;
    public string hitAnimationTrigger = "Hit";
    public string deathAnimationTrigger = "Dead";
    public float deathAnimationDuration = 2f;

    [Header("Visuals")]
    public ParticleSystem damageEffect;
    public ParticleSystem blockedEffect;

    [Header("Experience/Level")]
    public int experience = 0;
    public int level = 1;
    public int experienceCap;

    [Header("I-Frames")]
    public float invincibilityDuration;
    private float invincibilityTimer;
    private bool isInvincible;

    [Header("UI")]
    public Image healthBar;
    public Image expBar;
    public TMP_Text levelText;

    // References to other components
    private PlayerInventory inventory;
    private PlayerCollector collector;
    private PlayerAnimatorr playerAnimator;
    private MonoBehaviour playerMovementScript;

    public int weaponIndex;
    public int passiveItemIndex;
    
    public List<LevelRange> levelRanges;

    #region Properties
    public CharacterData.Stats Stats
    {
        get { return actualStats; }
        set { actualStats = value; }
    }

    public CharacterData.Stats Actual
    {
        get { return actualStats; }
    }

    public float CurrentBaseDamage
    {
        get { return actualStats.baseDamage; }
        set
        {
            if (actualStats.baseDamage != value)
            {
                actualStats.baseDamage = value;
            }
        }
    }

    public float CurrentHealth
    {
        get { return health; }
        set
        {
            if (health != value)
            {
                health = value;
                UpdateHealthBar();
            }
        }
    }
    #endregion

    [System.Serializable]
    public class LevelRange
    {
        public int startLevel;
        public int endLevel;
        public int experienceCapIncrease;
    }

    #region Unity Lifecycle Methods
    private void Awake()
    {
        InitializeComponents();
        InitializeStats();
    }

    private void Start()
    {
        InitializeWeapon();
        InitializeExperienceCap();
        UpdateAllUI();
    }

    private void Update()
    {
        UpdateInvincibility();
        
        if (!isDead)
        {
            Recover();
        }
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        characterData = UICharacterSelector.GetData();
        inventory = GetComponent<PlayerInventory>();
        collector = GetComponentInChildren<PlayerCollector>();
        
        // Find animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
        }

        // Try to get the movement script with common names
        playerMovementScript = GetComponent("PlayerMovement") as MonoBehaviour 
                            ?? GetComponent("PlayerController") as MonoBehaviour;

        playerAnimator = GetComponent<PlayerAnimatorr>();
    }

    private void InitializeStats()
    {
        baseStats = actualStats = characterData.stats;
        collector.SetRadius(actualStats.magnet);
        health = actualStats.maxHealth;
        
        if(characterData.controller)
            playerAnimator.SetAnimatorController(characterData.controller);
    }

    private void InitializeWeapon()
    {
        if (characterData != null && characterData.StartingWeapon != null)
        {
            inventory.Add(characterData.StartingWeapon);
        }
    }

    private void InitializeExperienceCap()
    {
        if (levelRanges != null && levelRanges.Count > 0)
        {
            experienceCap = levelRanges[0].experienceCapIncrease;
        }
        else
        {
            experienceCap = 100;
        }
    }
    #endregion

    #region Stats Management
    public void RecalculateStats()
    {
        actualStats = baseStats;
        
        foreach (PlayerInventory.Slot slot in inventory.passiveSlots)
        {
            if (slot.item is Passive passive)
            {
                actualStats += passive.GetBoosts();
            }
        }

        collector.SetRadius(actualStats.magnet);
    }

    private void UpdateInvincibility()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
        else if (isInvincible)
        {
            isInvincible = false;
        }
    }

    private void Recover()
    {
        if (CurrentHealth < actualStats.maxHealth)
        {
            CurrentHealth += Stats.recovery * Time.deltaTime;

            // Clamp health to max
            if (CurrentHealth > actualStats.maxHealth)
            {
                CurrentHealth = actualStats.maxHealth;
            }
        }
    }
    #endregion

    #region Combat System
    public void TakeDamage(float dmg)
    {
        if (isInvincible) return;

        // Apply armor reduction
        dmg -= actualStats.armor;

        if (dmg > 0)
        {
            // Apply damage
            CurrentHealth -= dmg;
            
            // Trigger hit animation
            if (animator != null && !string.IsNullOrEmpty(hitAnimationTrigger))
            {
                animator.SetTrigger(hitAnimationTrigger);
            }

            // Show damage effects
            if (damageEffect) 
                Destroy(Instantiate(damageEffect, transform.position, Quaternion.identity), 5f);

            // Check for death
            if (CurrentHealth <= 0)
            {
                Kill();
            }
        }
        else
        {
            // Damage fully blocked
            if (blockedEffect) 
                Destroy(Instantiate(blockedEffect, transform.position, Quaternion.identity), 5f);
        }

        // Activate invincibility
        invincibilityTimer = invincibilityDuration;
        isInvincible = true;
    }

    public void Kill()
    {
        if (isDead) return;

        isDead = true;
        
        // Disable all colliders
        DisableAllColliders();
        
        // Stop movement
        DisableMovement();

        // Trigger death animation or immediate game over
        if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
        {
            animator.SetTrigger(deathAnimationTrigger);
            StartCoroutine(DeathSequenceCoroutine());
        }
        else
        {
            PerformGameOverActions();
        }
    }

    private void DisableAllColliders()
    {
        // Disable colliders on this GameObject
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }
        
        // Disable colliders on children
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }
    }

    private void DisableMovement()
    {
        // Try to disable player movement component
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        else
        {
            // Fallback: stop any physics-based movement
            Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
            }
            
            Rigidbody rb3d = GetComponent<Rigidbody>();
            if (rb3d != null)
            {
                rb3d.linearVelocity = Vector3.zero;
                rb3d.angularVelocity = Vector3.zero;
            }
        }
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        PerformGameOverActions();
    }

    private void PerformGameOverActions()
    {
        if (GameManager.instance != null && !GameManager.instance.isGameOver)
        {
            GameManager.instance.AssignLevelReachedUI(level);
            GameManager.instance.GameOver();
        }
    }

    public void RestoreHealth(float amount)
    {
        if (isDead) return;

        if (CurrentHealth < actualStats.maxHealth)
        {
            CurrentHealth += amount;
            
            // Clamp to max health
            if (CurrentHealth > actualStats.maxHealth)
            {
                CurrentHealth = actualStats.maxHealth;
            }
        }
    }
    #endregion

    #region Experience and Leveling
    public void IncreaseExperience(int amount)
    {
        if (isDead) return;

        experience += amount;
        LevelUpChecker();
        UpdateExpBar();
    }

    private void LevelUpChecker()
    {
        if (experience >= experienceCap)
        {
            level++;
            experience -= experienceCap;

            // Calculate new experience cap
            int experienceCapIncrease = 0;
            if (levelRanges != null)
            {
                foreach (LevelRange range in levelRanges)
                {
                    if (level >= range.startLevel && level <= range.endLevel)
                    {
                        experienceCapIncrease = range.experienceCapIncrease;
                        break;
                    }
                }
            }
            experienceCap += experienceCapIncrease;

            UpdateLevelText();

            // Trigger level up UI
            if (GameManager.instance != null)
            {
                GameManager.instance.StartLevelUp();
            }
            
            // Recursive level up if experience still exceeds cap
            if (experience >= experienceCap) 
                LevelUpChecker();
        }
    }
    #endregion

    #region UI Updates
    private void UpdateAllUI()
    {
        UpdateHealthBar();
        UpdateExpBar();
        UpdateLevelText();
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null && actualStats.maxHealth > 0)
        {
            healthBar.fillAmount = CurrentHealth / actualStats.maxHealth;
        }
        else if (healthBar != null)
        {
            healthBar.fillAmount = 0;
        }
    }

    private void UpdateExpBar()
    {
        if (expBar != null)
        {
            expBar.fillAmount = experienceCap > 0 ? (float)experience / experienceCap : 0;
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "LV " + level.ToString();
        }
    }
    #endregion
}