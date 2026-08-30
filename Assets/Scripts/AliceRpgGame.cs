using System;
using System.Collections.Generic;
using UnityEngine;

public static class AliceRpgBuildInfo
{
    public const string Version = "1.0.0";
}

public static class AliceRpgBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (UnityEngine.Object.FindFirstObjectByType<AliceRpgGame>() != null) return;
        var host = new GameObject("Alice & The Broken Crown");
        UnityEngine.Object.DontDestroyOnLoad(host);

        var cameraHost = new GameObject("UI Camera");
        cameraHost.transform.SetParent(host.transform, false);
        var camera = cameraHost.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.orthographic = true;
        cameraHost.AddComponent<AudioListener>();

        host.AddComponent<AliceRpgGame>();
    }
}

public sealed class AliceRpgGame : MonoBehaviour
{
    private const int LogicalWidth = 960;
    private const int LogicalHeight = 540;
    private const int Tile = 32;
    private const int MapWidth = 30;
    private const int MapHeight = 16;
    private const int CurrentSaveVersion = 5;

    private enum GameMode { Title, Intro, Explore, Dialogue, Battle, Pause, Settings, SaveSlots, Controls, DialogueLog, Records, Credits, Ending, GameOver }
    private enum PendingBattle { None, Menu, Victory, Defeat }
    private enum SaveSlotPurpose { Load, Save, Manage }

    [Serializable]
    private sealed class SaveData
    {
        public int version = CurrentSaveVersion;
        public string savedAt = "";
        public string chapterName = "";
        public int chapter;
        public int fragments;
        public int playerX;
        public int playerY;
        public int facingX;
        public int facingY;
        public int level;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int attack;
        public int experience;
        public int potions;
        public int steps;
        public int battlesWon;
        public int playSeconds;
        public int deaths;
        public int teaLeaves;
        public int openedChests;
        public bool cleared;
        public bool tutorialSeen;
        public bool teaCharm;
        public int introStage;
        public string[] dialogueLog;
        public string checksum = "";
    }

    private sealed class Npc
    {
        public string id;
        public string displayName;
        public Vector2Int position;
        public Texture2D sprite;
    }

    private sealed class Enemy
    {
        public string name;
        public string flavor;
        public int hp;
        public int maxHp;
        public int attack;
        public int xp;
        public bool boss;
        public string specialName;
        public Texture2D sprite;
    }

    private GameMode mode = GameMode.Title;
    private Vector2Int playerPosition = new Vector2Int(3, 13);
    private Vector2Int facing = Vector2Int.down;
    private readonly List<Npc> npcs = new List<Npc>();
    private readonly HashSet<int> shrubs = new HashSet<int>();
    private readonly List<Vector2Int> chestPositions = new List<Vector2Int>();
    private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private readonly List<string> dialoguePages = new List<string>();
    private readonly List<string> dialogueHistory = new List<string>();
    private readonly List<Vector2Int> autoPath = new List<Vector2Int>();

    private Font gameFont;
    private GUIStyle titleStyle;
    private GUIStyle titleOnDarkStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private GUIStyle smallOnDarkStyle;
    private GUIStyle centerStyle;
    private GUIStyle centerOnDarkStyle;
    private GUIStyle menuStyle;
    private GUIStyle selectedStyle;
    private GUIStyle dialogueStyle;
    private GUIStyle speakerStyle;
    private GUIStyle questStyle;
    private GUIStyle bodyStyle;
    private GUIStyle hintStyle;
    private AudioSource audioSource;
    private AudioClip moveSound;
    private AudioClip confirmSound;
    private AudioClip hitSound;
    private AudioClip magicSound;

    private int titleSelection;
    private int chapter;
    private int fragments;
    private int stepsSinceBattle;
    private int dialogueIndex;
    private string dialogueSpeaker = "";
    private Action dialogueFinished;
    private string toast = "";
    private float toastUntil;
    private bool showQuest;
    private bool hasSave;
    private bool confirmNewGame;
    private bool confirmQuit;
    private int pauseSelection;
    private int settingsSelection;
    private int gameOverSelection;
    private int endingSelection;
    private int saveSlotSelection;
    private int activeSaveSlot;
    private SaveSlotPurpose saveSlotPurpose;
    private GameMode saveSlotReturnMode = GameMode.Title;
    private bool confirmSlotOverwrite;
    private bool confirmBackupRestore;
    private bool confirmSlotDeletion;
    private int saveSlotAction;
    private int dialogueLogScroll;
    private GameMode dialogueLogReturnMode = GameMode.Explore;
    private int controlsSelection;
    private int rebindAction = -1;
    private float rebindStartedAt;
    private GameMode controlsReturnMode = GameMode.Title;
    private GameMode settingsReturnMode = GameMode.Title;
    private GameMode pauseReturnMode = GameMode.Explore;
    private GameMode recordsReturnMode = GameMode.Title;
    private float musicVolume = 0.55f;
    private float sfxVolume = 0.7f;
    private int difficulty = 1;
    private bool highContrast;
    private bool reducedMotion;
    private int textSpeed = 1;
    private float uiTextScale = 1f;
    private bool gentleEncounters;
    private bool fullscreen = true;
    private int resolutionIndex = 1;
    private float dialogueStartedAt;
    private float nextMoveAt;
    private float nextMenuAxisAt;
    private float nextMenuHorizontalAt;
    private Vector2Int heldDirection;
    private bool guarding;
    private bool enemyInspected;
    private int weakenedTurns;
    private int stepsTaken;
    private int battlesWon;
    private int deaths;
    private int teaLeaves;
    private int openedChests;
    private bool cleared;
    private bool tutorialSeen;
    private int introStage;
    private bool teaCharm;
    private float sessionStartedAt;
    private float lastAutoSaveAt;
    private int previousPlaySeconds;
    private float guiScale = 1f;
    private Vector2 guiOffset;

    private KeyCode keyUp = KeyCode.W;
    private KeyCode keyDown = KeyCode.S;
    private KeyCode keyLeft = KeyCode.A;
    private KeyCode keyRight = KeyCode.D;
    private KeyCode keyConfirm = KeyCode.Space;
    private KeyCode keyCancel = KeyCode.X;
    private KeyCode keyQuest = KeyCode.Q;
    private KeyCode keyLog = KeyCode.L;

    private AudioSource musicSource;
    private AudioClip musicClip;
    private float saveIndicatorUntil;
    private string lastSaveNotice = "";
    private bool confirmResetSettings;
    private bool confirmDisplayChange;
    private float displayConfirmUntil;
    private bool previousFullscreen;
    private int previousResolutionIndex;

    private int level = 1;
    private int hp = 34;
    private int maxHp = 34;
    private int mp = 12;
    private int maxMp = 12;
    private int attack = 8;
    private int experience;
    private int potions = 2;
    private Enemy enemy;
    private int battleSelection;
    private string battleMessage = "";
    private PendingBattle pendingBattle;
    private Action battleVictory;
    private System.Random random = new System.Random();

    private readonly Color ink = new Color32(35, 29, 49, 255);
    private readonly Color cream = new Color32(255, 246, 216, 255);
    private readonly Color surface = new Color32(248, 239, 207, 255);
    private readonly Color blue = new Color32(70, 154, 204, 255);
    private readonly Color gold = new Color32(246, 195, 68, 255);
    private readonly Color rose = new Color32(190, 54, 87, 255);

    private void Awake()
    {
        Application.targetFrameRate = 60;
        gameFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Yu Gothic UI", "Meiryo", "Noto Sans CJK JP", "Arial" }, 18);
        CreateTextures();
        CreateWorld();
        CreateAudio();
        LoadSettings();
        MigrateLegacySaveIfNeeded();
        SelectRecoverableActiveSlot();
        hasSave = HasRecoverableSaveInSlot(activeSaveSlot);
        titleSelection = hasSave ? 0 : 1;
        sessionStartedAt = Time.unscaledTime;
    }

    private void CreateAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.22f * sfxVolume;
        moveSound = MakeTone("step", 330f, 0.035f);
        confirmSound = MakeTone("confirm", 660f, 0.07f);
        hitSound = MakeTone("hit", 120f, 0.09f);
        magicSound = MakeTone("magic", 880f, 0.14f);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.075f * musicVolume;
        musicClip = MakeMusic();
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    private AudioClip MakeTone(string clipName, float frequency, float duration)
    {
        const int rate = 22050;
        int count = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float fade = 1f - i / (float)count;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.35f;
        }
        AudioClip clip = AudioClip.Create(clipName, count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip MakeMusic()
    {
        const int rate = 22050;
        const float duration = 8f;
        int count = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[count];
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 392f, 329.63f, 293.66f, 349.23f };
        int noteLength = count / notes.Length;
        for (int i = 0; i < count; i++)
        {
            int noteIndex = Mathf.Min(notes.Length - 1, i / noteLength);
            float local = (i % noteLength) / (float)noteLength;
            float envelope = Mathf.Sin(local * Mathf.PI) * 0.24f;
            float t = i / (float)rate;
            samples[i] = (Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) + Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 0.5f * t) * 0.24f) * envelope;
        }
        AudioClip clip = AudioClip.Create("wonderland_theme", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void CreateWorld()
    {
        shrubs.Clear();
        int[,] points = {
            { 2, 3 }, { 3, 3 }, { 5, 3 }, { 11, 2 }, { 12, 2 }, { 4, 6 },
            { 10, 6 }, { 11, 6 }, { 17, 3 }, { 18, 3 }, { 20, 3 }, { 21, 6 },
            { 3, 10 }, { 9, 11 }, { 11, 13 }, { 18, 13 }, { 21, 13 }, { 27, 10 }
        };
        for (int i = 0; i < points.GetLength(0); i++) shrubs.Add(Key(points[i, 0], points[i, 1]));

        npcs.Clear();
        npcs.Add(new Npc { id = "rabbit", displayName = "白ウサギ", position = new Vector2Int(6, 12), sprite = textures["rabbit"] });
        npcs.Add(new Npc { id = "hatter", displayName = "帽子屋", position = new Vector2Int(5, 8), sprite = textures["hatter"] });
        npcs.Add(new Npc { id = "caterpillar", displayName = "青いイモムシ", position = new Vector2Int(9, 4), sprite = textures["caterpillar"] });
        npcs.Add(new Npc { id = "cat", displayName = "チェシャ猫", position = new Vector2Int(20, 11), sprite = textures["cat"] });
        npcs.Add(new Npc { id = "queen", displayName = "ハートの女王", position = new Vector2Int(26, 3), sprite = textures["queen"] });

        chestPositions.Clear();
        chestPositions.Add(new Vector2Int(3, 11));
        chestPositions.Add(new Vector2Int(16, 10));
        chestPositions.Add(new Vector2Int(22, 7));
    }

    private void Update()
    {
        EnforceMinimumWindowSize();
        if (mode == GameMode.Title) UpdateTitle();
        else if (mode == GameMode.Intro || mode == GameMode.Dialogue) UpdateDialogue();
        else if (mode == GameMode.Explore) UpdateExplore();
        else if (mode == GameMode.Battle) UpdateBattle();
        else if (mode == GameMode.Pause) UpdatePause();
        else if (mode == GameMode.Settings) UpdateSettings();
        else if (mode == GameMode.SaveSlots) UpdateSaveSlots();
        else if (mode == GameMode.Controls) UpdateControls();
        else if (mode == GameMode.DialogueLog) UpdateDialogueLog();
        else if (mode == GameMode.Records) UpdateRecords();
        else if (mode == GameMode.Credits) UpdateCredits();
        else if (mode == GameMode.Ending || mode == GameMode.GameOver) UpdateTerminal();
    }

    private void EnforceMinimumWindowSize()
    {
        if (Application.isEditor || fullscreen || Screen.width >= LogicalWidth && Screen.height >= LogicalHeight) return;
        Screen.SetResolution(LogicalWidth, LogicalHeight, false);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && (mode == GameMode.Explore || mode == GameMode.Battle || mode == GameMode.Dialogue || mode == GameMode.Intro))
        {
            pauseReturnMode = mode;
            if (mode == GameMode.Explore) SaveGame(false);
            pauseSelection = 0;
            mode = GameMode.Pause;
            Toast("一時停止しました。戻ったら再開できます。");
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) OnApplicationFocus(false);
    }

    private void OnApplicationQuit()
    {
        if (mode == GameMode.Explore) SaveGame(false);
    }

    private bool PressConfirm()
    {
        return Input.GetKeyDown(keyConfirm) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    private bool PressCancel()
    {
        return Input.GetKeyDown(keyCancel) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    private int VerticalInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(keyUp)) return -1;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(keyDown)) return 1;
        float axis = GamepadVerticalDown();
        if (Mathf.Abs(axis) <= 0.5f) { nextMenuAxisAt = 0f; return 0; }
        if (Time.unscaledTime < nextMenuAxisAt) return 0;
        nextMenuAxisAt = Time.unscaledTime + 0.18f;
        if (axis < -0.5f) return -1;
        if (axis > 0.5f) return 1;
        return 0;
    }

    private void UpdateTitle()
    {
        if (confirmNewGame || confirmQuit)
        {
            if (PressCancel()) { confirmNewGame = false; confirmQuit = false; Play(confirmSound); }
            else if (PressConfirm())
            {
                bool start = confirmNewGame;
                confirmNewGame = false;
                confirmQuit = false;
                Play(confirmSound);
                if (start) StartNewGame(); else Application.Quit();
            }
            return;
        }
        int vertical = VerticalInput();
        if (vertical != 0)
        {
            titleSelection = (titleSelection + vertical + 7) % 7;
            Play(moveSound);
        }
        if (!PressConfirm()) return;
        Play(confirmSound);
        ActivateTitleSelection();
    }

    private void ActivateTitleSelection()
    {
        if (titleSelection == 0)
        {
            if (hasSave) RequestLoadSlot(activeSaveSlot, GameMode.Title);
            else Toast("つづきから遊べるデータがありません。");
        }
        else if (titleSelection == 1)
        {
            if (hasSave) confirmNewGame = true;
            else StartNewGame();
        }
        else if (titleSelection == 2)
        {
            OpenSaveSlots(SaveSlotPurpose.Manage, GameMode.Title);
        }
        else if (titleSelection == 3)
        {
            recordsReturnMode = GameMode.Title;
            mode = GameMode.Records;
        }
        else if (titleSelection == 4)
        {
            settingsReturnMode = GameMode.Title;
            settingsSelection = 0;
            mode = GameMode.Settings;
        }
        else if (titleSelection == 5)
        {
            mode = GameMode.Credits;
        }
        else confirmQuit = true;
    }

    private void StartNewGame(bool newGamePlus = false)
    {
        chapter = 0;
        fragments = 0;
        playerPosition = new Vector2Int(3, 13);
        facing = Vector2Int.down;
        level = newGamePlus ? 2 : 1;
        maxHp = newGamePlus ? 42 : 34;
        maxMp = newGamePlus ? 15 : 12;
        hp = maxHp;
        mp = maxMp;
        attack = newGamePlus ? 11 : 8;
        experience = 0;
        potions = newGamePlus ? 3 : 2;
        stepsTaken = 0;
        battlesWon = 0;
        deaths = newGamePlus ? deaths : 0;
        PlayerPrefs.SetInt("AliceRpg.Deaths." + activeSaveSlot, deaths);
        teaLeaves = 0;
        openedChests = 0;
        cleared = newGamePlus;
        tutorialSeen = false;
        introStage = 1;
        teaCharm = newGamePlus;
        previousPlaySeconds = 0;
        sessionStartedAt = Time.unscaledTime;
        stepsSinceBattle = 0;
        guarding = false;
        autoPath.Clear();
        dialogueHistory.Clear();
        showQuest = false;
        BeginIntroNarrative();
        SaveGame(false);
    }

    private void BeginIntroNarrative()
    {
        ShowDialogue("語り手", new[]
        {
            "退屈な午後。アリスは、金色の懐中時計を抱えた白ウサギを追って\n古い樫の木の穴へ飛び込みました。",
            "落ちた先は、時間が止まりかけた『ワンダーランド』。\n空には割れた時計の月が浮かんでいます。",
            "ハートの女王が《時の冠》を壊し、国じゅうの明日を閉じ込めたのです。\nまずは南の草原にいる白ウサギを探しましょう。"
        }, FinishIntro);
        mode = GameMode.Intro;
    }

    private void FinishIntro()
    {
        if (tutorialSeen)
        {
            introStage = 0;
            mode = GameMode.Explore;
            Toast("目的：白ウサギを探す　[Q] クエスト");
            return;
        }
        introStage = 2;
        tutorialSeen = true;
        SaveGame(false);
        BeginTutorialDialogue();
    }

    private void BeginTutorialDialogue()
    {
        ShowDialogue("旅のしおり", new[]
        {
            "移動はWASD・矢印・左スティック、または地面のクリック。\n人や宝箱に近づいたら、決定キー / Aボタンで調べよう。",
            "[Q / Y] で目的、[L / X] で会話ログ、[Esc / B] でメニュー。\n設定からキーや音量、難易度をいつでも変えられる。"
        }, delegate { introStage = 0; mode = GameMode.Explore; Toast("目的：白ウサギを探す　[Q] クエスト"); SaveGame(false); });
    }

    private void UpdateDialogue()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            SkipDialogue();
            return;
        }
        if (!PressConfirm() && !PressCancel()) return;
        AdvanceDialogue();
    }

    private void SkipDialogue()
    {
        dialogueIndex = dialoguePages.Count;
        Action callback = dialogueFinished;
        dialogueFinished = null;
        mode = GameMode.Explore;
        Play(confirmSound);
        if (callback != null) callback();
    }

    private void AdvanceDialogue()
    {
        if (!DialoguePageFullyRevealed())
        {
            dialogueStartedAt = -1000f;
            Play(confirmSound);
            return;
        }
        Play(confirmSound);
        dialogueIndex++;
        dialogueStartedAt = Time.unscaledTime;
        if (dialogueIndex < dialoguePages.Count) return;
        Action callback = dialogueFinished;
        dialogueFinished = null;
        mode = GameMode.Explore;
        if (callback != null) callback();
    }

    private void UpdateExplore()
    {
        if (showQuest)
        {
            if (Input.GetKeyDown(keyQuest) || Input.GetKeyDown(KeyCode.JoystickButton3) || PressCancel())
            {
                showQuest = false;
                Play(confirmSound);
            }
            return;
        }
        if (Input.GetKeyDown(keyQuest) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            showQuest = !showQuest;
            Play(confirmSound);
            return;
        }
        if (Input.GetKeyDown(keyLog) || Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            dialogueLogScroll = 0;
            dialogueLogReturnMode = GameMode.Explore;
            mode = GameMode.DialogueLog;
            Play(confirmSound);
            return;
        }
        if (PressCancel())
        {
            pauseReturnMode = GameMode.Explore;
            pauseSelection = 0;
            mode = GameMode.Pause;
            Play(confirmSound);
            return;
        }
        if (PressConfirm())
        {
            Interact();
            return;
        }

        Vector2Int movement = ReadMovement();
        if (movement != Vector2Int.zero) autoPath.Clear();
        else if (autoPath.Count > 0 && Time.unscaledTime >= nextMoveAt)
        {
            movement = autoPath[0] - playerPosition;
            autoPath.RemoveAt(0);
            nextMoveAt = Time.unscaledTime + 0.105f;
        }
        if (movement == Vector2Int.zero) return;

        facing = movement;
        Vector2Int next = playerPosition + movement;
        if (CanWalk(next))
        {
            playerPosition = next;
            stepsSinceBattle++;
            stepsTaken++;
            Play(moveSound);
            CheckRandomBattle();
            if (Time.unscaledTime - lastAutoSaveAt > 30f) SaveGame(false);
        }
        else if (TileAt(next.x, next.y) == 'd' && chapter < 3)
        {
            autoPath.Clear();
            Toast("時の欠片を3つ集めなければ、城門は開かない。");
        }
    }

    private void UpdatePause()
    {
        if (PressCancel())
        {
            mode = pauseReturnMode;
            Play(confirmSound);
            return;
        }
        int vertical = VerticalInput();
        if (vertical != 0)
        {
            pauseSelection = (pauseSelection + vertical + 6) % 6;
            Play(moveSound);
        }
        if (!PressConfirm()) return;
        ActivatePauseSelection();
    }

    private void ActivatePauseSelection()
    {
        Play(confirmSound);
        if (pauseSelection == 0) mode = pauseReturnMode;
        else if (pauseSelection == 1)
        {
            if (pauseReturnMode == GameMode.Explore) OpenSaveSlots(SaveSlotPurpose.Save, GameMode.Pause);
            else Toast("会話・戦闘中は安全な区切りまで保存できません。");
        }
        else if (pauseSelection == 2)
        {
            dialogueLogScroll = 0;
            dialogueLogReturnMode = GameMode.Pause;
            mode = GameMode.DialogueLog;
        }
        else if (pauseSelection == 3)
        {
            controlsSelection = 0;
            rebindAction = -1;
            controlsReturnMode = GameMode.Pause;
            mode = GameMode.Controls;
        }
        else if (pauseSelection == 4)
        {
            settingsReturnMode = GameMode.Pause;
            settingsSelection = 0;
            mode = GameMode.Settings;
        }
        else
        {
            if (pauseReturnMode == GameMode.Explore) SaveGame(false);
            mode = GameMode.Title;
            titleSelection = HasRecoverableSaveInSlot(activeSaveSlot) ? 0 : 1;
        }
    }

    private void OpenSaveSlots(SaveSlotPurpose purpose, GameMode returnMode)
    {
        saveSlotPurpose = purpose;
        saveSlotReturnMode = returnMode;
        saveSlotSelection = activeSaveSlot;
        confirmSlotOverwrite = false;
        confirmBackupRestore = false;
        confirmSlotDeletion = false;
        saveSlotAction = 0;
        mode = GameMode.SaveSlots;
    }

    private void UpdateSaveSlots()
    {
        if (confirmSlotOverwrite)
        {
            if (PressCancel()) { confirmSlotOverwrite = false; Play(confirmSound); }
            else if (PressConfirm()) { confirmSlotOverwrite = false; CommitSaveSlot(saveSlotSelection); }
            return;
        }
        if (confirmBackupRestore)
        {
            if (PressCancel()) { confirmBackupRestore = false; Play(confirmSound); }
            else if (PressConfirm()) { confirmBackupRestore = false; RestoreBackupAndLoad(saveSlotSelection); }
            return;
        }
        if (confirmSlotDeletion)
        {
            if (PressCancel()) { confirmSlotDeletion = false; Play(confirmSound); }
            else if (PressConfirm()) { confirmSlotDeletion = false; DeleteSaveSlot(saveSlotSelection); }
            return;
        }
        if (PressCancel()) { mode = saveSlotReturnMode; Play(confirmSound); return; }
        int vertical = VerticalInput();
        if (vertical != 0) { saveSlotSelection = (saveSlotSelection + vertical + 3) % 3; Play(moveSound); }
        if (saveSlotPurpose == SaveSlotPurpose.Manage)
        {
            int horizontal = HorizontalInput();
            if (horizontal != 0) { saveSlotAction = (saveSlotAction + horizontal + 3) % 3; Play(moveSound); }
        }
        if (!PressConfirm()) return;
        ActivateSaveSlotSelection();
    }

    private void ActivateSaveSlotSelection()
    {
        if (saveSlotPurpose == SaveSlotPurpose.Save)
        {
            if (HasSaveInSlot(saveSlotSelection)) confirmSlotOverwrite = true;
            else CommitSaveSlot(saveSlotSelection);
            return;
        }
        if (saveSlotPurpose == SaveSlotPurpose.Manage)
        {
            if (saveSlotAction == 0) RequestLoadSlot(saveSlotSelection, saveSlotReturnMode);
            else if (saveSlotAction == 1)
            {
                if (!HasBackupInSlot(saveSlotSelection)) Toast("このスロットに復元できるバックアップはありません。");
                else confirmBackupRestore = true;
            }
            else
            {
                if (!HasRecoverableSaveInSlot(saveSlotSelection)) Toast("このスロットは空です。");
                else confirmSlotDeletion = true;
            }
            return;
        }
        RequestLoadSlot(saveSlotSelection, saveSlotReturnMode);
    }

    private void CommitSaveSlot(int slot)
    {
        activeSaveSlot = slot;
        SaveGame(true);
        SaveSettings();
        mode = saveSlotReturnMode;
    }

    private void RequestLoadSlot(int slot, GameMode returnMode)
    {
        if (HasSaveInSlot(slot))
        {
            activeSaveSlot = slot;
            hasSave = true;
            SaveSettings();
            LoadGame();
            return;
        }
        if (HasBackupInSlot(slot))
        {
            saveSlotPurpose = SaveSlotPurpose.Manage;
            saveSlotReturnMode = returnMode;
            saveSlotSelection = slot;
            confirmBackupRestore = true;
            mode = GameMode.SaveSlots;
            return;
        }
        Toast("このスロットは空か、復元できない状態です。");
    }

    private void RestoreBackupAndLoad(int slot)
    {
        SaveData backup = ReadBackup(slot);
        if (backup == null)
        {
            Toast("バックアップを読み込めませんでした。");
            return;
        }
        string key = SaveKey(slot);
        PlayerPrefs.SetString(key, JsonUtility.ToJson(backup));
        PlayerPrefs.Save();
        activeSaveSlot = slot;
        hasSave = true;
        SaveSettings();
        Toast("バックアップのしおりを復元しました。");
        LoadGame();
    }

    private void DeleteSaveSlot(int slot)
    {
        string key = SaveKey(slot);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(key + ".backup");
        PlayerPrefs.DeleteKey("AliceRpg.Deaths." + Mathf.Clamp(slot, 0, 2));
        PlayerPrefs.Save();
        if (slot == activeSaveSlot)
        {
            int fallbackSlot = FirstRecoverableSaveSlot();
            activeSaveSlot = fallbackSlot >= 0 ? fallbackSlot : 0;
            hasSave = fallbackSlot >= 0;
            titleSelection = hasSave ? 0 : 1;
            SaveSettings();
        }
        Toast("スロット " + (slot + 1) + " のセーブデータを削除しました。");
    }

    private void UpdateDialogueLog()
    {
        if (PressCancel() || PressConfirm()) { mode = dialogueLogReturnMode; Play(confirmSound); return; }
        int vertical = VerticalInput();
        if (vertical != 0) dialogueLogScroll = Mathf.Clamp(dialogueLogScroll + vertical, 0, Mathf.Max(0, dialogueHistory.Count - 6));
    }

    private void UpdateRecords()
    {
        if (PressCancel() || PressConfirm()) { mode = recordsReturnMode; Play(confirmSound); }
    }

    private void UpdateCredits()
    {
        if (PressCancel() || PressConfirm()) { mode = GameMode.Title; Play(confirmSound); }
    }

    private void UpdateControls()
    {
        if (rebindAction >= 0) return;
        if (PressCancel()) { SaveSettings(); mode = controlsReturnMode; Play(confirmSound); return; }
        int vertical = VerticalInput();
        if (vertical != 0) { controlsSelection = (controlsSelection + vertical + 9) % 9; Play(moveSound); }
        if (!PressConfirm()) return;
        if (controlsSelection == 8) { SaveSettings(); mode = controlsReturnMode; }
        else
        {
            rebindAction = controlsSelection;
            rebindStartedAt = Time.unscaledTime;
            Toast("新しいキーを押してください。Escでキャンセル");
        }
    }

    private string ControlName(int index)
    {
        string[] names = { "上へ移動", "下へ移動", "左へ移動", "右へ移動", "決定 / 話す", "キャンセル / メニュー", "クエスト", "会話ログ" };
        return index >= 0 && index < names.Length ? names[index] : "もどる";
    }

    private KeyCode BindingAt(int index)
    {
        if (index == 0) return keyUp; if (index == 1) return keyDown; if (index == 2) return keyLeft; if (index == 3) return keyRight;
        if (index == 4) return keyConfirm; if (index == 5) return keyCancel; if (index == 6) return keyQuest; return keyLog;
    }

    private bool TrySetBinding(int index, KeyCode key)
    {
        if (key == KeyCode.None) return false;
        if (key == KeyCode.Escape)
        {
            rebindAction = -1;
            return false;
        }
        if (IsReservedKey(key))
        {
            Toast("そのキーは共通操作に使われるため割り当てできません。");
            return false;
        }
        for (int i = 0; i < 8; i++)
        {
            if (i != index && BindingAt(i) == key) { Toast("そのキーは別の操作に割り当て済みです。"); return false; }
        }
        SetBindingValue(index, key);
        rebindAction = -1;
        SaveSettings();
        Toast(ControlName(index) + "を " + key + " に変更しました。");
        Play(confirmSound);
        return true;
    }

    private bool IsReservedKey(KeyCode key)
    {
        return key == KeyCode.Escape || key == KeyCode.Return || key == KeyCode.Z ||
               key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
               key == KeyCode.LeftArrow || key == KeyCode.RightArrow;
    }

    private void UpdateTerminal()
    {
        if (mode == GameMode.Ending)
        {
            int endingVertical = VerticalInput();
            if (endingVertical != 0) { endingSelection = (endingSelection + endingVertical + 3) % 3; Play(moveSound); }
            if (!PressConfirm()) return;
            Play(confirmSound);
            if (endingSelection == 0) StartNewGame(true);
            else if (endingSelection == 1) { recordsReturnMode = GameMode.Ending; mode = GameMode.Records; }
            else { mode = GameMode.Title; titleSelection = 0; }
            return;
        }
        int vertical = VerticalInput();
        if (vertical != 0)
        {
            gameOverSelection = 1 - gameOverSelection;
            Play(moveSound);
        }
        if (!PressConfirm()) return;
        Play(confirmSound);
        if (gameOverSelection == 0 && hasSave) RequestLoadSlot(activeSaveSlot, GameMode.GameOver);
        else
        {
            mode = GameMode.Title;
            titleSelection = hasSave ? 0 : 1;
        }
    }

    private Vector2Int ReadMovement()
    {
        Vector2Int direction = Vector2Int.zero;
        float horizontal = GamepadHorizontal();
        float vertical = GamepadVerticalDown();
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(keyLeft) || horizontal < -0.45f) direction = Vector2Int.left;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(keyRight) || horizontal > 0.45f) direction = Vector2Int.right;
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(keyUp) || vertical < -0.45f) direction = Vector2Int.down;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(keyDown) || vertical > 0.45f) direction = Vector2Int.up;
        if (direction == Vector2Int.zero)
        {
            heldDirection = Vector2Int.zero;
            return Vector2Int.zero;
        }
        bool fresh = direction != heldDirection;
        if (!fresh && Time.unscaledTime < nextMoveAt) return Vector2Int.zero;
        heldDirection = direction;
        nextMoveAt = Time.unscaledTime + (fresh ? 0.22f : 0.105f);
        return direction;
    }

    private float GamepadHorizontal()
    {
        float dpad = ReadInputAxis("DPadHorizontal");
        return Mathf.Abs(dpad) > 0.45f ? dpad : ReadInputAxis("Horizontal");
    }

    private int HorizontalInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(keyLeft)) return -1;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(keyRight)) return 1;
        float axis = GamepadHorizontal();
        if (Mathf.Abs(axis) <= 0.5f) { nextMenuHorizontalAt = 0f; return 0; }
        if (Time.unscaledTime < nextMenuHorizontalAt) return 0;
        nextMenuHorizontalAt = Time.unscaledTime + 0.18f;
        return axis < 0f ? -1 : 1;
    }

    private float GamepadVerticalDown()
    {
        float dpad = ReadInputAxis("DPadVertical");
        return Mathf.Abs(dpad) > 0.45f ? -dpad : -ReadInputAxis("Vertical");
    }

    private float ReadInputAxis(string axisName)
    {
        try { return Input.GetAxisRaw(axisName); }
        catch (ArgumentException) { return 0f; }
    }

    private void UpdateSettings()
    {
        if (confirmDisplayChange)
        {
            if (Time.unscaledTime >= displayConfirmUntil || PressCancel())
            {
                fullscreen = previousFullscreen;
                resolutionIndex = previousResolutionIndex;
                ApplyDisplaySettings();
                confirmDisplayChange = false;
                Toast("表示設定を元に戻しました。");
            }
            else if (PressConfirm())
            {
                confirmDisplayChange = false;
                SaveSettings();
                Toast("表示設定を保存しました。");
            }
            return;
        }
        if (confirmResetSettings)
        {
            if (PressCancel()) { confirmResetSettings = false; Play(confirmSound); }
            else if (PressConfirm())
            {
                confirmResetSettings = false;
                ResetSettings();
                Toast("設定を初期状態に戻しました。");
            }
            return;
        }
        if (PressCancel())
        {
            SaveSettings();
            mode = settingsReturnMode;
            return;
        }
        int vertical = VerticalInput();
        if (vertical != 0)
        {
            settingsSelection = (settingsSelection + vertical + 13) % 13;
            Play(moveSound);
        }
        int horizontal = HorizontalInput();
        if (horizontal != 0) ChangeSetting(horizontal);
        if (!PressConfirm()) return;
        if (settingsSelection == 10)
        {
            controlsSelection = 0;
            rebindAction = -1;
            controlsReturnMode = GameMode.Settings;
            mode = GameMode.Controls;
        }
        else if (settingsSelection == 11)
        {
            confirmResetSettings = true;
        }
        else if (settingsSelection == 12)
        {
            SaveSettings();
            mode = settingsReturnMode;
        }
        else ChangeSetting(1);
    }

    private void ChangeSetting(int direction)
    {
        Play(moveSound);
        if (settingsSelection == 0) musicVolume = Mathf.Clamp01(musicVolume + direction * 0.1f);
        else if (settingsSelection == 1) sfxVolume = Mathf.Clamp01(sfxVolume + direction * 0.1f);
        else if (settingsSelection == 2) difficulty = (difficulty + direction + 3) % 3;
        else if (settingsSelection == 3) textSpeed = (textSpeed + direction + 3) % 3;
        else if (settingsSelection == 4)
        {
            uiTextScale = Mathf.Clamp(uiTextScale + direction * 0.1f, 0.8f, 1.3f);
            titleStyle = null;
        }
        else if (settingsSelection == 5) highContrast = !highContrast;
        else if (settingsSelection == 6) reducedMotion = !reducedMotion;
        else if (settingsSelection == 7) gentleEncounters = !gentleEncounters;
        else if (settingsSelection == 8) RequestDisplayChange(!fullscreen, resolutionIndex);
        else if (settingsSelection == 9)
            RequestDisplayChange(fullscreen, (resolutionIndex + direction + 3) % 3);
        if (audioSource != null) audioSource.volume = 0.22f * sfxVolume;
        if (musicSource != null) musicSource.volume = 0.075f * musicVolume;
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("AliceRpg.MusicVolume", 0.55f);
        sfxVolume = PlayerPrefs.GetFloat("AliceRpg.SfxVolume", PlayerPrefs.GetFloat("AliceRpg.Volume", 0.7f));
        difficulty = PlayerPrefs.GetInt("AliceRpg.Difficulty", 1);
        textSpeed = PlayerPrefs.GetInt("AliceRpg.TextSpeed", 1);
        uiTextScale = Mathf.Clamp(PlayerPrefs.GetFloat("AliceRpg.UiTextScale", 1f), 0.8f, 1.3f);
        highContrast = PlayerPrefs.GetInt("AliceRpg.HighContrast", 0) == 1;
        reducedMotion = PlayerPrefs.GetInt("AliceRpg.ReducedMotion", 0) == 1;
        gentleEncounters = PlayerPrefs.GetInt("AliceRpg.GentleEncounters", 0) == 1;
        fullscreen = PlayerPrefs.GetInt("AliceRpg.Fullscreen", 1) == 1;
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt("AliceRpg.Resolution", 1), 0, 2);
        activeSaveSlot = Mathf.Clamp(PlayerPrefs.GetInt("AliceRpg.ActiveSlot", 0), 0, 2);
        keyUp = LoadKey("Up", KeyCode.W);
        keyDown = LoadKey("Down", KeyCode.S);
        keyLeft = LoadKey("Left", KeyCode.A);
        keyRight = LoadKey("Right", KeyCode.D);
        keyConfirm = LoadKey("Confirm", KeyCode.Space);
        keyCancel = LoadKey("Cancel", KeyCode.X);
        keyQuest = LoadKey("Quest", KeyCode.Q);
        keyLog = LoadKey("Log", KeyCode.L);
        if (NormalizeKeyBindings()) SaveKeyBindings();
        if (audioSource != null) audioSource.volume = 0.22f * sfxVolume;
        if (musicSource != null) musicSource.volume = 0.075f * musicVolume;
        ApplyDisplaySettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("AliceRpg.MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("AliceRpg.SfxVolume", sfxVolume);
        PlayerPrefs.SetInt("AliceRpg.Difficulty", difficulty);
        PlayerPrefs.SetInt("AliceRpg.TextSpeed", textSpeed);
        PlayerPrefs.SetFloat("AliceRpg.UiTextScale", uiTextScale);
        PlayerPrefs.SetInt("AliceRpg.HighContrast", highContrast ? 1 : 0);
        PlayerPrefs.SetInt("AliceRpg.ReducedMotion", reducedMotion ? 1 : 0);
        PlayerPrefs.SetInt("AliceRpg.GentleEncounters", gentleEncounters ? 1 : 0);
        PlayerPrefs.SetInt("AliceRpg.Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("AliceRpg.Resolution", resolutionIndex);
        PlayerPrefs.SetInt("AliceRpg.ActiveSlot", activeSaveSlot);
        SaveKeyBindings();
        PlayerPrefs.Save();
    }

    private KeyCode LoadKey(string action, KeyCode fallback)
    {
        return (KeyCode)PlayerPrefs.GetInt("AliceRpg.Key." + action, (int)fallback);
    }

    private void SaveKeyBinding(string action, KeyCode key)
    {
        PlayerPrefs.SetInt("AliceRpg.Key." + action, (int)key);
    }

    private void SaveKeyBindings()
    {
        SaveKeyBinding("Up", keyUp); SaveKeyBinding("Down", keyDown); SaveKeyBinding("Left", keyLeft); SaveKeyBinding("Right", keyRight);
        SaveKeyBinding("Confirm", keyConfirm); SaveKeyBinding("Cancel", keyCancel); SaveKeyBinding("Quest", keyQuest); SaveKeyBinding("Log", keyLog);
        PlayerPrefs.Save();
    }

    private bool NormalizeKeyBindings()
    {
        KeyCode[] defaults = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.X, KeyCode.Q, KeyCode.L };
        HashSet<KeyCode> used = new HashSet<KeyCode>();
        bool changed = false;
        for (int i = 0; i < defaults.Length; i++)
        {
            KeyCode key = BindingAt(i);
            if (!IsBindingAllowed(key) || used.Contains(key))
            {
                key = FirstAvailableDefaultBinding(defaults, used);
                SetBindingValue(i, key);
                changed = true;
            }
            used.Add(key);
        }
        return changed;
    }

    private KeyCode FirstAvailableDefaultBinding(KeyCode[] defaults, HashSet<KeyCode> used)
    {
        for (int i = 0; i < defaults.Length; i++)
            if (!used.Contains(defaults[i])) return defaults[i];
        return KeyCode.W;
    }

    private bool IsBindingAllowed(KeyCode key)
    {
        return key != KeyCode.None && !IsReservedKey(key) && Enum.IsDefined(typeof(KeyCode), key);
    }

    private void SetBindingValue(int index, KeyCode key)
    {
        if (index == 0) keyUp = key; else if (index == 1) keyDown = key; else if (index == 2) keyLeft = key; else if (index == 3) keyRight = key;
        else if (index == 4) keyConfirm = key; else if (index == 5) keyCancel = key; else if (index == 6) keyQuest = key; else keyLog = key;
    }

    private void ResetSettings()
    {
        musicVolume = 0.55f; sfxVolume = 0.7f; difficulty = 1; textSpeed = 1; uiTextScale = 1f;
        highContrast = false; reducedMotion = false; gentleEncounters = false; fullscreen = true; resolutionIndex = 1;
        keyUp = KeyCode.W; keyDown = KeyCode.S; keyLeft = KeyCode.A; keyRight = KeyCode.D;
        keyConfirm = KeyCode.Space; keyCancel = KeyCode.X; keyQuest = KeyCode.Q; keyLog = KeyCode.L;
        ApplyDisplaySettings();
        SaveSettings();
    }

    private void ApplyDisplaySettings()
    {
        int[] widths = { 960, 1280, 1920 };
        int[] heights = { 540, 720, 1080 };
        int index = Mathf.Clamp(resolutionIndex, 0, widths.Length - 1);
        Screen.SetResolution(widths[index], heights[index], fullscreen);
    }

    private void RequestDisplayChange(bool requestedFullscreen, int requestedResolutionIndex)
    {
        if (confirmDisplayChange) return;
        previousFullscreen = fullscreen;
        previousResolutionIndex = resolutionIndex;
        fullscreen = requestedFullscreen;
        resolutionIndex = Mathf.Clamp(requestedResolutionIndex, 0, 2);
        ApplyDisplaySettings();
        confirmDisplayChange = true;
        displayConfirmUntil = Time.unscaledTime + 12f;
    }

    private void SaveGame(bool notify)
    {
        SaveData data = new SaveData
        {
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            chapterName = ChapterName(),
            chapter = chapter,
            fragments = fragments,
            playerX = playerPosition.x,
            playerY = playerPosition.y,
            facingX = facing.x,
            facingY = facing.y,
            level = level,
            hp = Mathf.Max(1, hp),
            maxHp = maxHp,
            mp = mp,
            maxMp = maxMp,
            attack = attack,
            experience = experience,
            potions = potions,
            steps = stepsTaken,
            battlesWon = battlesWon,
            playSeconds = previousPlaySeconds + Mathf.RoundToInt(Time.unscaledTime - sessionStartedAt),
            deaths = deaths,
            teaLeaves = teaLeaves,
            openedChests = openedChests,
            cleared = cleared,
            tutorialSeen = tutorialSeen,
            teaCharm = teaCharm,
            introStage = introStage,
            dialogueLog = dialogueHistory.ToArray()
        };
        data.checksum = SaveChecksum(data);
        try
        {
            string key = SaveKey(activeSaveSlot);
            if (PlayerPrefs.HasKey(key))
            {
                string previous = PlayerPrefs.GetString(key);
                if (DeserializeSave(previous) != null) PlayerPrefs.SetString(key + ".backup", previous);
            }
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            hasSave = true;
            lastAutoSaveAt = Time.unscaledTime;
            saveIndicatorUntil = Time.unscaledTime + 2.2f;
            lastSaveNotice = notify ? "しおりに保存しました" : "自動保存しました";
            if (notify) Toast(lastSaveNotice + "（スロット " + (activeSaveSlot + 1) + "）");
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save Alice RPG data: " + exception.Message);
            Toast("保存に失敗しました。空き容量を確認してください。");
        }
    }

    private void LoadGame()
    {
        SaveData data = ReadSave(activeSaveSlot, false);
        if (data == null)
        {
            if (HasBackupInSlot(activeSaveSlot))
            {
                saveSlotPurpose = SaveSlotPurpose.Manage;
                saveSlotReturnMode = mode;
                saveSlotSelection = activeSaveSlot;
                confirmBackupRestore = true;
                mode = GameMode.SaveSlots;
                Toast("通常データを読み込めません。バックアップを復元できます。");
            }
            else
            {
                hasSave = false;
                titleSelection = 1;
                Toast("セーブデータを読み込めませんでした。");
            }
            return;
        }
        ApplySave(data);
    }

    private void ApplySave(SaveData data)
    {
        chapter = Mathf.Clamp(data.chapter, 0, 4);
        fragments = Mathf.Clamp(data.fragments, 0, 3);
        playerPosition = new Vector2Int(data.playerX, data.playerY);
        if (!CanWalk(playerPosition)) playerPosition = ChapterCheckpoint();
        facing = new Vector2Int(data.facingX, data.facingY);
        if (facing == Vector2Int.zero) facing = Vector2Int.down;
        level = Mathf.Max(1, data.level);
        maxHp = Mathf.Max(34, data.maxHp);
        hp = Mathf.Clamp(data.hp, 1, maxHp);
        maxMp = Mathf.Max(12, data.maxMp);
        mp = Mathf.Clamp(data.mp, 0, maxMp);
        attack = Mathf.Max(8, data.attack);
        experience = Mathf.Max(0, data.experience);
        potions = Mathf.Clamp(data.potions, 0, 9);
        stepsTaken = Mathf.Max(0, data.steps);
        battlesWon = Mathf.Max(0, data.battlesWon);
        previousPlaySeconds = Mathf.Max(0, data.playSeconds);
        deaths = Mathf.Max(data.deaths, PlayerPrefs.GetInt("AliceRpg.Deaths." + activeSaveSlot, 0));
        teaLeaves = Mathf.Clamp(data.teaLeaves, 0, 9);
        openedChests = Mathf.Max(0, data.openedChests);
        cleared = data.cleared || chapter >= 4;
        tutorialSeen = data.tutorialSeen;
        introStage = Mathf.Clamp(data.introStage, 0, 2);
        teaCharm = data.teaCharm;
        dialogueHistory.Clear();
        if (data.dialogueLog != null)
        {
            for (int i = Mathf.Max(0, data.dialogueLog.Length - 80); i < data.dialogueLog.Length; i++)
                if (!string.IsNullOrEmpty(data.dialogueLog[i])) dialogueHistory.Add(data.dialogueLog[i]);
        }
        sessionStartedAt = Time.unscaledTime;
        stepsSinceBattle = 0;
        showQuest = false;
        autoPath.Clear();
        if (chapter == 0 && introStage == 1)
        {
            dialogueHistory.Clear();
            BeginIntroNarrative();
            return;
        }
        if (chapter == 0 && introStage == 2)
        {
            BeginTutorialDialogue();
            return;
        }
        mode = chapter >= 4 ? GameMode.Ending : GameMode.Explore;
        Toast("しおりの場所から物語を再開します。");
    }

    private string SaveKey(int slot) { return "AliceRpg.Save.v5." + Mathf.Clamp(slot, 0, 2); }

    private bool HasSaveInSlot(int slot) { return ReadSave(slot, false) != null; }

    private bool HasBackupInSlot(int slot) { return ReadBackup(slot) != null; }

    private bool HasRecoverableSaveInSlot(int slot) { return HasSaveInSlot(slot) || HasBackupInSlot(slot); }

    private int FirstRecoverableSaveSlot()
    {
        for (int slot = 0; slot < 3; slot++)
            if (HasRecoverableSaveInSlot(slot)) return slot;
        return -1;
    }

    private void SelectRecoverableActiveSlot()
    {
        if (HasRecoverableSaveInSlot(activeSaveSlot)) return;
        int fallbackSlot = FirstRecoverableSaveSlot();
        if (fallbackSlot < 0) return;
        activeSaveSlot = fallbackSlot;
        PlayerPrefs.SetInt("AliceRpg.ActiveSlot", activeSaveSlot);
        PlayerPrefs.Save();
    }

    private SaveData ReadSave(int slot, bool useBackup)
    {
        string key = SaveKey(slot);
        SaveData data = DeserializeSave(PlayerPrefs.GetString(key, ""));
        if (data != null) return data;
        return useBackup ? ReadBackup(slot) : null;
    }

    private SaveData ReadBackup(int slot)
    {
        return DeserializeSave(PlayerPrefs.GetString(SaveKey(slot) + ".backup", ""));
    }

    private SaveData DeserializeSave(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.version < 2 || data.version > CurrentSaveVersion) return null;
            if (data.version >= CurrentSaveVersion && (string.IsNullOrEmpty(data.checksum) || data.checksum != SaveChecksum(data))) return null;
            return data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string SaveChecksum(SaveData data)
    {
        string existing = data.checksum;
        data.checksum = "";
        string payload = JsonUtility.ToJson(data);
        data.checksum = existing;
        uint hash = 2166136261u;
        for (int i = 0; i < payload.Length; i++)
        {
            hash ^= payload[i];
            hash *= 16777619u;
        }
        return hash.ToString("X8");
    }

    private void MigrateLegacySaveIfNeeded()
    {
        if (PlayerPrefs.GetInt("AliceRpg.SaveMigratedV5", 0) == 1) return;
        for (int slot = 0; slot < 3; slot++)
        {
            string oldKey = "AliceRpg.Save.v4." + slot;
            MigrateSaveValue(oldKey, SaveKey(slot));
            MigrateSaveValue(oldKey + ".backup", SaveKey(slot) + ".backup");
        }
        if (!PlayerPrefs.HasKey(SaveKey(0)))
        {
            SaveData legacy = DeserializeSave(PlayerPrefs.GetString("AliceRpg.Save.v2", ""));
            if (legacy != null)
            {
                legacy.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                legacy.chapterName = "以前のしおり";
                UpgradeSaveData(legacy);
                PlayerPrefs.SetString(SaveKey(0), JsonUtility.ToJson(legacy));
            }
        }
        PlayerPrefs.SetInt("AliceRpg.SaveMigratedV5", 1);
        PlayerPrefs.Save();
    }

    private void MigrateSaveValue(string sourceKey, string destinationKey)
    {
        if (PlayerPrefs.HasKey(destinationKey)) return;
        SaveData legacy = DeserializeSave(PlayerPrefs.GetString(sourceKey, ""));
        if (legacy == null) return;
        UpgradeSaveData(legacy);
        PlayerPrefs.SetString(destinationKey, JsonUtility.ToJson(legacy));
    }

    private void UpgradeSaveData(SaveData data)
    {
        data.version = CurrentSaveVersion;
        data.checksum = SaveChecksum(data);
    }

    private string ChapterName()
    {
        if (chapter == 0) return "白ウサギを探す";
        if (chapter == 1) return "青いイモムシを訪ねる";
        if (chapter == 2) return "チェシャ猫を訪ねる";
        if (chapter == 3) return "ハートの城へ";
        return "物語を完了";
    }

    private Vector2Int ChapterCheckpoint()
    {
        if (chapter >= 3) return new Vector2Int(24, 7);
        if (chapter == 2) return new Vector2Int(16, 8);
        if (chapter == 1) return new Vector2Int(7, 10);
        return new Vector2Int(3, 13);
    }

    private void Interact()
    {
        Vector2Int target = playerPosition + facing;
        int chestIndex = ChestIndexAt(target);
        if (chestIndex >= 0)
        {
            OpenChest(chestIndex);
            return;
        }
        Npc npc = NpcAt(target);
        if (npc == null)
        {
            Toast("風がページをめくる音がする……");
            return;
        }
        Play(confirmSound);
        if (npc.id == "rabbit") TalkRabbit();
        else if (npc.id == "hatter") TalkHatter();
        else if (npc.id == "caterpillar") TalkCaterpillar();
        else if (npc.id == "cat") TalkCat();
        else if (npc.id == "queen") TalkQueen();
    }

    private int ChestIndexAt(Vector2Int position)
    {
        for (int i = 0; i < chestPositions.Count; i++) if (chestPositions[i] == position) return i;
        return -1;
    }

    private bool IsChestOpened(int index) { return (openedChests & (1 << index)) != 0; }

    private void OpenChest(int index)
    {
        if (IsChestOpened(index))
        {
            Toast("宝箱は空っぽだ。");
            return;
        }
        openedChests |= 1 << index;
        Play(magicSound);
        if (index == 0)
        {
            potions = Mathf.Min(9, potions + 2);
            ShowDialogue("古い宝箱", new[] { "『DRINK ME』の小瓶を2つ見つけた！\n戦いが不安なら、まず回復の準備をしておこう。" }, delegate { Toast("小瓶を2つ手に入れた！"); SaveGame(false); });
        }
        else if (index == 1)
        {
            teaLeaves++;
            ShowDialogue("月白の宝箱", new[] { "香り高い《月白の茶葉》を見つけた。\n帽子屋なら、この葉の使い道を知っているかもしれない。" }, delegate { Toast("月白の茶葉を手に入れた！"); SaveGame(false); });
        }
        else
        {
            experience += 14;
            ShowDialogue("時計仕掛けの宝箱", new[] { "星のようにきらめく歯車がほどけた。\nアリスは14 EXPを得た！" }, delegate { Toast("14 EXPを得た！"); SaveGame(false); });
        }
    }

    private void TalkRabbit()
    {
        if (chapter == 0)
        {
            ShowDialogue("白ウサギ", new[]
            {
                "遅刻だ、遅刻だ！……ああアリス、やっと来た！\n女王が《時の冠》を三つに割ってしまったんだ。",
                "これは僕が隠しておいた一つ目――《朝の針》。\n残る欠片は、北西の青いイモムシと、橋の向こうのチェシャ猫が知っている。",
                "君なら物語の続きを選べる。さあ、止まった明日を取り戻して！"
            }, delegate
            {
                fragments = 1;
                chapter = 1;
                Toast("《朝の針》を手に入れた！　欠片 1/3");
                SaveGame(false);
            });
        }
        else ShowDialogue("白ウサギ", new[] { "時計はまだ動かない。欠片を追って！\n青いイモムシは北西、チェシャ猫は橋の東だよ。" }, null);
    }

    private void TalkHatter()
    {
        if (teaLeaves > 0 && !teaCharm)
        {
            ShowDialogue("帽子屋", new[]
            {
                "おやおや、《月白の茶葉》じゃないか！\n止まった夜を少しだけ静かにしてくれる、とびきりの葉だよ。",
                "特製《お茶会のお守り》にしてあげよう。\n草むらの騒がしい出会いが、少し遠のくはずさ。"
            }, delegate { teaCharm = true; Toast("《お茶会のお守り》を手に入れた！　遭遇率が下がった。 "); SaveGame(false); });
            return;
        }
        ShowDialogue("帽子屋", new[]
        {
            "ようこそ、終わらないお茶会へ！\n空のカップほど、可能性で満ちたものはない。",
            "ミルクをひとさじ、勇気をふたさじ。\nHPとMPをすっかり元どおりにしておいたよ。"
        }, delegate
        {
            hp = maxHp;
            mp = maxMp;
            if (potions < 3) potions++;
            Toast("元気になった！　小瓶も補充された。");
            SaveGame(false);
        });
    }

    private void TalkCaterpillar()
    {
        if (chapter < 1)
        {
            ShowDialogue("青いイモムシ", new[] { "『君は誰だい？』……答えを急ぐ必要はない。\nまず白ウサギの物語を聞いておいで。" }, null);
        }
        else if (chapter == 1)
        {
            ShowDialogue("青いイモムシ", new[]
            {
                "君は誰だい？　昨日の君か、明日の君か。",
                "大きくなったり、小さくなったりしても、選んだ一歩は君のもの。\nその答えなら十分だ。",
                "二つ目の欠片《昼の歯車》を持っておいき。\n笑いだけを残す猫には気をつけるんだね。"
            }, delegate
            {
                fragments = 2;
                chapter = 2;
                Toast("《昼の歯車》を手に入れた！　欠片 2/3");
                SaveGame(false);
            });
        }
        else ShowDialogue("青いイモムシ", new[] { "道に迷うことと、道を選ぶことは違う。\n東へ進み、君自身の足で確かめるんだ。" }, null);
    }

    private void TalkCat()
    {
        if (chapter < 2)
        {
            ShowDialogue("チェシャ猫", new[] { "ここでは誰もが迷子さ。君も、ボクも。\n欠片を二つ揃えたら、もっと面白い顔を見せてあげる。" }, null);
        }
        else if (chapter == 2)
        {
            ShowDialogue("チェシャ猫", new[]
            {
                "三つ目の欠片？　もちろんボクの笑顔の裏さ。",
                "でも女王が落とした影が、ボクの笑顔を食べている。\nさあアリス――怖がる君と、勇敢な君、どちらが本物かな？"
            }, delegate
            {
                StartBattle("にやにや影", "笑顔の裏側から、真っ黒な影が這い出した！", 44, 8, 24, true, textures["shadow"], delegate
                {
                    fragments = 3;
                    chapter = 3;
                    hp = Mathf.Min(maxHp, hp + 12);
                    Toast("《夜の鏡》を手に入れた！　城門が開いた。");
                    SaveGame(false);
                });
            });
        }
        else ShowDialogue("チェシャ猫", new[] { "道があるから進むんじゃない。\n君が進むから、そこが道になるのさ。城は北東だよ。" }, null);
    }

    private void TalkQueen()
    {
        if (chapter < 3)
        {
            ShowDialogue("ハートの女王", new[] { "欠けた冠に用はない！　城門から出ておゆき！" }, null);
            return;
        }
        ShowDialogue("ハートの女王", new[]
        {
            "誰の許しで明日を持ち込んだ！\nこの国では、すべての時刻をわらわが決める！",
            "昨日も今日も同じなら、誰もわらわに逆らわない。\n物語は永遠に、わらわのページで止まるのだ！",
            "アリス：『いいえ。次のページを選ぶのは、ここに生きるみんなよ！』"
        }, delegate
        {
            StartBattle("ハートの女王", "トランプの嵐とともに、女王が立ちはだかった！", 78, 11, 60, true, textures["queenBattle"], FinalVictory);
        });
    }

    private void FinalVictory()
    {
        chapter = 4;
        cleared = true;
        SaveGame(false);
        ShowDialogue("語り手", new[]
        {
            "三つの欠片が空へ舞い、《時の冠》は再びひとつになりました。\n止まっていた時計の月が、やさしく時を刻み始めます。",
            "女王の兵隊たちはカードに戻り、帽子屋のお茶はようやく冷め、\n白ウサギは――やっぱり少しだけ遅刻しました。",
            "チェシャ猫：『夢だったと思うかい？　それを決めるのも君さ。』",
            "アリスが目を開けると、樫の木の下。\n手のひらには、小さな金色の歯車が残っていました。"
        }, delegate { mode = GameMode.Ending; });
    }

    private void CheckRandomBattle()
    {
        if (stepsSinceBattle < 7 || TileAt(playerPosition.x, playerPosition.y) != 'g') return;
        double encounterChance = difficulty == 0 ? 0.09 : difficulty == 2 ? 0.19 : 0.14;
        if (gentleEncounters) encounterChance *= 0.48;
        if (teaCharm) encounterChance *= 0.72;
        if (random.NextDouble() > encounterChance) return;
        stepsSinceBattle = 0;
        bool east = playerPosition.x > 14;
        double roll = random.NextDouble();
        if (east && roll > 0.68)
            StartBattle("時計ネズミ", "歯車を背負ったネズミが、秒針のように駆け寄った！", 24 + level * 3, 7 + level, 13, false, textures["shadow"], null);
        else if (east && roll > 0.32)
            StartBattle("トランプ兵", "トランプ兵が道をふさいだ！", 28 + level * 3, 6 + level, 11, false, textures["card"], null);
        else if (!east && roll > 0.62)
            StartBattle("笑う花", "花びらが笑い声をあげ、アリスを囲んだ！", 18 + level * 3, 5 + level, 10, false, textures["mushroom"], null);
        else
            StartBattle("おしゃべりキノコ", "おしゃべりキノコが難しい顔で現れた！", 20 + level * 3, 5 + level, 8, false, textures["mushroom"], null);
    }

    private void StartBattle(string enemyName, string flavor, int enemyHp, int enemyAttack, int xp, bool boss, Texture2D sprite, Action victory)
    {
        enemy = new Enemy
        {
            name = enemyName,
            flavor = flavor,
            hp = enemyHp,
            maxHp = enemyHp,
            attack = enemyAttack,
            xp = xp,
            boss = boss,
            specialName = enemyName == "ハートの女王" ? "女王の宣告" : enemyName == "にやにや影" ? "月蝕" : "",
            sprite = sprite
        };
        battleSelection = 0;
        battleMessage = flavor;
        pendingBattle = PendingBattle.Menu;
        battleVictory = victory;
        guarding = false;
        enemyInspected = false;
        weakenedTurns = 0;
        autoPath.Clear();
        SaveGame(false);
        mode = GameMode.Battle;
        Play(hitSound);
    }

    private void UpdateBattle()
    {
        if (pendingBattle != PendingBattle.None)
        {
            if (!PressConfirm()) return;
            AdvanceBattleMessage();
            return;
        }

        int vertical = VerticalInput();
        if (vertical != 0)
        {
            battleSelection = (battleSelection + vertical * 2 + 6) % 6;
            Play(moveSound);
        }
        int horizontal = HorizontalInput();
        if (horizontal != 0)
        {
            battleSelection = (battleSelection + horizontal + 6) % 6;
            Play(moveSound);
        }
        if (!PressConfirm()) return;
        Play(confirmSound);
        if (battleSelection == 0) PlayerAttack(false);
        else if (battleSelection == 1) PlayerAttack(true);
        else if (battleSelection == 2) Guard();
        else if (battleSelection == 3) UsePotion();
        else if (battleSelection == 4) InspectEnemy();
        else TryRun();
    }

    private void AdvanceBattleMessage()
    {
        Play(confirmSound);
        if (pendingBattle == PendingBattle.Menu)
        {
            pendingBattle = PendingBattle.None;
            battleMessage = "行動を選んでください。";
        }
        else if (pendingBattle == PendingBattle.Victory) FinishBattleVictory();
        else if (pendingBattle == PendingBattle.Defeat)
        {
            deaths++;
            PlayerPrefs.SetInt("AliceRpg.Deaths." + activeSaveSlot, deaths);
            PlayerPrefs.Save();
            gameOverSelection = 0;
            mode = GameMode.GameOver;
        }
    }

    private void PlayerAttack(bool magic)
    {
        if (magic && mp < 4)
        {
            battleMessage = "MPが足りない！";
            pendingBattle = PendingBattle.Menu;
            return;
        }
        int damage;
        string actionText;
        if (magic)
        {
            mp -= 4;
            damage = random.Next(11, 17) + level * 2;
            actionText = "アリスは《好奇心の光》を放った！";
            Play(magicSound);
        }
        else
        {
            damage = random.Next(Mathf.Max(2, attack - 2), attack + 4);
            bool critical = random.NextDouble() < 0.12;
            if (critical) damage *= 2;
            actionText = critical ? "アリスの会心の一撃！" : "アリスの攻撃！";
            Play(hitSound);
        }
        if (weakenedTurns > 0)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.7f));
            weakenedTurns--;
            actionText += "\n《女王の宣告》で力が出ない……";
        }
        enemy.hp = Mathf.Max(0, enemy.hp - damage);
        battleMessage = actionText + "\n" + enemy.name + "に " + damage + " ダメージ。";
        if (enemy.hp <= 0)
        {
            pendingBattle = PendingBattle.Victory;
            battleMessage += "\n\n" + enemy.name + "を倒した！　[決定]";
            return;
        }
        EnemyTurn();
    }

    private void UsePotion()
    {
        if (potions <= 0)
        {
            battleMessage = "小瓶はもう空っぽだ。";
            pendingBattle = PendingBattle.Menu;
            return;
        }
        if (hp >= maxHp)
        {
            battleMessage = "HPは満タンだ。";
            pendingBattle = PendingBattle.Menu;
            return;
        }
        potions--;
        int healed = Mathf.Min(20, maxHp - hp);
        hp += healed;
        battleMessage = "『DRINK ME』の小瓶を使った。\nHPが " + healed + " 回復！";
        Play(magicSound);
        EnemyTurn();
    }

    private void Guard()
    {
        guarding = true;
        int restored = Mathf.Min(2, maxMp - mp);
        mp += restored;
        battleMessage = "アリスは身構えた。次のダメージを半減！";
        if (restored > 0) battleMessage += "\n落ち着きを取り戻し、MPが " + restored + " 回復。";
        EnemyTurn();
    }

    private void InspectEnemy()
    {
        enemyInspected = true;
        battleMessage = enemy.name + "を観察した。\nHP " + enemy.hp + "/" + enemy.maxHp + "　攻撃 " + enemy.attack;
        battleMessage += enemy.boss ? "\n強敵。逃げられないが、守りが有効だ。" : "\n逃走成功率 62%。弱ったら無理をしないで。";
        pendingBattle = PendingBattle.Menu;
    }

    private void TryRun()
    {
        if (enemy.boss)
        {
            battleMessage = "ここで物語から逃げるわけにはいかない！";
            EnemyTurn();
            return;
        }
        if (random.NextDouble() < 0.62)
        {
            battleMessage = "アリスは木立の向こうへ逃げ切った！";
            pendingBattle = PendingBattle.Victory;
            enemy.xp = 0;
            battleVictory = null;
        }
        else
        {
            battleMessage = "しかし、回り込まれてしまった！";
            EnemyTurn();
        }
    }

    private void EnemyTurn()
    {
        int variance = random.Next(-2, 3);
        int reduction = level - 1;
        float difficultyScale = difficulty == 0 ? 0.72f : difficulty == 2 ? 1.28f : 1f;
        bool special = enemy.boss && random.NextDouble() < 0.32;
        int bonus = special ? (enemy.name == "ハートの女王" ? 4 : 2) : 0;
        int damage = Mathf.Max(1, Mathf.RoundToInt((enemy.attack + variance + bonus - reduction) * difficultyScale));
        if (guarding)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));
            guarding = false;
        }
        hp = Mathf.Max(0, hp - damage);
        if (special)
        {
            battleMessage += "\n\n" + enemy.name + "の《" + enemy.specialName + "》！\nアリスは " + damage + " ダメージ。";
            if (enemy.name == "ハートの女王")
            {
                weakenedTurns = 2;
                battleMessage += "\n次の2回の攻撃が弱まる！";
            }
            else
            {
                int drained = Mathf.Min(2, mp);
                mp -= drained;
                battleMessage += "\n月明かりがMPを " + drained + " 奪った。";
            }
        }
        else battleMessage += "\n\n" + enemy.name + "の反撃！\nアリスは " + damage + " ダメージ。";
        Play(hitSound);
        if (hp <= 0)
        {
            pendingBattle = PendingBattle.Defeat;
            battleMessage += "\n\nアリスは力尽きた……　[決定]";
        }
        else pendingBattle = PendingBattle.Menu;
    }

    private void FinishBattleVictory()
    {
        int earned = enemy.xp;
        experience += earned;
        if (earned > 0) battlesWon++;
        string levelText = "";
        int needed = level * 22;
        if (earned > 0 && experience >= needed)
        {
            experience -= needed;
            level++;
            maxHp += 8;
            maxMp += 3;
            attack += 3;
            hp = maxHp;
            mp = maxMp;
            levelText = " レベル" + level + "になった！";
        }
        Action callback = battleVictory;
        battleVictory = null;
        mode = GameMode.Explore;
        if (earned > 0) Toast(earned + " EXPを得た。" + levelText);
        SaveGame(false);
        if (callback != null) callback();
    }

    private bool CanWalk(Vector2Int p)
    {
        if (p.x < 1 || p.y < 1 || p.x >= MapWidth - 1 || p.y >= MapHeight - 1) return false;
        char tile = TileAt(p.x, p.y);
        if (tile == '#' || tile == 'w' || tile == 's' || tile == 'd' || tile == 'c') return false;
        return NpcAt(p) == null;
    }

    private Npc NpcAt(Vector2Int p)
    {
        for (int i = 0; i < npcs.Count; i++)
            if (npcs[i].position == p) return npcs[i];
        return null;
    }

    private char TileAt(int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= MapWidth - 1 || y >= MapHeight - 1) return '#';
        if (shrubs.Contains(Key(x, y))) return 's';
        int chestIndex = ChestIndexAt(new Vector2Int(x, y));
        if (chestIndex >= 0 && !IsChestOpened(chestIndex)) return 'c';
        if (x == 14 && y != 8) return 'w';
        if (x == 14 && y == 8) return 'b';

        bool castleEdge = x >= 23 && x <= 28 && y >= 1 && y <= 5 &&
                          (x == 23 || x == 28 || y == 1 || y == 5);
        if (castleEdge)
        {
            if (x == 25 && y == 5) return chapter >= 3 ? '.' : 'd';
            return '#';
        }
        if ((x < 13 && y < 7) || (x > 15 && y > 8)) return 'g';
        return '.';
    }

    private int Key(int x, int y) { return x + y * MapWidth; }

    private void ShowDialogue(string speaker, string[] pages, Action finished)
    {
        dialogueSpeaker = speaker;
        dialoguePages.Clear();
        dialoguePages.AddRange(pages);
        for (int i = 0; i < pages.Length; i++) dialogueHistory.Add(speaker + "： " + pages[i].Replace("\n", " "));
        if (dialogueHistory.Count > 80) dialogueHistory.RemoveRange(0, dialogueHistory.Count - 80);
        dialogueIndex = 0;
        dialogueStartedAt = Time.unscaledTime;
        dialogueFinished = finished;
        mode = GameMode.Dialogue;
    }

    private bool DialoguePageFullyRevealed()
    {
        if (dialogueIndex >= dialoguePages.Count || dialogueStartedAt < 0f) return true;
        string page = dialoguePages[dialogueIndex];
        float charactersPerSecond = DialogueCharactersPerSecond();
        return (Time.unscaledTime - dialogueStartedAt) * charactersPerSecond >= page.Length;
    }

    private string VisibleDialogueText()
    {
        if (dialogueIndex >= dialoguePages.Count) return "";
        string page = dialoguePages[dialogueIndex];
        if (dialogueStartedAt < 0f) return page;
        float charactersPerSecond = DialogueCharactersPerSecond();
        int count = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - dialogueStartedAt) * charactersPerSecond), 0, page.Length);
        return page.Substring(0, count);
    }

    private float DialogueCharactersPerSecond()
    {
        float speed = textSpeed == 0 ? 22f : textSpeed == 2 ? 80f : 42f;
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.JoystickButton5) ? speed * 4f : speed;
    }

    private void Toast(string message)
    {
        toast = message;
        toastUntil = Time.unscaledTime + 3.4f;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void OnGUI()
    {
        InitStyles();
        float scale = Mathf.Min(Screen.width / (float)LogicalWidth, Screen.height / (float)LogicalHeight);
        float offsetX = (Screen.width - LogicalWidth * scale) * 0.5f;
        float offsetY = (Screen.height - LogicalHeight * scale) * 0.5f;
        guiScale = Mathf.Max(0.0001f, scale);
        guiOffset = new Vector2(offsetX, offsetY);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(-offsetX / scale, -offsetY / scale, Screen.width / scale, Screen.height / scale), textures["white"]);
        GUI.color = Color.white;

        if (mode == GameMode.Title) DrawTitle();
        else if (mode == GameMode.Battle) DrawBattle();
        else if (mode == GameMode.Settings) DrawSettings();
        else if (mode == GameMode.SaveSlots) DrawSaveSlots();
        else if (mode == GameMode.Controls) DrawControls();
        else if (mode == GameMode.Records) DrawRecords();
        else if (mode == GameMode.Credits) DrawCredits();
        else if (mode == GameMode.Ending) DrawEnding();
        else if (mode == GameMode.GameOver) DrawGameOver();
        else
        {
            DrawWorld();
            if (mode == GameMode.Intro || mode == GameMode.Dialogue) DrawDialogue();
            if (mode == GameMode.Pause) DrawPause();
            if (mode == GameMode.DialogueLog) DrawDialogueLog();
            if (showQuest && mode == GameMode.Explore) DrawQuest();
        }
        GUI.matrix = previous;
    }

    private void DrawTitle()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;

        for (int i = 0; i < 42; i++)
        {
            int x = (i * 83 + 31) % LogicalWidth;
            int y = (i * 47 + 19) % 350;
            int size = (i % 3) + 2;
            GUI.color = i % 4 == 0 ? gold : new Color(0.65f, 0.78f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, size, size), textures["white"]);
        }
        GUI.color = Color.white;
        DrawTexturePixel(textures["alice"], new Rect(414, 118, 132, 132));
        GUI.Label(new Rect(100, 40, 760, 64), "ALICE", titleOnDarkStyle);
        GUI.Label(new Rect(100, 91, 760, 42), "& THE BROKEN CROWN", centerOnDarkStyle);
        GUI.Label(new Rect(100, 248, 760, 36), "アリスと壊れた時の冠", centerOnDarkStyle);

        DrawPanel(new Rect(310, 280, 340, 226));
        string[] titleItems =
        {
            hasSave ? "つづきから　[スロット " + (activeSaveSlot + 1) + "]" : "つづきから（データなし）",
            "はじめから", "セーブデータを管理", "クリア記録", "設定", "クレジット・サポート", "ゲームをおわる"
        };
        for (int i = 0; i < titleItems.Length; i++)
        {
            Rect itemRect = new Rect(335, 291 + i * 29, 290, 27);
            DrawMenuItem(itemRect, titleItems[i], titleSelection == i);
            if (!confirmNewGame && !confirmQuit && MouseActivated(itemRect)) { titleSelection = i; ActivateTitleSelection(); }
        }
        SaveData titlePreview = ReadSave(activeSaveSlot, false);
        if (titlePreview == null) titlePreview = ReadBackup(activeSaveSlot);
        if (hasSave && titlePreview != null)
        {
            string progress = string.IsNullOrEmpty(titlePreview.chapterName) ? "物語の途中" : titlePreview.chapterName;
            GUI.Label(new Rect(650, 404, 250, 48), "しおりを読み込めます\n" + progress, centerOnDarkStyle);
        }
        GUI.Label(new Rect(110, 508, 740, 22), "A WONDERLAND STORY　—　キーボード / マウス / ゲームパッド対応", smallOnDarkStyle);
        if (Time.unscaledTime < toastUntil)
        {
            DrawPanel(new Rect(190, 255, 580, 38));
            GUI.Label(new Rect(205, 261, 550, 26), toast, hintStyle);
        }
        if (confirmNewGame || confirmQuit)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, 960, 540), textures["white"]);
            GUI.color = Color.white;
            DrawPanel(new Rect(250, 178, 460, 184));
            GUI.Label(new Rect(280, 202, 400, 62), confirmNewGame ? "現在のしおりを上書きして\nはじめから遊びますか？" : "ゲームを終了しますか？", centerStyle);
            Rect yesRect = new Rect(300, 294, 160, 38);
            Rect noRect = new Rect(500, 294, 160, 38);
            DrawMenuItem(yesRect, "はい [決定]", true);
            DrawMenuItem(noRect, "いいえ [Esc]", false);
            if (MouseActivated(yesRect)) { bool start = confirmNewGame; confirmNewGame = false; confirmQuit = false; if (start) StartNewGame(); else Application.Quit(); }
            if (MouseActivated(noRect)) { confirmNewGame = false; confirmQuit = false; }
        }
    }

    private void DrawWorld()
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                char tile = TileAt(x, y);
                string textureName = "ground";
                if (tile == '#') textureName = "wall";
                else if (tile == 'g') textureName = "grass";
                else if (tile == 'w') textureName = "water";
                else if (tile == 'b') textureName = "bridge";
                else if (tile == 's') textureName = "shrub";
                else if (tile == 'd') textureName = "door";
                else if (tile == 'c') textureName = "chest";
                GUI.DrawTexture(new Rect(x * Tile, y * Tile, Tile, Tile), textures[textureName]);

                if (tile == 'g' && (x * 7 + y * 11) % 17 == 0)
                    GUI.DrawTexture(new Rect(x * Tile + 12, y * Tile + 9, 8, 8), textures["flower"]);
            }
        }

        DrawWorldLabel(new Rect(742, 8, 200, 24), "♥ ハートの城");
        DrawWorldLabel(new Rect(22, 244, 180, 24), "☕ 終わらないお茶会");
        DrawWorldLabel(new Rect(386, 260, 190, 24), "時忘れの橋");

        List<Npc> sorted = new List<Npc>(npcs);
        sorted.Sort(delegate(Npc a, Npc b) { return a.position.y.CompareTo(b.position.y); });
        bool playerDrawn = false;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (!playerDrawn && playerPosition.y < sorted[i].position.y)
            {
                DrawTexturePixel(textures["alice"], SpriteRect(playerPosition));
                playerDrawn = true;
            }
            DrawTexturePixel(sorted[i].sprite, SpriteRect(sorted[i].position));
            if (mode == GameMode.Explore && !showQuest && Vector2Int.Distance(playerPosition, sorted[i].position) <= 1.1f && MouseActivated(SpriteRect(sorted[i].position)))
            {
                facing = sorted[i].position - playerPosition;
                Interact();
            }
            if (Vector2Int.Distance(playerPosition, sorted[i].position) <= 1.1f)
                DrawWorldLabel(new Rect(sorted[i].position.x * Tile - 28, sorted[i].position.y * Tile - 16, 88, 18), sorted[i].displayName);
        }
        if (!playerDrawn) DrawTexturePixel(textures["alice"], SpriteRect(playerPosition));

        for (int i = 0; i < chestPositions.Count; i++)
        {
            Vector2Int chestPosition = chestPositions[i];
            if (!IsChestOpened(i) && mode == GameMode.Explore && !showQuest && Vector2Int.Distance(playerPosition, chestPosition) <= 1.1f && MouseActivated(SpriteRect(chestPosition)))
            {
                facing = chestPosition - playerPosition;
                OpenChest(i);
            }
        }

        DrawObjectiveGuide();

        Npc nearby = NpcAt(playerPosition + facing);
        int chestAhead = ChestIndexAt(playerPosition + facing);
        if (nearby != null && mode == GameMode.Explore)
        {
            DrawPanel(new Rect(playerPosition.x * Tile - 42, Mathf.Max(8, playerPosition.y * Tile - 42), 116, 28));
            GUI.Label(new Rect(playerPosition.x * Tile - 38, Mathf.Max(9, playerPosition.y * Tile - 41), 108, 24), "[" + ConfirmHint() + "] 話す", hintStyle);
        }
        else if (chestAhead >= 0 && mode == GameMode.Explore)
        {
            DrawPanel(new Rect(playerPosition.x * Tile - 42, Mathf.Max(8, playerPosition.y * Tile - 42), 116, 28));
            GUI.Label(new Rect(playerPosition.x * Tile - 38, Mathf.Max(9, playerPosition.y * Tile - 41), 108, 24), "[" + ConfirmHint() + "] 調べる", hintStyle);
        }

        GUI.color = ink;
        GUI.DrawTexture(new Rect(0, 512, 960, 28), textures["white"]);
        GUI.color = Color.white;
        GUI.Label(new Rect(14, 515, 500, 22), "アリス  Lv." + level + "   HP " + hp + "/" + maxHp + "   MP " + mp + "/" + maxMp, smallOnDarkStyle);
        GUI.Label(new Rect(525, 515, 419, 22), "欠片 ◆ " + fragments + "/3　 [" + keyQuest + "]目的　 [" + keyLog + "]ログ　 [" + keyCancel + "]メニュー", smallOnDarkStyle);
        if (MouseActivated(new Rect(630, 512, 88, 28))) { showQuest = !showQuest; Play(confirmSound); }

        if (Time.unscaledTime < saveIndicatorUntil)
        {
            DrawPanel(new Rect(690, 48, 242, 30));
            GUI.Label(new Rect(700, 52, 222, 22), "◆ " + lastSaveNotice, hintStyle);
        }

        if (Time.unscaledTime < toastUntil)
        {
            DrawPanel(new Rect(150, 25, 660, 54));
            GUI.Label(new Rect(174, 39, 612, 28), toast, centerStyle);
        }
        HandleWorldClick();
    }

    private void HandleWorldClick()
    {
        if (mode != GameMode.Explore) return;
        Event current = Event.current;
        if (current == null || current.type != EventType.MouseDown || current.button != 0) return;
        Vector2 logicalMouse = (current.mousePosition - guiOffset) / guiScale;
        if (showQuest)
        {
            current.Use();
            if (new Rect(735, 252, 160, 30).Contains(logicalMouse)) { showQuest = false; Play(confirmSound); }
            return;
        }
        if (logicalMouse.y >= 512f)
        {
            current.Use();
            pauseSelection = 0;
            pauseReturnMode = GameMode.Explore;
            mode = GameMode.Pause;
            Play(confirmSound);
            return;
        }
        if (logicalMouse.x < 0f || logicalMouse.y < 0f || logicalMouse.x >= 960f) return;
        Vector2Int destination = new Vector2Int(Mathf.FloorToInt(logicalMouse.x / Tile), Mathf.FloorToInt(logicalMouse.y / Tile));
        List<Vector2Int> path = FindPath(playerPosition, destination);
        if (path.Count == 0 && destination != playerPosition) Toast("そこへは歩いて行けないようだ。");
        else
        {
            autoPath.Clear();
            autoPath.AddRange(path);
        }
        current.Use();
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int destination)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        if (!CanWalk(destination) || destination == start) return result;
        Queue<Vector2Int> open = new Queue<Vector2Int>();
        Dictionary<int, int> previous = new Dictionary<int, int>();
        open.Enqueue(start);
        previous[Key(start.x, start.y)] = -1;
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            if (current == destination) break;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                int nextKey = Key(next.x, next.y);
                if (previous.ContainsKey(nextKey) || !CanWalk(next)) continue;
                previous[nextKey] = Key(current.x, current.y);
                open.Enqueue(next);
            }
        }
        int destinationKey = Key(destination.x, destination.y);
        if (!previous.ContainsKey(destinationKey)) return result;
        int cursor = destinationKey;
        while (previous[cursor] != -1)
        {
            result.Add(new Vector2Int(cursor % MapWidth, cursor / MapWidth));
            cursor = previous[cursor];
        }
        result.Reverse();
        return result;
    }

    private Rect SpriteRect(Vector2Int p)
    {
        return new Rect(p.x * Tile, p.y * Tile - 4, Tile, Tile + 4);
    }

    private void DrawDialogue()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        Rect box = new Rect(54, 357, 852, 147);
        DrawPanel(box);
        GUI.Label(new Rect(77, 344, 210, 34), " " + dialogueSpeaker + " ", speakerStyle);
        string text = VisibleDialogueText();
        GUI.Label(new Rect(82, 389, 796, 80), text, dialogueStyle);
        string continueHint = DialoguePageFullyRevealed() ? "▼ " + ConfirmHint() + "でつづく　Tabでスキップ" : "▼ " + ConfirmHint() + "で全文表示　Shiftで早送り";
        GUI.Label(new Rect(590, 472, 284, 22), continueHint, smallStyle);
        if (MouseActivated(box)) AdvanceDialogue();
    }

    private void DrawBattle()
    {
        GUI.color = new Color32(28, 30, 62, 255);
        GUI.DrawTexture(new Rect(0, 0, 960, 540), textures["white"]);
        GUI.color = new Color32(55, 61, 105, 255);
        GUI.DrawTexture(new Rect(0, 285, 960, 115), textures["white"]);
        GUI.color = new Color32(82, 74, 116, 255);
        for (int i = 0; i < 14; i++) GUI.DrawTexture(new Rect(i * 76 - 20, 270 + (i % 2) * 14, 62, 130), textures["white"]);
        GUI.color = Color.white;

        DrawPanel(new Rect(28, 22, 290, 95));
        GUI.Label(new Rect(48, 38, 250, 24), "アリス　Lv." + level, labelStyle);
        GUI.Label(new Rect(48, 68, 250, 24), "HP " + hp + "/" + maxHp + "　 MP " + mp + "/" + maxMp, labelStyle);
        if (weakenedTurns > 0) GUI.Label(new Rect(48, 95, 250, 18), "状態：力が弱い　残り " + weakenedTurns + " 回", hintStyle);

        GUI.Label(new Rect(535, 30, 380, 38), enemy.name, centerOnDarkStyle);
        DrawTexturePixel(enemy.sprite, new Rect(605, 92, 240, 240));
        DrawBar(new Rect(590, 332, 270, 16), enemy.hp, enemy.maxHp, rose);

        DrawPanel(new Rect(28, 377, 420, 143));
        string[] commands = { "たたかう", "ひかり (MP 4)", "まもる +MP", "小瓶 × " + potions, "しらべる", enemy.boss ? "にげる ×" : "にげる" };
        for (int i = 0; i < commands.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Rect commandRect = new Rect(48 + col * 190, 389 + row * 40, 176, 34);
            DrawMenuItem(commandRect, commands[i], pendingBattle == PendingBattle.None && battleSelection == i);
            if (pendingBattle == PendingBattle.None && MouseActivated(commandRect))
            {
                battleSelection = i;
                if (i == 0) PlayerAttack(false); else if (i == 1) PlayerAttack(true); else if (i == 2) Guard();
                else if (i == 3) UsePotion(); else if (i == 4) InspectEnemy(); else TryRun();
            }
        }
        DrawPanel(new Rect(466, 377, 466, 143));
        Rect messageRect = new Rect(492, 398, 412, 92);
        GUI.Label(messageRect, battleMessage, dialogueStyle);
        if (pendingBattle != PendingBattle.None) GUI.Label(new Rect(760, 492, 140, 20), "▼ 決定", smallStyle);
        if (pendingBattle != PendingBattle.None && MouseActivated(new Rect(466, 377, 466, 143))) AdvanceBattleMessage();
        if (enemyInspected) GUI.Label(new Rect(602, 352, 246, 20), "HP " + enemy.hp + "/" + enemy.maxHp + "  ATK " + enemy.attack, smallOnDarkStyle);
    }

    private void DrawPause()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(0, 0, 960, 540), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(244, 48, 472, 444));
        GUI.Label(new Rect(274, 94, 412, 44), "STORY MENU", titleStyle);
        GUI.Label(new Rect(286, 147, 388, 50), "Lv." + level + "　HP " + hp + "/" + maxHp + "　MP " + mp + "/" + maxMp +
            "\n歩数 " + stepsTaken + "　勝利 " + battlesWon + "　欠片 " + fragments + "/3", hintStyle);
        string badges = "記録：" + (stepsTaken >= 100 ? "旅人✓ " : "旅人 " + stepsTaken + "/100  ") +
            (battlesWon >= 5 ? "勇者✓" : "勇者 " + battlesWon + "/5");
        GUI.Label(new Rect(286, 188, 388, 24), badges, hintStyle);
        string resumeLabel = pauseReturnMode == GameMode.Battle ? "戦闘にもどる" : pauseReturnMode == GameMode.Dialogue || pauseReturnMode == GameMode.Intro ? "会話にもどる" : "物語にもどる";
        string saveLabel = pauseReturnMode == GameMode.Explore ? "セーブデータ" : "セーブ（探索中のみ）";
        string[] pauseItems = { resumeLabel, saveLabel, "会話ログ", "キー設定", "設定", "タイトルへ" };
        for (int i = 0; i < pauseItems.Length; i++)
        {
            Rect itemRect = new Rect(320, 218 + i * 35, 320, 31);
            DrawMenuItem(itemRect, pauseItems[i], pauseSelection == i);
            if (MouseActivated(itemRect)) { pauseSelection = i; ActivatePauseSelection(); }
        }
        GUI.Label(new Rect(286, 450, 388, 24), "Esc / X：すぐにもどる", smallStyle);
    }

    private void DrawSettings()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(180, 24, 600, 494));
        GUI.Label(new Rect(210, 42, 540, 48), "SETTINGS", titleStyle);
        string difficultyName = difficulty == 0 ? "ストーリー" : difficulty == 2 ? "チャレンジ" : "スタンダード";
        string speedName = textSpeed == 0 ? "ゆっくり" : textSpeed == 2 ? "はやい" : "ふつう";
        string[] items =
        {
            "BGM音量　" + Mathf.RoundToInt(musicVolume * 100f) + "%",
            "SE音量　" + Mathf.RoundToInt(sfxVolume * 100f) + "%",
            "難易度　" + difficultyName,
            "文字速度　" + speedName,
            "文字サイズ　" + Mathf.RoundToInt(uiTextScale * 100f) + "%",
            "高コントラスト　" + (highContrast ? "ON" : "OFF"),
            "演出を控えめに　" + (reducedMotion ? "ON" : "OFF"),
            "ゆったり探索　" + (gentleEncounters ? "ON" : "OFF"),
            "フルスクリーン　" + (fullscreen ? "ON" : "OFF"),
            "解像度　" + ResolutionLabel(),
            "キー設定を開く",
            "設定を初期化",
            "決定してもどる"
        };
        for (int i = 0; i < items.Length; i++)
        {
            Rect itemRect = new Rect(225, 96 + i * 27, 510, 26);
            DrawMenuItem(itemRect, (i < 10 ? "‹  " : "") + items[i] + (i < 10 ? "  ›" : ""), settingsSelection == i);
            if (MouseActivated(itemRect))
            {
                settingsSelection = i;
                if (i == 10) { controlsSelection = 0; rebindAction = -1; controlsReturnMode = GameMode.Settings; mode = GameMode.Controls; }
                else if (i == 11) confirmResetSettings = true;
                else if (i == 12) { SaveSettings(); mode = settingsReturnMode; }
                else ChangeSetting(1);
            }
        }
        GUI.Label(new Rect(240, 488, 480, 20), "← → で変更　　Esc / X でもどる", smallStyle);
        if (confirmResetSettings) DrawSettingsConfirmation("すべての設定とキー割り当てを\n初期状態に戻しますか？", "初期化する");
        else if (confirmDisplayChange)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, displayConfirmUntil - Time.unscaledTime));
            DrawSettingsConfirmation("この表示設定を維持しますか？\n" + seconds + " 秒後に元に戻ります。", "維持する");
        }
    }

    private void DrawSettingsConfirmation(string message, string confirmText)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(250, 178, 460, 184));
        GUI.Label(new Rect(280, 202, 400, 62), message, centerStyle);
        Rect yesRect = new Rect(300, 294, 160, 38);
        Rect noRect = new Rect(500, 294, 160, 38);
        DrawMenuItem(yesRect, confirmText + " [決定]", true);
        DrawMenuItem(noRect, "元に戻す [Esc]", false);
        if (MouseActivated(yesRect))
        {
            if (confirmResetSettings) { confirmResetSettings = false; ResetSettings(); Toast("設定を初期状態に戻しました。"); }
            else { confirmDisplayChange = false; SaveSettings(); Toast("表示設定を保存しました。"); }
        }
        if (MouseActivated(noRect))
        {
            if (confirmResetSettings) confirmResetSettings = false;
            else
            {
                fullscreen = previousFullscreen;
                resolutionIndex = previousResolutionIndex;
                ApplyDisplaySettings();
                confirmDisplayChange = false;
                Toast("表示設定を元に戻しました。");
            }
        }
    }

    private void DrawSaveSlots()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(180, 44, 600, 452));
        string title = saveSlotPurpose == SaveSlotPurpose.Save ? "SAVE YOUR STORY" : "SAVE DATA";
        string description = saveSlotPurpose == SaveSlotPurpose.Save ? "保存先のしおりを選んでください" : "スロットを選び、下の操作を選んでください";
        GUI.Label(new Rect(210, 64, 540, 48), title, titleStyle);
        GUI.Label(new Rect(235, 116, 490, 24), description, hintStyle);
        for (int i = 0; i < 3; i++)
        {
            SaveData preview = ReadSave(i, false);
            bool backupAvailable = HasBackupInSlot(i);
            Rect slotRect = new Rect(230, 150 + i * 70, 500, 58);
            DrawMenuItem(slotRect, SlotSummary(i, preview, backupAvailable), saveSlotSelection == i);
            if (!confirmSlotOverwrite && !confirmBackupRestore && !confirmSlotDeletion && MouseActivated(slotRect))
            {
                saveSlotSelection = i;
                if (saveSlotPurpose == SaveSlotPurpose.Save) ActivateSaveSlotSelection();
            }
        }
        if (saveSlotPurpose == SaveSlotPurpose.Manage)
        {
            string[] actions = { "よみこむ", "バックアップ復元", "削除" };
            for (int i = 0; i < actions.Length; i++)
            {
                Rect actionRect = new Rect(230 + i * 167, 366, 157, 34);
                DrawMenuItem(actionRect, actions[i], saveSlotAction == i);
                if (MouseActivated(actionRect))
                {
                    saveSlotAction = i;
                    ActivateSaveSlotSelection();
                }
            }
            GUI.Label(new Rect(235, 408, 490, 22), "← →：操作を選ぶ　　各スロットは直前データを自動バックアップ", smallStyle);
        }
        else GUI.Label(new Rect(235, 390, 490, 22), "各スロットは直前データを自動バックアップします。", smallStyle);

        Rect backRect = new Rect(390, 442, 180, 30);
        DrawMenuItem(backRect, "もどる", false);
        if (MouseActivated(backRect)) { mode = saveSlotReturnMode; Play(confirmSound); }

        if (confirmSlotOverwrite)
            DrawSaveSlotConfirmation("このスロットを上書きしますか？\n直前の内容はバックアップされます。", "上書きする", delegate { confirmSlotOverwrite = false; CommitSaveSlot(saveSlotSelection); });
        else if (confirmBackupRestore)
            DrawSaveSlotConfirmation("バックアップを通常データへ復元します。\n現在の破損データは置き換えられます。", "復元する", delegate { confirmBackupRestore = false; RestoreBackupAndLoad(saveSlotSelection); });
        else if (confirmSlotDeletion)
            DrawSaveSlotConfirmation("このスロットとバックアップを削除しますか？\nこの操作は元に戻せません。", "削除する", delegate { confirmSlotDeletion = false; DeleteSaveSlot(saveSlotSelection); });
    }

    private void DrawSaveSlotConfirmation(string message, string confirmText, Action confirmed)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(250, 178, 460, 184));
        GUI.Label(new Rect(280, 202, 400, 62), message, centerStyle);
        Rect yesRect = new Rect(300, 294, 160, 38);
        Rect noRect = new Rect(500, 294, 160, 38);
        DrawMenuItem(yesRect, confirmText + " [決定]", true);
        DrawMenuItem(noRect, "いいえ [Esc]", false);
        if (MouseActivated(yesRect)) confirmed();
        if (MouseActivated(noRect)) { confirmSlotOverwrite = false; confirmBackupRestore = false; confirmSlotDeletion = false; }
    }

    private string SlotSummary(int slot, SaveData data, bool backupAvailable)
    {
        if (data == null)
            return backupAvailable ? "スロット " + (slot + 1) + "　通常データを読み込めません\nバックアップを復元できます" : "スロット " + (slot + 1) + "　　--- 空のしおり ---";
        string time = string.IsNullOrEmpty(data.savedAt) ? "日時不明" : data.savedAt;
        string progress = string.IsNullOrEmpty(data.chapterName) ? "物語の途中" : data.chapterName;
        return "スロット " + (slot + 1) + "　Lv." + Mathf.Max(1, data.level) + "　" + progress + "\n" + time + "　" + FormatSeconds(data.playSeconds) + (backupAvailable ? "　◆バックアップあり" : "");
    }

    private void DrawControls()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(200, 34, 560, 472));
        GUI.Label(new Rect(230, 54, 500, 48), "CONTROLS", titleStyle);
        GUI.Label(new Rect(240, 108, 480, 22), "項目を選んで決定し、新しいキーを押してください。", hintStyle);
        for (int i = 0; i < 9; i++)
        {
            string value = i < 8 ? BindingAt(i).ToString() : "設定へもどる";
            Rect itemRect = new Rect(255, 140 + i * 34, 450, 28);
            DrawMenuItem(itemRect, ControlName(i) + "　　" + value, controlsSelection == i);
            if (rebindAction < 0 && MouseActivated(itemRect))
            {
                controlsSelection = i;
                if (i == 8) { SaveSettings(); mode = controlsReturnMode; }
                else { rebindAction = i; rebindStartedAt = Time.unscaledTime; Toast("新しいキーを押してください。Escでキャンセル"); }
            }
        }
        GUI.Label(new Rect(240, 448, 480, 44), "ゲームパッド：左スティック/D-Padで移動　A：決定　B：もどる\nX：ログ　Y：クエスト", smallStyle);
        if (rebindAction < 0) return;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(250, 192, 460, 142));
        GUI.Label(new Rect(280, 214, 400, 65), "「" + ControlName(rebindAction) + "」に\n割り当てるキーを押してください", centerStyle);
        GUI.Label(new Rect(280, 292, 400, 22), "Esc：キャンセル", smallStyle);
        Event current = Event.current;
        if (current != null && current.type == EventType.KeyDown && Time.unscaledTime - rebindStartedAt > 0.08f)
        {
            KeyCode key = current.keyCode;
            current.Use();
            TrySetBinding(rebindAction, key);
        }
    }

    private void DrawDialogueLog()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.67f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(110, 58, 740, 420));
        GUI.Label(new Rect(140, 78, 680, 42), "DIALOGUE LOG", titleStyle);
        if (dialogueHistory.Count == 0) GUI.Label(new Rect(150, 205, 660, 40), "まだ記録された会話はありません。", centerStyle);
        else
        {
            int start = Mathf.Clamp(dialogueLogScroll, 0, Mathf.Max(0, dialogueHistory.Count - 1));
            int shown = Mathf.Min(6, dialogueHistory.Count - start);
            for (int i = 0; i < shown; i++)
            {
                GUI.Label(new Rect(150, 135 + i * 48, 660, 42), dialogueHistory[start + i], questStyle);
            }
        }
        GUI.Label(new Rect(145, 440, 430, 22), "↑ ↓ / ホイール：スクロール", smallStyle);
        Rect backRect = new Rect(610, 432, 190, 30);
        DrawMenuItem(backRect, "もどる", false);
        if (MouseActivated(backRect)) { mode = dialogueLogReturnMode; Play(confirmSound); }
        Event current = Event.current;
        if (current != null && current.type == EventType.ScrollWheel)
        {
            dialogueLogScroll = Mathf.Clamp(dialogueLogScroll + (current.delta.y > 0f ? 1 : -1), 0, Mathf.Max(0, dialogueHistory.Count - 6));
            current.Use();
        }
    }

    private void DrawRecords()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(170, 54, 620, 432));
        GUI.Label(new Rect(200, 74, 560, 48), "STORY RECORDS", titleStyle);
        for (int i = 0; i < 3; i++)
        {
            SaveData data = ReadSave(i, false);
            string record = data == null ? "---" : "Lv." + data.level + "　" + (data.cleared ? "CLEAR" : data.chapterName) + "\n" +
                "時間 " + FormatSeconds(data.playSeconds) + "　歩数 " + data.steps + "　勝利 " + data.battlesWon + "　宝箱 " + CountOpenedChests(data.openedChests) + "/" + chestPositions.Count;
            DrawPanel(new Rect(225, 140 + i * 85, 510, 70));
            GUI.Label(new Rect(240, 150 + i * 85, 480, 54), "スロット " + (i + 1) + "　" + record, questStyle);
        }
        Rect backRect = new Rect(390, 432, 180, 30);
        DrawMenuItem(backRect, recordsReturnMode == GameMode.Ending ? "エンディングへ" : "タイトルへ", false);
        if (MouseActivated(backRect)) { mode = recordsReturnMode; Play(confirmSound); }
    }

    private void DrawCredits()
    {
        GUI.color = new Color32(24, 21, 43, 255);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(160, 48, 640, 444));
        GUI.Label(new Rect(190, 68, 580, 48), "CREDITS & SUPPORT", titleStyle);
        GUI.Label(new Rect(205, 130, 550, 190),
            "Alice & The Broken Crown　Version " + AliceRpgBuildInfo.Version + "\n\n" +
            "Design, code, pixel art, and sound: Wonderland Workshop\n" +
            "Inspired by the public-domain Alice in Wonderland tradition.\n\n" +
            "このゲームは通信・分析・クラッシュ報告を行いません。\n" +
            "セーブと設定は、このPCのローカルなUnity PlayerPrefsに保存されます。\n" +
            "詳しいクレジット、プライバシー、既知の制限は同梱のDocumentationフォルダーをご覧ください。",
            bodyStyle);
        GUI.Label(new Rect(205, 338, 550, 50), "困ったときは、ゲームのバージョン、操作機器、再現手順と Player.log を添えて配布元へ連絡してください。", bodyStyle);
        Rect backRect = new Rect(390, 430, 180, 32);
        DrawMenuItem(backRect, "タイトルへ", false);
        if (MouseActivated(backRect)) { mode = GameMode.Title; Play(confirmSound); }
    }

    private void DrawQuest()
    {
        DrawPanel(new Rect(540, 80, 380, 220));
        GUI.Label(new Rect(565, 100, 330, 34), "ものがたりの栞", speakerStyle);
        GUI.Label(new Rect(570, 148, 320, 110), QuestText(), questStyle);
        GUI.Label(new Rect(570, 264, 150, 20), "[" + keyQuest + " / " + keyCancel + "] とじる", smallStyle);
        Rect closeRect = new Rect(735, 252, 160, 30);
        DrawMenuItem(closeRect, "とじる", false);
        if (MouseActivated(closeRect)) showQuest = false;
    }

    private void DrawObjectiveGuide()
    {
        if (chapter >= 4) return;
        Vector2Int target = ObjectiveTarget();
        Vector2Int delta = target - playerPosition;
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        string arrow;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) arrow = delta.x > 0 ? "→" : "←";
        else arrow = delta.y > 0 ? "↓" : "↑";

        GUI.color = new Color(gold.r, gold.g, gold.b, reducedMotion ? 0.62f : 0.72f + Mathf.Sin(Time.unscaledTime * 4f) * 0.16f);
        GUI.DrawTexture(new Rect(target.x * Tile + 9, target.y * Tile - 8, 14, 14), textures["white"]);
        GUI.color = Color.white;
        DrawPanel(new Rect(14, 12, 198, 42));
        GUI.Label(new Rect(23, 20, 180, 26), "目的 " + arrow + "  あと" + distance + "歩", hintStyle);
    }

    private Vector2Int ObjectiveTarget()
    {
        if (chapter == 0) return new Vector2Int(6, 12);
        if (chapter == 1) return new Vector2Int(9, 4);
        if (chapter == 2) return new Vector2Int(20, 11);
        return new Vector2Int(26, 3);
    }

    private string QuestText()
    {
        if (chapter == 0) return "白ウサギを探そう。\n南西の草原にいるらしい。";
        if (chapter == 1) return "青いイモムシを訪ねよう。\n川より西、北の森にいる。";
        if (chapter == 2) return "チェシャ猫を訪ねよう。\n橋を渡った東の森にいる。";
        if (chapter == 3) return "欠片は3つ揃った。\n北東のハートの城へ！";
        return "ワンダーランドに明日が戻った。";
    }

    private string PlayTimeText()
    {
        return FormatSeconds(previousPlaySeconds + Mathf.RoundToInt(Time.unscaledTime - sessionStartedAt));
    }

    private string FormatSeconds(int total)
    {
        total = Mathf.Max(0, total);
        int hours = total / 3600;
        int minutes = (total / 60) % 60;
        int seconds = total % 60;
        return hours > 0 ? hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00") : minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private int CountOpenedChests(int mask)
    {
        int count = 0;
        for (int i = 0; i < chestPositions.Count; i++) if ((mask & (1 << i)) != 0) count++;
        return count;
    }

    private string ResolutionLabel()
    {
        string[] labels = { "960 × 540", "1280 × 720", "1920 × 1080" };
        return labels[Mathf.Clamp(resolutionIndex, 0, labels.Length - 1)];
    }

    private string ConfirmHint()
    {
        return keyConfirm + "/A";
    }

    private void DrawEnding()
    {
        GUI.color = new Color32(248, 229, 198, 255);
        GUI.DrawTexture(new Rect(0, 0, 960, 540), textures["white"]);
        GUI.color = ink;
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            float x = 480 + Mathf.Cos(angle) * (150 + i % 3 * 22);
            float y = 232 + Mathf.Sin(angle) * (110 + i % 2 * 18);
            GUI.DrawTexture(new Rect(x, y, 10, 10), textures["white"]);
        }
        GUI.color = Color.white;
        DrawTexturePixel(textures["alice"], new Rect(414, 165, 132, 132));
        GUI.Label(new Rect(100, 62, 760, 64), "THE END", titleStyle);
        GUI.Label(new Rect(140, 312, 680, 60), "明日は、選んだ一歩の先にある。", centerStyle);
        GUI.Label(new Rect(140, 384, 680, 28), "クリアレベル  " + level + "　　集めた時の欠片  " + fragments + "/3", centerStyle);
        GUI.Label(new Rect(140, 419, 680, 26), "歩数 " + stepsTaken + "　勝利 " + battlesWon + "　プレイ時間 " + PlayTimeText(), hintStyle);
        string[] endingItems = { "NEW GAME+　（Lv.2から再び物語へ）", "クリア記録を見る", "タイトルへ" };
        for (int i = 0; i < endingItems.Length; i++)
        {
            Rect itemRect = new Rect(280, 448 + i * 27, 400, 24);
            DrawMenuItem(itemRect, endingItems[i], endingSelection == i);
            if (MouseActivated(itemRect))
            {
                endingSelection = i;
                if (i == 0) StartNewGame(true); else if (i == 1) { recordsReturnMode = GameMode.Ending; mode = GameMode.Records; } else { mode = GameMode.Title; titleSelection = 0; }
            }
        }
    }

    private void DrawGameOver()
    {
        GUI.color = new Color32(19, 17, 29, 255);
        GUI.DrawTexture(new Rect(0, 0, 960, 540), textures["white"]);
        GUI.color = Color.white;
        GUI.Label(new Rect(100, 160, 760, 70), "THE STORY SLEEPS", titleOnDarkStyle);
        GUI.Label(new Rect(120, 255, 720, 60), "物語は閉じてしまった。\nけれど、ページはいつでも開き直せる。", centerOnDarkStyle);
        string[] choices = { hasSave ? "直前のしおりから再開" : "再開データなし", "タイトルへ" };
        for (int i = 0; i < choices.Length; i++)
        {
            Rect itemRect = new Rect(330, 360 + i * 48, 300, 38);
            DrawMenuItem(itemRect, choices[i], gameOverSelection == i);
            if (MouseActivated(itemRect))
            {
                gameOverSelection = i;
                if (i == 0 && hasSave) RequestLoadSlot(activeSaveSlot, GameMode.GameOver); else { mode = GameMode.Title; titleSelection = hasSave ? 0 : 1; }
            }
        }
    }

    private void DrawPanel(Rect rect)
    {
        GUI.color = highContrast ? Color.white : surface;
        GUI.DrawTexture(rect, textures["white"]);
        GUI.color = ink;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 4), textures["white"]);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 4, rect.width, 4), textures["white"]);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), textures["white"]);
        GUI.DrawTexture(new Rect(rect.xMax - 4, rect.y, 4, rect.height), textures["white"]);
        GUI.color = gold;
        GUI.DrawTexture(new Rect(rect.x + 7, rect.y + 7, rect.width - 14, 2), textures["white"]);
        GUI.color = Color.white;
    }

    private void DrawMenuItem(Rect rect, string text, bool selected)
    {
        if (selected)
        {
            GUI.color = rose;
            GUI.DrawTexture(rect, textures["white"]);
            GUI.color = Color.white;
            GUI.Label(rect, "◆ " + text, selectedStyle);
        }
        else GUI.Label(rect, "   " + text, menuStyle);
    }

    private bool MouseActivated(Rect rect)
    {
        Event current = Event.current;
        if (current == null || current.type != EventType.MouseDown || current.button != 0) return false;
        Vector2 logicalMouse = (current.mousePosition - guiOffset) / guiScale;
        if (!rect.Contains(logicalMouse)) return false;
        current.Use();
        Play(confirmSound);
        return true;
    }

    private void DrawBar(Rect rect, int value, int maximum, Color fill)
    {
        GUI.color = ink;
        GUI.DrawTexture(rect, textures["white"]);
        GUI.color = fill;
        float width = (rect.width - 6) * Mathf.Clamp01(value / (float)maximum);
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, width, rect.height - 6), textures["white"]);
        GUI.color = Color.white;
    }

    private void DrawWorldLabel(Rect rect, string text)
    {
        GUI.color = new Color(ink.r, ink.g, ink.b, 0.78f);
        GUI.DrawTexture(rect, textures["white"]);
        GUI.color = Color.white;
        GUI.Label(rect, text, smallOnDarkStyle);
    }

    private void DrawTexturePixel(Texture2D texture, Rect rect)
    {
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
    }

    private void InitStyles()
    {
        if (titleStyle != null) return;
        GUI.skin.font = gameFont;
        titleStyle = NewStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        titleOnDarkStyle = NewStyle(42, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
        labelStyle = NewStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        smallStyle = NewStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, ink);
        smallOnDarkStyle = NewStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, cream);
        centerStyle = NewStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        centerOnDarkStyle = NewStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
        menuStyle = NewStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, ink);
        selectedStyle = NewStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, cream);
        dialogueStyle = NewStyle(19, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        dialogueStyle.wordWrap = true;
        speakerStyle = NewStyle(18, FontStyle.Bold, TextAnchor.MiddleLeft, ink);
        questStyle = NewStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        questStyle.wordWrap = true;
        bodyStyle = NewStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, ink);
        bodyStyle.wordWrap = true;
        hintStyle = NewStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
    }

    private GUIStyle NewStyle(int size, FontStyle fontStyle, TextAnchor anchor, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.font = gameFont;
        style.fontSize = Mathf.RoundToInt(size * uiTextScale);
        style.fontStyle = fontStyle;
        style.alignment = anchor;
        style.normal.textColor = color;
        style.padding = new RectOffset(4, 4, 2, 2);
        return style;
    }

    private void CreateTextures()
    {
        textures["white"] = Solid(Color.white);
        textures["ground"] = TileTexture(new Color32(180, 203, 143, 255), new Color32(169, 192, 132, 255), false);
        textures["grass"] = TileTexture(new Color32(84, 151, 98, 255), new Color32(69, 132, 87, 255), true);
        textures["water"] = WaterTexture();
        textures["bridge"] = BridgeTexture();
        textures["wall"] = WallTexture();
        textures["shrub"] = ShrubTexture();
        textures["door"] = DoorTexture();
        textures["chest"] = ChestTexture();
        textures["flower"] = FlowerTexture();
        textures["alice"] = CharacterTexture(new Color32(80, 161, 215, 255), new Color32(246, 236, 206, 255), new Color32(236, 190, 83, 255));
        textures["rabbit"] = RabbitTexture();
        textures["hatter"] = CharacterTexture(new Color32(111, 71, 133, 255), new Color32(230, 119, 67, 255), new Color32(58, 42, 68, 255));
        textures["caterpillar"] = CaterpillarTexture();
        textures["cat"] = CatTexture();
        textures["queen"] = CharacterTexture(new Color32(180, 42, 66, 255), new Color32(47, 37, 51, 255), new Color32(246, 195, 68, 255));
        textures["mushroom"] = MushroomTexture();
        textures["card"] = CardTexture();
        textures["shadow"] = ShadowTexture();
        textures["queenBattle"] = QueenBattleTexture();
    }

    private Texture2D Solid(Color color)
    {
        Texture2D texture = NewTexture(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D TileTexture(Color a, Color b, bool blades)
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, a);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                if ((x * 5 + y * 7) % 23 == 0) t.SetPixel(x, y, b);
        if (blades)
        {
            for (int x = 2; x < 16; x += 5)
            {
                t.SetPixel(x, 3 + x % 7, b);
                t.SetPixel(x + 1, 4 + x % 7, b);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D WaterTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(56, 132, 181, 255));
        Color foam = new Color32(104, 190, 210, 255);
        for (int y = 3; y < 16; y += 6)
            for (int x = (y / 3) % 2; x < 14; x += 6)
                for (int i = 0; i < 3; i++) t.SetPixel(x + i, y, foam);
        t.Apply();
        return t;
    }

    private Texture2D BridgeTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(139, 91, 57, 255));
        Color line = new Color32(83, 56, 48, 255);
        for (int y = 0; y < 16; y += 4)
            for (int x = 0; x < 16; x++) t.SetPixel(x, y, line);
        t.Apply();
        return t;
    }

    private Texture2D WallTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(54, 77, 66, 255));
        Color leaf = new Color32(37, 103, 65, 255);
        for (int y = 2; y < 15; y += 5)
            for (int x = (y % 3); x < 15; x += 5) Block(t, x, y, 3, 3, leaf);
        t.Apply();
        return t;
    }

    private Texture2D ShrubTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(180, 203, 143, 255));
        Color dark = new Color32(36, 96, 62, 255);
        Color light = new Color32(65, 132, 71, 255);
        Block(t, 2, 5, 12, 8, dark);
        Block(t, 4, 3, 5, 8, light);
        Block(t, 9, 4, 4, 7, light);
        t.Apply();
        return t;
    }

    private Texture2D DoorTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(69, 61, 78, 255));
        Block(t, 3, 1, 10, 15, new Color32(142, 42, 65, 255));
        Block(t, 7, 1, 2, 15, new Color32(246, 195, 68, 255));
        t.Apply();
        return t;
    }

    private Texture2D ChestTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(180, 203, 143, 255));
        Color wood = new Color32(124, 75, 47, 255);
        Color edge = new Color32(73, 47, 43, 255);
        Block(t, 2, 3, 12, 9, wood);
        Block(t, 2, 11, 12, 2, edge);
        Block(t, 2, 8, 12, 1, gold);
        Block(t, 7, 7, 2, 4, cream);
        t.Apply();
        return t;
    }

    private Texture2D FlowerTexture()
    {
        Texture2D t = NewTexture(5, 5);
        Fill(t, Color.clear);
        Color petal = new Color32(250, 210, 229, 255);
        t.SetPixel(2, 0, petal); t.SetPixel(0, 2, petal); t.SetPixel(4, 2, petal); t.SetPixel(2, 4, petal);
        t.SetPixel(2, 2, gold);
        t.Apply();
        return t;
    }

    private Texture2D CharacterTexture(Color clothes, Color hair, Color accent)
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color skin = new Color32(250, 214, 183, 255);
        Block(t, 5, 11, 6, 5, hair);
        Block(t, 6, 10, 4, 5, skin);
        Block(t, 5, 5, 6, 6, clothes);
        Block(t, 3, 2, 10, 4, clothes);
        Block(t, 5, 0, 2, 3, ink);
        Block(t, 9, 0, 2, 3, ink);
        Block(t, 7, 6, 2, 5, accent);
        t.SetPixel(7, 12, ink); t.SetPixel(9, 12, ink);
        t.Apply();
        return t;
    }

    private Texture2D RabbitTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color white = new Color32(242, 238, 222, 255);
        Color pink = new Color32(228, 126, 148, 255);
        Block(t, 4, 12, 3, 6, white); Block(t, 9, 12, 3, 6, white);
        Block(t, 5, 8, 6, 7, white); Block(t, 4, 3, 8, 7, white);
        Block(t, 5, 5, 6, 5, new Color32(62, 113, 166, 255));
        t.SetPixel(6, 12, pink); t.SetPixel(9, 12, pink);
        t.SetPixel(7, 10, ink); t.SetPixel(10, 10, ink);
        t.Apply(); return t;
    }

    private Texture2D CaterpillarTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color body = new Color32(58, 151, 170, 255);
        for (int i = 0; i < 4; i++) Block(t, 3 + i * 3, 3 + (i % 2), 5, 5, body);
        Block(t, 5, 9, 7, 7, body);
        Block(t, 4, 15, 9, 2, new Color32(89, 58, 119, 255));
        t.SetPixel(7, 12, ink); t.SetPixel(10, 12, ink);
        t.Apply(); return t;
    }

    private Texture2D CatTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color purple = new Color32(133, 83, 163, 255);
        Color stripe = new Color32(226, 97, 153, 255);
        Block(t, 4, 9, 8, 7, purple);
        Block(t, 3, 13, 3, 4, purple); Block(t, 10, 13, 3, 4, purple);
        Block(t, 5, 4, 6, 7, purple); Block(t, 3, 1, 10, 5, purple);
        Block(t, 5, 6, 6, 2, stripe);
        Block(t, 6, 10, 5, 1, cream);
        t.SetPixel(6, 13, gold); t.SetPixel(10, 13, gold);
        t.Apply(); return t;
    }

    private Texture2D MushroomTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color cap = new Color32(190, 54, 87, 255);
        Block(t, 5, 16, 22, 9, cap); Block(t, 9, 23, 14, 5, cap);
        Block(t, 12, 5, 8, 13, new Color32(236, 218, 180, 255));
        Block(t, 8, 18, 4, 4, cream); Block(t, 20, 20, 4, 4, cream);
        t.SetPixel(14, 12, ink); t.SetPixel(18, 12, ink);
        t.Apply(); return t;
    }

    private Texture2D CardTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Block(t, 8, 4, 16, 24, cream);
        Block(t, 9, 5, 14, 22, new Color32(245, 240, 222, 255));
        Block(t, 5, 8, 4, 3, ink); Block(t, 23, 8, 4, 3, ink);
        Block(t, 10, 0, 3, 6, ink); Block(t, 20, 0, 3, 6, ink);
        Block(t, 14, 13, 5, 5, rose);
        t.Apply(); return t;
    }

    private Texture2D ShadowTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color shadow = new Color32(38, 27, 56, 255);
        Block(t, 4, 5, 24, 18, shadow); Block(t, 7, 21, 18, 7, shadow);
        Block(t, 9, 12, 14, 4, cream);
        Block(t, 11, 13, 10, 2, ink);
        t.SetPixel(10, 19, gold); t.SetPixel(21, 19, gold);
        t.Apply(); return t;
    }

    private Texture2D QueenBattleTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color red = new Color32(180, 42, 66, 255);
        Block(t, 7, 2, 18, 16, red); Block(t, 3, 0, 26, 8, red);
        Block(t, 10, 17, 12, 10, new Color32(250, 214, 183, 255));
        Block(t, 9, 26, 3, 6, gold); Block(t, 15, 28, 3, 4, gold); Block(t, 21, 26, 3, 6, gold);
        Block(t, 12, 20, 3, 2, ink); Block(t, 19, 20, 3, 2, ink);
        Block(t, 14, 17, 6, 3, ink);
        t.Apply(); return t;
    }

    private Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private void Fill(Texture2D texture, Color color)
    {
        Color[] colors = new Color[texture.width * texture.height];
        for (int i = 0; i < colors.Length; i++) colors[i] = color;
        texture.SetPixels(colors);
    }

    private void Block(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(texture.height, y + height); py++)
            for (int px = Mathf.Max(0, x); px < Mathf.Min(texture.width, x + width); px++)
                texture.SetPixel(px, py, color);
    }
}
