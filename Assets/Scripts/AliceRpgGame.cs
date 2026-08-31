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

public sealed partial class AliceRpgGame : MonoBehaviour
{
    private const int LogicalWidth = 960;
    private const int LogicalHeight = 540;
    private const int Tile = 32;
    private const int MapWidth = 30;
    private const int MapHeight = 16;
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
    private bool confirmNewGame;
    private bool confirmQuit;
    private int pauseSelection;
    private int settingsSelection;
    private int gameOverSelection;
    private int endingSelection;
    private int dialogueLogScroll;
    private GameMode dialogueLogReturnMode = GameMode.Explore;
    private int controlsSelection;
    private int rebindAction = -1;
    private float rebindStartedAt;
    private GameMode controlsReturnMode = GameMode.Title;
    private GameMode settingsReturnMode = GameMode.Title;
    private GameMode pauseReturnMode = GameMode.Explore;
    private GameMode recordsReturnMode = GameMode.Title;
    private float dialogueStartedAt;
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
    private float guiScale = 1f;
    private Vector2 guiOffset;

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
        if (vertical != 0) { controlsSelection = (controlsSelection + vertical + ControlMenuItemCount) % ControlMenuItemCount; Play(moveSound); }
        if (!PressConfirm()) return;
        if (controlsSelection == ControlBindingCount) { SaveSettings(); mode = controlsReturnMode; }
        else
        {
            rebindAction = controlsSelection;
            rebindStartedAt = Time.unscaledTime;
            Toast("新しいキーを押してください。Escでキャンセル");
        }
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

}
