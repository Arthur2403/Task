using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TextAnalyzerWPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationSource;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private bool _isRunning = false;
        private bool _isPaused = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonStartPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                if (!IsAnyAnalysisOptionSelected())
                {
                    MessageBox.Show("Оберіть хоча б один пункт для аналізу!", "Попередження",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IsAnyOutputOptionSelected())
                {
                    MessageBox.Show("Оберіть хоча б один спосіб виводу результату!", "Попередження",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _isRunning = true;
                _isPaused = false;
                ButtonStartPause.Content = "Пауза";
                _cancellationSource = new CancellationTokenSource();

                try
                {
                    await RunAnalysisAsync(_cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    TextBoxOutput.Text = "Аналіз перервано.";
                }
                finally
                {
                    _isRunning = false;
                    ButtonStartPause.Content = "Старт";
                }
            }
            else if (!_isPaused)
            {
                _pauseEvent.Reset();
                _isPaused = true;
                ButtonStartPause.Content = "Продовжити";
            }
            else
            {
                _pauseEvent.Set();
                _isPaused = false;
                ButtonStartPause.Content = "Пауза";
            }
        }

        private async Task RunAnalysisAsync(CancellationToken token)
        {
            string text = TextBoxInput.Text;
            TextBoxOutput.Clear();

            bool analyzeChars = CheckBoxCharacterCount.IsChecked == true;
            bool analyzeWords = CheckBoxWordCount.IsChecked == true;
            bool analyzeSentences = CheckBoxSentenceCount.IsChecked == true;
            bool analyzeQuestions = CheckBoxQuestionCount.IsChecked == true;
            bool analyzeExclaims = CheckBoxExclamationCount.IsChecked == true;

            bool outputToScreen = CheckBoxOutputToScreen.IsChecked == true;
            bool outputToFile = CheckBoxOutputToFile.IsChecked == true;

            string result = await Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    _pauseEvent.Wait();
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(1000, token);
                }

                return AnalyzeText(text, analyzeChars, analyzeWords, analyzeSentences, analyzeQuestions, analyzeExclaims);
            }, token);

            if (outputToScreen)
            {
                TextBoxOutput.Text = result;
            }

            if (outputToFile)
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "TextAnalysis.txt"
                );
                await File.WriteAllTextAsync(path, result);
                MessageBox.Show($"Результат записано у файл:\n{path}", "Успішно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string AnalyzeText(
            string text,
            bool includeCharCount,
            bool includeWordCount,
            bool includeSentenceCount,
            bool includeQuestionCount,
            bool includeExclaimCount)
        {
            int charCount = text.Length;
            int wordCount = Regex.Matches(text, @"\b\w+\b").Count;
            int sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
            int questionCount = Regex.Matches(text, @"\?").Count;
            int exclaimCount = Regex.Matches(text, @"!").Count;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Результати аналізу:");
            sb.AppendLine();

            if (includeCharCount) sb.AppendLine($"Символів: {charCount}");
            if (includeWordCount) sb.AppendLine($"Слів: {wordCount}");
            if (includeSentenceCount) sb.AppendLine($"Речень: {sentenceCount}");
            if (includeQuestionCount) sb.AppendLine($"Питальних речень: {questionCount}");
            if (includeExclaimCount) sb.AppendLine($"Окличних речень: {exclaimCount}");

            return sb.ToString();
        }

        private bool IsAnyAnalysisOptionSelected()
        {
            return CheckBoxCharacterCount.IsChecked == true ||
                   CheckBoxWordCount.IsChecked == true ||
                   CheckBoxSentenceCount.IsChecked == true ||
                   CheckBoxQuestionCount.IsChecked == true ||
                   CheckBoxExclamationCount.IsChecked == true;
        }

        private bool IsAnyOutputOptionSelected()
        {
            return CheckBoxOutputToScreen.IsChecked == true ||
                   CheckBoxOutputToFile.IsChecked == true;
        }
    }
}
