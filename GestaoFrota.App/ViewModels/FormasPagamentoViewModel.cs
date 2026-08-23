using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class FormasPagamentoViewModel : BaseViewModel
{
    private readonly IFormaPagamentoService _formaPagamentoService;
    private readonly IAuthService _authService;

    public ObservableCollection<FormaPagamento> Formas { get; } = new();

    [ObservableProperty]
    private string novaFormaNome = string.Empty;

    [ObservableProperty]
    private bool podeGerenciar;

    public FormasPagamentoViewModel(IFormaPagamentoService formaPagamentoService, IAuthService authService)
    {
        _formaPagamentoService = formaPagamentoService;
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
            PodeGerenciar = usuario?.Perfil is Models.PerfilUsuario.Administrador or Models.PerfilUsuario.Financeiro;

            var formas = await _formaPagamentoService.ListarAsync();

            Formas.Clear();
            foreach (var forma in formas)
            {
                Formas.Add(forma);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível carregar as formas de pagamento.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task AdicionarAsync()
    {
        if (string.IsNullOrWhiteSpace(NovaFormaNome))
        {
            return;
        }

        try
        {
            var forma = await _formaPagamentoService.CriarAsync(NovaFormaNome.Trim());
            Formas.Add(forma);
            NovaFormaNome = string.Empty;
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível criar a forma de pagamento.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
