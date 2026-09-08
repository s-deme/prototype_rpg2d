using System.Collections.Generic;
using UnityEngine;

public sealed partial class AliceRpgGame
{
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
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["titleBackdrop"]);
        GUI.color = new Color(ink.r, ink.g, ink.b, highContrast ? 1f : 0.58f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, 184), textures["white"]);
        GUI.color = Color.white;
        DrawTexturePixel(textures["crown"], new Rect(448, 17, 64, 40));
        GUI.Label(new Rect(60, 51, 840, 62), "ALICE & THE BROKEN CROWN", titleOnDarkStyle);
        GUI.Label(new Rect(120, 112, 720, 34), "アリスと壊れた時の冠", centerOnDarkStyle);
        GUI.color = gold;
        GUI.DrawTexture(new Rect(274, 151, 412, 3), textures["white"]);
        GUI.color = Color.white;

        DrawPanel(new Rect(48, 204, 450, 76));
        GUI.Label(new Rect(72, 216, 402, 50), "止まった明日を取り戻すため、\n三つの《時の欠片》をめぐる旅へ。", questStyle);
        DrawTexturePixel(textures["alice"], new Rect(202, 292, 144, 162));

        DrawPanel(new Rect(570, 190, 356, 278));
        GUI.Label(new Rect(594, 201, 308, 26), "旅をはじめる", hintStyle);
        string[] titleItems =
        {
            hasSave ? "つづきから　[しおり " + (activeSaveSlot + 1) + "]" : "つづきから（データなし）",
            "はじめから", "旅のしおり", "旅の記録", "さくせん", "クレジット", "ゲームをおわる"
        };
        for (int i = 0; i < titleItems.Length; i++)
        {
            Rect itemRect = new Rect(594, 229 + i * 31, 308, 28);
            DrawMenuItem(itemRect, titleItems[i], titleSelection == i);
            if (!confirmNewGame && !confirmQuit && MouseActivated(itemRect)) { titleSelection = i; ActivateTitleSelection(); }
        }
        SaveData titlePreview = ReadSave(activeSaveSlot);
        if (titlePreview == null) titlePreview = ReadBackup(activeSaveSlot);
        if (hasSave && titlePreview != null)
        {
            string progress = string.IsNullOrEmpty(titlePreview.chapterName) ? "物語の途中" : titlePreview.chapterName;
            DrawPanel(new Rect(48, 458, 450, 48));
            GUI.Label(new Rect(66, 463, 414, 38), "しおり " + (activeSaveSlot + 1) + "　" + progress + "　" + FormatSeconds(titlePreview.playSeconds), hintStyle);
        }
        GUI.color = ink;
        GUI.DrawTexture(new Rect(0, 512, LogicalWidth, 28), textures["white"]);
        GUI.color = Color.white;
        GUI.Label(new Rect(90, 515, 780, 21), "A WONDERLAND STORY　—　キーボード / マウス / ゲームパッド対応", smallOnDarkStyle);
        if (Time.unscaledTime < toastUntil)
        {
            DrawPanel(new Rect(160, 158, 640, 40));
            GUI.Label(new Rect(180, 165, 600, 25), toast, hintStyle);
        }
        if (confirmNewGame || confirmQuit)
        {
            DrawDimmer(0.72f);
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
        if ((nearby != null || chestAhead >= 0) && mode == GameMode.Explore)
        {
            DrawPanel(new Rect(playerPosition.x * Tile - 42, Mathf.Max(8, playerPosition.y * Tile - 42), 116, 28));
            GUI.Label(new Rect(playerPosition.x * Tile - 38, Mathf.Max(9, playerPosition.y * Tile - 41), 108, 24), "[" + ConfirmHint() + "] " + (nearby != null ? "話す" : "調べる"), hintStyle);
        }

        DrawPanel(new Rect(0, 506, 960, 34));
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
        DrawDimmer(0.5f);
        Rect box = new Rect(54, 357, 852, 147);
        DrawPanel(box);
        DrawPanel(new Rect(70, 338, 230, 42));
        GUI.Label(new Rect(84, 345, 202, 28), dialogueSpeaker, speakerStyle);
        string text = VisibleDialogueText();
        GUI.Label(new Rect(82, 389, 796, 80), text, dialogueStyle);
        string continueHint = DialoguePageFullyRevealed() ? "▼ " + ConfirmHint() + "でつづく　Tabでスキップ" : "▼ " + ConfirmHint() + "で全文表示　Shiftで早送り";
        GUI.Label(new Rect(590, 472, 284, 22), continueHint, smallStyle);
        if (MouseActivated(box)) AdvanceDialogue();
    }

    private void DrawBattle()
    {
        GUI.DrawTexture(new Rect(0, 0, 960, 377), textures["battleBackdrop"]);
        GUI.color = new Color(ink.r, ink.g, ink.b, 0.22f);
        GUI.DrawTexture(new Rect(0, 0, 960, 377), textures["white"]);
        GUI.color = Color.white;

        DrawPanel(new Rect(28, 22, 300, 104));
        GUI.Label(new Rect(48, 38, 250, 24), "アリス　Lv." + level, labelStyle);
        GUI.Label(new Rect(48, 68, 250, 24), "HP " + hp + "/" + maxHp + "　 MP " + mp + "/" + maxMp, labelStyle);
        if (weakenedTurns > 0) GUI.Label(new Rect(48, 95, 250, 18), "状態：力が弱い　残り " + weakenedTurns + " 回", hintStyle);

        DrawPanel(new Rect(590, 20, 270, 54));
        GUI.Label(new Rect(608, 29, 234, 34), enemy.name, centerOnDarkStyle);
        DrawTexturePixel(textures["battleShadow"], new Rect(620, 291, 210, 40));
        DrawTexturePixel(enemy.sprite, new Rect(605, 92, 240, 240));
        DrawBar(new Rect(590, 332, 270, 16), enemy.hp, enemy.maxHp, rose);

        DrawPanel(new Rect(28, 377, 420, 143));
        GUI.Label(new Rect(50, 381, 160, 22), "コマンド", speakerStyle);
        string[] commands = { "こうげき", "ひかり　MP 4", "まもる　+MP", "小瓶　× " + potions, "しらべる", enemy.boss ? "にげる　×" : "にげる" };
        for (int i = 0; i < commands.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Rect commandRect = new Rect(48 + col * 190, 404 + row * 35, 176, 30);
            DrawMenuItem(commandRect, commands[i], pendingBattle == PendingBattle.None && battleSelection == i);
            if (pendingBattle == PendingBattle.None && MouseActivated(commandRect))
            {
                battleSelection = i;
                ActivateBattleSelection();
            }
        }
        DrawPanel(new Rect(466, 377, 466, 143));
        GUI.Label(new Rect(490, 381, 180, 22), "戦いのようす", speakerStyle);
        Rect messageRect = new Rect(492, 409, 412, 78);
        GUI.Label(messageRect, battleMessage, dialogueStyle);
        if (pendingBattle != PendingBattle.None) GUI.Label(new Rect(760, 492, 140, 20), "▼ 決定", smallStyle);
        if (pendingBattle != PendingBattle.None && MouseActivated(new Rect(466, 377, 466, 143))) AdvanceBattleMessage();
        if (enemyInspected) DrawWorldLabel(new Rect(602, 350, 246, 22), "HP " + enemy.hp + "/" + enemy.maxHp + "  ATK " + enemy.attack);
    }

    private void DrawPause()
    {
        DrawDimmer(0.62f);
        DrawPanel(new Rect(244, 48, 472, 444));
        GUI.Label(new Rect(274, 94, 412, 44), "旅のメニュー", titleStyle);
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
        DrawMenuBackdrop();
        DrawPanel(new Rect(180, 24, 600, 494));
        GUI.Label(new Rect(210, 42, 540, 48), "さくせん", titleStyle);
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
                ActivateSettingsSelection();
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
        DrawDimmer(0.72f);
        DrawPanel(new Rect(250, 178, 460, 184));
        GUI.Label(new Rect(280, 202, 400, 62), message, centerStyle);
        Rect yesRect = new Rect(300, 294, 160, 38);
        Rect noRect = new Rect(500, 294, 160, 38);
        DrawMenuItem(yesRect, confirmText + " [決定]", true);
        DrawMenuItem(noRect, "元に戻す [Esc]", false);
        if (MouseActivated(yesRect))
        {
            if (confirmResetSettings) ConfirmSettingsReset();
            else ConfirmDisplayChange();
        }
        if (MouseActivated(noRect))
        {
            if (confirmResetSettings) confirmResetSettings = false;
            else CancelDisplayChange();
        }
    }

    private void DrawSaveSlots()
    {
        DrawMenuBackdrop();
        DrawPanel(new Rect(180, 44, 600, 452));
        string title = saveSlotPurpose == SaveSlotPurpose.Save ? "旅のしおりに記録" : "旅のしおり";
        string description = saveSlotPurpose == SaveSlotPurpose.Save ? "保存先のしおりを選んでください" : "スロットを選び、下の操作を選んでください";
        GUI.Label(new Rect(210, 64, 540, 48), title, titleStyle);
        GUI.Label(new Rect(235, 116, 490, 24), description, hintStyle);
        for (int i = 0; i < SaveSlotCount; i++)
        {
            SaveData preview = ReadSave(i);
            bool backupAvailable = HasBackupInSlot(i);
            Rect slotRect = new Rect(230, 150 + i * 70, 500, 58);
            DrawMenuItem(slotRect, SlotSummary(i, preview, backupAvailable), saveSlotSelection == i);
            if (saveSlotConfirmation == SaveSlotConfirmation.None && MouseActivated(slotRect))
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

        if (saveSlotConfirmation == SaveSlotConfirmation.Overwrite)
            DrawSaveSlotConfirmation("このスロットを上書きしますか？\n直前の内容はバックアップされます。", "上書きする");
        else if (saveSlotConfirmation == SaveSlotConfirmation.RestoreBackup)
            DrawSaveSlotConfirmation("バックアップを通常データへ復元します。\n現在の破損データは置き換えられます。", "復元する");
        else if (saveSlotConfirmation == SaveSlotConfirmation.Delete)
            DrawSaveSlotConfirmation("このスロットとバックアップを削除しますか？\nこの操作は元に戻せません。", "削除する");
    }

    private void DrawSaveSlotConfirmation(string message, string confirmText)
    {
        DrawDimmer(0.72f);
        DrawPanel(new Rect(250, 178, 460, 184));
        GUI.Label(new Rect(280, 202, 400, 62), message, centerStyle);
        Rect yesRect = new Rect(300, 294, 160, 38);
        Rect noRect = new Rect(500, 294, 160, 38);
        DrawMenuItem(yesRect, confirmText + " [決定]", true);
        DrawMenuItem(noRect, "いいえ [Esc]", false);
        if (MouseActivated(yesRect)) ConfirmSaveSlotSelection();
        if (MouseActivated(noRect)) saveSlotConfirmation = SaveSlotConfirmation.None;
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
        DrawMenuBackdrop();
        DrawPanel(new Rect(200, 34, 560, 472));
        GUI.Label(new Rect(230, 54, 500, 48), "キー設定", titleStyle);
        GUI.Label(new Rect(240, 108, 480, 22), "項目を選んで決定し、新しいキーを押してください。", hintStyle);
        for (int i = 0; i < ControlMenuItemCount; i++)
        {
            string value = i < ControlBindingCount ? BindingAt(i).ToString() : "設定へもどる";
            Rect itemRect = new Rect(255, 140 + i * 34, 450, 28);
            DrawMenuItem(itemRect, ControlName(i) + "　　" + value, controlsSelection == i);
            if (rebindAction < 0 && MouseActivated(itemRect))
            {
                controlsSelection = i;
                ActivateControlsSelection();
            }
        }
        GUI.Label(new Rect(240, 448, 480, 44), "ゲームパッド：左スティック/D-Padで移動　A：決定　B：もどる\nX：ログ　Y：クエスト", smallStyle);
        if (rebindAction < 0) return;
        DrawDimmer(0.72f);
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
        DrawDimmer(0.67f);
        DrawPanel(new Rect(110, 58, 740, 420));
        GUI.Label(new Rect(140, 78, 680, 42), "会話の記録", titleStyle);
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
        DrawMenuBackdrop();
        DrawPanel(new Rect(170, 54, 620, 432));
        GUI.Label(new Rect(200, 74, 560, 48), "旅の記録", titleStyle);
        for (int i = 0; i < SaveSlotCount; i++)
        {
            SaveData data = ReadSave(i);
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
        DrawMenuBackdrop();
        DrawPanel(new Rect(160, 48, 640, 444));
        GUI.Label(new Rect(190, 68, 580, 48), "クレジット・サポート", titleStyle);
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
        return DisplayResolutionLabels[Mathf.Clamp(resolutionIndex, 0, DisplayResolutionLabels.Length - 1)];
    }

    private string ConfirmHint()
    {
        return keyConfirm + "/A";
    }

    private void DrawEnding()
    {
        DrawMenuBackdrop();
        GUI.color = Color.white;
        DrawTexturePixel(textures["crown"], new Rect(448, 30, 64, 40));
        GUI.Label(new Rect(100, 75, 760, 54), "そして 物語は つづく", titleStyle);
        DrawTexturePixel(textures["alice"], new Rect(414, 130, 132, 148));
        DrawPanel(new Rect(140, 288, 680, 142));
        GUI.Label(new Rect(164, 304, 632, 46), "明日は、選んだ一歩の先にある。", centerStyle);
        GUI.Label(new Rect(164, 354, 632, 28), "クリアレベル  " + level + "　　集めた時の欠片  " + fragments + "/3", centerStyle);
        GUI.Label(new Rect(164, 390, 632, 24), "歩数 " + stepsTaken + "　勝利 " + battlesWon + "　プレイ時間 " + PlayTimeText(), hintStyle);
        DrawPanel(new Rect(250, 438, 460, 94));
        string[] endingItems = { "NEW GAME+　（Lv.2から再び物語へ）", "クリア記録を見る", "タイトルへ" };
        for (int i = 0; i < endingItems.Length; i++)
        {
            Rect itemRect = new Rect(276, 444 + i * 27, 408, 24);
            DrawMenuItem(itemRect, endingItems[i], endingSelection == i);
            if (MouseActivated(itemRect))
            {
                endingSelection = i;
                ActivateEndingSelection();
            }
        }
    }

    private void DrawGameOver()
    {
        DrawMenuBackdrop();
        DrawPanel(new Rect(160, 128, 640, 212));
        GUI.Label(new Rect(190, 158, 580, 54), "物語は いったん閉じられた", titleStyle);
        GUI.Label(new Rect(190, 234, 580, 60), "けれど、ページはいつでも開き直せる。\n旅のしおりから、もう一度。", centerStyle);
        DrawPanel(new Rect(300, 356, 360, 112));
        string[] choices = { hasSave ? "直前のしおりから再開" : "再開データなし", "タイトルへ" };
        for (int i = 0; i < choices.Length; i++)
        {
            Rect itemRect = new Rect(326, 373 + i * 43, 308, 34);
            DrawMenuItem(itemRect, choices[i], gameOverSelection == i);
            if (MouseActivated(itemRect))
            {
                gameOverSelection = i;
                ActivateGameOverSelection();
            }
        }
    }

    private void DrawMenuBackdrop()
    {
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["titleBackdrop"]);
        GUI.color = highContrast ? Color.black : new Color(ink.r, ink.g, ink.b, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
    }

    private void DrawDimmer(float opacity)
    {
        GUI.color = new Color(0f, 0f, 0f, opacity);
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), textures["white"]);
        GUI.color = Color.white;
    }

    private void DrawPanel(Rect rect)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.38f);
        GUI.DrawTexture(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), textures["white"]);
        GUI.color = highContrast ? Color.black : surface;
        GUI.DrawTexture(rect, textures["white"]);
        GUI.color = cream;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), textures["white"]);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 3, rect.width, 3), textures["white"]);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 3, rect.height), textures["white"]);
        GUI.DrawTexture(new Rect(rect.xMax - 3, rect.y, 3, rect.height), textures["white"]);
        if (rect.height > 48f)
        {
            GUI.color = highContrast ? Color.white : new Color32(112, 175, 238, 255);
            GUI.DrawTexture(new Rect(rect.x + 7, rect.y + 7, rect.width - 14, 1), textures["white"]);
            GUI.DrawTexture(new Rect(rect.x + 7, rect.yMax - 8, rect.width - 14, 1), textures["white"]);
            GUI.DrawTexture(new Rect(rect.x + 7, rect.y + 7, 1, rect.height - 14), textures["white"]);
            GUI.DrawTexture(new Rect(rect.xMax - 8, rect.y + 7, 1, rect.height - 14), textures["white"]);
        }
        GUI.color = Color.white;
    }

    private void DrawMenuItem(Rect rect, string text, bool selected)
    {
        Event current = Event.current;
        bool hovered = current != null && rect.Contains((current.mousePosition - guiOffset) / guiScale);
        if (selected || hovered)
        {
            GUI.color = highContrast ? new Color(1f, 1f, 1f, selected ? 0.24f : 0.12f) : new Color(blue.r, blue.g, blue.b, selected ? 1f : 0.55f);
            GUI.DrawTexture(rect, textures["white"]);
            GUI.color = Color.white;
        }
        GUI.Label(rect, (selected ? "▶ " : hovered ? "›  " : "   ") + text, selected ? selectedStyle : menuStyle);
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
        titleStyle = NewStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, gold);
        titleOnDarkStyle = NewStyle(42, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
        labelStyle = NewStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, cream);
        smallStyle = NewStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, cream);
        smallOnDarkStyle = NewStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, cream);
        centerStyle = NewStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
        centerOnDarkStyle = NewStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
        menuStyle = NewStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, cream);
        selectedStyle = NewStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, cream);
        dialogueStyle = NewStyle(19, FontStyle.Bold, TextAnchor.UpperLeft, cream);
        dialogueStyle.wordWrap = true;
        speakerStyle = NewStyle(18, FontStyle.Bold, TextAnchor.MiddleLeft, gold);
        questStyle = NewStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, cream);
        questStyle.wordWrap = true;
        bodyStyle = NewStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, cream);
        bodyStyle.wordWrap = true;
        hintStyle = NewStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, cream);
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

}
