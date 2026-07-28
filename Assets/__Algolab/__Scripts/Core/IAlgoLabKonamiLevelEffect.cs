/// <summary>
/// Efecto secreto que responde al codigo Konami dentro de un nivel concreto.
/// Implementaciones futuras permiten dar una sorpresa distinta a cada nivel
/// sin mezclarla con el desbloqueo de pruebas del menu principal.
/// </summary>
public interface IAlgoLabKonamiLevelEffect
{
    int KonamiLevelNumber { get; }
    bool ActivateKonamiLevelEffect();
}
