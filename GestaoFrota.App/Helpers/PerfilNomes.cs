using System.Globalization;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Helpers;

/// <summary>Nome legível de cada perfil, usado no menu lateral e na tela de Usuários.</summary>
public static class PerfilNomes
{
    public static string Nome(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Administrador => "Administrador",
        PerfilUsuario.GestorDeFrota => "Gestor de Frota",
        PerfilUsuario.Financeiro => "Financeiro",
        PerfilUsuario.OperadorMotorista => "Motorista / Operador",
        _ => perfil.ToString()
    };
}

/// <summary>Mostra o nome legível do perfil no Picker (ItemDisplayBinding).</summary>
public class PerfilNomeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is PerfilUsuario perfil ? PerfilNomes.Nome(perfil) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
