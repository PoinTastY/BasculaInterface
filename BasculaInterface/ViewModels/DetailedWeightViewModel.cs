﻿using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace BasculaInterface.ViewModels
{
    public class DetailedWeightViewModel : ViewModelBase
    {
        public WeightEntryDto? WeightEntry { get; private set; } = null;
        public ClienteProveedorDto? Partner { get; set; } = null;
        public double TotalWeight => WeightEntry?.WeightDetails?.Sum(d => d.Weight) + WeightEntry?.TareWeight ?? 0;
        public ObservableCollection<WeightEntryDetailRow> WeightEntryDetailRows { get; private set; } = new ObservableCollection<WeightEntryDetailRow>();
        
        private readonly IApiService _apiService = null!;

        public DetailedWeightViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public DetailedWeightViewModel() { }

        public async Task FetchNewWeightDetails()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before fetching new details.");
            }
            if (WeightEntry.Id <= 0)
            {
                throw new InvalidOperationException("WeightEntry.Id must be a valid positive integer.");
            }
            // Fetch the latest weight entry details from the API
            WeightEntryDto? updatedEntry = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/{WeightEntry.Id}");
            if (updatedEntry == null)
            {
                throw new InvalidOperationException("Failed to fetch updated weight entry details.");
            }
            // Update the WeightEntry property and reload products
            WeightEntry = updatedEntry;
            await LoadProductsAsync(WeightEntry, Partner);

            //ifweightentry has partnerid, fetch partner details
            if (WeightEntry.PartnerId.HasValue && WeightEntry.PartnerId.Value > 0)
            {
                Partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/{WeightEntry.PartnerId.Value}");
            }

            OnPropertyChanged(nameof(WeightEntry));
        }

        public async Task ConcludeWeightProcess()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before concluding the weight process.");
            }

            WeightEntry.ConcludeDate = DateTime.Now;

            //TODO: Validate tare + weights equal brute weight, maybe validate this from the API side

            // Send the updated weight entry to the API
            await _apiService.PutAsync<object>("api/Weight", WeightEntry);
        }

        public async Task PrintTicketAsync(string text)
        {
            if (_apiService is not null)
            {
                await _apiService.PostAsync<object>("api/print", text).ConfigureAwait(false);
            }
        }

        public async Task LoadProductsAsync(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null)
        {
            WeightEntry = weightEntry ?? throw new ArgumentNullException(nameof(weightEntry));

            Partner = partner ?? new ClienteProveedorDto { RazonSocial = "Socio no identificado"};

            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before loading products.");
            }

            WeightEntryDetailRows.Clear();

            if (WeightEntry.WeightDetails == null || !WeightEntry.WeightDetails.Any())
            {
                return; // No details to load
            }

            foreach (WeightDetailDto detail in WeightEntry.WeightDetails)
            {
                WeightEntryDetailRow row = new WeightEntryDetailRow
                {
                    Id = detail.Id,
                    Tare = detail.Tare,
                    Weight = detail.Weight,
                };

                if (detail.FK_WeightedProductId is not null)
                {
                    ProductoDto? product = await _apiService.GetAsync<ProductoDto>($"api/Productos/{detail.FK_WeightedProductId}");
                    row.Description = product?.Nombre ?? $"Unknown Product ({detail.FK_WeightedProductId})";
                }
                else
                {
                    row.Description = "Peso Libre";
                }

                WeightEntryDetailRows.Add(row);
            }

            //sort the rows by id, and assign the order index
            WeightEntryDetailRows = new ObservableCollection<WeightEntryDetailRow>(
                WeightEntryDetailRows.OrderBy(row => row.Id).Select((row, index) => 
                {
                    row.OrderIndex = index + 1;
                    return row;
                })
            );

            OnCollectionChanged(nameof(WeightEntryDetailRows));
        }
    }
}
