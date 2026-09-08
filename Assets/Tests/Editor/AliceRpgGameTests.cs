using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AliceRpgGameTests
{
    private GameObject host;
    private AliceRpgGame game;
    private readonly Dictionary<string, string> savedStringPrefs = new Dictionary<string, string>();
    private readonly Dictionary<string, int> savedIntPrefs = new Dictionary<string, int>();
    private readonly Dictionary<string, float> savedFloatPrefs = new Dictionary<string, float>();
    private static readonly string[] IntegerPreferenceKeys =
    {
        "AliceRpg.ActiveSlot", "AliceRpg.SaveMigratedV4", "AliceRpg.SaveMigratedV5",
        "AliceRpg.Difficulty", "AliceRpg.TextSpeed", "AliceRpg.HighContrast", "AliceRpg.ReducedMotion",
        "AliceRpg.GentleEncounters", "AliceRpg.Fullscreen", "AliceRpg.Resolution",
        "AliceRpg.Key.Up", "AliceRpg.Key.Down", "AliceRpg.Key.Left", "AliceRpg.Key.Right",
        "AliceRpg.Key.Confirm", "AliceRpg.Key.Cancel", "AliceRpg.Key.Quest", "AliceRpg.Key.Log"
    };
    private static readonly string[] FloatPreferenceKeys =
    {
        "AliceRpg.MusicVolume", "AliceRpg.SfxVolume", "AliceRpg.Volume", "AliceRpg.UiTextScale"
    };

    [SetUp]
    public void SetUp()
    {
        CaptureAliceRpgData();
        ClearAliceRpgData();
        host = new GameObject("AliceRpgGameTests");
        game = host.AddComponent<AliceRpgGame>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Texture2D texture in ((Dictionary<string, Texture2D>)Field("textures").GetValue(game)).Values)
            Object.DestroyImmediate(texture);
        Object.DestroyImmediate(host);
        ClearAliceRpgData();
        RestoreAliceRpgData();
    }

    [Test]
    public void ChapterCheckpoints_AreWalkableAndInBounds()
    {
        MethodInfo checkpoint = typeof(AliceRpgGame).GetMethod("ChapterCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo chapter = typeof(AliceRpgGame).GetField("chapter", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo canWalk = typeof(AliceRpgGame).GetMethod("CanWalk", BindingFlags.Instance | BindingFlags.NonPublic);

        for (int value = 0; value <= 3; value++)
        {
            chapter.SetValue(game, value);
            Vector2Int point = (Vector2Int)checkpoint.Invoke(game, null);
            Assert.That((bool)canWalk.Invoke(game, new object[] { point }), Is.True, "chapter " + value);
        }
    }

    [Test]
    public void SaveData_UsesCurrentVersion()
    {
        System.Type saveData = typeof(AliceRpgGame).GetNestedType("SaveData", BindingFlags.NonPublic);
        object value = System.Activator.CreateInstance(saveData);
        FieldInfo version = saveData.GetField("version");
        Assert.That((int)version.GetValue(value), Is.EqualTo(5));
    }

    [Test]
    public void CorruptPrimarySave_StillExposesBackupAndRestoresIt()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo keyForSlot = Method("SaveKey");
        MethodInfo hasRecoverable = Method("HasRecoverableSaveInSlot");
        MethodInfo restore = Method("ConfirmSaveSlotSelection");
        string key = (string)keyForSlot.Invoke(game, new object[] { 0 });

        save.Invoke(game, new object[] { false });
        save.Invoke(game, new object[] { false });
        PlayerPrefs.SetString(key, "{not valid save data");
        PlayerPrefs.Save();

        Assert.That((bool)hasRecoverable.Invoke(game, new object[] { 0 }), Is.True);
        Field("saveSlotSelection").SetValue(game, 0);
        FieldInfo confirmation = Field("saveSlotConfirmation");
        confirmation.SetValue(game, System.Enum.Parse(confirmation.FieldType, "RestoreBackup"));
        restore.Invoke(game, null);
        Assert.That(confirmation.GetValue(game).ToString(), Is.EqualTo("None"));
        Assert.That(PlayerPrefs.GetString(key), Does.Not.Contain("not valid"));
    }

    [Test]
    public void ModifiedSavePayload_IsRejectedByTheIntegrityCheck()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo keyForSlot = Method("SaveKey");
        MethodInfo hasSave = Method("HasSaveInSlot");
        string key = (string)keyForSlot.Invoke(game, new object[] { 0 });

        save.Invoke(game, new object[] { false });
        string original = PlayerPrefs.GetString(key);
        string modified = original.Replace("\"chapter\":0", "\"chapter\":3");
        Assert.That(modified, Is.Not.EqualTo(original));
        PlayerPrefs.SetString(key, modified);
        PlayerPrefs.Save();

        Assert.That((bool)hasSave.Invoke(game, new object[] { 0 }), Is.False);
    }

    [Test]
    public void Version4Save_MigratesToVersion5WithIntegrityData()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo keyForSlot = Method("SaveKey");
        MethodInfo migrate = Method("MigrateLegacySaveIfNeeded");
        MethodInfo hasSave = Method("HasSaveInSlot");
        string version5Key = (string)keyForSlot.Invoke(game, new object[] { 0 });

        save.Invoke(game, new object[] { false });
        string legacy = PlayerPrefs.GetString(version5Key).Replace("\"version\":5", "\"version\":4");
        PlayerPrefs.SetString("AliceRpg.Save.v4.0", legacy);
        PlayerPrefs.DeleteKey(version5Key);
        PlayerPrefs.DeleteKey("AliceRpg.SaveMigratedV5");
        PlayerPrefs.Save();

        migrate.Invoke(game, null);

        Assert.That((bool)hasSave.Invoke(game, new object[] { 0 }), Is.True);
    }

    [Test]
    public void IntroSave_ResumesTheIntroInsteadOfSkippingToExploration()
    {
        FieldInfo introStage = Field("introStage");
        MethodInfo save = Method("SaveGame");
        MethodInfo load = Method("LoadGame");
        FieldInfo mode = Field("mode");

        introStage.SetValue(game, 1);
        save.Invoke(game, new object[] { false });
        load.Invoke(game, null);

        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Intro"));
    }

    [Test]
    public void FocusPause_ReturnsToBattleWithoutDiscardingIt()
    {
        FieldInfo mode = Field("mode");
        FieldInfo selection = Field("pauseSelection");
        MethodInfo focus = Method("OnApplicationFocus");
        MethodInfo activatePause = Method("ActivatePauseSelection");
        System.Type gameMode = mode.FieldType;

        mode.SetValue(game, System.Enum.Parse(gameMode, "Battle"));
        focus.Invoke(game, new object[] { false });
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Pause"));

        selection.SetValue(game, 0);
        activatePause.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Battle"));
    }

    [Test]
    public void Rebinding_RejectsReservedArrowKeys()
    {
        MethodInfo setBinding = Method("TrySetBinding");
        FieldInfo keyUp = Field("keyUp");
        keyUp.SetValue(game, KeyCode.W);

        bool result = (bool)setBinding.Invoke(game, new object[] { 0, KeyCode.UpArrow });

        Assert.That(result, Is.False);
        Assert.That((KeyCode)keyUp.GetValue(game), Is.EqualTo(KeyCode.W));
    }

    [Test]
    public void LoadingSettings_RepairsInvalidOrDuplicatedKeyBindings()
    {
        MethodInfo loadSettings = Method("LoadSettings");
        FieldInfo keyUp = Field("keyUp");
        FieldInfo keyDown = Field("keyDown");

        PlayerPrefs.SetInt("AliceRpg.Key.Up", int.MaxValue);
        PlayerPrefs.SetInt("AliceRpg.Key.Down", (int)KeyCode.W);
        PlayerPrefs.Save();

        loadSettings.Invoke(game, null);

        Assert.That((KeyCode)keyUp.GetValue(game), Is.EqualTo(KeyCode.W));
        Assert.That((KeyCode)keyDown.GetValue(game), Is.EqualTo(KeyCode.S));
    }

    [Test]
    public void FantasyUiTextures_AreGeneratedAtTheirLogicalSizes()
    {
        Method("CreateTextures").Invoke(game, null);
        Dictionary<string, Texture2D> textures = (Dictionary<string, Texture2D>)Field("textures").GetValue(game);

        Assert.That(textures["titleBackdrop"].width, Is.EqualTo(240));
        Assert.That(textures["titleBackdrop"].height, Is.EqualTo(135));
        Assert.That(textures["battleBackdrop"].width, Is.EqualTo(240));
        Assert.That(textures["battleBackdrop"].height, Is.EqualTo(100));
        Assert.That(textures["crown"].width, Is.EqualTo(32));
        Assert.That(textures["battleShadow"].height, Is.EqualTo(10));
        Assert.That((Color32)textures["titleBackdrop"].GetPixel(0, 134), Is.EqualTo(new Color32(45, 105, 188, 255)));
        Assert.That((Color32)textures["battleBackdrop"].GetPixel(239, 99), Is.EqualTo(new Color32(63, 133, 205, 255)));
    }

    [Test]
    public void DeletingTheActiveSlot_SelectsAnotherRecoverableSlot()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo delete = Method("ConfirmSaveSlotSelection");
        FieldInfo activeSlot = Field("activeSaveSlot");
        FieldInfo hasSave = Field("hasSave");

        activeSlot.SetValue(game, 0);
        save.Invoke(game, new object[] { false });
        activeSlot.SetValue(game, 1);
        save.Invoke(game, new object[] { false });
        activeSlot.SetValue(game, 0);

        Field("saveSlotSelection").SetValue(game, 0);
        FieldInfo confirmation = Field("saveSlotConfirmation");
        confirmation.SetValue(game, System.Enum.Parse(confirmation.FieldType, "Delete"));
        delete.Invoke(game, null);
        Assert.That(confirmation.GetValue(game).ToString(), Is.EqualTo("None"));

        Assert.That((int)activeSlot.GetValue(game), Is.EqualTo(1));
        Assert.That((bool)hasSave.GetValue(game), Is.True);
    }

    [Test]
    public void SelectingAnEmptyActiveSlot_UsesAnotherRecoverableSlot()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo selectRecoverableSlot = Method("SelectRecoverableActiveSlot");
        FieldInfo activeSlot = Field("activeSaveSlot");

        activeSlot.SetValue(game, 1);
        save.Invoke(game, new object[] { false });
        activeSlot.SetValue(game, 0);

        selectRecoverableSlot.Invoke(game, null);

        Assert.That((int)activeSlot.GetValue(game), Is.EqualTo(1));
    }

    [Test]
    public void SettingsAndControlsSelections_OpenRebindAndReturnToTheirMenus()
    {
        FieldInfo mode = Field("mode");
        FieldInfo settingsSelection = Field("settingsSelection");
        FieldInfo controlsSelection = Field("controlsSelection");
        MethodInfo activateSettings = Method("ActivateSettingsSelection");
        MethodInfo activateControls = Method("ActivateControlsSelection");

        mode.SetValue(game, System.Enum.Parse(mode.FieldType, "Settings"));
        controlsSelection.SetValue(game, 3);
        Field("rebindAction").SetValue(game, 2);
        settingsSelection.SetValue(game, 10);
        activateSettings.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Controls"));
        Assert.That((int)controlsSelection.GetValue(game), Is.Zero);
        Assert.That((int)Field("rebindAction").GetValue(game), Is.EqualTo(-1));
        Assert.That(Field("controlsReturnMode").GetValue(game).ToString(), Is.EqualTo("Settings"));

        activateControls.Invoke(game, null);
        Assert.That((int)Field("rebindAction").GetValue(game), Is.Zero);
        Field("rebindAction").SetValue(game, -1);
        controlsSelection.SetValue(game, 8);
        activateControls.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Settings"));

        settingsSelection.SetValue(game, 11);
        activateSettings.Invoke(game, null);
        Assert.That((bool)Field("confirmResetSettings").GetValue(game), Is.True);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Settings"));
        Field("confirmResetSettings").SetValue(game, false);
        Field("settingsReturnMode").SetValue(game, System.Enum.Parse(mode.FieldType, "Pause"));
        settingsSelection.SetValue(game, 12);
        activateSettings.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Pause"));
    }

    [Test]
    public void TerminalSelections_OpenRecordsOrReturnToTheCorrectTitleItem()
    {
        FieldInfo mode = Field("mode");
        MethodInfo activateEnding = Method("ActivateEndingSelection");

        mode.SetValue(game, System.Enum.Parse(mode.FieldType, "Ending"));
        Field("endingSelection").SetValue(game, 1);
        activateEnding.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Records"));
        Assert.That(Field("recordsReturnMode").GetValue(game).ToString(), Is.EqualTo("Ending"));

        mode.SetValue(game, System.Enum.Parse(mode.FieldType, "Ending"));
        Field("titleSelection").SetValue(game, 3);
        Field("endingSelection").SetValue(game, 2);
        activateEnding.Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Title"));
        Assert.That((int)Field("titleSelection").GetValue(game), Is.Zero);

        mode.SetValue(game, System.Enum.Parse(mode.FieldType, "GameOver"));
        Field("hasSave").SetValue(game, false);
        Field("gameOverSelection").SetValue(game, 0);
        Method("ActivateGameOverSelection").Invoke(game, null);
        Assert.That(mode.GetValue(game).ToString(), Is.EqualTo("Title"));
        Assert.That((int)Field("titleSelection").GetValue(game), Is.EqualTo(1));
    }

    [Test]
    public void BattleSelections_WithMissingResourcesReturnToTheMenu()
    {
        FieldInfo selection = Field("battleSelection");
        FieldInfo pending = Field("pendingBattle");
        MethodInfo activateBattle = Method("ActivateBattleSelection");
        Field("mp").SetValue(game, 0);
        Field("potions").SetValue(game, 0);

        selection.SetValue(game, 1);
        activateBattle.Invoke(game, null);
        Assert.That(pending.GetValue(game).ToString(), Is.EqualTo("Menu"));
        Assert.That(Field("battleMessage").GetValue(game), Is.EqualTo("MPが足りない！"));
        Assert.That((int)Field("mp").GetValue(game), Is.Zero);

        pending.SetValue(game, System.Enum.Parse(pending.FieldType, "None"));
        selection.SetValue(game, 3);
        activateBattle.Invoke(game, null);
        Assert.That(pending.GetValue(game).ToString(), Is.EqualTo("Menu"));
        Assert.That(Field("battleMessage").GetValue(game), Is.EqualTo("小瓶はもう空っぽだ。"));
        Assert.That((int)Field("potions").GetValue(game), Is.Zero);
    }

    [Test]
    public void ChestIndexAt_ReturnsTheFirstMatchOrMinusOne()
    {
        List<Vector2Int> positions = (List<Vector2Int>)Field("chestPositions").GetValue(game);
        positions.Clear();
        positions.Add(new Vector2Int(1, 2));
        positions.Add(new Vector2Int(3, 4));
        positions.Add(new Vector2Int(3, 4));
        MethodInfo chestIndexAt = Method("ChestIndexAt");

        Assert.That((int)chestIndexAt.Invoke(game, new object[] { new Vector2Int(3, 4) }), Is.EqualTo(1));
        Assert.That((int)chestIndexAt.Invoke(game, new object[] { new Vector2Int(4, 3) }), Is.EqualTo(-1));
    }

    private MethodInfo Method(string name)
    {
        return typeof(AliceRpgGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private FieldInfo Field(string name)
    {
        return typeof(AliceRpgGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void ClearAliceRpgData()
    {
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.DeleteKey("AliceRpg.Save.v4." + i);
            PlayerPrefs.DeleteKey("AliceRpg.Save.v4." + i + ".backup");
            PlayerPrefs.DeleteKey("AliceRpg.Save.v5." + i);
            PlayerPrefs.DeleteKey("AliceRpg.Save.v5." + i + ".backup");
            PlayerPrefs.DeleteKey("AliceRpg.Deaths." + i);
        }
        PlayerPrefs.DeleteKey("AliceRpg.Save.v2");
        for (int i = 0; i < IntegerPreferenceKeys.Length; i++) PlayerPrefs.DeleteKey(IntegerPreferenceKeys[i]);
        for (int i = 0; i < FloatPreferenceKeys.Length; i++) PlayerPrefs.DeleteKey(FloatPreferenceKeys[i]);
        PlayerPrefs.Save();
    }

    private void CaptureAliceRpgData()
    {
        savedStringPrefs.Clear();
        savedIntPrefs.Clear();
        savedFloatPrefs.Clear();
        for (int i = 0; i < 3; i++)
        {
            CaptureString("AliceRpg.Save.v4." + i);
            CaptureString("AliceRpg.Save.v4." + i + ".backup");
            CaptureString("AliceRpg.Save.v5." + i);
            CaptureString("AliceRpg.Save.v5." + i + ".backup");
            CaptureInt("AliceRpg.Deaths." + i);
        }
        CaptureString("AliceRpg.Save.v2");
        for (int i = 0; i < IntegerPreferenceKeys.Length; i++) CaptureInt(IntegerPreferenceKeys[i]);
        for (int i = 0; i < FloatPreferenceKeys.Length; i++) CaptureFloat(FloatPreferenceKeys[i]);
    }

    private void RestoreAliceRpgData()
    {
        foreach (KeyValuePair<string, string> item in savedStringPrefs) PlayerPrefs.SetString(item.Key, item.Value);
        foreach (KeyValuePair<string, int> item in savedIntPrefs) PlayerPrefs.SetInt(item.Key, item.Value);
        foreach (KeyValuePair<string, float> item in savedFloatPrefs) PlayerPrefs.SetFloat(item.Key, item.Value);
        PlayerPrefs.Save();
    }

    private void CaptureString(string key)
    {
        if (PlayerPrefs.HasKey(key)) savedStringPrefs[key] = PlayerPrefs.GetString(key);
    }

    private void CaptureInt(string key)
    {
        if (PlayerPrefs.HasKey(key)) savedIntPrefs[key] = PlayerPrefs.GetInt(key);
    }

    private void CaptureFloat(string key)
    {
        if (PlayerPrefs.HasKey(key)) savedFloatPrefs[key] = PlayerPrefs.GetFloat(key);
    }
}
