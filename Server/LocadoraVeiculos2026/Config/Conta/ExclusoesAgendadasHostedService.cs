using LocadoraDeVeiculos.Aplicacao.ModuloConta;

namespace LocadoraDeVeiculos.WebApi.Config.Conta;

/// <summary>
/// Uma vez por dia (e logo ao subir), executa as exclusões de locadora cujo prazo (7 dias) já passou. O intervalo vem de
/// <c>EXCLUSOES_INTERVALO_HORAS</c> (padrão 24): uma consulta a cada hora impediria o banco gratuito da Azure de pausar.
/// Fica desligado quando <c>EXCLUSOES_AGENDADAS_DESLIGADAS=true</c> (os testes chamam o serviço direto).
/// </summary>
public sealed class ExclusoesAgendadasHostedService(
    IServiceScopeFactory escopos,
    IConfiguration configuracao,
    ILogger<ExclusoesAgendadasHostedService> logger
) : BackgroundService
{
    private const int HorasPadrao = 24;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int horas = int.TryParse(configuracao["EXCLUSOES_INTERVALO_HORAS"], out int valor) && valor is >= 1 and <= 168
            ? valor
            : HorasPadrao;

        using PeriodicTimer relogio = new(TimeSpan.FromHours(horas));

        // Primeira passada logo depois de subir (o servidor pode ter ficado fora do ar na hora marcada).
        do
        {
            try
            {
                using IServiceScope escopo = escopos.CreateScope();

                int concluidas = await escopo.ServiceProvider
                    .GetRequiredService<ContaServico>()
                    .ExecutarAgendadasAsync(DateTime.UtcNow, stoppingToken);

                if (concluidas > 0)
                    logger.LogInformation("{Quantas} exclusão(ões) de locadora concluída(s).", concluidas);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao executar as exclusões agendadas.");
            }
        }
        while (await ProximaPassadaAsync(relogio, stoppingToken));
    }

    private static async Task<bool> ProximaPassadaAsync(PeriodicTimer relogio, CancellationToken cancellationToken)
    {
        try
        {
            return await relogio.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
