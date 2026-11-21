using LocadoraDeVeiculos.Aplicacao.ModuloConta;

namespace LocadoraDeVeiculos.WebApi.Config.Conta;

/// <summary>
/// De hora em hora, executa as exclusões de locadora cujo prazo (7 dias) já passou.
/// Fica desligado quando <c>EXCLUSOES_AGENDADAS_DESLIGADAS=true</c> (os testes chamam o serviço direto).
/// </summary>
public sealed class ExclusoesAgendadasHostedService(
    IServiceScopeFactory escopos,
    ILogger<ExclusoesAgendadasHostedService> logger
) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer relogio = new(Intervalo);

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
