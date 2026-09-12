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

    /// <summary>
    /// Só o Administrador pode desativar (DELETE /categorias/{id} exige
    /// Roles = Perfis.Administrador na API) — Financeiro pode criar/editar
    /// mas não desativar.
    /// </summary>
    [ObservableProperty]
    private bool podeDesativar;

    /// <summary>Categoria em edição no momento (null quando nenhuma está sendo editada).</summary>
    [ObservableProperty]
    private Categoria? categoriaEmEdicao;

    [ObservableProperty]
    private string edicaoNome = string.Empty;

    [ObservableProperty]
    private TipoCategoria edicaoTipo;

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
            PodeDesativar = usuario?.Perfil is Models.PerfilUsuario.Administrador;

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

    [RelayCommand]
    private void IniciarEdicao(Categoria categoria)
    {
        CategoriaEmEdicao = categoria;
        EdicaoNome = categoria.Nome;
        EdicaoTipo = categoria.Tipo;
    }

    [RelayCommand]
    private void CancelarEdicao()
    {
        CategoriaEmEdicao = null;
        EdicaoNome = string.Empty;
    }

    [RelayCommand]
    private async Task SalvarEdicaoAsync()
    {
        if (CategoriaEmEdicao is null || string.IsNullOrWhiteSpace(EdicaoNome))
        {
            return;
        }

        try
        {
            var atualizada = await _categoriaService.AtualizarAsync(
                CategoriaEmEdicao.Id, EdicaoNome.Trim(), EdicaoTipo, CategoriaEmEdicao.Ativo);

            var indice = Categorias.IndexOf(CategoriaEmEdicao);
            if (indice >= 0)
            {
                Categorias[indice] = atualizada;
            }

            CategoriaEmEdicao = null;
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível salvar a edição. Verifique sua conexão.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    /// <summary>
    /// Soft delete (US01): a categoria não é removida, só marcada como
    /// inativa — os lançamentos históricos continuam apontando para ela.
    /// </summary>
    [RelayCommand]
    private async Task DesativarAsync(Categoria categoria)
    {
        try
        {
            await _categoriaService.DesativarAsync(categoria.Id);
            categoria.Ativo = false;

            var indice = Categorias.IndexOf(categoria);
            if (indice >= 0)
            {
                // Força o CollectionView a re-renderizar a linha.
                Categorias[indice] = categoria;
            }
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível desativar a categoria. Verifique sua conexão.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
