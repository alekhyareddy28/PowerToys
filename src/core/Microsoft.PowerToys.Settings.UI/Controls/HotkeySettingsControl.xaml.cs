// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.PowerToys.Settings.UI.Lib;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.PowerToys.Settings.UI.Controls
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Naming used in Win32 dll")]
    public sealed partial class HotkeySettingsControl : UserControl
    {
        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            internal INPUTTYPE type;
            internal InputUnion data;

            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal uint dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal short wVk;
            internal short wScan;
            internal uint dwFlags;
            internal int time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            internal int uMsg;
            internal short wParamL;
            internal short wParamH;
        }

        internal enum INPUTTYPE : uint
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2,
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern short GetAsyncKeyState(int vKey);

        public string Header { get; set; }

        public string Keys { get; set; }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                "Enabled",
                typeof(bool),
                typeof(HotkeySettingsControl),
                null);

        private bool _enabled = false;

        private bool _shiftKeyDownOnEntering = false;

        private bool _shiftToggled = false;

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                SetValue(IsActiveProperty, value);
                _enabled = value;

                if (value)
                {
                    HotkeyTextBox.IsEnabled = true;

                    // TitleText.IsActive = "True";
                    // TitleGlyph.IsActive = "True";
                }
                else
                {
                    HotkeyTextBox.IsEnabled = false;

                    // TitleText.IsActive = "False";
                    // TitleGlyph.IsActive = "False";
                }
            }
        }

        public static readonly DependencyProperty HotkeySettingsProperty =
            DependencyProperty.Register(
                "HotkeySettings",
                typeof(HotkeySettings),
                typeof(HotkeySettingsControl),
                null);

        private HotkeySettings hotkeySettings;
        private HotkeySettings internalSettings;
        private HotkeySettings lastValidSettings;
        private HotkeySettingsControlHook hook;
        private bool _isActive;

        public HotkeySettings HotkeySettings
        {
            get
            {
                return hotkeySettings;
            }

            set
            {
                if (hotkeySettings != value)
                {
                    hotkeySettings = value;
                    SetValue(HotkeySettingsProperty, value);
                    HotkeyTextBox.Text = HotkeySettings.ToString();
                }
            }
        }

        public HotkeySettingsControl()
        {
            InitializeComponent();
            internalSettings = new HotkeySettings();

            HotkeyTextBox.GettingFocus += HotkeyTextBox_GettingFocus;
            HotkeyTextBox.LosingFocus += HotkeyTextBox_LosingFocus;
            HotkeyTextBox.Unloaded += HotkeyTextBox_Unloaded;
            hook = new HotkeySettingsControlHook(Hotkey_KeyDown, Hotkey_KeyUp, Hotkey_IsActive, FilterAccessibleKeyboardEvents);
        }

        private void HotkeyTextBox_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the HotkeySettingsControlHook object to terminate the hook threads when the textbox is unloaded
            hook.Dispose();
        }

        private void KeyEventHandler(int key, bool matchValue, int matchValueCode, string matchValueText)
        {
            switch ((Windows.System.VirtualKey)key)
            {
                case Windows.System.VirtualKey.LeftWindows:
                case Windows.System.VirtualKey.RightWindows:
                    internalSettings.Win = matchValue;
                    break;
                case Windows.System.VirtualKey.Control:
                case Windows.System.VirtualKey.LeftControl:
                case Windows.System.VirtualKey.RightControl:
                    internalSettings.Ctrl = matchValue;
                    break;
                case Windows.System.VirtualKey.Menu:
                case Windows.System.VirtualKey.LeftMenu:
                case Windows.System.VirtualKey.RightMenu:
                    internalSettings.Alt = matchValue;
                    break;
                case Windows.System.VirtualKey.Shift:
                case Windows.System.VirtualKey.LeftShift:
                case Windows.System.VirtualKey.RightShift:
                    _shiftToggled = true;
                    internalSettings.Shift = matchValue;
                    break;
                case Windows.System.VirtualKey.Escape:
                    internalSettings = new HotkeySettings();
                    HotkeySettings = new HotkeySettings();
                    return;
                default:
                    internalSettings.Code = matchValueCode;
                    break;
            }
        }

        private async void Hotkey_KeyDown(int key)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                KeyEventHandler(key, true, key, Lib.Utilities.Helper.GetKeyName((uint)key));
                if (internalSettings.Code > 0)
                {
                    lastValidSettings = internalSettings.Clone();
                    HotkeyTextBox.Text = lastValidSettings.ToString();
                }
            });
        }

        private AccessibleKeysPressed FilterAccessibleKeyboardEvents(int key)
        {
            if (key == 0x09)
            {
                // TODO: Others should not be pressed
                if (!internalSettings.Shift && !_shiftKeyDownOnEntering)
                {
                    return AccessibleKeysPressed.Tab;
                }

                // shift was not pressed while entering but it was pressed while leaving the hotkey
                else if (internalSettings.Shift && !_shiftKeyDownOnEntering)
                {
                    internalSettings.Shift = false;

                    INPUT inputShift = new INPUT
                    {
                        type = INPUTTYPE.INPUT_KEYBOARD,
                        data = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0x10,
                                dwFlags = (uint)KeyEventF.KeyDown,
                                dwExtraInfo = (UIntPtr)0x5555,
                            },
                        },
                    };

                    INPUT[] inputs = new INPUT[] { inputShift };

                    _ = SendInput(1, inputs, INPUT.Size);

                    return AccessibleKeysPressed.Tab;
                }

                // Shift was pressed on entering and remained pressed
                else if (!internalSettings.Shift && _shiftKeyDownOnEntering && !_shiftToggled)
                {
                    INPUT inputShift = new INPUT
                    {
                        type = INPUTTYPE.INPUT_KEYBOARD,
                        data = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0x10,
                                dwFlags = (uint)KeyEventF.KeyDown,
                                dwExtraInfo = (UIntPtr)0x5555,
                            },
                        },
                    };

                    INPUT[] inputs = new INPUT[] { inputShift };

                    _ = SendInput(1, inputs, INPUT.Size);

                    return AccessibleKeysPressed.Tab;
                }

                // Shift was pressed on entering but it was released and later pressed again
                else if (internalSettings.Shift && _shiftKeyDownOnEntering && _shiftToggled)
                {
                    internalSettings.Shift = false;

                    INPUT inputShift = new INPUT
                    {
                        type = INPUTTYPE.INPUT_KEYBOARD,
                        data = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0x10,
                                dwFlags = (uint)KeyEventF.KeyDown,
                                dwExtraInfo = (UIntPtr)0x5555,
                            },
                        },
                    };

                    INPUT[] inputs = new INPUT[] { inputShift };

                    _ = SendInput(1, inputs, INPUT.Size);

                    return AccessibleKeysPressed.Tab;
                }

                // Shift was pressed on entering and was later released
                else if (!internalSettings.Shift && _shiftKeyDownOnEntering && _shiftToggled)
                {
                    INPUT inputShift = new INPUT
                    {
                        type = INPUTTYPE.INPUT_KEYBOARD,
                        data = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0x10,
                                dwFlags = (uint)KeyEventF.KeyUp,
                                dwExtraInfo = (UIntPtr)0x5555,
                            },
                        },
                    };

                    INPUT[] inputs = new INPUT[] { inputShift };

                    _ = SendInput(1, inputs, INPUT.Size);

                    return AccessibleKeysPressed.Tab;
                }
            }

            return AccessibleKeysPressed.Other;
        }

        private async void Hotkey_KeyUp(int key)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                KeyEventHandler(key, false, 0, string.Empty);
            });
        }

        private bool Hotkey_IsActive()
        {
            return _isActive;
        }

        private void HotkeyTextBox_GettingFocus(object sender, RoutedEventArgs e)
        {
            _shiftKeyDownOnEntering = false;
            _shiftToggled = false;

            if ((GetAsyncKeyState(0x10) & 0x8000) != 0)
            {
                _shiftKeyDownOnEntering = true;
            }

            _isActive = true;
        }

        private void HotkeyTextBox_LosingFocus(object sender, RoutedEventArgs e)
        {
            if (lastValidSettings != null && (lastValidSettings.IsValid() || lastValidSettings.IsEmpty()))
            {
                HotkeySettings = lastValidSettings.Clone();
            }

            HotkeyTextBox.Text = hotkeySettings.ToString();
            _isActive = false;
        }
    }
}
