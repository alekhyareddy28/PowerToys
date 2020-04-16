using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SearchBox.UI
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        // Boolean to keep track of whether or not you should ignore the next TextChanged event.  
        // This is needed to support the correct behavior when backspace is tapped.
        public bool m_ignoreNextTextChanged = false;

        // Sample list of strings to use in the autocomplete.
        public string[] m_options = { "Apple", "Banana", "Caramel", "Donut" };

        private void textBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            string s = ForeGroundTextBox.Text;
            // Needed for the backspace scenario.
            if (s.Length > 0)
            {
                bool flag = false;
                for (int i = 0; i < m_options.Length; i++)
                {
                    if (m_options[i].IndexOf(s) >= 0)
                    {
                        if (s == m_options[i])
                            break;

                        flag = true;
                        BackGroundTextBox.Text = m_options[i];
                        break;
                    }
                }

                if(!flag)
                {
                    BackGroundTextBox.Text = String.Empty;
                }
            }
            else
            {
                BackGroundTextBox.Text = String.Empty;
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Back
                || e.Key == Windows.System.VirtualKey.Delete)
            {
                m_ignoreNextTextChanged = true;
            }
            base.OnKeyDown(e);
        }
    }
}
