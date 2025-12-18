using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls; // ContextMenu ve ListBox için gerekli
using Microsoft.Win32;

namespace JSONFileExplorer
{
    public partial class MainWindow : Window
    {
        private HashSet<string> databaseIds = new HashSet<string>();
        private string currentFilePath = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        // --- 1. DOSYA YÜKLEME ---
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Hem JSON hem TXT hem de uzantısız dosyaları görebilmek için filtre:
            openFileDialog.Filter = "Tüm Dosyalar (*.*)|*.*|JSON Dosyaları (*.json)|*.json|Metin Dosyaları (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                LoadData(currentFilePath);
            }
        }

        private void LoadData(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                databaseIds.Clear();

                try
                {
                    // JSON formatı denemesi
                    var tempDict = JsonSerializer.Deserialize<Dictionary<string, bool>>(content);
                    if (tempDict != null)
                    {
                        foreach (var key in tempDict.Keys)
                        {
                            databaseIds.Add(key);
                        }
                    }
                }
                catch
                {
                    // Düz yazı formatı denemesi
                    string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string cleanId = line.Trim().Replace("\"", "").Replace(",", "");
                        if (!string.IsNullOrWhiteSpace(cleanId))
                        {
                            databaseIds.Add(cleanId);
                        }
                    }
                }

                UpdateUI();
                lblStatus.Text = $"✅ Yüklendi: {databaseIds.Count} adet kayıt.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        // --- 2. ARAYÜZ GÜNCELLEME ---
        private void UpdateUI()
        {
            lstCurrentIds.ItemsSource = null;
            // Tüm listeyi göster (Take(100) kaldırdık)
            lstCurrentIds.ItemsSource = databaseIds.ToList();
            ((System.Windows.Controls.GroupBox)lstCurrentIds.Parent).Header = $"Mevcut İçerik (Toplam: {databaseIds.Count})";
        }

        // --- 3. İŞLE VE KAYDET ---
        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                MessageBox.Show("Lütfen önce bir dosya yükleyin!");
                return;
            }

            string rawInput = txtBulkInput.Text;
            string[] newLines = rawInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int addedCount = 0;
            int duplicateCount = 0;

            foreach (var line in newLines)
            {
                string idToCheck = line.Trim().Replace("\"", "").Replace(",", "");

                if (string.IsNullOrWhiteSpace(idToCheck)) continue;

                if (databaseIds.Contains(idToCheck))
                {
                    duplicateCount++;
                }
                else
                {
                    databaseIds.Add(idToCheck);
                    addedCount++;
                }
            }

            SaveDatabase();

            txtBulkInput.Clear();
            UpdateUI();

            MessageBox.Show($"EKLENEN: {addedCount}\nZATEN VARDI: {duplicateCount}\nTOPLAM: {databaseIds.Count}", "İşlem Tamam");

            lblStatus.Text = "Kayıt başarılı.";
        }

        private void SaveDatabase()
        {
            // Dosyanın uzantısını kontrol et (.json mu?)
            string extension = Path.GetExtension(currentFilePath).ToLower();

            // DURUM 1: Eğer dosya bir JSON ise, eski formatı koru (Roblox Table Formatı)
            if (extension == ".json")
            {
                var exportDict = new Dictionary<string, bool>();
                foreach (var id in databaseIds)
                {
                    exportDict[id] = true;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonOutput = JsonSerializer.Serialize(exportDict, options);
                File.WriteAllText(currentFilePath, jsonOutput);
            }
            // DURUM 2: JSON değilse (txt veya uzantısız BLScriptsData gibi), DÜZ METİN kaydet
            else
            {
                // HashSet içindeki tüm ID'leri alt alta yaz
                File.WriteAllLines(currentFilePath, databaseIds);
            }
        }

        // --- YENİ EKLENEN ÖZELLİKLER (SİLME & KOPYALAMA) ---

        // Ortak Silme Fonksiyonu
        private void RemoveSelectedItems()
        {
            if (lstCurrentIds.SelectedItems.Count == 0) return;

            // Seçilenleri listeye al
            var itemsToRemove = lstCurrentIds.SelectedItems.Cast<string>().ToList();

            int deletedCount = 0;
            foreach (var id in itemsToRemove)
            {
                if (databaseIds.Contains(id))
                {
                    databaseIds.Remove(id);
                    deletedCount++;
                }
            }

            UpdateUI();
            lblStatus.Text = $"🗑️ {deletedCount} adet kayıt silindi.";

            // Değişikliği anında kaydetmek istersen burayı aç:
            // SaveDatabase(); 
        }

        // Klavye Tuşuna Basınca (Delete Tuşu)
        private void LstCurrentIds_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                RemoveSelectedItems();
            }
        }

        // Sağ Tık Menüsü: Sil
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedItems();
        }

        // Sağ Tık Menüsü: Kopyala
        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (lstCurrentIds.SelectedItems.Count == 0) return;

            var selectedList = lstCurrentIds.SelectedItems.Cast<string>();
            string clipboardText = string.Join(Environment.NewLine, selectedList);

            Clipboard.SetText(clipboardText);
            lblStatus.Text = "📋 Seçilenler kopyalandı.";
        }
    }
}