namespace LocadoraDeVeiculos.Aplicacao.Compartilhado;

/// <summary>Validação dos dígitos verificadores de CPF e CNPJ (aceita com ou sem pontuação).</summary>
public static class DocumentosUtil
{
    public static bool CpfValido(string? cpf)
    {
        string digitos = ApenasDigitos(cpf);

        if (digitos.Length != 11 || TodosIguais(digitos))
            return false;

        int primeiro = CalcularDigito(digitos, 9, 10);
        int segundo = CalcularDigito(digitos, 10, 11);

        return digitos[9] - '0' == primeiro && digitos[10] - '0' == segundo;
    }

    public static bool CnpjValido(string? cnpj)
    {
        string digitos = ApenasDigitos(cnpj);

        if (digitos.Length != 14 || TodosIguais(digitos))
            return false;

        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        int primeiro = CalcularDigito(digitos, pesos1);
        int segundo = CalcularDigito(digitos, pesos2);

        return digitos[12] - '0' == primeiro && digitos[13] - '0' == segundo;
    }

    private static string ApenasDigitos(string? texto) =>
        new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

    private static bool TodosIguais(string digitos) => digitos.Distinct().Count() == 1;

    // CPF: pesos decrescentes a partir de `pesoInicial` sobre os primeiros `quantidade` dígitos.
    private static int CalcularDigito(string digitos, int quantidade, int pesoInicial)
    {
        int soma = 0;

        for (int i = 0; i < quantidade; i++)
            soma += (digitos[i] - '0') * (pesoInicial - i);

        int resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }

    // CNPJ: pesos fixos.
    private static int CalcularDigito(string digitos, int[] pesos)
    {
        int soma = 0;

        for (int i = 0; i < pesos.Length; i++)
            soma += (digitos[i] - '0') * pesos[i];

        int resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }
}
