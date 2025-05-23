﻿using System.Text;
using System.Text.Json;
using BasculaInterface.ViewModels.Base;
using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaInterface.ViewModels
{
    public class BasculaViewModel : ViewModelBase
    {
        private double _fontSizePeso = 120;
        public double FontSizePeso
        {
            get => _fontSizePeso;
            set 
            {
                _fontSizePeso = value;
                OnPropertyChanged();
            }
        }
        private double _fontSizeTara;
        public double FontSizeTara
        {
            get => _fontSizeTara;
            set
            {
                _fontSizeTara = value;
                OnPropertyChanged();
            }
        }
        private string _peso = "0.00";
        public string Peso
        {
            get => _peso;
            set
            {
                if (_peso != value)
                {
                    _peso = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tara = "0.00";
        public string Tara
        {
            get => _tara;
            set
            {
                if (_tara != value)
                {
                    _tara = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _diferencia = "0.00";
        public string Diferencia
        {
            get => _diferencia;
            set
            {
                if (_diferencia != value)
                {
                    _diferencia = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _estado = "Desconectado";
        public string Estado
        {
            get => _estado;
            private set
            {
                if (_estado != value)
                {
                    _estado = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TaraValue { get; set; }

        private HubConnection? _basculaSocketHub;

        event Action<double>? OnWeightReceived;

        private HttpClient? _httpClient;

        public BasculaViewModel() { }

        public async Task ConnectSocket(string socketUrl)
        {
            try
            {
                _basculaSocketHub = new HubConnectionBuilder()
                .WithUrl(socketUrl + "basculaSocket")
                .Build();

                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(socketUrl)
                };

                _basculaSocketHub.On<double>("ReceiveNumber", data =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnWeightReceived?.Invoke(data);
                    });
                });

                OnWeightReceived += UpdateWeight;

                await _basculaSocketHub.StartAsync();
                Estado = "Conectado";
            }
            catch (Exception ex)
            {
                Estado = $"Error: {socketUrl}: " + ex.Message;
            }
        }

        private void UpdateWeight(double data)
        {
            if (TaraValue != 0)
                Diferencia = Math.Abs(TaraValue - data).ToString("0.00");

            Peso = data.ToString("0.00");
        }

        public async Task PrintTicketAsync(string text)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(text);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                if (_httpClient is not null)
                {
                    var response = await _httpClient.PostAsync("api/print", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Estado = "Ticket enviado a la impresora";
                    }
                    else
                    {
                        Estado = "Error al enviar el ultimo ticket: " + response.ReasonPhrase;
                    }

                    await Task.Delay(5000);

                    Estado = "Conectado";
                }
                else
                {
                    Estado = "Error: porfavor, presiona Reconectar";
                }
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
        }

        public async Task DisconnectSocket()
        {
            try
            {
                if (_basculaSocketHub != null)
                {
                    await _basculaSocketHub.StopAsync();
                    await _basculaSocketHub.DisposeAsync();
                    _basculaSocketHub = null;
                }

                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }

                Estado = "Desconectado";
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
        }
    }
}
