using System;
using UnityEngine;

public sealed partial class AliceRpgGame
{
    private const int CurrentSaveVersion = 5;
    private const int SaveSlotCount = 3;
    private const int SaveSlotActionCount = 3;
    private const int ControlBindingCount = 8;
    private const int ControlMenuItemCount = ControlBindingCount + 1;

    private enum GameMode { Title, Intro, Explore, Dialogue, Battle, Pause, Settings, SaveSlots, Controls, DialogueLog, Records, Credits, Ending, GameOver }
    private enum PendingBattle { None, Menu, Victory, Defeat }
    private enum SaveSlotPurpose { Load, Save, Manage }
    private enum SaveSlotConfirmation { None, Overwrite, RestoreBackup, Delete }

    private static readonly string[] ControlNames =
    {
        "上へ移動", "下へ移動", "左へ移動", "右へ移動",
        "決定 / 話す", "キャンセル / メニュー", "クエスト", "会話ログ"
    };

    private static readonly string[] ControlPreferenceKeys =
    {
        "Up", "Down", "Left", "Right", "Confirm", "Cancel", "Quest", "Log"
    };

    private static readonly KeyCode[] DefaultControlBindings =
    {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D,
        KeyCode.Space, KeyCode.X, KeyCode.Q, KeyCode.L
    };

    private static readonly Vector2Int[] DisplayResolutions =
    {
        new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1920, 1080)
    };

    private static readonly string[] DisplayResolutionLabels =
    {
        "960 × 540", "1280 × 720", "1920 × 1080"
    };

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
}
