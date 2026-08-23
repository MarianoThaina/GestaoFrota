using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class CategoriasViewModel : BaseViewModel
{
    private readonly ICategoriaService _categoriaService;
    private readonly IAuthService _authService;

    public ObservableCollection<Categoria> Categorias { get; } = new();

    [ObservableProperty]
    private string novaCategoriaNome = string.Empty;

    [ObservableProperty]
    private TipoCategoria novaCategoriaTipo = TipoCategoria.Despesa;

    /// <summary>
    /// Controla a visibilidade do formulário de criação — apenas
    /// Administrador e Financeiro podem cadastrar categorias (RBAC).
    /// </summary>
    [ObservableProperty]
    private bool podeGerenciar;

    public CategoriasViewModel(ICategoriaService categoriaService, IAuthService authService)
    {
        _categoriaService = categoriaService;
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

            var categorias = await _categoriaService.ListarAsync();

            Categorias.Clear();
            foreach (var categoria in categorias)
            {
                Categorias.Add(categoria);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível carregar as categorias.";
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
        if (string.IsNullOrWhiteSpace(NovaCategoriaNome))
        {
            return;
        }

        try
        {
            var categoria = await _categoriaService.CriarAsync(NovaCategoriaNome.Trim(), NovaCategoriaTipo);
            Categorias.Add(categoria);
            NovaCategoriaNome = string.Empty;
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível criar a categoria.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
