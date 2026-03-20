using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Binance.Net.Enums;
using ConfluenceAIBot.Models;
using ConfluenceAIBot.Services;
using Skender.Stock.Indicators;

namespace ConfluenceAIBot
{
    public partial class MainPage : ContentPage
    {
        private readonly BinanceDataService _binance;
        private readonly GeminiAnalyzer _gemini;
        private CancellationTokenSource _cts;
        private bool _isScanning;

        public MainPage()
        {
            InitializeComponent();
            _binance = new BinanceDataService();
            _gemini = new GeminiAnalyzer();
            
            // Load key from Android Secure Storage
            ApiKeyBox.Text = Preferences.Default.Get("GeminiApiKey", "");
            TradingStyleCombo.SelectedIndex = 0; // Default to SWING
        }

        private async void StartButton_Clicked(object sender, EventArgs e)
        {
            if (_isScanning) return;

            string apiKey = ApiKeyBox.Text?.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                await DisplayAlert("Error", "Please enter your Gemini API key.", "OK");
                return;
            }

            // Save key to phone
            Preferences.Default.Set("GeminiApiKey", apiKey);
            
            string symbol = SymbolBox.Text?.Trim().ToUpper() ?? "BTCUSDT";
            string style = TradingStyleCombo.SelectedItem?.ToString() ?? "SWING";
            bool useNews = UseNewsCheck.IsChecked;

            await RunScanAsync(symbol, apiKey, style, useNews);
        }

        private async Task RunScanAsync(string symbol, string apiKey, string style, bool useNews)
        {
            _isScanning = true;
            _cts = new CancellationTokenSource();
            
            StartButton.IsEnabled = false;
            LoadingPanel.IsVisible = true;
            VerdictCard.IsVisible = false;
            TechnicalsCard.IsVisible = false;

            try
            {
                StatusText.Text = $"Fetching {symbol} Data & News...";
                
                var task5m   = Task.Run(() => _binance.GetKlinesAsync(symbol, KlineInterval.FiveMinutes, 500, _cts.Token), _cts.Token);
                var task15m  = Task.Run(() => _binance.GetKlinesAsync(symbol, KlineInterval.FifteenMinutes, 500, _cts.Token), _cts.Token);
                var task1h   = Task.Run(() => _binance.GetKlinesAsync(symbol, KlineInterval.OneHour, 500, _cts.Token), _cts.Token);
                var task4h   = Task.Run(() => _binance.GetKlinesAsync(symbol, KlineInterval.FourHour, 500, _cts.Token), _cts.Token);
                var task1d   = Task.Run(() => _binance.GetKlinesAsync(symbol, KlineInterval.OneDay, 500, _cts.Token), _cts.Token);
                var taskNews = useNews ? Task.Run(() => NewsFetcher.GetCryptoNewsAsync(symbol)) : Task.FromResult(new List<string>());

                await Task.WhenAll(task5m, task15m, task1h, task4h, task1d, taskNews);
                _cts.Token.ThrowIfCancellationRequested();

                var ind15m = ComputeIndicators(task15m.Result);
                var ind1h  = ComputeIndicators(task1h.Result);
                var ind4h  = ComputeIndicators(task4h.Result);
                var ind1d  = ComputeIndicators(task1d.Result);

                decimal currentPrice = task5m.Result.Last().Close;
                var gann = GannCalculator.Calculate(currentPrice);
                var smc = SmcAnalyzer.Analyze(task5m.Result);

                var snapshot = new MarketSnapshot
                {
                    Symbol            = symbol,
                    CurrentPrice      = currentPrice,
                    TradingStyle      = style,
                    UseNews           = useNews,
                    RecentNews        = taskNews.Result,
                    Ema200_15m        = ind15m.Ema200,
                    Rsi14_15m         = ind15m.Rsi14,
                    MacdLine_15m      = ind15m.MacdLine,
                    MacdSignal_15m    = ind15m.MacdSignal,
                    MacdHistogram_15m = ind15m.MacdHistogram,
                    Ichimoku_15m      = ind15m.IchimokuCloud,
                    Metrics1H = new TimeframeMetrics { Ema200 = ind1h.Ema200, Rsi14 = ind1h.Rsi14, MacdHistogram = ind1h.MacdHistogram },
                    Metrics4H = new TimeframeMetrics { Ema200 = ind4h.Ema200, Rsi14 = ind4h.Rsi14, MacdHistogram = ind4h.MacdHistogram },
                    Metrics1D = new TimeframeMetrics { Ema200 = ind1d.Ema200, Rsi14 = ind1d.Rsi14, MacdHistogram = ind1d.MacdHistogram },
                    GannLevels        = gann,
                    SmcStructure      = smc,
                    Timestamp         = DateTime.UtcNow
                };

                StatusText.Text = "Gemini AI Analyzing...";
                var signal = await _gemini.AnalyzeAsync(apiKey, snapshot, _cts.Token);

                // Update UI on the Main Thread for Android
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateUI(snapshot, signal);
                    VerdictCard.IsVisible = true;
                    TechnicalsCard.IsVisible = true;
                });

                // Trigger Telegram Alert
                if (signal.ConfidenceScore >= 75 && (signal.IsBullish || signal.IsBearish))
                {
                    await SendTelegramAlert(signal, symbol, currentPrice, style);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Scan Error", ex.Message, "OK");
            }
            finally
            {
                _isScanning = false;
                StartButton.IsEnabled = true;
                LoadingPanel.IsVisible = false;
            }
        }

        private void UpdateUI(MarketSnapshot s, GeminiSignal signal)
        {
            // Update AI Verdict
            BiasText.Text = signal.Bias.ToUpper();
            if (signal.IsBullish) { BiasText.TextColor = Color.FromArgb("#3FB950"); BiasBadge.Stroke = Color.FromArgb("#3FB950"); }
            else if (signal.IsBearish) { BiasText.TextColor = Color.FromArgb("#F85149"); BiasBadge.Stroke = Color.FromArgb("#F85149"); }
            else if (signal.IsWait) { BiasText.TextColor = Color.FromArgb("#8B949E"); BiasBadge.Stroke = Color.FromArgb("#484F58"); }
            else { BiasText.TextColor = Color.FromArgb("#D29922"); BiasBadge.Stroke = Color.FromArgb("#D29922"); }

            SetupTypeText.Text = signal.SetupType;
            EntryZoneText.Text = signal.EntryZone;
            TP1Text.Text = signal.TakeProfit1;
            TP2Text.Text = signal.TakeProfit2;
            TP3Text.Text = signal.TakeProfit3;
            SLText.Text = signal.StopLoss;

            // Update Technicals Card
            CurrentPriceText.Text = $"${s.CurrentPrice:N2}";
            Ema200Text.Text = $"${s.Ema200_15m:N2}";
            RsiValueText.Text = $"{s.Rsi14_15m:F2}";
            MacdHistText.Text = $"{s.MacdHistogram_15m:F4}";
            StructureText.Text = s.SmcStructure.MarketStructure;
            GannBaseText.Text = $"${s.GannLevels.GannBase:N2}";

            if (s.CurrentPrice > s.Ichimoku_15m.SenkouSpanA && s.CurrentPrice > s.Ichimoku_15m.SenkouSpanB)
                IchiStatusText.Text = "Above Cloud (Bull)";
            else if (s.CurrentPrice < s.Ichimoku_15m.SenkouSpanA && s.CurrentPrice < s.Ichimoku_15m.SenkouSpanB)
                IchiStatusText.Text = "Below Cloud (Bear)";
            else
                IchiStatusText.Text = "Inside Cloud";
        }

        private record IndicatorSet(decimal Ema200, double Rsi14, double MacdLine, double MacdSignal, double MacdHistogram, IchimokuCloudData IchimokuCloud);

        private static IndicatorSet ComputeIndicators(List<CandleData> candles)
        {
            var quotes = candles.Select(c => new Quote { Date = c.OpenTime, Open = c.Open, High = c.High, Low = c.Low, Close = c.Close, Volume = c.Volume }).ToList();
            var emaRaw  = quotes.GetEma(200).LastOrDefault(r => r.Ema.HasValue)?.Ema ?? 0;
            var rsi14   = quotes.GetRsi(14).LastOrDefault(r => r.Rsi.HasValue)?.Rsi ?? 50;
            var lastMacd = quotes.GetMacd(12, 26, 9).LastOrDefault(r => r.Macd.HasValue);

            var ichiList = quotes.GetIchimoku(9, 26, 52).ToList();
            var currentIchi = ichiList.LastOrDefault(x => x.Date == quotes.Last().Date);
            var ichimokuData = new IchimokuCloudData
            {
                TenkanSen   = currentIchi?.TenkanSen ?? 0m, KijunSen = currentIchi?.KijunSen ?? 0m,
                SenkouSpanA = currentIchi?.SenkouSpanA ?? 0m, SenkouSpanB = currentIchi?.SenkouSpanB ?? 0m
            };

            return new IndicatorSet((decimal)emaRaw, rsi14, lastMacd?.Macd ?? 0, lastMacd?.Signal ?? 0, lastMacd?.Histogram ?? 0, ichimokuData);
        }

        private async Task SendTelegramAlert(GeminiSignal signal, string symbol, decimal price, string tradingStyle)
        {
            string botToken = "YOUR_BOT_TOKEN"; 
            string chatId = "YOUR_CHAT_ID";
            if (botToken == "YOUR_BOT_TOKEN") return;

            string emoji = signal.IsBullish ? "🟢" : "🔴";
            string message = $"{emoji} *MOBILE AI ALERT* {emoji}\n\n*Pair:* {symbol}\n*Mode:* {tradingStyle}\n*Live Price:* ${price:N2}\n*Bias:* {signal.Bias} ({signal.ConfidenceScore}%)\n\n*Setup:* {signal.SetupType}\n*Entry:* {signal.EntryZone}\n*TP1:* {signal.TakeProfit1}\n*TP2:* {signal.TakeProfit2}\n*TP3:* {signal.TakeProfit3}\n*SL:* {signal.StopLoss}";
            string url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";
            try { using var http = new System.Net.Http.HttpClient(); await http.GetAsync(url); } catch { }
        }
    }
}