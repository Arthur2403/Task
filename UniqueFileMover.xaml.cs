using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace UniqueFileMover
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
                SourcePathBox.Text = dialog.SelectedPath;
        }

        private void BrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DestinationPathBox.Text = dialog.SelectedPath;
        }

        private async void MoveFiles_Click(object sender, RoutedEventArgs e)
        {
            string source = SourcePathBox.Text.Trim();
            string destination = DestinationPathBox.Text.Trim();

            if (!Directory.Exists(source))
            {
                MessageBox.Show("Директорія джерела не існує!");
                return;
            }
            if (!Directory.Exists(destination))
            {
                MessageBox.Show("Директорія приймача не існує!");
                return;
            }
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Директорії не можуть співпадати!");
                return;
            }

            OutputBox.Clear();
            OutputBox.AppendText("Аналіз файлів...\n");

            try
            {
                await Task.Run(() => MoveUniqueFiles(source, destination));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void MoveUniqueFiles(string sourceDir, string destDir)
        {
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            int total = files.Length;
            int duplicates = 0;
            int moved = 0;

            var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                try
                {
                    string hash = GetFileHash(file);
                    if (!seenHashes.Add(hash))
                    {
                        duplicates++;
                        continue;
                    }

                    string destPath = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destPath, true);
                    moved++;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        OutputBox.AppendText($"Помилка з файлом {Path.GetFileName(file)}: {ex.Message}\n"));
                }
            }

            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText($"\nУсього файлів знайдено: {total}\n");
                OutputBox.AppendText($"Дублікатів: {duplicates}\n");
                OutputBox.AppendText($"Переміщено оригінальних файлів: {moved}\n");
                OutputBox.AppendText("\nГотово");
            });
        }

        private static string GetFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
