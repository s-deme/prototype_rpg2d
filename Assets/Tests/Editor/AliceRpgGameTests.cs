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
        MethodInfo restore = Method("RestoreBackupAndLoad");
        string key = (string)keyForSlot.Invoke(game, new object[] { 0 });

        save.Invoke(game, new object[] { false });
        save.Invoke(game, new object[] { false });
        PlayerPrefs.SetString(key, "{not valid save data");
        PlayerPrefs.Save();

        Assert.That((bool)hasRecoverable.Invoke(game, new object[] { 0 }), Is.True);
        restore.Invoke(game, new object[] { 0 });
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
    public void DeletingTheActiveSlot_SelectsAnotherRecoverableSlot()
    {
        MethodInfo save = Method("SaveGame");
        MethodInfo delete = Method("DeleteSaveSlot");
        FieldInfo activeSlot = Field("activeSaveSlot");
        FieldInfo hasSave = Field("hasSave");

        activeSlot.SetValue(game, 0);
        save.Invoke(game, new object[] { false });
        activeSlot.SetValue(game, 1);
        save.Invoke(game, new object[] { false });
        activeSlot.SetValue(game, 0);

        delete.Invoke(game, new object[] { 0 });

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
