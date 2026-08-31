using System;
using UnityEngine;

public sealed partial class AliceRpgGame
{
    private bool hasSave;
    private int saveSlotSelection;
    private int activeSaveSlot;
    private SaveSlotPurpose saveSlotPurpose;
    private GameMode saveSlotReturnMode = GameMode.Title;
    private SaveSlotConfirmation saveSlotConfirmation;
    private int saveSlotAction;
    private float lastAutoSaveAt;
    private int previousPlaySeconds;
    private float saveIndicatorUntil;
    private string lastSaveNotice = "";

    private void OpenSaveSlots(SaveSlotPurpose purpose, GameMode returnMode)
    {
        saveSlotPurpose = purpose;
        saveSlotReturnMode = returnMode;
        saveSlotSelection = activeSaveSlot;
        saveSlotConfirmation = SaveSlotConfirmation.None;
        saveSlotAction = 0;
        mode = GameMode.SaveSlots;
    }

    private void UpdateSaveSlots()
    {
        if (HandleSaveSlotConfirmation()) return;
        if (PressCancel()) { mode = saveSlotReturnMode; Play(confirmSound); return; }
        int vertical = VerticalInput();
        if (vertical != 0) { saveSlotSelection = (saveSlotSelection + vertical + SaveSlotCount) % SaveSlotCount; Play(moveSound); }
        if (saveSlotPurpose == SaveSlotPurpose.Manage)
        {
            int horizontal = HorizontalInput();
            if (horizontal != 0) { saveSlotAction = (saveSlotAction + horizontal + SaveSlotActionCount) % SaveSlotActionCount; Play(moveSound); }
        }
        if (!PressConfirm()) return;
        ActivateSaveSlotSelection();
    }

    private bool HandleSaveSlotConfirmation()
    {
        if (saveSlotConfirmation == SaveSlotConfirmation.None) return false;
        if (PressCancel())
        {
            saveSlotConfirmation = SaveSlotConfirmation.None;
            Play(confirmSound);
        }
        else if (PressConfirm())
        {
            SaveSlotConfirmation confirmation = saveSlotConfirmation;
            saveSlotConfirmation = SaveSlotConfirmation.None;
            if (confirmation == SaveSlotConfirmation.Overwrite) CommitSaveSlot(saveSlotSelection);
            else if (confirmation == SaveSlotConfirmation.RestoreBackup) RestoreBackupAndLoad(saveSlotSelection);
            else if (confirmation == SaveSlotConfirmation.Delete) DeleteSaveSlot(saveSlotSelection);
        }
        return true;
    }

    private void ActivateSaveSlotSelection()
    {
        if (saveSlotPurpose == SaveSlotPurpose.Save)
        {
            if (HasSaveInSlot(saveSlotSelection)) saveSlotConfirmation = SaveSlotConfirmation.Overwrite;
            else CommitSaveSlot(saveSlotSelection);
            return;
        }
        if (saveSlotPurpose == SaveSlotPurpose.Manage)
        {
            if (saveSlotAction == 0) RequestLoadSlot(saveSlotSelection, saveSlotReturnMode);
            else if (saveSlotAction == 1)
            {
                if (!HasBackupInSlot(saveSlotSelection)) Toast("このスロットに復元できるバックアップはありません。");
                else saveSlotConfirmation = SaveSlotConfirmation.RestoreBackup;
            }
            else
            {
                if (!HasRecoverableSaveInSlot(saveSlotSelection)) Toast("このスロットは空です。");
                else saveSlotConfirmation = SaveSlotConfirmation.Delete;
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
            saveSlotConfirmation = SaveSlotConfirmation.RestoreBackup;
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
        PlayerPrefs.DeleteKey("AliceRpg.Deaths." + Mathf.Clamp(slot, 0, SaveSlotCount - 1));
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
        SaveData data = ReadSave(activeSaveSlot);
        if (data == null)
        {
            if (HasBackupInSlot(activeSaveSlot))
            {
                saveSlotPurpose = SaveSlotPurpose.Manage;
                saveSlotReturnMode = mode;
                saveSlotSelection = activeSaveSlot;
                saveSlotConfirmation = SaveSlotConfirmation.RestoreBackup;
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

    private string SaveKey(int slot) { return "AliceRpg.Save.v5." + Mathf.Clamp(slot, 0, SaveSlotCount - 1); }

    private bool HasSaveInSlot(int slot) { return ReadSave(slot) != null; }

    private bool HasBackupInSlot(int slot) { return ReadBackup(slot) != null; }

    private bool HasRecoverableSaveInSlot(int slot) { return HasSaveInSlot(slot) || HasBackupInSlot(slot); }

    private int FirstRecoverableSaveSlot()
    {
        for (int slot = 0; slot < SaveSlotCount; slot++)
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

    private SaveData ReadSave(int slot)
    {
        string key = SaveKey(slot);
        return DeserializeSave(PlayerPrefs.GetString(key, ""));
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
        for (int slot = 0; slot < SaveSlotCount; slot++)
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
}
