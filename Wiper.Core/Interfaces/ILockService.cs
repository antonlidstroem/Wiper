namespace Wiper.Core.Interfaces;

/// <summary>
/// Detekterar fillås och identifierar vilka processer som håller låset.
/// Förberedd för framtida implementation med t.ex. RestartManager API.
/// </summary>
public interface ILockService
{
    /// <summary>Returnerar namn på processer som låser angiven sökväg.</summary>
    Task<IEnumerable<string>> GetLockingProcessesAsync(string path);

    /// <summary>Returnerar true om minst en fil i mappen är låst av en annan process.</summary>
    Task<bool> IsLockedAsync(string path);
}
