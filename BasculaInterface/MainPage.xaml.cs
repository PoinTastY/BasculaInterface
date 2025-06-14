﻿using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage
    {
        public double Tara { get; set; }

        private readonly BasculaViewModel _viewModel;

        private CancellationTokenSource _cancellationTokenSource = null!;

        public MainPage(BasculaViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            _viewModel = viewModel;

            this.SizeChanged += AdjustFontSize;
        }

        private void AdjustFontSize(object? sender = null, EventArgs? e = null)
        {
            var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            _viewModel.FontSizePeso = screenWidth * 0.09;
            _viewModel.FontSizeTara = screenWidth * 0.04;
        }

        public MainPage() : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>())
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AdjustFontSize();
            
            await _viewModel.ConnectSocket(MauiProgram.BasculaSocketUrl);
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.DisconnectSocket();
        }

        private void OnTaraClicked(object sender, EventArgs e)
        {
            var tara = double.Parse(_viewModel.Peso);

            _viewModel.TaraValue = tara;

            _viewModel.Tara = tara.ToString("F2");
        }

        private async void OnImprimirClicked(object sender, EventArgs e)
        {
            
            string fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");

            string template = MauiProgram.PrintTemplate;
            string ticket = template
                .Replace("{fechaHora}", fechaHora)
                .Replace("{tara}", _viewModel.Tara)
                .Replace("{neto}", _viewModel.Peso)
                .Replace("{bruto}", _viewModel.Diferencia);

            await _viewModel.PrintTicketAsync(ticket);

            EscribirLog($"Tara: {_viewModel.Tara} kg | " +
                    $"Neto: {_viewModel.Peso} | " +
                    $"Diferencia: {_viewModel.Diferencia} kg");

        }

        private void EscribirLog(string mensaje)
        {
            string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            string logPath = Path.Combine(logDir, "log.txt");

            // Crear carpeta si no existe
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Abrir, escribir y cerrar el archivo inmediatamente
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] {mensaje}");
            }
        }


        private async void BtnReconect_Clicked(object sender, EventArgs e)
        {
            try
            {

                var popup = new WaitPopUp();

                this.ShowPopup(popup);
                try
                {
                    await _viewModel.DisconnectSocket();
                    await _viewModel.ConnectSocket(MauiProgram.BasculaSocketUrl);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Error al reconectar a la báscula: " + ex.Message, "OK");
                }
                finally
                {
                    popup.Close();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error inesperado: " + ex.Message, "OK");
            }
        }

        private void Cero_Pressed(object sender, EventArgs e)
        {
            _cancellationTokenSource = new();

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(4000, _cancellationTokenSource.Token);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        
                        SetHostPopUp popup = new (MauiProgram.BasculaSocketUrl);

                        popup.OnHostSet += async (host) =>
                        {
                            await OnHostSet(host, popup);
                        };

                        this.ShowPopup(popup);
                    });
                }
                catch (TaskCanceledException)
                {
                    // La tarea fue cancelada, no hacer nada
                }
                catch (Exception ex)
                {
                    // Manejar cualquier otra excepción
                    await DisplayAlert("Error", "Error al tratar de cambiar e host: " + ex.Message, "OK");
                }
            });
        }

        private async Task OnHostSet(string host, SetHostPopUp popup)
        {
            WaitPopUp waitPopUp = new();

            this.ShowPopup(waitPopUp);
            try
            {
                MauiProgram.BasculaSocketUrl = host;
                await _viewModel.ConnectSocket(host);
                popup.Close();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al conectar a la báscula: " + ex.Message, "OK");
            }
            finally
            {
                waitPopUp.Close();
            }
        }

        private void Cero_Released(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _viewModel.TaraValue = 0;
            _viewModel.Tara = "0.00";
            _viewModel.Diferencia = "0.00 ";
        }
    }

}
