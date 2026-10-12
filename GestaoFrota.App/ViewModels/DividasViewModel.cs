using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class DividasViewModel : BaseViewModel
{
    private readonly IDividaService _dividaService;
    private readonly IAuthService _authService;

    public ObservableCollection<Divida> Dividas { get; } = new();

    [ObservableProperty]
    private bool podeGerenciar;

    public DividasViewModel(IDividaService dividaService, IAuthService authService)
    {
        _dividaService = dividaService;
        _authService = authService;
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuario = await _authService.ObterUsuarioLogadoAsync();
            PodeGerenciar = usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Financeiro;

            var dividas = await _dividaService.ListarAsync();

            Dividas.Clear();
            foreach (var divida in dividas)
            {
                Dividas.Add(divida);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar as dívidas.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task NovaAsync() => await Shell.Current.GoToAsync(Rotas.DividaForm);

    [RelayCommand]
    private async Task AbrirAsync(Divida divida)
        => await Shell.Current.GoToAsync($"{Rotas.DividaDetalhe}?id={divida.Id}");
}
