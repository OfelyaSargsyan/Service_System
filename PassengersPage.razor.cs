using BLSShared.Models;
using BLSUI.Components.Dialogs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using NLog;
using System.Security.Claims;
using System.Text.Json;

namespace BLSUI.Components.Pages;

public partial class PassengersPage : IAsyncDisposable
{
	private PaginationState Pagination { get; set; } = new() { ItemsPerPage = 20 };
	private IQueryable<Passenger> passengers = new List<Passenger>().AsQueryable();
	private Settings settings = new();
	private Dictionary<string, FieldSetting> GridSettingsMap { get; set; } = [];
	private FilterDto Filter { get; set; } = new();
	private bool IsLoading = false;
    private Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    protected override async Task OnInitializedAsync()
	{
		IsLoading = true;

		var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		var userId = authState?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

		SignalRService.ToastCallback = ShowToast;
		await SignalRService.StartAsync();

		await LoadPassengersAsync();
		var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
		settings = await httpClient.GetFromJsonAsync<Settings>($"/settings/{userId}") ?? new Settings();
		GridSettingsMap = JsonSerializer.Deserialize<Dictionary<string, FieldSetting>>(settings.GridSettingsJson) ?? [];

		IsLoading = false;
	}

	public async ValueTask DisposeAsync() => await SignalRService.StopAsync();

	private async Task ApplyFilterAsync() => await LoadPassengersWithProgressAsync();

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
		{
			await LoadPassengersWithProgressAsync();
		}
	}

	private async Task ClearFilterAsync()
	{
		Filter = new();
		await LoadPassengersWithProgressAsync();
	}

	private void ShowToast(Notify notify)
	{
		var passenger = JsonSerializer.Deserialize<Passenger>(notify.Message);
		ToastService.ShowCommunicationToast(new ToastParameters<CommunicationToastContent>
		{
			Intent = ToastIntent.Success,
			Title = "Passenger Expired",
			Content = new CommunicationToastContent
			{
				Subtitle = "User evaluate",
				Details = $"{passenger.NameSurname} BLS has timed out",
			},
		});
	}

	private async Task GoToPageAsync(int pageIndex)
		=> await LoadPassengersWithProgressAsync(pageIndex);

	private Appearance PageButtonAppearance(int pageIndex)
		=> Pagination.CurrentPageIndex == pageIndex ? Appearance.Accent : Appearance.Neutral;

	private string? AriaCurrentValue(int pageIndex)
		=> Pagination.CurrentPageIndex == pageIndex ? "page" : null;

	private static string AriaLabel(int pageIndex) => $"Go to page {pageIndex}";

	private async Task LoadPassengersAsync(int page = 0)
	{
		IsLoading = true;
		var pagedFilter = new PagedFilterDto
		{
			Filter = Filter,
			PageNumber = page > 0 ? page : 1,
			PageSize = Pagination.ItemsPerPage,
			SortColumn = "row_id"
		};

		var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
		var response = await httpClient.PostAsJsonAsync("/passengers/paged", pagedFilter);
		if (response.IsSuccessStatusCode)
		{
			var pagedDto = await response.Content.ReadFromJsonAsync<PagedDto>();
			if (pagedDto != null)
			{
				passengers = pagedDto.Items.AsQueryable();
				foreach (var p in passengers)
				{
					p.ShowDetails = p.Prolongation == 0;
				}

				await Pagination.SetCurrentPageIndexAsync(pagedFilter.PageNumber);
				await Pagination.SetTotalItemCountAsync((int)pagedDto.TotalCount);
			}
		}
		IsLoading = false;
	}

	private async Task LoadPassengersWithProgressAsync(int page = 0)
	{
		IsLoading = true;
		await LoadPassengersAsync(page);
		IsLoading = false;
	}

    private async Task HandleRowDoubleClickAsync(FluentDataGridRow<Passenger> row)
   {
        try
        {
            var passenger = row.Item;
            var parameters = new DialogParameters
            {
                Title = "Update Data",
                PrimaryAction = "Yes",
                PrimaryActionEnabled = false,
                SecondaryAction = "No",
                Width = "500px",
                Modal = true,
                PreventScroll = true
            };

            var dialog = passenger?.Prolongation == 0
                ? await DialogService.ShowDialogAsync<AddEditPassengerDialog>(passenger, parameters)
                : await DialogService.ShowDialogAsync<AddEditProlongationDialog>(passenger.GetProlongationDto(), parameters);

            var result = await dialog.Result;
            if (result.Data is Passenger updatedPassenger)
            {
                var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
                await httpClient.PutAsJsonAsync("/passengers/update", updatedPassenger);
            }
            else if (result.Data is ProlongationDto updatedProlongation)
            {
                var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
                await httpClient.PutAsJsonAsync("/passengers/updateprolong", updatedProlongation);
            }

            await LoadPassengersWithProgressAsync();
        }
        catch(Exception ex)
        {
			Logger.Error($"Error loading flights: {ex.Message}");
		}
    }

	private async Task DeletePassengerAsync()
	{
		var parameters = new DialogParameters
		{
			Title = "Confirm Delete",
			PrimaryAction = "Yes",
			SecondaryAction = "No",
			Width = "500px",
			Modal = true,
			PreventScroll = true
		};

		var dialog = await DialogService.ShowDialogAsync<ConfirmDeleteDialog>(parameters);
		var result = await dialog.Result;
		if (result.Data is bool isConfirmed && isConfirmed)
		{
			var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
			var deleteTasks = passengers.Where(p => p.IsSelected).Select(p => httpClient.DeleteAsync($"/passengers/deletebyrow/{p.RowId}"));
			await Task.WhenAll(deleteTasks);
		}
		await dialog.CloseAsync();
		await LoadPassengersWithProgressAsync();
	}

	protected async Task AddPassengerAsync()
	{
		var model = new Passenger { ServiceType = "Cash Payment" };
		var parameters = new DialogParameters
		{
			Title = "Add Service",
			PrimaryAction = "Yes",
			PrimaryActionEnabled = false,
			SecondaryAction = "No",
			Width = "500px",
			Modal = true,
			PreventScroll = true
		};

		var dialog = await DialogService.ShowDialogAsync<AddEditPassengerDialog>(model, parameters);
		var result = await dialog.Result;
		if (result.Data is Passenger passenger)
		{
			var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
			await httpClient.PostAsJsonAsync(URLInfo.Passengers.Add, passenger);
		}

		await dialog.CloseAsync();
		await LoadPassengersWithProgressAsync();
	}

	private async Task EditPassengerAsync(Passenger passenger)
	{
		var parameters = new DialogParameters
		{
			Title = "Update Data",
			PrimaryAction = "Yes",
			PrimaryActionEnabled = false,
			SecondaryAction = "No",
			Width = "500px",
			Modal = true,
			PreventScroll = true
		};

		var dialog = await DialogService.ShowDialogAsync<AddEditPassengerDialog>(passenger, parameters);
		var result = await dialog.Result;
		if (result.Data is Passenger updatedPassenger)
		{
			var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
			await httpClient.PutAsJsonAsync("/passengers/update", updatedPassenger);
		}
		await LoadPassengersWithProgressAsync();
	}

	private async Task EditProlongationAsync(ProlongationDto passenger)
	{
		var parameters = new DialogParameters
		{
			Title = "Update Data",
			PrimaryAction = "Yes",
			PrimaryActionEnabled = false,
			SecondaryAction = "No",
			Width = "500px",
			Modal = true,
			PreventScroll = true
		};

		var dialog = await DialogService.ShowDialogAsync<AddEditProlongationDialog>(passenger, parameters);
		var result = await dialog.Result;
		if (result.Data is ProlongationDto updatedPassenger)
		{
			var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
			await httpClient.PutAsJsonAsync("/passengers/updateprolong", updatedPassenger);
		}

		await LoadPassengersWithProgressAsync();
	}

	private async Task AddProlongationAsync(Guid userId)
	{
		var parameters = new DialogParameters
		{
			Title = "Prolongation",
			PrimaryAction = "Yes",
			PrimaryActionEnabled = false,
			SecondaryAction = "No",
			Width = "500px",
			Modal = true,
			PreventScroll = true
		};

		var dialog = await DialogService.ShowDialogAsync<AddEditProlongationDialog>(new ProlongationDto { UserId = userId }, parameters);
		var result = await dialog.Result;
		if (result.Data is ProlongationDto prolongationDto)
		{
			var httpClient = HttpClientFactory.CreateClient(Consts.HttpClientName);
			var response = await httpClient.PostAsJsonAsync("/passengers/prolongation", prolongationDto);
			if (response.IsSuccessStatusCode)
			{
				var passenger = passengers.FirstOrDefault(p => p.UserId == userId);
				passenger?.Prolongations.Add(prolongationDto);
				await LoadPassengersWithProgressAsync();
			}
		}
	}

	private void ToggleDetails(Passenger passenger)
	{
		var detailedPassengers = passengers.Where(p => p.UserId == passenger.UserId && p.Prolongation > 0);
		foreach (var p in detailedPassengers)
		{
			p.ShowDetails = !p.ShowDetails;
		}
	}

	private bool IsVisible(string fieldLabel)
	{
		if (string.IsNullOrEmpty(fieldLabel) || GridSettingsMap == null)
		{
			return true;
		}

		return !GridSettingsMap.TryGetValue(fieldLabel, out var fieldSetting) || fieldSetting.ShowInGrid;
	}
}
