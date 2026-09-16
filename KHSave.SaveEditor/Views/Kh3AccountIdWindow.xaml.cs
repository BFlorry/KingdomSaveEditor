/*
    Kingdom Save Editor
    Copyright (C) 2026 Mikko Mäntylä (BFlorry)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Windows;
using KHSave.Lib3;

namespace KHSave.SaveEditor.Views
{
    /// <summary>
    /// Asks the user for the Steam / Epic account ID used to encrypt a KH3 PC save.
    /// </summary>
    public partial class Kh3AccountIdWindow : Window
    {
        public Kh3AccountIdWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += (s, e) => AccountIdTextBox.Focus();
        }

        public string AccountId { get; set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            AccountId = AccountId?.Trim();
            if (!SaveKh3PcCrypto.IsValidAccountId(AccountId))
            {
                MessageBox.Show(this,
                    "The account ID must not be empty and may only contain letters, digits, '-' and '_'.",
                    "Invalid account ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}
