using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AliceRpgGame
{
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
    private float nextMoveAt;
    private float nextMenuAxisAt;
    private float nextMenuHorizontalAt;
    private Vector2Int heldDirection;

    private KeyCode keyUp = KeyCode.W;
    private KeyCode keyDown = KeyCode.S;
    private KeyCode keyLeft = KeyCode.A;
    private KeyCode keyRight = KeyCode.D;
    private KeyCode keyConfirm = KeyCode.Space;
    private KeyCode keyCancel = KeyCode.X;
    private KeyCode keyQuest = KeyCode.Q;
    private KeyCode keyLog = KeyCode.L;

    private bool confirmResetSettings;
    private bool confirmDisplayChange;
    private float displayConfirmUntil;
    private bool previousFullscreen;
    private int previousResolutionIndex;

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

    private string ControlName(int index)
    {
        return index >= 0 && index < ControlBindingCount ? ControlNames[index] : "もどる";
    }

    private KeyCode BindingAt(int index)
    {
        switch (index)
        {
            case 0: return keyUp;
            case 1: return keyDown;
            case 2: return keyLeft;
            case 3: return keyRight;
            case 4: return keyConfirm;
            case 5: return keyCancel;
            case 6: return keyQuest;
            case 7: return keyLog;
            default: return KeyCode.None;
        }
    }

    private bool TrySetBinding(int index, KeyCode key)
    {
        if (index < 0 || index >= ControlBindingCount || key == KeyCode.None) return false;
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
        for (int i = 0; i < ControlBindingCount; i++)
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
            RequestDisplayChange(fullscreen, (resolutionIndex + direction + DisplayResolutions.Length) % DisplayResolutions.Length);
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
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt("AliceRpg.Resolution", 1), 0, DisplayResolutions.Length - 1);
        activeSaveSlot = Mathf.Clamp(PlayerPrefs.GetInt("AliceRpg.ActiveSlot", 0), 0, SaveSlotCount - 1);
        for (int i = 0; i < ControlBindingCount; i++)
            SetBindingValue(i, LoadKey(ControlPreferenceKeys[i], DefaultControlBindings[i]));
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
        for (int i = 0; i < ControlBindingCount; i++)
            SaveKeyBinding(ControlPreferenceKeys[i], BindingAt(i));
        PlayerPrefs.Save();
    }

    private bool NormalizeKeyBindings()
    {
        HashSet<KeyCode> used = new HashSet<KeyCode>();
        bool changed = false;
        for (int i = 0; i < ControlBindingCount; i++)
        {
            KeyCode key = BindingAt(i);
            if (!IsBindingAllowed(key) || used.Contains(key))
            {
                key = FirstAvailableDefaultBinding(used);
                SetBindingValue(i, key);
                changed = true;
            }
            used.Add(key);
        }
        return changed;
    }

    private KeyCode FirstAvailableDefaultBinding(HashSet<KeyCode> used)
    {
        for (int i = 0; i < ControlBindingCount; i++)
            if (!used.Contains(DefaultControlBindings[i])) return DefaultControlBindings[i];
        return KeyCode.W;
    }

    private bool IsBindingAllowed(KeyCode key)
    {
        return key != KeyCode.None && !IsReservedKey(key) && Enum.IsDefined(typeof(KeyCode), key);
    }

    private void SetBindingValue(int index, KeyCode key)
    {
        switch (index)
        {
            case 0: keyUp = key; break;
            case 1: keyDown = key; break;
            case 2: keyLeft = key; break;
            case 3: keyRight = key; break;
            case 4: keyConfirm = key; break;
            case 5: keyCancel = key; break;
            case 6: keyQuest = key; break;
            case 7: keyLog = key; break;
        }
    }

    private void ResetSettings()
    {
        musicVolume = 0.55f; sfxVolume = 0.7f; difficulty = 1; textSpeed = 1; uiTextScale = 1f;
        highContrast = false; reducedMotion = false; gentleEncounters = false; fullscreen = true; resolutionIndex = 1;
        for (int i = 0; i < ControlBindingCount; i++) SetBindingValue(i, DefaultControlBindings[i]);
        ApplyDisplaySettings();
        SaveSettings();
    }

    private void ApplyDisplaySettings()
    {
        int index = Mathf.Clamp(resolutionIndex, 0, DisplayResolutions.Length - 1);
        Vector2Int resolution = DisplayResolutions[index];
        Screen.SetResolution(resolution.x, resolution.y, fullscreen);
    }

    private void RequestDisplayChange(bool requestedFullscreen, int requestedResolutionIndex)
    {
        if (confirmDisplayChange) return;
        previousFullscreen = fullscreen;
        previousResolutionIndex = resolutionIndex;
        fullscreen = requestedFullscreen;
        resolutionIndex = Mathf.Clamp(requestedResolutionIndex, 0, DisplayResolutions.Length - 1);
        ApplyDisplaySettings();
        confirmDisplayChange = true;
        displayConfirmUntil = Time.unscaledTime + 12f;
    }
}
