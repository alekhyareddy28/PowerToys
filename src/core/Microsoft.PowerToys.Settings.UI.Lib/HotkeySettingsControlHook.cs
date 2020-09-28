﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using interop;

namespace Microsoft.PowerToys.Settings.UI.Lib
{
    public delegate void KeyEvent(int key);

    public delegate bool IsActive();

    public enum AccessibleKeysPressed
    {
        Tab,
        ShiftTab,
        Other,
    }

    public delegate AccessibleKeysPressed FilterAccessibleKeyboardEvents(int key);

    public class HotkeySettingsControlHook
    {
        private const int WmKeyDown = 0x100;
        private const int WmKeyUp = 0x101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private KeyboardHook _hook;
        private KeyEvent _keyDown;
        private KeyEvent _keyUp;
        private IsActive _isActive;
        private FilterAccessibleKeyboardEvents _filterKeyboardEvent;

        public HotkeySettingsControlHook(KeyEvent keyDown, KeyEvent keyUp, IsActive isActive, FilterAccessibleKeyboardEvents filterAccessibleKeyboardEvents)
        {
            _keyDown = keyDown;
            _keyUp = keyUp;
            _isActive = isActive;
            _filterKeyboardEvent = filterAccessibleKeyboardEvents;
            _hook = new KeyboardHook(HotkeySettingsHookCallback, IsActive, FilterKeyboardEvents);
            _hook.Start();
        }

        private bool IsActive()
        {
            return _isActive();
        }

        private bool FilterKeyboardEvents(KeyboardEvent ev)
        {
            AccessibleKeysPressed keysPressed = _filterKeyboardEvent(ev.key);
            if (keysPressed == AccessibleKeysPressed.Tab)
            {
                return false;
            }
            else if (keysPressed == AccessibleKeysPressed.ShiftTab)
            {
                return false;
            }

            return true;
        }

        private void HotkeySettingsHookCallback(KeyboardEvent ev)
        {
            switch (ev.message)
            {
                case WmKeyDown:
                case WmSysKeyDown:
                    _keyDown(ev.key);
                    break;
                case WmKeyUp:
                case WmSysKeyUp:
                    _keyUp(ev.key);
                    break;
            }
        }

        public void Dispose()
        {
            // Dispose the KeyboardHook object to terminate the hook threads
            _hook.Dispose();
        }
    }
}
